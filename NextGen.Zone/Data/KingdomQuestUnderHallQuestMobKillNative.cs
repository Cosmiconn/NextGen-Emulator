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
        public const int UnderHallCanonicalLine = 379;
        public const ushort UnderHallQuestId =
            KingdomQuestPineUsedQuestMobKillNative.UsedQuestId;
        public const string UnderHallMobIndex =
            KingdomQuestPineUsedQuestMobKillNative.UsedMobIndex;
        public const ushort UnderHallMobId =
            KingdomQuestPineUsedQuestMobKillNative.UsedMobId;
        public const int UnderHallRepeatOperand =
            KingdomQuestPineUsedQuestMobKillNative.UsedRepeatOperand;

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

            KingdomQuestPineUsedQuestMobKillNativePlan nativePlan;
            if (!KingdomQuestPineUsedQuestMobKillNative.TryBuild(
                    source.RawNumeric1,
                    source.SourceToken,
                    source.Count,
                    data,
                    out nativePlan) ||
                nativePlan == null)
                return false;

            plan = new KingdomQuestUnderHallQuestMobKillNativePlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                nativePlan.QuestId,
                nativePlan.MobIndex,
                nativePlan.MobId,
                nativePlan.RepeatOperand);
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
            return plan != null &&
                KingdomQuestPineUsedQuestMobKillNative
                    .TryGetEffectiveRepeatCount(
                        plan.RepeatOperand,
                        selectedPlayerCount,
                        out effectiveRepeatCount);
        }

        public static bool UsesDirectQuestVariant(
            KingdomQuestUnderHallQuestMobKillNativePlan plan)
        {
            return plan != null &&
                KingdomQuestPineUsedQuestMobKillNative
                    .UsesDirectQuestVariant(plan.QuestId);
        }
    }
}
