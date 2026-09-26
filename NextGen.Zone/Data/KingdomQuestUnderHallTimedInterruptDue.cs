namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHallTimedInterruptEvaluation : byte
    {
        Unsupported = 0,
        NotDue = 1,
        Due = 2,
        Invalid = 3,
    }

    /// <summary>
    /// Mutation-free due-candidate evaluation for the two UnderHall interrupt
    /// predicates whose native BlastCheck timing semantics are already
    /// recovered.
    ///
    /// UnderHall source uses:
    /// - Sec:     18 registrations
    /// - TimeOut: 19 registrations
    ///
    /// ScriptInterruptInterval::sib_BlastCheck (0x0050AB90) is due when
    /// nextDeadline <= currentTick.
    ///
    /// ScriptInterruptTimeOut::sib_BlastCheck (0x0050AE40) asks the current
    /// Movie::TimeLimit for its signed ticks-left value and fires when that is
    /// <= 0. KingdomQuestPineInterruptPlan.IsTimeLimitExpired preserves that
    /// signed subtraction boundary.
    ///
    /// This helper deliberately does NOT select one candidate from a manager,
    /// advance an interval deadline, decrement RepeatCount, erase an entry,
    /// create InterruptArg, or decide BlastCheck iteration/order. It only says
    /// whether one explicitly supplied plan is due at one explicitly supplied
    /// tick.
    /// </summary>
    public static class KingdomQuestUnderHallTimedInterruptDue
    {
        public const int UnderHallSecondIntervalCount = 18;
        public const int UnderHallTimeOutCount = 19;
        public const int UnderHallTimedInterruptCount =
            UnderHallSecondIntervalCount + UnderHallTimeOutCount;

        public static KingdomQuestUnderHallTimedInterruptEvaluation Evaluate(
            KingdomQuestPineInterruptSetPlan plan,
            uint currentTick,
            KingdomQuestPineTimeLimitPlan timeLimit)
        {
            if (plan == null)
                return KingdomQuestUnderHallTimedInterruptEvaluation.Invalid;

            switch (plan.Kind)
            {
                case KingdomQuestPineInterruptKind.SecondInterval:
                    if (!plan.InitialIntervalDeadlineTick.HasValue ||
                        !plan.IntervalDurationTicks.HasValue)
                        return KingdomQuestUnderHallTimedInterruptEvaluation.Invalid;

                    return KingdomQuestPineInterruptPlan.IsIntervalDue(
                            plan.InitialIntervalDeadlineTick.Value,
                            currentTick)
                        ? KingdomQuestUnderHallTimedInterruptEvaluation.Due
                        : KingdomQuestUnderHallTimedInterruptEvaluation.NotDue;

                case KingdomQuestPineInterruptKind.TimeOut:
                    if (timeLimit == null || !timeLimit.NativeActive)
                        return KingdomQuestUnderHallTimedInterruptEvaluation.Invalid;

                    return KingdomQuestPineInterruptPlan.IsTimeLimitExpired(
                            timeLimit.DeadlineTick,
                            currentTick)
                        ? KingdomQuestUnderHallTimedInterruptEvaluation.Due
                        : KingdomQuestUnderHallTimedInterruptEvaluation.NotDue;

                default:
                    return KingdomQuestUnderHallTimedInterruptEvaluation.Unsupported;
            }
        }
    }
}
