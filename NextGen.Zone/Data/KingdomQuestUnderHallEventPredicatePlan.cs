using System;
using System.Globalization;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHallEventPredicateKind : byte
    {
        HPLow = 1,
        PlayerEliminate = 2,
    }

    /// <summary>
    /// Source-resolved projection of the two non-timing interrupt predicates
    /// used by KQ/UnderHall.
    ///
    /// RawThreshold deliberately preserves the Pine numeric operand without
    /// naming its native unit/comparison semantics.
    /// </summary>
    public sealed class KingdomQuestUnderHallEventPredicatePlan
    {
        public KingdomQuestUnderHallEventPredicateKind Kind { get; private set; }
        public KingdomQuestUnderHallMobSourceRef TargetMob { get; private set; }
        public int? RawThreshold { get; private set; }
        public string ActionBlock { get; private set; }

        internal KingdomQuestUnderHallEventPredicatePlan(
            KingdomQuestUnderHallEventPredicateKind kind,
            KingdomQuestUnderHallMobSourceRef targetMob,
            int? rawThreshold,
            string actionBlock)
        {
            Kind = kind;
            TargetMob = targetMob;
            RawThreshold = rawThreshold;
            ActionBlock = actionBlock ?? string.Empty;
        }
    }

    /// <summary>
    /// Accepts only the exact source-used UnderHall HPLow/PlayerEliminate
    /// registration shapes.
    ///
    /// HPLow source:
    ///   KQ_BossRobo 800 -> Summon1
    ///   KQ_BossRobo 600 -> Summon2
    ///   KQ_BossRobo 400 -> Summon3
    ///   KQ_BossRobo 200 -> Summon4
    ///   KQ_BossRobo 100 -> Summon5
    ///
    /// PlayerEliminate occurs 19 times and always targets QuestFail.
    /// </summary>
    public static class KingdomQuestUnderHallEventPredicatePlanBuilder
    {
        public const int HPLowCount = 5;
        public const int PlayerEliminateCount = 19;

        public static bool TryBuild(
            KingdomQuestPineInterruptSetPlan source,
            out KingdomQuestUnderHallEventPredicatePlan plan)
        {
            plan = null;
            if (source == null ||
                source.RepeatCount != 1 ||
                source.Arguments == null)
                return false;

            switch (source.Kind)
            {
                case KingdomQuestPineInterruptKind.HPLow:
                    return TryBuildHPLow(source, out plan);

                case KingdomQuestPineInterruptKind.PlayerEliminate:
                    return TryBuildPlayerEliminate(source, out plan);

                default:
                    return false;
            }
        }

        private static bool TryBuildHPLow(
            KingdomQuestPineInterruptSetPlan source,
            out KingdomQuestUnderHallEventPredicatePlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 2 ||
                !string.Equals(
                    source.Arguments[0],
                    "KQ_BossRobo",
                    StringComparison.Ordinal))
                return false;

            int rawThreshold;
            if (!int.TryParse(
                    source.Arguments[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out rawThreshold))
                return false;

            string expectedAction;
            switch (rawThreshold)
            {
                case 800:
                    expectedAction = "Summon1";
                    break;
                case 600:
                    expectedAction = "Summon2";
                    break;
                case 400:
                    expectedAction = "Summon3";
                    break;
                case 200:
                    expectedAction = "Summon4";
                    break;
                case 100:
                    expectedAction = "Summon5";
                    break;
                default:
                    return false;
            }

            if (!string.Equals(
                    source.ActionBlock,
                    expectedAction,
                    StringComparison.Ordinal))
                return false;

            KingdomQuestUnderHallMobSourceRef mob;
            if (!KingdomQuestUnderHallSourceCatalog.TryGetMob(
                    source.Arguments[0], out mob) ||
                mob == null ||
                mob.MobId != 1068)
                return false;

            plan = new KingdomQuestUnderHallEventPredicatePlan(
                KingdomQuestUnderHallEventPredicateKind.HPLow,
                mob,
                rawThreshold,
                source.ActionBlock);
            return true;
        }

        private static bool TryBuildPlayerEliminate(
            KingdomQuestPineInterruptSetPlan source,
            out KingdomQuestUnderHallEventPredicatePlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 0 ||
                !string.Equals(
                    source.ActionBlock,
                    KingdomQuestUnderHallSourceFlow.FailureBlock,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestUnderHallEventPredicatePlan(
                KingdomQuestUnderHallEventPredicateKind.PlayerEliminate,
                null,
                null,
                source.ActionBlock);
            return true;
        }
    }
}
