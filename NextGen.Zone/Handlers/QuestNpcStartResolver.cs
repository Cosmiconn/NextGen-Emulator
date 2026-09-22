using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database;
using NextGen.Util;

namespace NextGen.Zone.Handlers
{
    /// <summary>
    /// Resolves the first dialog(s) offered by an NPC from the authoritative
    /// QuestData-derived SQL mapping. StartingNpc is a MobInfo ID; the live
    /// ShineNpc instance exposes the corresponding MobName.
    ///
    /// No quest eligibility is guessed here. If one NPC type has several
    /// distinct start dialogs, the caller must use character quest state to
    /// choose one; until that state exists this resolver reports ambiguity.
    /// </summary>
    internal static class QuestNpcStartResolver
    {
        private sealed class Candidate
        {
            public uint QuestID;
            public uint DialogID;
        }

        private static readonly object Sync = new object();
        private static bool _initialized;
        private static bool _available;
        private static readonly Dictionary<string, List<Candidate>> ByMobName =
            new Dictionary<string, List<Candidate>>(StringComparer.OrdinalIgnoreCase);

        // Exact status-priority table written by the original Zone.exe CQuest constructor.
        // Lower numeric priority wins. Equal-priority candidates require additional
        // original predicates and therefore are deliberately not guessed here.
        private static readonly byte[] QuestStatusPriority =
            { 22, 21, 21, 21, 21, 2, 1, 2, 0, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 2 };

        public static bool TryResolveForCharacter(NextGen.Zone.Game.ZoneCharacter character, string mobName, out uint dialogId)
        {
            dialogId = 0;
            if (character == null) return false;

            List<Candidate> candidates;
            if (!TryGetCandidates(mobName, out candidates)) return false;

            // Preserve the already-proven fast path: all matching quests lead to the
            // same first SAY dialog, so no quest-status tie-break is observable.
            uint sameDialog = 0;
            bool haveDialog = false;
            bool allSame = true;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!haveDialog) { sameDialog = candidates[i].DialogID; haveDialog = true; }
                else if (sameDialog != candidates[i].DialogID) { allSame = false; break; }
            }
            if (haveDialog && allSame) { dialogId = sameDialog; return true; }

            try
            {
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    Candidate best = null;
                    byte bestPriority = byte.MaxValue;
                    bool tied = false;

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        Candidate candidate = candidates[i];
                        byte status = 0;
                        DataTable state = db.ReadDataTable(
                            "SELECT nStatus FROM tQuest WHERE nCharNo=@c AND nQuestNo=@q LIMIT 1",
                            new MySqlConnector.MySqlParameter("@c", character.ID),
                            new MySqlConnector.MySqlParameter("@q", candidate.QuestID));
                        if (state != null && state.Rows.Count != 0)
                            status = Convert.ToByte(state.Rows[0]["nStatus"]);

                        byte priority = status < QuestStatusPriority.Length ? QuestStatusPriority[status] : byte.MaxValue;
                        if (best == null || priority < bestPriority)
                        {
                            best = candidate;
                            bestPriority = priority;
                            tied = false;
                        }
                        else if (priority == bestPriority && candidate.DialogID != best.DialogID)
                        {
                            // Zone.exe evaluates Start.bLevel/level, quest-condition bytes,
                            // then quest-type priority for this case. Until those predicates
                            // are reconstructed byte-exactly, falling back is safer than
                            // selecting the wrong quest.
                            tied = true;
                        }
                    }

                    if (best != null && !tied)
                    {
                        dialogId = best.DialogID;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn, "Quest NPC status selection failed for {0}: {1}", mobName, ex.Message);
            }
            return false;
        }

        public static bool TryResolveUnique(string mobName, out uint dialogId)
        {
            dialogId = 0;
            List<Candidate> candidates;
            if (!TryGetCandidates(mobName, out candidates))
                return false;

            uint uniqueDialog = 0;
            bool haveDialog = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!haveDialog)
                {
                    uniqueDialog = candidates[i].DialogID;
                    haveDialog = true;
                }
                else if (candidates[i].DialogID != uniqueDialog)
                {
                    Log.WriteLine(LogLevel.Debug,
                        "Quest NPC {0} has {1} distinct start dialogs; quest state is required.",
                        mobName, CountDistinctDialogs(candidates));
                    return false;
                }
            }

            if (!haveDialog) return false;
            dialogId = uniqueDialog;
            return true;
        }

        public static bool TryGetCandidates(string mobName, out List<uint> dialogIds)
        {
            dialogIds = new List<uint>();
            List<Candidate> candidates;
            if (!TryGetCandidatesInternal(mobName, out candidates))
                return false;

            HashSet<uint> seen = new HashSet<uint>();
            for (int i = 0; i < candidates.Count; i++)
                if (seen.Add(candidates[i].DialogID))
                    dialogIds.Add(candidates[i].DialogID);
            return true;
        }

        private static bool TryGetCandidates(string mobName, out List<Candidate> candidates)
        {
            return TryGetCandidatesInternal(mobName, out candidates);
        }

        private static bool TryGetCandidatesInternal(string mobName, out List<Candidate> candidates)
        {
            candidates = null;
            if (string.IsNullOrWhiteSpace(mobName)) return false;
            EnsureLoaded();
            lock (Sync)
            {
                if (!_available) return false;
                return ByMobName.TryGetValue(mobName, out candidates);
            }
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_initialized) return;
                _initialized = true;

                try
                {
                    using (DatabaseClient db = Program.DatabaseManager.GetClient())
                    {
                        const string sql =
                            "SELECT m.InxName AS MobName, q.QuestID, s.DialogID " +
                            "FROM data_quest q " +
                            "INNER JOIN data_quest_start_dialog s ON s.QuestID=q.QuestID " +
                            "INNER JOIN data_mobinfo m ON m.ID=q.StartingNpc " +
                            "WHERE q.StartingNpc<>0 " +
                            "ORDER BY m.InxName, q.QuestID";

                        DataTable data = db.ReadDataTable(sql);
                        if (data == null) return;

                        foreach (DataRow row in data.Rows)
                        {
                            string mobName = (string)row["MobName"];
                            uint questId = NextGen.Database.DataStore.GetDataTypes.GetUint(row["QuestID"]);
                            uint dialogId = NextGen.Database.DataStore.GetDataTypes.GetUint(row["DialogID"]);

                            List<Candidate> list;
                            if (!ByMobName.TryGetValue(mobName, out list))
                            {
                                list = new List<Candidate>();
                                ByMobName.Add(mobName, list);
                            }
                            list.Add(new Candidate { QuestID = questId, DialogID = dialogId });
                        }

                        _available = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogLevel.Warn,
                        "Quest NPC start mapping unavailable: {0}", ex.Message);
                }
            }
        }

        private static int CountDistinctDialogs(List<Candidate> candidates)
        {
            HashSet<uint> ids = new HashSet<uint>();
            for (int i = 0; i < candidates.Count; i++) ids.Add(candidates[i].DialogID);
            return ids.Count;
        }
    }
}
