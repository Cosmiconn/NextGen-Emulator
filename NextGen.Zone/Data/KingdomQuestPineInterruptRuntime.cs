using System;
using System.Globalization;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Mutable execution state for one native ScriptInterruptArgument entry.
    /// Source syntax remains in SourcePlan; only fields proven mutable by
    /// sib_BlastCheck live here.
    /// </summary>
    public sealed class KingdomQuestPineInterruptRuntimeEntry
    {
        public KingdomQuestPineInterruptSetPlan SourcePlan { get; private set; }
        public int RemainingRepeatCount { get; internal set; }
        public uint? NextIntervalDeadlineTick { get; internal set; }
        public ushort? HPLowObjectHandle { get; private set; }
        public int? HPLowThresholdPermille { get; private set; }
        public uint CachedHPLowMaxHp { get; internal set; }

        internal KingdomQuestPineInterruptRuntimeEntry(
            KingdomQuestPineInterruptSetPlan sourcePlan)
        {
            SourcePlan = sourcePlan;
            RemainingRepeatCount =
                sourcePlan == null ? 0 : sourcePlan.RepeatCount;
            NextIntervalDeadlineTick =
                sourcePlan == null
                    ? null
                    : sourcePlan.InitialIntervalDeadlineTick;
        }

        internal bool TryBindRegistration(
            KingdomQuestPineVariableStack variables)
        {
            if (SourcePlan == null)
                return false;

            if (SourcePlan.Kind != KingdomQuestPineInterruptKind.HPLow)
                return true;

            if (variables == null ||
                SourcePlan.Arguments == null ||
                SourcePlan.Arguments.Count != 2)
                return false;

            int handleNumber;
            int threshold;
            if (!TryEvaluateNumber(
                    SourcePlan.Arguments[0],
                    variables,
                    out handleNumber) ||
                !TryEvaluateNumber(
                    SourcePlan.Arguments[1],
                    variables,
                    out threshold))
                return false;

            // sim_InterruptSet_HPLow stores pst_GetNumber(handle) as WORD at
            // +0x11c and the threshold as DWORD at +0x120.
            HPLowObjectHandle = unchecked((ushort)handleNumber);
            HPLowThresholdPermille = threshold;
            CachedHPLowMaxHp = 0;
            return true;
        }

        private static bool TryEvaluateNumber(
            string expression,
            KingdomQuestPineVariableStack variables,
            out int value)
        {
            value = 0;
            var token = new KingdomQuestPineTokenValue();
            if (KingdomQuestPineBasicExpression.TryCalculate(
                    expression,
                    variables,
                    token) != KingdomQuestPineExpressionResolution.Success)
                return false;

            return int.TryParse(
                token.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }
    }

    /// <summary>
    /// Native object/map observations required by the two non-timing
    /// UnderHall interrupt kinds. Runtime-handle identity is intentionally
    /// supplied by the live map owner rather than inferred from MobInfo IDs.
    /// </summary>
    public interface IKingdomQuestUnderHallNativeInterruptView
    {
        bool TryGetObjectHealth(
            ushort runtimeHandle,
            out bool exists,
            out uint currentHp,
            out uint maximumHp);

        bool TryGetQualifyingPlayerCount(out int playerCount);
    }

    /// <summary>
    /// Source-equivalent ScriptInterruptManager selector for the four
    /// interrupt kinds used by KQ/UnderHall.
    ///
    /// Zone.exe/PDB:
    /// - sim_Alloc uses List::l_AllocZ, so registration appends at the tail;
    /// - sim_InterruptBlast walks from the A/head side and returns on the first
    ///   sib_BlastCheck that fires;
    /// - every firing BlastCheck decrements RepeatCount and removes itself
    ///   immediately when the count reaches zero;
    /// - Interval advances its deadline from the previous deadline before the
    ///   decrement/removal;
    /// - HPLow fires for a missing object, otherwise when
    ///   (currentHP * 1000 / cachedMaxHP) <= threshold;
    /// - PlayerEliminate fires when the native qualifying-player count is zero.
    ///
    /// The supplied UnderHall source initializes InterruptArg to empty and none
    /// of these four BlastCheck routines writes the second output token, so the
    /// delivery preserves that exact empty value.
    /// </summary>
    public sealed class KingdomQuestUnderHallNativeInterruptDeliverySource :
        IKingdomQuestPineWaitInterruptSource
    {
        public const uint NativeHpScale = 1000u;

        private readonly IKingdomQuestPineNativeTickSource tickSource;
        private readonly KingdomQuestPineLocalCommandState localState;
        private readonly IKingdomQuestUnderHallNativeInterruptView nativeView;

        public KingdomQuestUnderHallNativeInterruptDeliverySource(
            IKingdomQuestPineNativeTickSource tickSource,
            KingdomQuestPineLocalCommandState localState,
            IKingdomQuestUnderHallNativeInterruptView nativeView)
        {
            this.tickSource = tickSource;
            this.localState = localState;
            this.nativeView = nativeView;
        }

        public bool TryTake(
            KingdomQuestPineInterruptRegistryState activeInterrupts,
            out KingdomQuestPineWaitInterruptDelivery delivery)
        {
            delivery = null;
            if (activeInterrupts == null ||
                tickSource == null ||
                localState == null ||
                nativeView == null)
                return false;

            uint currentTick;
            if (!tickSource.TryGetCurrentTick(out currentTick))
                return false;

            for (int i = 0; i < activeInterrupts.RuntimeEntries.Count; i++)
            {
                KingdomQuestPineInterruptRuntimeEntry entry =
                    activeInterrupts.RuntimeEntries[i];
                bool due;
                if (!TryIsDue(entry, currentTick, out due))
                    return false;
                if (!due)
                    continue;

                if (entry.SourcePlan.Kind ==
                        KingdomQuestPineInterruptKind.SecondInterval)
                {
                    if (!entry.NextIntervalDeadlineTick.HasValue ||
                        !entry.SourcePlan.IntervalDurationTicks.HasValue)
                        return false;

                    entry.NextIntervalDeadlineTick =
                        KingdomQuestPineInterruptPlan.AdvanceIntervalDeadline(
                            entry.NextIntervalDeadlineTick.Value,
                            entry.SourcePlan.IntervalDurationTicks.Value);
                }

                KingdomQuestPineInterruptSetPlan selected = entry.SourcePlan;
                entry.RemainingRepeatCount--;
                if (entry.RemainingRepeatCount <= 0)
                    activeInterrupts.RemoveRuntimeEntry(entry);

                delivery = new KingdomQuestPineWaitInterruptDelivery(
                    selected,
                    string.Empty);
                return true;
            }

            return false;
        }

        private bool TryIsDue(
            KingdomQuestPineInterruptRuntimeEntry entry,
            uint currentTick,
            out bool due)
        {
            due = false;
            if (entry == null ||
                entry.SourcePlan == null ||
                entry.RemainingRepeatCount <= 0)
                return false;

            switch (entry.SourcePlan.Kind)
            {
                case KingdomQuestPineInterruptKind.SecondInterval:
                    if (!entry.NextIntervalDeadlineTick.HasValue)
                        return false;
                    due = KingdomQuestPineInterruptPlan.IsIntervalDue(
                        entry.NextIntervalDeadlineTick.Value,
                        currentTick);
                    return true;

                case KingdomQuestPineInterruptKind.TimeOut:
                    KingdomQuestPineTimeLimitPlan timeLimit =
                        localState.TimeLimit;
                    if (timeLimit == null || !timeLimit.NativeActive)
                        return false;
                    due = KingdomQuestPineInterruptPlan.IsTimeLimitExpired(
                        timeLimit.DeadlineTick,
                        currentTick);
                    return true;

                case KingdomQuestPineInterruptKind.HPLow:
                    return TryIsHpLowDue(entry, out due);

                case KingdomQuestPineInterruptKind.PlayerEliminate:
                    int playerCount;
                    if (!nativeView.TryGetQualifyingPlayerCount(
                            out playerCount) ||
                        playerCount < 0)
                        return false;
                    due = playerCount == 0;
                    return true;

                default:
                    // KQ/UnderHall registers no other interrupt kind.
                    return false;
            }
        }

        private bool TryIsHpLowDue(
            KingdomQuestPineInterruptRuntimeEntry entry,
            out bool due)
        {
            due = false;
            if (!entry.HPLowObjectHandle.HasValue ||
                !entry.HPLowThresholdPermille.HasValue)
                return false;

            bool exists;
            uint currentHp;
            uint maximumHp;
            if (!nativeView.TryGetObjectHealth(
                    entry.HPLowObjectHandle.Value,
                    out exists,
                    out currentHp,
                    out maximumHp))
                return false;

            // Native som_GetObject miss takes the firing branch immediately.
            if (!exists)
            {
                due = true;
                return true;
            }

            if (entry.CachedHPLowMaxHp == 0)
                entry.CachedHPLowMaxHp = maximumHp;

            uint ratio = 0;
            if (entry.CachedHPLowMaxHp != 0)
            {
                ratio = unchecked((uint)(
                    ((ulong)currentHp * NativeHpScale) /
                    entry.CachedHPLowMaxHp));
            }

            due =
                (long)ratio <=
                (long)entry.HPLowThresholdPermille.Value;
            return true;
        }
    }
}
