using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NextGen.Database;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.Zone.Game;

namespace NextGen.Zone.Data
{
    public static class QuestRuntime
    {
        public static bool Complete(ZoneCharacter character, uint questId)
        {
            if (character == null) return false;
            if (!HasActiveMobObjectives(character, questId)) return false;
            using (DatabaseClient db = Program.DatabaseManager.GetClient())
            {
                db.ExecuteQuery("UPDATE tQuest SET nStatus=2 WHERE nCharNo=" + character.ID + " AND nQuestNo=" + questId);
            }
            return true;
        }

        public static int GetItemLot(ZoneCharacter character, ushort itemId)
        {
            if (character == null || character.Inventory == null) return 0;
            int total = 0;
            foreach (Item item in character.Inventory.InventoryItems.Values)
            {
                if (item != null && item.ID == itemId) total += item.Ammount;
            }
            return total;
        }

        public static int GetEmptyInventorySlots(ZoneCharacter character)
        {
            if (character == null || character.Inventory == null) return 0;
            return Math.Max(0, character.Inventory.InventoryCount * 24 - character.Inventory.InventoryItems.Count);
        }

        public static bool CreateItem(ZoneCharacter character, ushort itemId, ushort amount)
        {
            if (character == null || amount == 0) return false;
            character.Inventory.GiveItem(itemId, amount);
            return true;
        }

        public static bool DeleteItem(ZoneCharacter character, ushort itemId, string amountToken)
        {
            if (character == null || character.Inventory == null) return false;
            if (string.Equals(amountToken, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Item item in character.Inventory.InventoryItems.Values.Where(x => x != null && x.ID == itemId).ToList())
                    item.Delete();
                foreach (ushort key in character.Inventory.InventoryItems.Where(x => x.Value != null && x.Value.ID == itemId).Select(x => x.Key).ToList())
                    character.Inventory.InventoryItems.Remove(key);
                return true;
            }
            ushort amount;
            if (!ushort.TryParse(amountToken, out amount) || amount == 0) return false;
            ushort remaining = amount;
            foreach (Item item in character.Inventory.InventoryItems.Values.Where(x => x != null && x.ID == itemId).ToList())
            {
                if (remaining == 0) break;
                if (item.Ammount > remaining)
                {
                    item.Ammount = (ushort)(item.Ammount - remaining);
                    item.Save();
                    remaining = 0;
                }
                else
                {
                    remaining = (ushort)(remaining - item.Ammount);
                    item.Delete();
                    character.Inventory.InventoryItems.Remove(item.UniqueID);
                }
            }
            return remaining == 0;
        }

        public static bool NeedsRewardSelection(DatabaseClient db, uint questId)
        {
            if (db == null) return false;
            DataTable data = db.ReadDataTable("SELECT * FROM data_quest_reward WHERE QuestID=" + questId);
            if (data == null) return false;
            foreach (DataRow row in data.Rows)
            {
                if (row.Table.Columns.Contains("Use") && Convert.ToInt32(row["Use"]) == 2) return true;
            }
            return false;
        }

        private static bool HasActiveMobObjectives(ZoneCharacter character, uint questId)
        {
            try
            {
                using (DatabaseClient db = Program.DatabaseManager.GetClient())
                {
                    DataTable data = db.ReadDataTable("SELECT * FROM data_quest_mobkill WHERE QuestID=" + questId);
                    if (data == null || data.Rows.Count == 0) return false;
                    foreach (DataRow row in data.Rows)
                    {
                        ushort mobId = Convert.ToUInt16(row["MobID"]);
                        int required = Convert.ToInt32(row["Amount"]);
                        int current = GetQuestKillCount(character, questId, mobId);
                        if (current < required) return false;
                    }
                    return true;
                }
            }
            catch { return false; }
        }

        private static int GetQuestKillCount(ZoneCharacter character, uint questId, ushort mobId)
        {
            return 0;
        }
    }
}
