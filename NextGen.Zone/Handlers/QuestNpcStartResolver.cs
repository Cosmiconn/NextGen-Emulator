using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database;
using NextGen.Util;
using NextGen.Zone.Data;
using NextGen.FiestaLib.Data;

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
            public uint ActionDialogID;
            public uint FinishDialogID;
            public byte Type;
            public byte Repeatable;
            public byte StartLevelEnabled;
            public byte StartLevelMin;
            public byte StartLevelMax;
            public byte StartQuestEnabled;
            public uint StartQuestID;
            public byte StartItemEnabled;
            public ushort StartItemID;
            public ushort StartItemLot;
            public byte StartLocationEnabled;
            public ushort StartLocationMap;
            public int StartLocationX;
            public int StartLocationY;
            public uint StartLocationRange;
            public byte StartRaceEnabled;
            public byte StartRace;
            public byte StartClassEnabled;
            public byte StartClass;
            public byte StartGenderEnabled;
            public byte StartGender;
            public byte StartDateEnabled;
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

            // Proven IsSoonableQuest subset. Level and item predicates are direct
            // Zone.exe behavior. Prerequisite state 2/4 is also observed, but the
            // additional state-2 quest-state call remains unresolved; status 2 is
            // therefore not guessed here and falls back to legacy interaction.
            Dictionary<uint, byte> prerequisiteStatuses = null;
            List<Candidate> eligible = new List<Candidate>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate c = candidates[i];
                if (c.StartLevelEnabled != 0)
                {
                    int soonLevel = character.Level + 5;
                    if (soonLevel < c.StartLevelMin || soonLevel > c.StartLevelMax) continue;
                }
                if (c.StartItemEnabled != 0 && QuestRuntime.GetItemLot(character, c.StartItemID) < c.StartItemLot)
                    continue;
                if (c.StartLocationEnabled != 0)
                {
                    if (character.MapID != c.StartLocationMap) continue;
                    long dx = (long)character.Character.PositionInfo.XPos - c.StartLocationX;
                    long dy = (long)character.Character.PositionInfo.YPos - c.StartLocationY;
                    if (dx * dx + dy * dy > (long)c.StartLocationRange * c.StartLocationRange) continue;
                }

                // ClassName.shn uses the same numeric class IDs as Character.Job
                // (Fighter=1, Cleric=6, Archer=11, Mage=16, ...). The original
                // IsSoonableQuest compares the player class directly with Start.Class.
                if (c.StartClassEnabled != 0 && (byte)character.Job != c.StartClass)
                    continue;

                // Original IsSoonableQuest compares the player gender directly.
                // Fiesta's character gender bit is 1=male, 0=female; the only two
                // gender-gated NA2016 quests independently match that convention.
                if (c.StartGenderEnabled != 0 &&
                    (character.IsMale ? (byte)1 : (byte)0) != c.StartGender)
                    continue;

                // The supplied NA2016 QuestData corpus contains no active bRace or
                // bDate start conditions. Their original comparisons/date modes are
                // not normalized here; if future data enables them, do not guess.
                if (c.StartRaceEnabled != 0 || c.StartDateEnabled != 0)
                {
                    Log.WriteLine(LogLevel.Debug,
                        "Quest {0} uses unresolved race/date start eligibility; using legacy interaction.",
                        c.QuestID);
                    return false;
                }
                if (c.StartQuestEnabled != 0)
                {
                    if (prerequisiteStatuses == null)
                        prerequisiteStatuses = LoadQuestStatuses(character.ID);
                    byte prerequisiteStatus;
                    if (!prerequisiteStatuses.TryGetValue(c.StartQuestID, out prerequisiteStatus))
                        continue;
                    if (prerequisiteStatus == 4)
                    {
                        // Proven accepted predecessor state.
                    }
                    else if (prerequisiteStatus == 2)
                    {
                        Log.WriteLine(LogLevel.Debug,
                            "Quest {0} predecessor {1} is status 2; original performs an unresolved extra state check.",
                            c.QuestID, c.StartQuestID);
                        return false;
                    }
                    else continue;
                }
                eligible.Add(c);
            }
            if (eligible.Count == 0) return false;
            candidates = eligible;

            try
            {
                Dictionary<uint, byte> statuses = prerequisiteStatuses ?? LoadQuestStatuses(character.ID);

                Candidate best = null;
                byte bestStatus = 0;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate candidate = candidates[i];
                    byte candidateStatus;
                    if (!statuses.TryGetValue(candidate.QuestID, out candidateStatus))
                    {
                        // Original NPC selection computes a state for quests not yet
                        // persisted in the player list. From the proven eligibility
                        // routines: +5 level window is SOON, current-level window is ABLE.
                        candidateStatus = GetUnpersistedStatus(character, candidate);
                    }
                    else if (candidateStatus == QuestRuntime.PqsInProgress &&
                             QuestRuntime.IsComplete(character, candidate.QuestID))
                    {
                        // GetQuestStatusWithNPC has a distinct rewardable path and the
                        // original status table gives PQS_REWARD the highest priority.
                        // Promote only the effective NPC-selection state; persistence
                        // remains PQS_ING until the finish script actually completes.
                        candidateStatus = QuestRuntime.PqsReward;
                    }
                    if (best == null)
                    {
                        best = candidate;
                        bestStatus = candidateStatus;
                        continue;
                    }

                    int comparison;
                    TryCompareOriginal(candidate, candidateStatus, best, bestStatus, out comparison);
                    if (comparison < 0)
                    {
                        best = candidate;
                        bestStatus = candidateStatus;
                    }
                }

                if (best != null)
                    return TryGetDialogForStatus(character, best, bestStatus, out dialogId);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn, "Quest NPC status selection failed for {0}: {1}", mobName, ex.Message);
            }
            return false;
        }

        // Returns comparison < 0 when candidate replaces retained. The ordering below
        // follows CQuest::GetQuestStatusWithNPC: status priority, level predicate,
        // the exact Type==3 special branch, condition flags, then type priority.
        private static void TryCompareOriginal(Candidate candidate, byte candidateStatus,
            Candidate retained, byte retainedStatus, out int comparison)
        {
            comparison = 0;
            byte cp = candidateStatus < QuestStatusPriority.Length ? QuestStatusPriority[candidateStatus] : byte.MaxValue;
            byte rp = retainedStatus < QuestStatusPriority.Length ? QuestStatusPriority[retainedStatus] : byte.MaxValue;
            if (cp != rp) { comparison = cp < rp ? -1 : 1; return; }

            if (candidate.StartLevelEnabled != 0)
            {
                if (candidate.StartLevelMin != retained.StartLevelMin)
                {
                    comparison = candidate.StartLevelMin > retained.StartLevelMin ? -1 : 1;
                    return;
                }
            }

            // Exact original branch at Zone.exe 0x0063103A..0x00631060:
            // candidate Type 3 replaces any non-3 retained candidate; a retained
            // Type 3 rejects every non-3 candidate; Type 3 vs Type 3 keeps the first.
            if (candidate.Type == 3)
            {
                comparison = retained.Type == 3 ? 1 : -1;
                return;
            }
            if (retained.Type == 3)
            {
                comparison = 1;
                return;
            }

            int flag = ComparePreferZero(candidate.StartQuestEnabled, retained.StartQuestEnabled);
            if (flag != 0) { comparison = flag; return; }
            flag = ComparePreferZero(candidate.StartItemEnabled, retained.StartItemEnabled);
            if (flag != 0) { comparison = flag; return; }
            flag = ComparePreferZero(candidate.Repeatable, retained.Repeatable);
            if (flag != 0) { comparison = flag; return; }

            byte ctp = candidate.Type < QuestTypePriority.Length ? QuestTypePriority[candidate.Type] : byte.MaxValue;
            byte rtp = retained.Type < QuestTypePriority.Length ? QuestTypePriority[retained.Type] : byte.MaxValue;
            // Original rejects candidate on >=, so equal type priority retains the
            // first candidate from the authoritative QuestID-ordered SQL result.
            comparison = ctp < rtp ? -1 : 1;
        }

        private static bool TryGetDialogForStatus(NextGen.Zone.Game.ZoneCharacter character,
            Candidate candidate, byte status, out uint dialogId)
        {
            dialogId = 0;
            if (candidate == null) return false;

            if (status == QuestRuntime.PqsAble)
            {
                dialogId = candidate.DialogID;
                return dialogId != 0;
            }

            if (status == QuestRuntime.PqsRepeat)
            {
                // Database evidence defines PQS_REPEAT as a completed repeat quest that
                // is re-acceptable. The original doingable check is stricter than SOON
                // only by the current-level window; all other proven predicates already
                // passed above.
                if (!IsDoingableLevel(character, candidate)) return false;
                dialogId = candidate.DialogID;
                return dialogId != 0;
            }

            if (status == QuestRuntime.PqsInProgress)
            {
                dialogId = candidate.ActionDialogID;
                return dialogId != 0;
            }

            if (status == QuestRuntime.PqsReward)
            {
                dialogId = candidate.FinishDialogID;
                return dialogId != 0;
            }

            // PQS_SOON is deliberately not sent through the executable StartScript:
            // doing so would expose ACCEPT before the current-level requirement is met.
            // DONE/ABORT/FAILED/LOWABLE/READ_ABLE semantics also stay on the legacy
            // interaction path until their exact NPC-dialog mapping is proven.
            return false;
        }

        private static bool IsDoingableLevel(NextGen.Zone.Game.ZoneCharacter character, Candidate candidate)
        {
            if (candidate.StartLevelEnabled == 0) return true;
            return character.Level >= candidate.StartLevelMin && character.Level <= candidate.StartLevelMax;
        }

        private static uint GetFirstSayDialogId(string script)
        {
            QuestScriptProgram program = QuestScriptProgram.Parse(script);
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                QuestScriptInstruction instruction = program.Instructions[i];
                if (!instruction.OpCode.Equals("SAY", StringComparison.OrdinalIgnoreCase)) continue;
                string[] args = instruction.Arguments.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                uint id;
                if (args.Length > 0 && uint.TryParse(args[0], out id)) return id;
            }
            return 0;
        }

        private static byte GetUnpersistedStatus(NextGen.Zone.Game.ZoneCharacter character, Candidate candidate)
        {
            if (candidate.StartLevelEnabled != 0)
            {
                if (character.Level >= candidate.StartLevelMin && character.Level <= candidate.StartLevelMax)
                    return QuestRuntime.PqsAble;
                int soonLevel = character.Level + 5;
                if (soonLevel >= candidate.StartLevelMin && soonLevel <= candidate.StartLevelMax)
                    return QuestRuntime.PqsSoon;
                return QuestRuntime.PqsNone;
            }

            // IsDoingableQuest adds no further test when bLevel == 0; candidates
            // reaching this point have already passed the proven soonable subset.
            return QuestRuntime.PqsAble;
        }

        private static Dictionary<uint, byte> LoadQuestStatuses(int characterId)
        {
            Dictionary<uint, byte> statuses = new Dictionary<uint, byte>();
            using (DatabaseClient db = Program.CharDBManager.GetClient())
            {
                DataTable state = db.ReadDataTable(
                    "SELECT nQuestNo,nStatus FROM tQuest WHERE nCharNo=@c",
                    new MySqlConnector.MySqlParameter("@c", characterId));
                if (state != null)
                    foreach (DataRow row in state.Rows)
                        statuses[Convert.ToUInt32(row["nQuestNo"])] = Convert.ToByte(row["nStatus"]);
            }
            return statuses;
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
                        const string sqlNormalized =
                            "SELECT m.InxName AS MobName, q.QuestID, d.DialogID, q.Type, q.Repeatable, ds.ActionScript, ds.FinishScript, " +
                            "s.bLevel, s.LevelMin, s.LevelMax, s.bQuest, s.QuestPrerequisiteID, s.bItem, s.ItemID, s.ItemLot, " +
                            "s.bLocation, s.LocationRaw, s.LocationMap, s.LocationX, s.LocationY, s.LocationRange, " +
                            "s.bRace, s.Race, s.bClass, s.Class, s.bGender, s.Gender, s.bDate " +
                            "FROM QuestData q " +
                            "INNER JOIN QuestData_ConditionStart s ON s.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_start_dialog d ON d.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_script ds ON ds.QuestID=q.QuestID " +
                            "INNER JOIN data_mobinfo m ON m.ID=s.NPCID " +
                            "WHERE s.NPCID<>0 " +
                            "ORDER BY m.InxName, q.QuestID";
                        const string sqlLegacy =
                            "SELECT m.InxName AS MobName, q.QuestID, d.DialogID, q.Type, q.Repeatable, ds.ActionScript, ds.FinishScript, " +
                            "s.bLevel, s.LevelMin, s.LevelMax, s.bQuest, s.QuestPrerequisiteID, s.bItem, s.ItemID, s.ItemLot, " +
                            "s.bLocation, s.LocationRaw, s.bRace, s.Race, s.bClass, s.Class, s.bGender, s.Gender, s.bDate " +
                            "FROM QuestData q " +
                            "INNER JOIN QuestData_ConditionStart s ON s.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_start_dialog d ON d.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_script ds ON ds.QuestID=q.QuestID " +
                            "INNER JOIN data_mobinfo m ON m.ID=s.NPCID " +
                            "WHERE s.NPCID<>0 " +
                            "ORDER BY m.InxName, q.QuestID";

                        DataTable data;
                        bool normalizedLocation = true;
                        try
                        {
                            data = db.ReadDataTable(sqlNormalized);
                        }
                        catch
                        {
                            normalizedLocation = false;
                            data = db.ReadDataTable(sqlLegacy);
                            Log.WriteLine(LogLevel.Debug,
                                "Quest start SQL uses legacy LocationRaw layout; re-import QuestData.shn to get normalized location columns.");
                        }
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
                            byte[] locationRaw = row["LocationRaw"] as byte[];
                            ushort locationMap = normalizedLocation
                                ? Convert.ToUInt16(row["LocationMap"])
                                : (locationRaw != null && locationRaw.Length >= 4 ? BitConverter.ToUInt16(locationRaw, 2) : (ushort)0);
                            int locationX = normalizedLocation
                                ? Convert.ToInt32(row["LocationX"])
                                : (locationRaw != null && locationRaw.Length >= 8 ? BitConverter.ToInt32(locationRaw, 6) : 0);
                            int locationY = normalizedLocation
                                ? Convert.ToInt32(row["LocationY"])
                                : (locationRaw != null && locationRaw.Length >= 12 ? BitConverter.ToInt32(locationRaw, 10) : 0);
                            uint locationRange = normalizedLocation
                                ? Convert.ToUInt32(row["LocationRange"])
                                : (locationRaw != null && locationRaw.Length >= 18 ? BitConverter.ToUInt32(locationRaw, 14) : 0);
                            list.Add(new Candidate
                            {
                                QuestID = questId,
                                DialogID = dialogId,
                                ActionDialogID = GetFirstSayDialogId(row["ActionScript"] == DBNull.Value ? string.Empty : (string)row["ActionScript"]),
                                FinishDialogID = GetFirstSayDialogId(row["FinishScript"] == DBNull.Value ? string.Empty : (string)row["FinishScript"]),
                                Type = Convert.ToByte(row["Type"]),
                                Repeatable = Convert.ToByte(row["Repeatable"]),
                                StartLevelEnabled = Convert.ToByte(row["bLevel"]),
                                StartLevelMin = Convert.ToByte(row["LevelMin"]),
                                StartLevelMax = Convert.ToByte(row["LevelMax"]),
                                StartQuestEnabled = Convert.ToByte(row["bQuest"]),
                                StartQuestID = Convert.ToUInt32(row["QuestPrerequisiteID"]),
                                StartItemEnabled = Convert.ToByte(row["bItem"]),
                                StartItemID = Convert.ToUInt16(row["ItemID"]),
                                StartItemLot = Convert.ToUInt16(row["ItemLot"]),
                                StartLocationEnabled = Convert.ToByte(row["bLocation"]),
                                StartLocationMap = locationMap,
                                StartLocationX = locationX,
                                StartLocationY = locationY,
                                StartLocationRange = locationRange,
                                StartRaceEnabled = Convert.ToByte(row["bRace"]),
                                StartRace = Convert.ToByte(row["Race"]),
                                StartClassEnabled = Convert.ToByte(row["bClass"]),
                                StartClass = Convert.ToByte(row["Class"]),
                                StartGenderEnabled = Convert.ToByte(row["bGender"]),
                                StartGender = Convert.ToByte(row["Gender"]),
                                StartDateEnabled = Convert.ToByte(row["bDate"])
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
