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
            public byte Type;
            public byte Repeatable;
            public byte StartLevelEnabled;
            public byte StartLevelMin;
            public byte StartQuestEnabled;
            public byte StartItemEnabled;
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
        private static readonly byte[] QuestTypePriority =
            { 5, 11, 1, 0, 11, 3, 2, 2, 2, 4, 11 };

        public static bool TryResolveForCharacter(NextGen.Zone.Game.ZoneCharacter character, string mobName, out uint dialogId)
        {
            dialogId = 0;
            if (character == null) return false;

            List<Candidate> candidates;
            if (!TryGetCandidates(mobName, out candidates)) return false;

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
                Dictionary<uint, byte> statuses = new Dictionary<uint, byte>();
                using (DatabaseClient db = Program.CharDBManager.GetClient())
                {
                    DataTable state = db.ReadDataTable(
                        "SELECT nQuestNo,nStatus FROM tQuest WHERE nCharNo=@c",
                        new MySqlConnector.MySqlParameter("@c", character.ID));
                    if (state != null)
                        foreach (DataRow row in state.Rows)
                            statuses[Convert.ToUInt32(row["nQuestNo"])] = Convert.ToByte(row["nStatus"]);
                }

                Candidate best = null;
                byte bestStatus = 0;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate candidate = candidates[i];
                    byte candidateStatus;
                    if (!statuses.TryGetValue(candidate.QuestID, out candidateStatus)) candidateStatus = 0;
                    if (best == null)
                    {
                        best = candidate;
                        bestStatus = candidateStatus;
                        continue;
                    }

                    int comparison;
                    if (!TryCompareOriginal(candidate, candidateStatus, best, bestStatus, out comparison))
                    {
                        Log.WriteLine(LogLevel.Debug,
                            "Quest NPC {0} requires unresolved original Type==3 tie-break; using legacy interaction.", mobName);
                        return false;
                    }
                    if (comparison < 0)
                    {
                        best = candidate;
                        bestStatus = candidateStatus;
                    }
                }

                if (best != null)
                {
                    dialogId = best.DialogID;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn, "Quest NPC status selection failed for {0}: {1}", mobName, ex.Message);
            }
            return false;
        }

        // Returns comparison < 0 when candidate replaces retained. The ordering below
        // follows CQuest::GetQuestStatusWithNPC: status priority, level predicate,
        // condition flags, then type priority. Type==3 has a special original branch
        // whose complete semantics remain unresolved, so that one case is not guessed.
        private static bool TryCompareOriginal(Candidate candidate, byte candidateStatus,
            Candidate retained, byte retainedStatus, out int comparison)
        {
            comparison = 0;
            byte cp = candidateStatus < QuestStatusPriority.Length ? QuestStatusPriority[candidateStatus] : byte.MaxValue;
            byte rp = retainedStatus < QuestStatusPriority.Length ? QuestStatusPriority[retainedStatus] : byte.MaxValue;
            if (cp != rp) { comparison = cp < rp ? -1 : 1; return true; }

            if (candidate.StartLevelEnabled != 0)
            {
                if (candidate.StartLevelMin != retained.StartLevelMin)
                {
                    comparison = candidate.StartLevelMin > retained.StartLevelMin ? -1 : 1;
                    return true;
                }
            }

            if (candidate.Type != retained.Type && (candidate.Type == 3 || retained.Type == 3))
                return false;

            int flag = ComparePreferZero(candidate.StartQuestEnabled, retained.StartQuestEnabled);
            if (flag != 0) { comparison = flag; return true; }
            flag = ComparePreferZero(candidate.StartItemEnabled, retained.StartItemEnabled);
            if (flag != 0) { comparison = flag; return true; }
            flag = ComparePreferZero(candidate.Repeatable, retained.Repeatable);
            if (flag != 0) { comparison = flag; return true; }

            byte ctp = candidate.Type < QuestTypePriority.Length ? QuestTypePriority[candidate.Type] : byte.MaxValue;
            byte rtp = retained.Type < QuestTypePriority.Length ? QuestTypePriority[retained.Type] : byte.MaxValue;
            // Original rejects candidate on >=, so equal type priority retains the
            // first candidate from the authoritative QuestID-ordered SQL result.
            comparison = ctp < rtp ? -1 : 1;
            return true;
        }

        private static int ComparePreferZero(byte candidate, byte retained)
        {
            if (candidate == retained) return 0;
            return candidate == 0 ? -1 : 1;
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
                            "SELECT m.InxName AS MobName, q.QuestID, d.DialogID, q.Type, q.Repeatable, " +
                            "s.bLevel, s.LevelMin, s.bQuest, s.bItem " +
                            "FROM QuestData q " +
                            "INNER JOIN QuestData_ConditionStart s ON s.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_start_dialog d ON d.QuestID=q.QuestID " +
                            "INNER JOIN data_mobinfo m ON m.ID=s.NPCID " +
                            "WHERE s.NPCID<>0 " +
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
                            list.Add(new Candidate
                            {
                                QuestID = questId,
                                DialogID = dialogId,
                                Type = Convert.ToByte(row["Type"]),
                                Repeatable = Convert.ToByte(row["Repeatable"]),
                                StartLevelEnabled = Convert.ToByte(row["bLevel"]),
                                StartLevelMin = Convert.ToByte(row["LevelMin"]),
                                StartQuestEnabled = Convert.ToByte(row["bQuest"]),
                                StartItemEnabled = Convert.ToByte(row["bItem"])
                            });
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
