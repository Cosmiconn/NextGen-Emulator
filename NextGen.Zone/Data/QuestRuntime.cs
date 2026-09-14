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
    /// <summary>
    /// Runtime quest state. Quest definitions come only from SQL generated from
    /// the original QuestData.shn; the .shn file is not read at runtime.
    /// </summary>
    internal static class QuestRuntime
    {
        public const byte PqsNone = 0;
        public const byte PqsAbort = 1;
        public const byte PqsDone = 2;
        public const byte PqsSoon = 3;
        public const byte PqsRepeat = 4;
        public const byte PqsAble = 5;
        public const byte PqsInProgress = 6;
        public const byte PqsFailed = 7;
        public const byte PqsReward = 8;
        private const byte ScenarioFlagSlot = 250;

        public static void Accept(ZoneCharacter character, uint questId)
        {
            if (character == null || questId == 0) return;
            try
            {
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    db.ExecuteQuery(
                        "INSERT INTO tQuest (nCharNo,nQuestNo,nStatus,sData) VALUES (@c,@q,@s,NULL) " +
                        "ON DUPLICATE KEY UPDATE nStatus=IF(nStatus IN (2,4),nStatus,@s)",
                        new MySqlParameter("@c", character.ID),
                        new MySqlParameter("@q", questId),
                        new MySqlParameter("@s", PqsInProgress));
                    db.ExecuteQuery(
                        "DELETE FROM character_quest_progress WHERE CharID=@c AND QuestID=@q",
                        new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId));
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest accept failed {0}: {1}", questId, ex.Message); }
        }

        public static void RecordMobKill(ZoneCharacter character, ushort mobId)
        {
            if (character == null || mobId == 0) return;
            try
            {
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    DataTable rows = db.ReadDataTable(
                        "SELECT s.nQuestNo AS QuestID,o.Slot,o.NPCMobCount,COALESCE(p.Progress,0) Progress " +
                        "FROM tQuest s INNER JOIN QuestData_NPCMob o ON o.QuestID=s.nQuestNo " +
                        "LEFT JOIN character_quest_progress p ON p.CharID=s.nCharNo AND p.QuestID=s.nQuestNo AND p.Slot=o.Slot " +
                        "WHERE s.nCharNo=@c AND s.nStatus=@status AND o.bNPCMob=1 AND o.NPCMobID=@mob AND o.NPCMobAction=1",
                        new MySqlParameter("@c", character.ID),
                        new MySqlParameter("@status", PqsInProgress),
                        new MySqlParameter("@mob", mobId));
                    if (rows == null) return;
                    foreach (DataRow row in rows.Rows)
                    {
                        uint q = Convert.ToUInt32(row["QuestID"]);
                        byte slot = Convert.ToByte(row["Slot"]);
                        uint amount = Convert.ToUInt32(row["NPCMobCount"]);
                        uint oldValue = Convert.ToUInt32(row["Progress"]);
                        uint value = Math.Min(amount, oldValue + 1);
                        db.ExecuteQuery(
                            "INSERT INTO character_quest_progress (CharID,QuestID,Slot,Progress) VALUES (@c,@q,@slot,@p) " +
                            "ON DUPLICATE KEY UPDATE Progress=@p",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q),
                            new MySqlParameter("@slot", slot), new MySqlParameter("@p", value));
                        SendProgress(character, mobId, q);
                    }
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest kill progress failed for mob {0}: {1}", mobId, ex.Message); }
        }

        public static void RecordScenarioDone(ZoneCharacter character, ushort scenarioId)
        {
            if (character == null || scenarioId == 0) return;
            try
            {
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    DataTable rows = db.ReadDataTable(
                        "SELECT s.nQuestNo FROM tQuest s INNER JOIN QuestData_ConditionEnd q ON q.QuestID=s.nQuestNo " +
                        "WHERE s.nCharNo=@c AND s.nStatus=@status AND q.bScenario=1 AND q.ScenarioID=@scenario",
                        new MySqlParameter("@c", character.ID), new MySqlParameter("@status", PqsInProgress),
                        new MySqlParameter("@scenario", scenarioId));
                    if (rows == null) return;
                    foreach (DataRow row in rows.Rows)
                    {
                        db.ExecuteQuery(
                            "INSERT INTO character_quest_progress (CharID,QuestID,Slot,Progress) VALUES (@c,@q,@slot,1) " +
                            "ON DUPLICATE KEY UPDATE Progress=1",
                            new MySqlParameter("@c", character.ID),
                            new MySqlParameter("@q", Convert.ToUInt32(row["nQuestNo"])),
                            new MySqlParameter("@slot", ScenarioFlagSlot));
                    }
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest scenario progress failed for {0}: {1}", scenarioId, ex.Message); }
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

        private static void SendProgress(ZoneCharacter c, ushort targetId, uint questId)
        {
            if (targetId == 0) return;
            using (var p = new Packet(SH17Type.QuestProgressUpdate))
            {
                p.WriteUShort(targetId); p.WriteUShort((ushort)questId); p.WriteByte(0); c.Client.SendPacket(p);
            }
        }

        public static uint GetItemLot(ZoneCharacter character, ushort itemId)
        {
            if (character == null) return 0;
            uint total = 0;
            foreach (Item item in character.Inventory.InventoryItems.Values)
                if (item != null && item.ID == itemId) total += item.Ammount;
            return total;
        }

        public static int GetEmptyInventorySlots(ZoneCharacter character)
        {
            if (character == null) return 0;
            int capacity = character.Inventory.InventoryCount * 24;
            return Math.Max(0, capacity - character.Inventory.InventoryItems.Count);
        }

        public static bool CreateItem(ZoneCharacter character, ushort itemId, ushort amount)
        {
            if (character == null || itemId == 0 || amount == 0) return false;
            return character.GiveItem(itemId, amount) == InventoryStatus.Added;
        }

        public static bool DeleteItem(ZoneCharacter character, ushort itemId, string amountToken)
        {
            if (character == null || itemId == 0) return false;
            uint wanted = 0;
            bool all = string.Equals(amountToken, "ALL", StringComparison.OrdinalIgnoreCase);
            if (!all && !uint.TryParse(amountToken, out wanted)) return false;
            uint remaining = all ? uint.MaxValue : wanted;
            List<Item> matches = new List<Item>();
            foreach (Item item in character.Inventory.InventoryItems.Values)
                if (item != null && item.ID == itemId) matches.Add(item);
            foreach (Item item in matches)
            {
                if (all || remaining >= item.Ammount)
                {
                    uint removed = item.Ammount;
                    character.Inventory.RemoveInventory(item);
                    if (!all) remaining -= removed;
                    if (!all && remaining == 0) break;
                }
                else
                {
                    item.Ammount = (ushort)(item.Ammount - remaining);
                    item.Save();
                    Handler12.ModifyInventorySlot(character, 0x24, (byte)item.Slot, (byte)item.Slot, item);
                    remaining = 0;
                    break;
                }
            }
            return all || remaining == 0;
        }

        public static bool NeedsRewardSelection(DatabaseClient db, uint questId)
        {
            DataTable rows = db.ReadDataTable("SELECT 1 FROM QuestData_Reward WHERE QuestID=@q AND UseType=2 LIMIT 1",
                new MySqlParameter("@q", questId));
            return rows != null && rows.Rows.Count != 0;
        }

        public static bool IsComplete(ZoneCharacter c, uint questId)
        {
            if (c == null || questId == 0) return false;
            try
            {
                using (DatabaseClient db = Program.DatabaseManager.GetClient())
                {
                    DataTable qrows = db.ReadDataTable("SELECT RawData FROM QuestData_ConditionEnd WHERE QuestID=@q",
                        new MySqlParameter("@q", questId));
                    if (qrows == null || qrows.Rows.Count == 0) return false;
                    byte[] end = (byte[])qrows.Rows[0]["RawData"];
                    if (end == null || end.Length < 0x68) return false;

                    if (end[1] != 0 && c.Level < end[2]) return false;

                    DataTable npc = db.ReadDataTable("SELECT Slot,bNPCMob,NPCMobID,NPCMobAction,NPCMobCount,TargetGroup FROM QuestData_NPCMob WHERE QuestID=@q AND bNPCMob=1",
                        new MySqlParameter("@q", questId));
                    if (npc != null)
                    {
                        foreach (DataRow r in npc.Rows)
                        {
                            byte action = Convert.ToByte(r["NPCMobAction"]);
                            if (action == 1 || action == 2 || action == 3)
                            {
                                DataTable p = db.ReadDataTable("SELECT Progress FROM character_quest_progress WHERE CharID=@c AND QuestID=@q AND Slot=@slot",
                                    new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId), new MySqlParameter("@slot", Convert.ToByte(r["Slot"])));
                                uint have = p != null && p.Rows.Count > 0 ? Convert.ToUInt32(p.Rows[0]["Progress"]) : 0;
                                if (have < Convert.ToUInt32(r["NPCMobCount"])) return false;
                            }
                        }
                    }

                    DataTable items = db.ReadDataTable("SELECT ItemID,ItemLot FROM QuestData_Item WHERE QuestID=@q AND bItem=1",
                        new MySqlParameter("@q", questId));
                    if (items != null)
                        foreach (DataRow r in items.Rows)
                            if (GetItemLot(c, Convert.ToUInt16(r["ItemID"])) < Convert.ToUInt32(r["ItemLot"])) return false;

                    if (end[0x4A] != 0)
                    {
                        ushort map = BitConverter.ToUInt16(end, 0x4C);
                        int x = BitConverter.ToInt32(end, 0x50);
                        int y = BitConverter.ToInt32(end, 0x54);
                        uint range = BitConverter.ToUInt32(end, 0x58);
                        if (c.MapID != map) return false;
                        long dx = (long)c.Character.PositionInfo.XPos - x;
                        long dy = (long)c.Character.PositionInfo.YPos - y;
                        if (dx * dx + dy * dy > (long)range * range) return false;
                    }

                    if (end[0x5C] != 0)
                    {
                        DataTable p = db.ReadDataTable("SELECT Progress FROM character_quest_progress WHERE CharID=@c AND QuestID=@q AND Slot=@slot",
                            new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId), new MySqlParameter("@slot", ScenarioFlagSlot));
                        if (p == null || p.Rows.Count == 0 || Convert.ToUInt32(p.Rows[0]["Progress"]) == 0) return false;
                    }

                    // The supplied 2304-record QuestData corpus contains no active
                    // race/class/time end gates. Their native offsets are retained
                    // in SQL, but no unproven emulator-side mapping is invented.
                    return true;
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest completion check failed {0}: {1}", questId, ex.Message); return false; }
        }

        public static bool Complete(ZoneCharacter c, uint questId, uint selectedIndex = 0, bool selectionProvided = false)
        {
            if (!IsComplete(c, questId)) return false;
            try
            {
                using (DatabaseClient db = Program.DatabaseManager.GetClient())
                {
                    if (!selectionProvided && NeedsRewardSelection(db, questId)) return false;
                    if (selectionProvided && !HasSelectableRewardIndex(db, questId, selectedIndex)) return false;
                    DataTable state = db.ReadDataTable("SELECT nStatus FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q",
                        new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId));
                    if (state == null || state.Rows.Count == 0 || Convert.ToByte(state.Rows[0]["nStatus"]) != PqsInProgress) return false;
                    byte completionStatus = IsRepeatable(db, questId) ? PqsRepeat : PqsDone;
                    ApplyRewards(db, c, questId, selectedIndex);
                    db.ExecuteQuery("UPDATE tQuest SET nStatus=@s WHERE nCharNo=@c AND nQuestNo=@q",
                        new MySqlParameter("@s", completionStatus), new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId));
                    if (completionStatus == PqsRepeat)
                        db.ExecuteQuery("INSERT INTO tQuestTimes (nCharNo,nQuestNo,nTimes,dLastComplete) VALUES (@c,@q,1,NOW()) ON DUPLICATE KEY UPDATE nTimes=nTimes+1,dLastComplete=NOW()",
                            new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId));
                    return true;
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest completion failed {0}: {1}", questId, ex.Message); return false; }
        }

        private static bool IsRepeatable(DatabaseClient db, uint questId)
        {
            DataTable rows = db.ReadDataTable("SELECT Repeatable FROM QuestData WHERE QuestID=@q", new MySqlParameter("@q", questId));
            return rows != null && rows.Rows.Count > 0 && Convert.ToByte(rows.Rows[0]["Repeatable"]) != 0;
        }

        private static bool HasSelectableRewardIndex(DatabaseClient db, uint questId, uint selectedIndex)
        {
            DataTable rows = db.ReadDataTable("SELECT Slot FROM QuestData_Reward WHERE QuestID=@q AND UseType=2 ORDER BY Slot",
                new MySqlParameter("@q", questId));
            return rows != null && selectedIndex < rows.Rows.Count;
        }

        private static void ApplyRewards(DatabaseClient db, ZoneCharacter c, uint questId, uint selectedIndex)
        {
            DataTable rows = db.ReadDataTable("SELECT Slot,UseType,RewardType,Value1,Value2 FROM QuestData_Reward WHERE QuestID=@q AND (UseType=1 OR UseType=2) ORDER BY Slot",
                new MySqlParameter("@q", questId));
            if (rows == null) return;
            uint selectable = 0;
            foreach (DataRow r in rows.Rows)
            {
                byte use = Convert.ToByte(r["UseType"]);
                byte type = Convert.ToByte(r["RewardType"]);
                if (use == 2)
                {
                    if (selectable++ != selectedIndex) continue;
                }
                else if (use != 1) continue;
                uint v1 = Convert.ToUInt32(r["Value1"]);
                switch (type)
                {
                    case 0: c.GiveExp(v1); break;
                    case 1: c.ChangeMoney(c.Inventory.Money + v1); break;
                    case 2:
                        ushort itemId = (ushort)(v1 & 0xffff);
                        ushort lot = (ushort)(v1 >> 16);
                        if (itemId != 0 && lot != 0) c.GiveItem(itemId, lot);
                        break;
                    case 4: c.Fame += (int)v1; break;
                    case 8: c.KillPoints += (int)v1; break;
                    default:
                        Log.WriteLine(LogLevel.Debug, "Quest reward type {0} for quest {1} retained but not applied.", type, questId);
                        break;
                }
            }
        }
    }
}
