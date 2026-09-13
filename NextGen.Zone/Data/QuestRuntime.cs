using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using NextGen.Database;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.Zone.Game;
using NextGen.Zone.Handlers;

namespace NextGen.Zone.Data
{
    internal static class QuestRuntime
    {
        public const byte PqsDone = 2;
        public const byte PqsSoon = 3;
        public const byte PqsInProgress = 6;

        public static void Accept(ZoneCharacter character, uint questId)
        {
            if (character == null) return;
            try { using (DatabaseClient db = Program.DatabaseManager.GetClient()) db.ExecuteQuery("INSERT INTO character_quest_state (CharID,QuestID,Status,Data) VALUES (@c,@q,@s,NULL) ON DUPLICATE KEY UPDATE Status=IF(Status IN (2,4),Status,@s)", new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId), new MySqlParameter("@s", PqsInProgress)); }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest accept failed {0}: {1}", questId, ex.Message); }
        }

        public static void SetAbstate(ZoneCharacter character, string abStateName, uint strength, uint keepTimeMs)
        {
            if (character == null || string.IsNullOrWhiteSpace(abStateName)) return;
            AbStateInfo abState;
            if (!DataProvider.Instance.AbStatesByName.TryGetValue(abStateName, out abState) || abState == null)
            {
                Log.WriteLine(LogLevel.Warn, "Quest SET_ABSTATE: unknown AbState '{0}'.", abStateName);
                return;
            }
            if (strength < 1) strength = 1;
            if (strength > 40) strength = 40;
            character.AddBuff(abState, strength, null, keepTimeMs == 0 ? (uint?)null : keepTimeMs);
        }

        public static void ResetAbstate(ZoneCharacter character, string abStateName)
        {
            if (character == null || string.IsNullOrWhiteSpace(abStateName)) return;
            AbStateInfo abState;
            if (!DataProvider.Instance.AbStatesByName.TryGetValue(abStateName, out abState) || abState == null)
            {
                Log.WriteLine(LogLevel.Warn, "Quest RESET_ABSTATE: unknown AbState '{0}'.", abStateName);
                return;
            }
            character.RemoveBuff(abState.ID);
        }

        public static void RecordMobKill(ZoneCharacter character, ushort mobId)
        {
            if (character == null) return;
            try
            {
                using (DatabaseClient db = Program.DatabaseManager.GetClient())
                {
                    DataTable rows = db.ReadDataTable("SELECT q.QuestID,o.Slot,o.Amount,COALESCE(p.Progress,0) Progress FROM character_quest_state s INNER JOIN data_quest q ON q.QuestID=s.QuestID INNER JOIN data_quest_objective o ON o.QuestID=q.QuestID LEFT JOIN character_quest_progress p ON p.CharID=s.CharID AND p.QuestID=o.QuestID AND p.Slot=o.Slot WHERE s.CharID=@c AND s.Status=@status AND o.Active=1 AND o.IsMob=1 AND o.HasToBeKilled=1 AND o.TargetID=@mob", new MySqlParameter("@c", character.ID), new MySqlParameter("@status", PqsInProgress), new MySqlParameter("@mob", mobId));
                    if (rows == null) return;
                    foreach (DataRow row in rows.Rows)
                    {
                        uint q = Convert.ToUInt32(row["QuestID"]); byte slot = Convert.ToByte(row["Slot"]); uint amount = Convert.ToUInt32(row["Amount"]); uint oldValue = Convert.ToUInt32(row["Progress"]); uint value = Math.Min(amount, oldValue + 1);
                        db.ExecuteQuery("INSERT INTO character_quest_progress (CharID,QuestID,Slot,Progress) VALUES (@c,@q,@slot,@p) ON DUPLICATE KEY UPDATE Progress=@p", new MySqlParameter("@c",character.ID),new MySqlParameter("@q",q),new MySqlParameter("@slot",slot),new MySqlParameter("@p",value)); SendProgress(character, mobId, q);
                    }
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest kill progress failed for mob {0}: {1}", mobId, ex.Message); }
        }

        private static void SendProgress(ZoneCharacter c, ushort targetId, uint questId) { if (targetId == 0) return; using (var p = new Packet(SH17Type.QuestProgressUpdate)) { p.WriteUShort(targetId); p.WriteUShort((ushort)questId); p.WriteByte(0); c.Client.SendPacket(p); } }
        public static uint GetItemLot(ZoneCharacter character, ushort itemId) { if (character == null) return 0; uint total=0; foreach(Item item in character.Inventory.InventoryItems.Values) if(item!=null&&item.ID==itemId) total+=item.Ammount; return total; }
        public static int GetEmptyInventorySlots(ZoneCharacter character) { if(character==null)return 0; int capacity=character.Inventory.InventoryCount*24; return Math.Max(0,capacity-character.Inventory.InventoryItems.Count); }
        public static bool CreateItem(ZoneCharacter character, ushort itemId, ushort amount) { if(character==null||itemId==0||amount==0)return false; return character.GiveItem(itemId,amount)==InventoryStatus.Added; }
        public static bool DeleteItem(ZoneCharacter character, ushort itemId, string amountToken)
        {
            if(character==null||itemId==0)return false; uint wanted=0; bool all=string.Equals(amountToken,"ALL",StringComparison.OrdinalIgnoreCase); if(!all&&!uint.TryParse(amountToken,out wanted))return false; uint remaining=all?uint.MaxValue:wanted; List<Item> matches=new List<Item>(); foreach(Item item in character.Inventory.InventoryItems.Values) if(item!=null&&item.ID==itemId)matches.Add(item); foreach(Item item in matches){if(all||remaining>=item.Ammount){uint removed=item.Ammount;character.Inventory.RemoveInventory(item);if(!all)remaining-=removed;if(!all&&remaining==0)break;}else{item.Ammount=(ushort)(item.Ammount-remaining);item.Save();Handler12.ModifyInventorySlot(character,0x24,(byte)item.Slot,(byte)item.Slot,item);remaining=0;break;}} return all||remaining==0;
        }
        public static bool NeedsRewardSelection(DatabaseClient db,uint questId){return HasSelectableRewards(null,questId,db);}
        public static bool IsComplete(ZoneCharacter c,uint questId){try{using(DatabaseClient db=Program.DatabaseManager.GetClient()){DataTable rows=db.ReadDataTable("SELECT o.Slot,o.Amount,COALESCE(p.Progress,0) Progress FROM data_quest_objective o LEFT JOIN character_quest_progress p ON p.CharID=@c AND p.QuestID=o.QuestID AND p.Slot=o.Slot WHERE o.QuestID=@q AND o.Active=1 AND o.IsMob=1 AND o.HasToBeKilled=1",new MySqlParameter("@c",c.ID),new MySqlParameter("@q",questId));if(rows==null||rows.Rows.Count==0)return false;foreach(DataRow r in rows.Rows)if(Convert.ToUInt32(r["Progress"])<Convert.ToUInt32(r["Amount"]))return false;return true;}}catch{return false;}}
        public static bool Complete(ZoneCharacter c,uint questId,uint selectedIndex=0,bool selectionProvided=false){if(!IsComplete(c,questId))return false;if(!selectionProvided){using(DatabaseClient rewardDb=Program.DatabaseManager.GetClient())if(NeedsRewardSelection(rewardDb,questId))return false;}if(selectionProvided&&!HasSelectableRewardIndex(c,questId,selectedIndex))return false;try{using(DatabaseClient db=Program.DatabaseManager.GetClient()){DataTable state=db.ReadDataTable("SELECT Status FROM character_quest_state WHERE CharID=@c AND QuestID=@q",new MySqlParameter("@c",c.ID),new MySqlParameter("@q",questId));if(state==null||state.Rows.Count==0||Convert.ToByte(state.Rows[0]["Status"])!=PqsInProgress)return false;byte completionStatus=IsRepeatable(db,questId)?PqsSoon:PqsDone;db.ExecuteQuery("UPDATE character_quest_state SET Status=@s WHERE CharID=@c AND QuestID=@q",new MySqlParameter("@s",completionStatus),new MySqlParameter("@c",c.ID),new MySqlParameter("@q",questId));ApplyRewards(db,c,questId,selectedIndex);return true;}}catch(Exception ex){Log.WriteLine(LogLevel.Warn,"Quest completion failed {0}: {1}",questId,ex.Message);return false;}}
        private static bool HasSelectableRewards(ZoneCharacter c,uint questId,DatabaseClient db){DataTable rows=db.ReadDataTable("SELECT RewardsHex FROM data_quest_rewards_raw WHERE QuestID=@q",new MySqlParameter("@q",questId));if(rows==null||rows.Rows.Count==0)return false;byte[] b=Hex(Convert.ToString(rows.Rows[0]["RewardsHex"]));for(int off=0;off+12<=b.Length;off+=12)if(b[off+8]==2)return true;return false;}
        private static bool IsRepeatable(DatabaseClient db,uint questId){DataTable rows=db.ReadDataTable("SELECT Repeatable FROM data_quest WHERE QuestID=@q",new MySqlParameter("@q",questId));return rows!=null&&rows.Rows.Count>0&&Convert.ToByte(rows.Rows[0]["Repeatable"])!=0;}
        private static bool HasSelectableRewardIndex(ZoneCharacter c,uint questId,uint selectedIndex){try{using(DatabaseClient db=Program.DatabaseManager.GetClient()){DataTable rows=db.ReadDataTable("SELECT RewardsHex FROM data_quest_rewards_raw WHERE QuestID=@q",new MySqlParameter("@q",questId));if(rows==null||rows.Rows.Count==0)return false;uint ordinal=0;byte[] b=Hex(Convert.ToString(rows.Rows[0]["RewardsHex"]));for(int off=0;off+12<=b.Length;off+=12){if(b[off+8]!=2)continue;if(ordinal==selectedIndex)return true;ordinal++;}}}catch{}return false;}
        private static void ApplyRewards(DatabaseClient db,ZoneCharacter c,uint questId,uint selectedIndex){DataTable rows=db.ReadDataTable("SELECT RewardsHex FROM data_quest_rewards_raw WHERE QuestID=@q",new MySqlParameter("@q",questId));if(rows==null||rows.Rows.Count==0)return;byte[] b=Hex(Convert.ToString(rows.Rows[0]["RewardsHex"]));if(b.Length<12)return;uint selectable=0;for(int off=0;off+12<=b.Length;off+=12){byte use=b[off+8],type=b[off+9];if(use==2){if(selectable++!=selectedIndex)continue;}else if(use!=1)continue;uint value=BitConverter.ToUInt32(b,off);switch(type){case 0:c.GiveExp(value);break;case 1:c.ChangeMoney(c.Inventory.Money+value);break;case 2:ushort itemId=(ushort)(value&0xffff),lot=(ushort)(value>>16);if(itemId!=0&&lot!=0)c.GiveItem(itemId,lot);break;case 4:c.Fame+=(int)value;break;case 8:c.KillPoints+=(int)value;break;default:break;}}}
        private static byte[] Hex(string s){if(string.IsNullOrEmpty(s)||(s.Length&1)!=0)return new byte[0];byte[] r=new byte[s.Length/2];for(int i=0;i<r.Length;i++)r[i]=Convert.ToByte(s.Substring(i*2,2),16);return r;}
    }
}
