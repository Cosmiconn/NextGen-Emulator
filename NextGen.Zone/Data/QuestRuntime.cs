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
                    db.ExecuteQuery("INSERT INTO tQuest (nCharNo,nQuestNo,nStatus,sData) VALUES (@c,@q,@s,NULL) ON DUPLICATE KEY UPDATE nStatus=@s,sData=NULL",
                        new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId), new MySqlParameter("@s", PqsInProgress));
                    db.ExecuteQuery("DELETE FROM character_quest_progress WHERE CharID=@c AND QuestID=@q",
                        new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId));
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest accept failed {0}: {1}", questId, ex.Message); }
        }

        public static void Cancel(ZoneCharacter character, uint questId)
        {
            if (character == null || questId == 0) return;
            try
            {
                using (DatabaseClient dataDb = Program.DatabaseManager.GetClient())
                using (DatabaseClient charDb = Program.CharDBManager.GetClient())
                {
                    DataTable def = dataDb.ReadDataTable("SELECT Repeatable FROM QuestData WHERE QuestID=@q", new MySqlParameter("@q", questId));
                    bool repeatable = def != null && def.Rows.Count > 0 && Convert.ToByte(def.Rows[0]["Repeatable"]) != 0;
                    if (repeatable)
                        charDb.ExecuteQuery("UPDATE tQuest SET nStatus=@s,sData=NULL WHERE nCharNo=@c AND nQuestNo=@q",
                            new MySqlParameter("@s", PqsRepeat), new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId));
                    else
                        charDb.ExecuteQuery("DELETE FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId));
                    charDb.ExecuteQuery("DELETE FROM character_quest_progress WHERE CharID=@c AND QuestID=@q",
                        new MySqlParameter("@c", character.ID), new MySqlParameter("@q", questId));
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest cancel failed {0}: {1}", questId, ex.Message); }
        }

        public static void RecordMobKill(ZoneCharacter character, ushort mobId)
        {
            if (character == null || mobId == 0) return;
            try
            {
                DataTable definitions;
                using (DatabaseClient dataDb = Program.DatabaseManager.GetClient())
                    definitions = dataDb.ReadDataTable("SELECT QuestID,Slot,NPCMobCount FROM QuestData_NPCMob WHERE bNPCMob=1 AND NPCMobID=@mob AND NPCMobAction=1", new MySqlParameter("@mob", mobId));
                if (definitions == null || definitions.Rows.Count == 0) return;
                using (DatabaseClient charDb = Program.CharDBManager.GetClient())
                {
                    foreach (DataRow def in definitions.Rows)
                    {
                        uint q = Convert.ToUInt32(def["QuestID"]);
                        byte slot = Convert.ToByte(def["Slot"]);
                        uint amount = Convert.ToUInt32(def["NPCMobCount"]);
                        DataTable active = charDb.ReadDataTable("SELECT nStatus FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q AND nStatus=@status",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q), new MySqlParameter("@status", PqsInProgress));
                        if (active == null || active.Rows.Count == 0) continue;
                        DataTable progress = charDb.ReadDataTable("SELECT Progress FROM character_quest_progress WHERE CharID=@c AND QuestID=@q AND Slot=@slot",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q), new MySqlParameter("@slot", slot));
                        uint oldValue = progress != null && progress.Rows.Count > 0 ? Convert.ToUInt32(progress.Rows[0]["Progress"]) : 0;
                        uint value = Math.Min(amount, oldValue + 1);
                        charDb.ExecuteQuery("INSERT INTO character_quest_progress (CharID,QuestID,Slot,Progress) VALUES (@c,@q,@slot,@p) ON DUPLICATE KEY UPDATE Progress=@p",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q), new MySqlParameter("@slot", slot), new MySqlParameter("@p", value));
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
                DataTable definitions;
                using (DatabaseClient dataDb = Program.DatabaseManager.GetClient())
                    definitions = dataDb.ReadDataTable("SELECT QuestID FROM QuestData_ConditionEnd WHERE bScenario=1 AND ScenarioID=@scenario", new MySqlParameter("@scenario", scenarioId));
                if (definitions == null || definitions.Rows.Count == 0) return;
                using (DatabaseClient charDb = Program.CharDBManager.GetClient())
                {
                    foreach (DataRow def in definitions.Rows)
                    {
                        uint q = Convert.ToUInt32(def["QuestID"]);
                        DataTable active = charDb.ReadDataTable("SELECT nStatus FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q AND nStatus=@status",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q), new MySqlParameter("@status", PqsInProgress));
                        if (active == null || active.Rows.Count == 0) continue;
                        charDb.ExecuteQuery("INSERT INTO character_quest_progress (CharID,QuestID,Slot,Progress) VALUES (@c,@q,@slot,1) ON DUPLICATE KEY UPDATE Progress=1",
                            new MySqlParameter("@c", character.ID), new MySqlParameter("@q", q), new MySqlParameter("@slot", ScenarioFlagSlot));
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
                Log.WriteLine(LogLevel.Warn, "Quest SET_ABSTATE: unknown AbState '{0}'.", abStateName); return;
            }
            if (strength == 0 || strength > 40) return;
            character.AddBuff(abState, strength, null, keepTimeMs == 0 ? (uint?)null : keepTimeMs);
        }

        public static void ResetAbstate(ZoneCharacter character, string abStateName)
        {
            if (character == null || string.IsNullOrWhiteSpace(abStateName)) return;
            AbStateInfo abState;
            if (!DataProvider.Instance.AbStatesByName.TryGetValue(abStateName, out abState) || abState == null) return;
            character.RemoveBuff(abState.ID);
        }

        private static void SendProgress(ZoneCharacter c, ushort targetId, uint questId)
        {
            if (targetId == 0) return;
            using (var p = new Packet(SH17Type.QuestProgressUpdate))
            { p.WriteUShort(targetId); p.WriteUShort((ushort)questId); p.WriteByte(0); c.Client.SendPacket(p); }
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

        public static bool CreateItem(ZoneCharacter character, ushort itemId, uint amount)
        {
            if (character == null || itemId == 0 || amount == 0) return false;
            return character.GiveItemLots(itemId, amount) == InventoryStatus.Added;
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
                    uint removed = item.Ammount; character.Inventory.RemoveInventory(item);
                    if (!all) remaining -= removed;
                    if (!all && remaining == 0) break;
                }
                else
                {
                    item.Ammount = (ushort)(item.Ammount - remaining); item.Save();
                    Handler12.ModifyInventorySlot(character, 0x24, (byte)item.Slot, (byte)item.Slot, item);
                    remaining = 0; break;
                }
            }
            return all || remaining == 0;
        }

        public static bool NeedsRewardSelection(DatabaseClient db, uint questId)
        {
            DataTable rows = db.ReadDataTable("SELECT 1 FROM QuestData_Reward WHERE QuestID=@q AND UseType=2 LIMIT 1", new MySqlParameter("@q", questId));
            return rows != null && rows.Rows.Count != 0;
        }

        public static bool IsComplete(ZoneCharacter c, uint questId)
        {
            if (c == null || questId == 0) return false;
            try
            {
                using (DatabaseClient dataDb = Program.DatabaseManager.GetClient())
                using (DatabaseClient charDb = Program.CharDBManager.GetClient())
                {
                    bool endLevelEnabled;
                    byte endLevel;
                    bool endLocationEnabled;
                    ushort endLocationMap;
                    int endLocationX;
                    int endLocationY;
                    uint endLocationRange;
                    bool endScenarioEnabled;

                    try
                    {
                        DataTable qrows = dataDb.ReadDataTable(
                            "SELECT bLevel,Level,bLocation,LocationMap,LocationX,LocationY,LocationRange,bScenario " +
                            "FROM QuestData_ConditionEnd WHERE QuestID=@q",
                            new MySqlParameter("@q", questId));
                        if (qrows == null || qrows.Rows.Count == 0) return false;
                        DataRow endRow = qrows.Rows[0];
                        endLevelEnabled = Convert.ToByte(endRow["bLevel"]) != 0;
                        endLevel = Convert.ToByte(endRow["Level"]);
                        endLocationEnabled = Convert.ToByte(endRow["bLocation"]) != 0;
                        endLocationMap = Convert.ToUInt16(endRow["LocationMap"]);
                        endLocationX = Convert.ToInt32(endRow["LocationX"]);
                        endLocationY = Convert.ToInt32(endRow["LocationY"]);
                        endLocationRange = Convert.ToUInt32(endRow["LocationRange"]);
                        endScenarioEnabled = Convert.ToByte(endRow["bScenario"]) != 0;
                    }
                    catch
                    {
                        // Backward compatibility for SQL generated before normalized
                        // end-location columns were added to import-questdata.py.
                        DataTable qrows = dataDb.ReadDataTable(
                            "SELECT RawData FROM QuestData_ConditionEnd WHERE QuestID=@q",
                            new MySqlParameter("@q", questId));
                        if (qrows == null || qrows.Rows.Count == 0) return false;
                        byte[] end = qrows.Rows[0]["RawData"] as byte[];
                        if (end == null || end.Length < 0x68) return false;
                        endLevelEnabled = end[1] != 0;
                        endLevel = end[2];
                        endLocationEnabled = end[0x4A] != 0;
                        endLocationMap = BitConverter.ToUInt16(end, 0x4C);
                        endLocationX = BitConverter.ToInt32(end, 0x50);
                        endLocationY = BitConverter.ToInt32(end, 0x54);
                        endLocationRange = BitConverter.ToUInt32(end, 0x58);
                        endScenarioEnabled = end[0x5C] != 0;
                    }

                    if (endLevelEnabled && c.Level < endLevel) return false;

                    DataTable npc = dataDb.ReadDataTable("SELECT Slot,NPCMobAction,NPCMobCount FROM QuestData_NPCMob WHERE QuestID=@q AND bNPCMob=1", new MySqlParameter("@q", questId));
                    if (npc != null)
                        foreach (DataRow r in npc.Rows)
                        {
                            byte action = Convert.ToByte(r["NPCMobAction"]);
                            if (action != 1 && action != 2 && action != 3) continue;
                            DataTable p = charDb.ReadDataTable("SELECT Progress FROM character_quest_progress WHERE CharID=@c AND QuestID=@q AND Slot=@slot", new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId), new MySqlParameter("@slot", Convert.ToByte(r["Slot"])));
                            uint have = p != null && p.Rows.Count > 0 ? Convert.ToUInt32(p.Rows[0]["Progress"]) : 0;
                            if (have < Convert.ToUInt32(r["NPCMobCount"])) return false;
                        }

                    DataTable items = dataDb.ReadDataTable("SELECT ItemID,ItemLot FROM QuestData_Item WHERE QuestID=@q AND bItem=1", new MySqlParameter("@q", questId));
                    if (items != null)
                        foreach (DataRow r in items.Rows)
                            if (GetItemLot(c, Convert.ToUInt16(r["ItemID"])) < Convert.ToUInt32(r["ItemLot"])) return false;

                    if (endLocationEnabled)
                    {
                        if (c.MapID != endLocationMap) return false;
                        long dx = (long)c.Character.PositionInfo.XPos - endLocationX;
                        long dy = (long)c.Character.PositionInfo.YPos - endLocationY;
                        if (dx * dx + dy * dy > (long)endLocationRange * endLocationRange) return false;
                    }

                    if (endScenarioEnabled)
                    {
                        DataTable p = charDb.ReadDataTable("SELECT Progress FROM character_quest_progress WHERE CharID=@c AND QuestID=@q AND Slot=@slot", new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId), new MySqlParameter("@slot", ScenarioFlagSlot));
                        if (p == null || p.Rows.Count == 0 || Convert.ToUInt32(p.Rows[0]["Progress"]) == 0) return false;
                    }

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
                using (DatabaseClient dataDb = Program.DatabaseManager.GetClient())
                using (DatabaseClient charDb = Program.CharDBManager.GetClient())
                {
                    if (!selectionProvided && NeedsRewardSelection(dataDb, questId)) return false;
                    if (selectionProvided && !HasSelectableRewardIndex(dataDb, questId, selectedIndex)) return false;
                    DataTable state = charDb.ReadDataTable("SELECT nStatus FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q", new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId));
                    if (state == null || state.Rows.Count == 0 || Convert.ToByte(state.Rows[0]["nStatus"]) != PqsInProgress) return false;
                    // Native CQuest::SetQuestDone reaches the 0x0062F610 mutation helper:
                    // non-repeatable -> PQS_DONE (2), repeatable -> PQS_SOON (3).
                    // PQS_REPEAT (4) is a later/re-acceptable state and must not be
                    // collapsed into the completion transition.
                    byte completionStatus = IsRepeatable(dataDb, questId) ? PqsSoon : PqsDone;
                    ApplyRewards(dataDb, c, questId, selectedIndex);
                    DateTime completedAt = DateTime.Now;
                    charDb.ExecuteQuery("UPDATE tQuest SET nStatus=@s WHERE nCharNo=@c AND nQuestNo=@q", new MySqlParameter("@s", completionStatus), new MySqlParameter("@c", c.ID), new MySqlParameter("@q", questId));
                    // Native completion stores a completion counter at
                    // PLAYER_QUEST_INFO+0x13 and a 64-bit completion timestamp at
                    // +0x0B/+0x0F. Keep the normalized SQL equivalent in tQuestTimes.
                    charDb.ExecuteQuery(
                        "INSERT INTO tQuestTimes (nCharNo,nQuestNo,nTimes,dLastComplete) VALUES (@c,@q,1,@t) " +
                        "ON DUPLICATE KEY UPDATE nTimes=nTimes+1,dLastComplete=@t",
                        new MySqlParameter("@c", c.ID),
                        new MySqlParameter("@q", questId),
                        new MySqlParameter("@t", completedAt));
                    return true;
                }
            }
            catch (Exception ex) { Log.WriteLine(LogLevel.Warn, "Quest completion failed {0}: {1}", questId, ex.Message); return false; }
        }

        public static bool TryEvaluateDailyPrerequisite(int characterId, uint questId,
            byte dailyQuestType, out bool satisfied)
        {
            satisfied = false;

            // CQuest::IsSoonableDailyQuest returns false for DQT_NONE and for
            // out-of-range values. In the caller that means status-2 prerequisite
            // eligibility passes without a reset-time rejection.
            if (dailyQuestType == 0 || dailyQuestType > 4)
            {
                satisfied = true;
                return true;
            }

            try
            {
                DateTime lastComplete;
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    DataTable rows = db.ReadDataTable(
                        "SELECT dLastComplete FROM tQuestTimes WHERE nCharNo=@c AND nQuestNo=@q",
                        new MySqlParameter("@c", characterId),
                        new MySqlParameter("@q", questId));
                    if (rows == null || rows.Rows.Count == 0 || rows.Rows[0]["dLastComplete"] == DBNull.Value)
                        return false;
                    lastComplete = Convert.ToDateTime(rows.Rows[0]["dLastComplete"]);
                }

                // WorldManager's original CDailyQuestTimer uses localtime/mktime.
                // Raw enum values are proven by the PDB:
                // 1=DAY, 2=WEEK, 3=MONTH, 4=YEAR.
                DateTime now = DateTime.Now;
                DateTime resetBoundary;
                switch (dailyQuestType)
                {
                    case 1:
                        resetBoundary = now.Date;
                        break;
                    case 2:
                        int daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
                        resetBoundary = now.Date.AddDays(-daysSinceMonday);
                        break;
                    case 3:
                        resetBoundary = new DateTime(now.Year, now.Month, 1);
                        break;
                    case 4:
                        resetBoundary = new DateTime(now.Year, 1, 1);
                        break;
                    default:
                        satisfied = true;
                        return true;
                }

                // Native IsSoonableDailyQuest returns true when completion time is
                // older than the selected reset boundary. IsSoonableQuest rejects a
                // status-2 predecessor in that case, so the prerequisite is satisfied
                // only when completion is on/after the current boundary.
                satisfied = lastComplete >= resetBoundary;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn,
                    "Quest daily prerequisite check failed for {0}: {1}", questId, ex.Message);
                return false;
            }
        }

        private static bool IsRepeatable(DatabaseClient db, uint questId)
        {
            DataTable rows = db.ReadDataTable("SELECT Repeatable FROM QuestData WHERE QuestID=@q", new MySqlParameter("@q", questId));
            return rows != null && rows.Rows.Count > 0 && Convert.ToByte(rows.Rows[0]["Repeatable"]) != 0;
        }

        private static bool HasSelectableRewardIndex(DatabaseClient db, uint questId, uint selectedIndex)
        {
            DataTable rows = db.ReadDataTable("SELECT Slot FROM QuestData_Reward WHERE QuestID=@q AND UseType=2 ORDER BY Slot", new MySqlParameter("@q", questId));
            return rows != null && selectedIndex < rows.Rows.Count;
        }

        private static void ApplyRewards(DatabaseClient db, ZoneCharacter c, uint questId, uint selectedIndex)
        {
            DataTable rows = db.ReadDataTable("SELECT Slot,UseType,RewardType,Value1,Value2 FROM QuestData_Reward WHERE QuestID=@q AND (UseType=1 OR UseType=2) ORDER BY Slot", new MySqlParameter("@q", questId));
            if (rows == null) return;
            uint selectable = 0;
            foreach (DataRow r in rows.Rows)
            {
                byte use = Convert.ToByte(r["UseType"]), type = Convert.ToByte(r["RewardType"]);
                if (use == 2) { if (selectable++ != selectedIndex) continue; }
                else if (use != 1) continue;
                uint v1 = Convert.ToUInt32(r["Value1"]);
                switch (type)
                {
                    case 0: c.GiveExp(v1); break;
                    case 1: c.ChangeMoney(c.Inventory.Money + v1); break;
                    case 2:
                        ushort itemId = (ushort)(v1 & 0xffff), lot = (ushort)(v1 >> 16);
                        if (itemId != 0 && lot != 0) c.GiveItemLots(itemId, lot);
                        break;
                    case 4: c.Fame += (int)v1; break;
                    case 8: c.KillPoints += (int)v1; break;
                    default: Log.WriteLine(LogLevel.Debug, "Quest reward type {0} for quest {1} retained but not applied.", type, questId); break;
                }
            }
        }
    }
}
