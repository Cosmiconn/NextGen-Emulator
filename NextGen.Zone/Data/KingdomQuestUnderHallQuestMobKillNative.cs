using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Source/native-resolved UnderHall projection of
    /// questmobkill 2668 "Daliy_Check" 1.
    /// </summary>
    public sealed class KingdomQuestUnderHallQuestMobKillNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public ushort QuestId { get; private set; }
        public string MobIndex { get; private set; }
        public ushort MobId { get; private set; }
        public int RepeatOperand { get; private set; }

        internal KingdomQuestUnderHallQuestMobKillNativePlan(
            int canonicalLine,
            string topLevelBlock,
            ushort questId,
            string mobIndex,
            ushort mobId,
            int repeatOperand)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            QuestId = questId;
            MobIndex = mobIndex ?? string.Empty;
            MobId = mobId;
            RepeatOperand = repeatOperand;
        }
    }

    /// <summary>
    /// Mutation-free native projection of the sole KQ/UnderHall questmobkill
    /// command.
    ///
    /// ShineQuestMobkill::sa_Step resolves the first operand to a WORD QuestID,
    /// resolves the quoted second operand through the native mob data box, and
    /// reads the third operand as the repeat bound. It then scans current-map
    /// native Player objects through AxialListNearScanObjectType(2, 0), skips
    /// so_IsEmpty players, obtains CQuestZone through so_ply_GetQuestZone and
    /// invokes QuestPlayer_ScriptMobKill or its _All variant.
    ///
    /// UnderHall fixes the operands to QuestID 2668, Daliy_Check / MobID 50000
    /// and repeat operand 1. Therefore it always selects the direct
    /// QuestPlayer_ScriptMobKill branch and performs at most one call per
    /// selected non-empty player with a QuestZone.
    ///
    /// This class validates those source/data identities and native loop bounds.
    /// It does not enumerate map players, resolve emulator player identity, or
    /// mutate CQuestZone.
    /// </summary>
    public static class KingdomQuestUnderHallQuestMobKillNative
    {
        public const uint ShineQuestMobKillStepAddress = 0x004F8270u;
        public const uint AxialListNearScanObjectTypeCtorAddress = 0x004C1B40u;
        public const uint QuestPlayerScriptMobKillAddress = 0x005C0B40u;
        public const uint QuestPlayerScriptMobKillAllAddress = 0x005C0CF0u;

        public const byte NativePlayerObjectType =
            (byte)KingdomQuestPineNativeObjectType.Player;
        public const byte NativePlayerScanFlag = 0;
        public const int IsEmptyVtableOffset = 0x300;
        public const int GetQuestZoneVtableOffset = 0x808;

        public const ushort AllQuestId = 0xFFFF;
        public const int UnderHallCanonicalLine = 379;
        public const ushort UnderHallQuestId = 2668;
        public const string UnderHallMobIndex = "Daliy_Check";
        public const ushort UnderHallMobId = 50000;
        public const int UnderHallRepeatOperand = 1;

        public static bool TryBuild(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            DataProvider data,
            out KingdomQuestUnderHallQuestMobKillNativePlan plan)
        {
            plan = null;
            if (source == null ||
                sourceSite == null ||
                source.Kind !=
                    KingdomQuestUnderHallExternalPlanKind.QuestMobKill ||
                sourceSite.Kind !=
                    KingdomQuestUnderHallExternalPlanKind.QuestMobKill ||
                sourceSite.CanonicalLine != UnderHallCanonicalLine ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    KingdomQuestUnderHallSourceFlow.SuccessBlock,
                    StringComparison.Ordinal) ||
                source.RawNumeric1 != UnderHallQuestId ||
                source.Count != UnderHallRepeatOperand ||
                !string.Equals(
                    source.SourceToken,
                    UnderHallMobIndex,
                    StringComparison.Ordinal) ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            if (!data.MobsByName.TryGetValue(
                    UnderHallMobIndex, out clientInfo) ||
                clientInfo == null ||
                !data.MobData.TryGetValue(
                    UnderHallMobIndex, out serverInfo) ||
                serverInfo == null ||
                clientInfo.ID != UnderHallMobId ||
                serverInfo.ID != UnderHallMobId ||
                clientInfo.ID != unchecked((ushort)serverInfo.ID))
                return false;

            plan = new KingdomQuestUnderHallQuestMobKillNativePlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                UnderHallQuestId,
                UnderHallMobIndex,
                UnderHallMobId,
                UnderHallRepeatOperand);
            return true;
        }

        /// <summary>
        /// Native sa_Step computes min(selected-player-count, operand3) before
        /// entering each selected player's inner ScriptMobKill loop.
        /// </summary>
        public static bool TryGetEffectiveRepeatCount(
            KingdomQuestUnderHallQuestMobKillNativePlan plan,
            int selectedPlayerCount,
            out int effectiveRepeatCount)
        {
            effectiveRepeatCount = 0;
            if (plan == null ||
                plan.RepeatOperand != UnderHallRepeatOperand ||
                selectedPlayerCount < 0)
                return false;

            effectiveRepeatCount =
                selectedPlayerCount < plan.RepeatOperand
                    ? selectedPlayerCount
                    : plan.RepeatOperand;
            return true;
        }

        public static bool UsesDirectQuestVariant(
            KingdomQuestUnderHallQuestMobKillNativePlan plan)
        {
            return plan != null &&
                plan.QuestId != AllQuestId &&
                plan.QuestId == UnderHallQuestId;
        }
    }
}
