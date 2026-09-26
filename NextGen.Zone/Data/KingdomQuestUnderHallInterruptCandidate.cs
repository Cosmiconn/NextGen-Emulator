namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHallInterruptCandidateResult : byte
    {
        Unsupported = 0,
        NotDue = 1,
        Due = 2,
        Invalid = 3,
    }

    /// <summary>
    /// Explicit native predicate boundary for the two non-timing interrupt
    /// kinds used by KQ/UnderHall. Implementations own the original gameplay
    /// predicate; this interface assigns no HP/elimination meaning to source
    /// operands beyond the already-parsed native plan.
    /// </summary>
    public interface IKingdomQuestUnderHallEventPredicateSource
    {
        bool TryEvaluateHPLow(
            KingdomQuestPineInterruptSetPlan plan,
            out bool due);

        bool TryEvaluatePlayerEliminate(
            KingdomQuestPineInterruptSetPlan plan,
            out bool due);
    }

    /// <summary>
    /// Per-plan candidate evaluation for all four interrupt kinds used by
    /// KQ/UnderHall:
    /// HPLow=5, PlayerEliminate=19, Sec=18, TimeOut=19.
    ///
    /// Sec/TimeOut use the recovered native tick predicates.
    /// HPLow/PlayerEliminate are delegated to explicit native predicate owners.
    ///
    /// This class does not iterate/select manager entries, mutate RepeatCount,
    /// remove an interrupt, construct InterruptArg, or choose BlastCheck order.
    /// </summary>
    public static class KingdomQuestUnderHallInterruptCandidate
    {
        public const int HPLowCount = 5;
        public const int PlayerEliminateCount = 19;
        public const int SecondIntervalCount = 18;
        public const int TimeOutCount = 19;
        public const int TotalCount =
            HPLowCount +
            PlayerEliminateCount +
            SecondIntervalCount +
            TimeOutCount;

        public static KingdomQuestUnderHallInterruptCandidateResult Evaluate(
            KingdomQuestPineInterruptSetPlan plan,
            uint currentTick,
            KingdomQuestPineTimeLimitPlan timeLimit,
            IKingdomQuestUnderHallEventPredicateSource eventSource)
        {
            if (plan == null)
                return KingdomQuestUnderHallInterruptCandidateResult.Invalid;

            switch (plan.Kind)
            {
                case KingdomQuestPineInterruptKind.SecondInterval:
                case KingdomQuestPineInterruptKind.TimeOut:
                    KingdomQuestUnderHallTimedInterruptEvaluation timed =
                        KingdomQuestUnderHallTimedInterruptDue.Evaluate(
                            plan, currentTick, timeLimit);
                    switch (timed)
                    {
                        case KingdomQuestUnderHallTimedInterruptEvaluation.Due:
                            return KingdomQuestUnderHallInterruptCandidateResult.Due;
                        case KingdomQuestUnderHallTimedInterruptEvaluation.NotDue:
                            return KingdomQuestUnderHallInterruptCandidateResult.NotDue;
                        case KingdomQuestUnderHallTimedInterruptEvaluation.Invalid:
                            return KingdomQuestUnderHallInterruptCandidateResult.Invalid;
                        default:
                            return KingdomQuestUnderHallInterruptCandidateResult.Unsupported;
                    }

                case KingdomQuestPineInterruptKind.HPLow:
                    if (eventSource == null ||
                        plan.Arguments == null ||
                        plan.Arguments.Count != 2)
                        return KingdomQuestUnderHallInterruptCandidateResult.Invalid;

                    bool hpLowDue;
                    if (!eventSource.TryEvaluateHPLow(plan, out hpLowDue))
                        return KingdomQuestUnderHallInterruptCandidateResult.Invalid;
                    return hpLowDue
                        ? KingdomQuestUnderHallInterruptCandidateResult.Due
                        : KingdomQuestUnderHallInterruptCandidateResult.NotDue;

                case KingdomQuestPineInterruptKind.PlayerEliminate:
                    if (eventSource == null ||
                        plan.Arguments == null ||
                        plan.Arguments.Count != 0)
                        return KingdomQuestUnderHallInterruptCandidateResult.Invalid;

                    bool eliminateDue;
                    if (!eventSource.TryEvaluatePlayerEliminate(
                            plan, out eliminateDue))
                        return KingdomQuestUnderHallInterruptCandidateResult.Invalid;
                    return eliminateDue
                        ? KingdomQuestUnderHallInterruptCandidateResult.Due
                        : KingdomQuestUnderHallInterruptCandidateResult.NotDue;

                default:
                    return KingdomQuestUnderHallInterruptCandidateResult.Unsupported;
            }
        }
    }
}
