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
            public byte DailyQuestType;
            public byte StartLevelEnabled;
            public byte StartLevelMin;
            public byte StartLevelMax;
            public byte StartQuestEnabled;
            public uint StartQuestID;
            public byte StartQuestType;
            public byte StartQuestDailyType;
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
            public bool IsStartNpcAssociation;
            public bool IsRewardNpcAssociation;
            public bool IsAction3NpcAssociation;
        }

        private static readonly object Sync = new object();
        private static bool _initialized;
        private static bool _available;
        private static readonly Dictionary<string, List<Candidate>> ByMobName =
            new Dictionary<string, List<Candidate>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<uint, Candidate> ByQuestId =
            new Dictionary<uint, Candidate>();

        // Exact status-priority table written by the original Zone.exe CQuest constructor.
        // Lower numeric priority wins. Equal-priority candidates require additional
        // original predicates and therefore are deliberately not guessed here.
        private static readonly byte[] QuestStatusPriority =
            { 22, 21, 21, 21, 21, 2, 1, 2, 0, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 2 };
        private static readonly byte[] QuestTypePriority =
            { 5, 11, 1, 0, 11, 3, 2, 2, 2, 4, 11 };

        public static bool TryResolveForCharacter(NextGen.Zone.Game.ZoneCharacter character, string mobName, out uint dialogId)
        {
            uint questId;
            QuestScriptStage stage;
            return TryResolveForCharacter(character, mobName, out questId, out dialogId, out stage);
        }

        public static bool TryResolveForCharacter(NextGen.Zone.Game.ZoneCharacter character, string mobName,
            out uint questId, out uint dialogId)
        {
            QuestScriptStage stage;
            return TryResolveForCharacter(character, mobName, out questId, out dialogId, out stage);
        }

        public static bool TryResolveForCharacter(NextGen.Zone.Game.ZoneCharacter character, string mobName,
            out uint questId, out uint dialogId, out QuestScriptStage stage)
        {
            questId = 0;
            dialogId = 0;
            stage = QuestScriptStage.Start;
            if (character == null) return false;

            List<Candidate> candidates;
            if (!TryGetCandidates(mobName, out candidates)) return false;

            try
            {
                Dictionary<uint, byte> statuses = LoadQuestStatuses(character.ID);
                Candidate best = null;
                byte bestStatus = 0;

                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate candidate = candidates[i];
                    byte rawStatus;
                    bool persisted = statuses.TryGetValue(candidate.QuestID, out rawStatus);
                    byte candidateStatus;
                    if (!TryGetEffectiveStatus(character, candidate, statuses,
                            persisted, rawStatus, out candidateStatus))
                    {
                        // A source condition exists but its emulator mapping is not
                        // proven (currently only future race/date data or a legacy
                        // daily completion without a timestamp). Preserve the old
                        // interaction path rather than guessing.
                        return false;
                    }

                    if (!TryApplyNpcAssociationStatus(candidate, candidateStatus, out candidateStatus))
                        continue;

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

                if (best != null && TryGetDialogForStatus(
                        character, best, bestStatus, out dialogId, out stage))
                {
                    questId = best.QuestID;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn, "Quest NPC status selection failed for {0}: {1}", mobName, ex.Message);
            }
            return false;
        }

        // Reconstructs CQuest::GetNewQuestStatus(QUEST_DATA*) at 0x00630320.
        // The original routine does NOT apply Start eligibility uniformly to every
        // persisted state. In particular PQS_ING/PQS_REWARD use end-condition
        // rewardability, while PQS_SOON/PQS_REPEAT/PQS_ABLE use Doingable/Soonable.
        private static bool TryGetEffectiveStatus(NextGen.Zone.Game.ZoneCharacter character,
            Candidate candidate, Dictionary<uint, byte> statuses, bool persisted,
            byte rawStatus, out byte effectiveStatus)
        {
            effectiveStatus = QuestRuntime.PqsNone;

            if (!persisted)
            {
                bool soonable;
                if (!TryIsSoonable(character, candidate, statuses, out soonable))
                    return false;
                if (!soonable) return true;
                effectiveStatus = IsDoingableLevel(character, candidate)
                    ? QuestRuntime.PqsAble
                    : QuestRuntime.PqsSoon;
                return true;
            }

            switch (rawStatus)
            {
                // Native jump-table group 0: NONE / ABORT / FAILED.
                // If the quest is soonable these become PQS_SOON, otherwise NONE.
                case QuestRuntime.PqsNone:
                case QuestRuntime.PqsAbort:
                case QuestRuntime.PqsFailed:
                {
                    bool soonable;
                    if (!TryIsSoonable(character, candidate, statuses, out soonable))
                        return false;
                    effectiveStatus = soonable ? QuestRuntime.PqsSoon : QuestRuntime.PqsNone;
                    return true;
                }

                // PQS_DONE only reopens through the native Type-10 daily reset path.
                case QuestRuntime.PqsDone:
                {
                    if (candidate.Type != 10)
                    {
                        effectiveStatus = QuestRuntime.PqsDone;
                        return true;
                    }

                    bool resetElapsed;
                    if (!QuestRuntime.TryIsDailyResetElapsed(
                            character.ID, candidate.QuestID, candidate.DailyQuestType,
                            out resetElapsed))
                    {
                        Log.WriteLine(LogLevel.Debug,
                            "Quest {0} is Type 10/PQS_DONE but has no normalized completion timestamp; using legacy interaction.",
                            candidate.QuestID);
                        return false;
                    }
                    if (!resetElapsed)
                    {
                        effectiveStatus = QuestRuntime.PqsDone;
                        return true;
                    }

                    bool doingable;
                    if (!TryIsDoingable(character, candidate, statuses, out doingable))
                        return false;
                    effectiveStatus = doingable ? QuestRuntime.PqsAble : QuestRuntime.PqsDone;
                    return true;
                }

                // Native PQS_SOON path: Doingable -> ABLE, otherwise
                // Soonable -> SOON, otherwise NONE.
                case QuestRuntime.PqsSoon:
                {
                    bool soonable;
                    if (!TryIsSoonable(character, candidate, statuses, out soonable))
                        return false;
                    if (!soonable)
                    {
                        effectiveStatus = QuestRuntime.PqsNone;
                        return true;
                    }
                    effectiveStatus = IsDoingableLevel(character, candidate)
                        ? QuestRuntime.PqsAble
                        : QuestRuntime.PqsSoon;
                    return true;
                }

                case QuestRuntime.PqsRepeat:
                {
                    bool doingable;
                    if (!TryIsDoingable(character, candidate, statuses, out doingable))
                        return false;
                    effectiveStatus = doingable ? QuestRuntime.PqsRepeat : QuestRuntime.PqsNone;
                    return true;
                }

                case QuestRuntime.PqsAble:
                {
                    bool doingable;
                    if (!TryIsDoingable(character, candidate, statuses, out doingable))
                        return false;
                    effectiveStatus = doingable ? QuestRuntime.PqsAble : QuestRuntime.PqsNone;
                    return true;
                }

                // Native statuses 6 and 8 share the same rewardability branch:
                // rewardable -> 8, otherwise -> 6.
                case QuestRuntime.PqsInProgress:
                case QuestRuntime.PqsReward:
                    effectiveStatus = QuestRuntime.IsComplete(character, candidate.QuestID)
                        ? QuestRuntime.PqsReward
                        : QuestRuntime.PqsInProgress;
                    return true;

                case QuestRuntime.PqsReadAble:
                {
                    bool doingable;
                    if (!TryIsDoingable(character, candidate, statuses, out doingable))
                        return false;
                    effectiveStatus = doingable ? QuestRuntime.PqsReadAble : QuestRuntime.PqsNone;
                    return true;
                }

                default:
                    // Native statuses 9..19 (except the explicit cases above) and
                    // out-of-range values return unchanged from the jump-table path.
                    effectiveStatus = rawStatus;
                    return true;
            }
        }

        private static bool TryIsDoingable(NextGen.Zone.Game.ZoneCharacter character,
            Candidate candidate, Dictionary<uint, byte> statuses, out bool doingable)
        {
            doingable = false;
            bool soonable;
            if (!TryIsSoonable(character, candidate, statuses, out soonable))
                return false;
            if (!soonable) return true;
            doingable = IsDoingableLevel(character, candidate);
            return true;
        }

        private static bool TryIsSoonable(NextGen.Zone.Game.ZoneCharacter character,
            Candidate c, Dictionary<uint, byte> statuses, out bool soonable)
        {
            soonable = false;

            if (c.StartLevelEnabled != 0)
            {
                int soonLevel = character.Level + 5;
                if (soonLevel < c.StartLevelMin || soonLevel > c.StartLevelMax)
                    return true;
            }

            if (c.StartItemEnabled != 0 &&
                QuestRuntime.GetItemLot(character, c.StartItemID) < c.StartItemLot)
                return true;

            if (c.StartLocationEnabled != 0)
            {
                if (character.MapID != c.StartLocationMap) return true;
                long dx = (long)character.Character.PositionInfo.XPos - c.StartLocationX;
                long dy = (long)character.Character.PositionInfo.YPos - c.StartLocationY;
                if (dx * dx + dy * dy > (long)c.StartLocationRange * c.StartLocationRange)
                    return true;
            }

            if (c.StartClassEnabled != 0 && (byte)character.Job != c.StartClass)
                return true;

            if (c.StartGenderEnabled != 0 &&
                (character.IsMale ? (byte)1 : (byte)0) != c.StartGender)
                return true;

            // The supplied NA2016 corpus has zero active start Race/Date gates.
            // Keep future data conservative until their exact runtime mappings are
            // normalized rather than silently accepting them.
            if (c.StartRaceEnabled != 0 || c.StartDateEnabled != 0)
            {
                Log.WriteLine(LogLevel.Debug,
                    "Quest {0} uses unresolved race/date start eligibility; using legacy interaction.",
                    c.QuestID);
                return false;
            }

            if (c.StartQuestEnabled != 0)
            {
                byte prerequisiteStatus;
                if (!statuses.TryGetValue(c.StartQuestID, out prerequisiteStatus))
                    return true;

                if (prerequisiteStatus == QuestRuntime.PqsRepeat)
                {
                    // Proven accepted predecessor state.
                }
                else if (prerequisiteStatus == QuestRuntime.PqsDone)
                {
                    // Status-2 predecessors are accepted unless their quest is
                    // Type 10 and the daily reset already elapsed.
                    if (c.StartQuestType == 10)
                    {
                        bool prerequisiteSatisfied;
                        if (!QuestRuntime.TryEvaluateDailyPrerequisite(
                                character.ID, c.StartQuestID, c.StartQuestDailyType,
                                out prerequisiteSatisfied))
                        {
                            Log.WriteLine(LogLevel.Debug,
                                "Quest {0} daily predecessor {1} has no normalized completion timestamp; using legacy interaction.",
                                c.QuestID, c.StartQuestID);
                            return false;
                        }
                        if (!prerequisiteSatisfied) return true;
                    }
                }
                else return true;
            }

            soonable = true;
            return true;
        }

        // Native ACCEPT command 6 calls the QuestID overload of
        // CQuest::IsDoingableQuest at 0x0062FEE0 before changing quest state.
        // This entry point deliberately ignores the current quest status and
        // evaluates only the proven Start conditions plus the current-level gate.
        internal static bool TryIsDoingableForQuest(
            NextGen.Zone.Game.ZoneCharacter character, uint questId,
            out bool doingable)
        {
            doingable = false;
            if (character == null || questId == 0) return false;

            EnsureLoaded();
            Candidate candidate;
            lock (Sync)
            {
                if (!_available || !ByQuestId.TryGetValue(questId, out candidate))
                    return false;
            }

            try
            {
                Dictionary<uint, byte> statuses = LoadQuestStatuses(character.ID);
                return TryIsDoingable(character, candidate, statuses, out doingable);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn,
                    "Quest doingable check failed for {0}: {1}", questId, ex.Message);
                return false;
            }
        }

        internal static bool TryResolveLinkedStage(
            NextGen.Zone.Game.ZoneCharacter character, uint targetQuestId,
            out QuestScriptStage stage)
        {
            stage = QuestScriptStage.Start;
            if (character == null || targetQuestId == 0) return false;

            EnsureLoaded();
            Candidate candidate;
            lock (Sync)
            {
                if (!_available || !ByQuestId.TryGetValue(targetQuestId, out candidate))
                    return false;
            }

            try
            {
                Dictionary<uint, byte> statuses = LoadQuestStatuses(character.ID);
                byte rawStatus;
                bool persisted = statuses.TryGetValue(targetQuestId, out rawStatus);
                byte effectiveStatus;
                if (!TryGetEffectiveStatus(character, candidate, statuses,
                        persisted, rawStatus, out effectiveStatus))
                    return false;

                // Original QSC_LINK command 11 dispatches the linked quest's
                // effective status exactly as follows:
                // 4/5/20 -> QuestStart, 6/7 -> QuestDoing, 8 -> QuestEnd.
                switch (effectiveStatus)
                {
                    case QuestRuntime.PqsRepeat:
                    case QuestRuntime.PqsAble:
                    case QuestRuntime.PqsReadAble:
                        stage = QuestScriptStage.Start;
                        return true;
                    case QuestRuntime.PqsInProgress:
                    case QuestRuntime.PqsFailed:
                        stage = QuestScriptStage.Action;
                        return true;
                    case QuestRuntime.PqsReward:
                        stage = QuestScriptStage.Finish;
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn,
                    "Quest LINK status resolution failed for {0}: {1}",
                    targetQuestId, ex.Message);
                return false;
            }
        }

        // Reconstructs the NPC-specific post-processing in
        // CQuest::GetQuestStatusWithNPC helper 0x00630570 with bQmark=0.
        // The broad 0x0062FB50 association test admits Start NPC and End
        // action 0/3 NPCs, but the effective status is then filtered by role.
        private static bool TryApplyNpcAssociationStatus(Candidate candidate,
            byte effectiveStatus, out byte npcStatus)
        {
            npcStatus = effectiveStatus;
            switch (effectiveStatus)
            {
                case QuestRuntime.PqsNone:
                case QuestRuntime.PqsAbort:
                case QuestRuntime.PqsDone:
                case QuestRuntime.PqsSoon:
                    return false;

                case QuestRuntime.PqsRepeat:
                case QuestRuntime.PqsAble:
                case QuestRuntime.PqsFailed:
                case QuestRuntime.PqsReadAble:
                    return candidate.IsStartNpcAssociation;

                case QuestRuntime.PqsInProgress:
                    return candidate.IsStartNpcAssociation ||
                           candidate.IsAction3NpcAssociation;

                case QuestRuntime.PqsReward:
                    if (candidate.IsRewardNpcAssociation)
                        return true;
                    if (candidate.IsStartNpcAssociation)
                    {
                        // Native 0x00630654 changes the NPC-visible state
                        // from REWARD to ING at the quest's Start NPC.
                        npcStatus = QuestRuntime.PqsInProgress;
                        return true;
                    }
                    return false;

                default:
                    // The native status table passes the remaining 9..19
                    // values through the common accepted path.
                    return true;
            }
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
            Candidate candidate, byte status, out uint dialogId, out QuestScriptStage stage)
        {
            dialogId = 0;
            stage = QuestScriptStage.Start;
            if (candidate == null) return false;

            if (status == QuestRuntime.PqsAble)
            {
                dialogId = candidate.DialogID;
                stage = QuestScriptStage.Start;
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
                stage = QuestScriptStage.Start;
                return dialogId != 0;
            }

            if (status == QuestRuntime.PqsInProgress)
            {
                dialogId = candidate.ActionDialogID;
                stage = QuestScriptStage.Action;
                return dialogId != 0;
            }

            if (status == QuestRuntime.PqsReward)
            {
                dialogId = candidate.FinishDialogID;
                stage = QuestScriptStage.Finish;
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
                            "SELECT m.InxName AS MobName, q.QuestID, COALESCE(d.DialogID,0) AS DialogID, q.Type, q.Repeatable, " +
                            "COALESCE(a.IsStartNPC,0) AS IsStartNPC, COALESCE(a.IsRewardNPC,0) AS IsRewardNPC, COALESCE(a.IsAction3NPC,0) AS IsAction3NPC, " +
                            "COALESCE(q.DailyQuestType,0) AS DailyQuestType, q.RawData AS QuestRawData, ds.ActionScript, ds.FinishScript, " +
                            "s.bLevel, s.LevelMin, s.LevelMax, s.bQuest, s.QuestPrerequisiteID, COALESCE(pq.Type,255) AS PrerequisiteType, " +
                            "COALESCE(pq.DailyQuestType,0) AS PrerequisiteDailyType, pq.RawData AS PrerequisiteRawData, s.bItem, s.ItemID, s.ItemLot, " +
                            "s.bLocation, s.LocationRaw, s.LocationMap, s.LocationX, s.LocationY, s.LocationRange, " +
                            "s.bRace, s.Race, s.bClass, s.Class, s.bGender, s.Gender, s.bDate " +
                            "FROM QuestData q " +
                            "INNER JOIN QuestData_ConditionStart s ON s.QuestID=q.QuestID " +
                            "LEFT JOIN data_quest_start_dialog d ON d.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_script ds ON ds.QuestID=q.QuestID " +
                            "LEFT JOIN QuestData pq ON pq.QuestID=s.QuestPrerequisiteID " +
                            "LEFT JOIN (" +
                                "SELECT QuestID,NPCID,MAX(IsStartNPC) AS IsStartNPC,MAX(IsRewardNPC) AS IsRewardNPC,MAX(IsAction3NPC) AS IsAction3NPC " +
                                "FROM (" +
                                    "SELECT QuestID,NPCID,1 AS IsStartNPC,0 AS IsRewardNPC,0 AS IsAction3NPC " +
                                    "FROM QuestData_ConditionStart WHERE bNPC<>0 AND NPCID<>0 " +
                                    "UNION ALL " +
                                    "SELECT QuestID,NPCMobID AS NPCID,0 AS IsStartNPC," +
                                    "CASE WHEN NPCMobAction=0 THEN 1 ELSE 0 END AS IsRewardNPC," +
                                    "CASE WHEN NPCMobAction=3 THEN 1 ELSE 0 END AS IsAction3NPC " +
                                    "FROM QuestData_NPCMob WHERE bNPCMob<>0 AND NPCMobID<>0 AND (NPCMobAction=0 OR NPCMobAction=3)" +
                                ") npc_roles GROUP BY QuestID,NPCID" +
                            ") a ON a.QuestID=q.QuestID " +
                            "LEFT JOIN data_mobinfo m ON m.ID=a.NPCID " +
                            "ORDER BY m.InxName, q.QuestID";
                        const string sqlLegacy =
                            "SELECT m.InxName AS MobName, q.QuestID, COALESCE(d.DialogID,0) AS DialogID, q.Type, q.Repeatable, " +
                            "COALESCE(a.IsStartNPC,0) AS IsStartNPC, COALESCE(a.IsRewardNPC,0) AS IsRewardNPC, COALESCE(a.IsAction3NPC,0) AS IsAction3NPC, " +
                            "q.RawData AS QuestRawData, ds.ActionScript, ds.FinishScript, " +
                            "s.bLevel, s.LevelMin, s.LevelMax, s.bQuest, s.QuestPrerequisiteID, COALESCE(pq.Type,255) AS PrerequisiteType, " +
                            "pq.RawData AS PrerequisiteRawData, s.bItem, s.ItemID, s.ItemLot, " +
                            "s.bLocation, s.LocationRaw, s.bRace, s.Race, s.bClass, s.Class, s.bGender, s.Gender, s.bDate " +
                            "FROM QuestData q " +
                            "INNER JOIN QuestData_ConditionStart s ON s.QuestID=q.QuestID " +
                            "LEFT JOIN data_quest_start_dialog d ON d.QuestID=q.QuestID " +
                            "INNER JOIN data_quest_script ds ON ds.QuestID=q.QuestID " +
                            "LEFT JOIN QuestData pq ON pq.QuestID=s.QuestPrerequisiteID " +
                            "LEFT JOIN (" +
                                "SELECT QuestID,NPCID,MAX(IsStartNPC) AS IsStartNPC,MAX(IsRewardNPC) AS IsRewardNPC,MAX(IsAction3NPC) AS IsAction3NPC " +
                                "FROM (" +
                                    "SELECT QuestID,NPCID,1 AS IsStartNPC,0 AS IsRewardNPC,0 AS IsAction3NPC " +
                                    "FROM QuestData_ConditionStart WHERE bNPC<>0 AND NPCID<>0 " +
                                    "UNION ALL " +
                                    "SELECT QuestID,NPCMobID AS NPCID,0 AS IsStartNPC," +
                                    "CASE WHEN NPCMobAction=0 THEN 1 ELSE 0 END AS IsRewardNPC," +
                                    "CASE WHEN NPCMobAction=3 THEN 1 ELSE 0 END AS IsAction3NPC " +
                                    "FROM QuestData_NPCMob WHERE bNPCMob<>0 AND NPCMobID<>0 AND (NPCMobAction=0 OR NPCMobAction=3)" +
                                ") npc_roles GROUP BY QuestID,NPCID" +
                            ") a ON a.QuestID=q.QuestID " +
                            "LEFT JOIN data_mobinfo m ON m.ID=a.NPCID " +
                            "ORDER BY m.InxName, q.QuestID";

                        DataTable data;
                        bool normalizedLocation = true;
                        bool normalizedDailyType = true;
                        try
                        {
                            data = db.ReadDataTable(sqlNormalized);
                        }
                        catch
                        {
                            normalizedLocation = false;
                            normalizedDailyType = false;
                            data = db.ReadDataTable(sqlLegacy);
                            Log.WriteLine(LogLevel.Debug,
                                "Quest SQL uses the legacy raw layout; re-import QuestData.shn to get normalized location/daily columns.");
                        }
                        if (data == null) return;

                        foreach (DataRow row in data.Rows)
                        {
                            string mobName = row["MobName"] == DBNull.Value
                                ? null
                                : (string)row["MobName"];
                            uint questId = NextGen.Database.DataStore.GetDataTypes.GetUint(row["QuestID"]);
                            uint dialogId = NextGen.Database.DataStore.GetDataTypes.GetUint(row["DialogID"]);

                            List<Candidate> list = null;
                            if (!string.IsNullOrWhiteSpace(mobName) &&
                                !ByMobName.TryGetValue(mobName, out list))
                            {
                                list = new List<Candidate>();
                                ByMobName.Add(mobName, list);
                            }
                            byte[] locationRaw = row["LocationRaw"] as byte[];
                            byte[] questRaw = row["QuestRawData"] as byte[];
                            byte[] prerequisiteRaw = row["PrerequisiteRawData"] as byte[];
                            byte dailyQuestType = normalizedDailyType
                                ? Convert.ToByte(row["DailyQuestType"])
                                : (questRaw != null && questRaw.Length > 0x13
                                    ? questRaw[0x13] : (byte)0);
                            byte prerequisiteDailyType = normalizedDailyType
                                ? Convert.ToByte(row["PrerequisiteDailyType"])
                                : (prerequisiteRaw != null && prerequisiteRaw.Length > 0x13
                                    ? prerequisiteRaw[0x13] : (byte)0);
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
                            Candidate candidate = new Candidate
                            {
                                    QuestID = questId,
                                    DialogID = dialogId,
                                    ActionDialogID = GetFirstSayDialogId(row["ActionScript"] == DBNull.Value ? string.Empty : (string)row["ActionScript"]),
                                    FinishDialogID = GetFirstSayDialogId(row["FinishScript"] == DBNull.Value ? string.Empty : (string)row["FinishScript"]),
                                    Type = Convert.ToByte(row["Type"]),
                                    Repeatable = Convert.ToByte(row["Repeatable"]),
                                    DailyQuestType = dailyQuestType,
                                    StartLevelEnabled = Convert.ToByte(row["bLevel"]),
                                    StartLevelMin = Convert.ToByte(row["LevelMin"]),
                                    StartLevelMax = Convert.ToByte(row["LevelMax"]),
                                    StartQuestEnabled = Convert.ToByte(row["bQuest"]),
                                    StartQuestID = Convert.ToUInt32(row["QuestPrerequisiteID"]),
                                    StartQuestType = Convert.ToByte(row["PrerequisiteType"]),
                                    StartQuestDailyType = prerequisiteDailyType,
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
                                    StartDateEnabled = Convert.ToByte(row["bDate"]),
                                    IsStartNpcAssociation = Convert.ToByte(row["IsStartNPC"]) != 0,
                                IsRewardNpcAssociation = Convert.ToByte(row["IsRewardNPC"]) != 0,
                                IsAction3NpcAssociation = Convert.ToByte(row["IsAction3NPC"]) != 0
                            };
                            if (!ByQuestId.ContainsKey(questId))
                                ByQuestId.Add(questId, candidate);
                            if (list != null)
                                list.Add(candidate);
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
