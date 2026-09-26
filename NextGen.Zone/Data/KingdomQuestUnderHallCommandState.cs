using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Already-selected interrupt result delivered to a waiting Pine command.
    ///
    /// This object does not decide which interrupt fires. The producer owns the
    /// native BlastCheck ordering/predicate semantics and supplies the exact
    /// ActionBlock plus argument text.
    /// </summary>
    public sealed class KingdomQuestUnderHallInterruptDelivery
    {
        public string ActionBlock { get; private set; }
        public string Argument { get; private set; }

        public KingdomQuestUnderHallInterruptDelivery(
            string actionBlock,
            string argument)
        {
            ActionBlock = actionBlock;
            Argument = argument;
        }
    }

    /// <summary>
    /// Explicit boundary for native ScriptInterruptManager delivery ordering.
    /// false means no selected interrupt is currently available; it does not
    /// mean a synthetic timeout or failure.
    /// </summary>
    public interface IKingdomQuestUnderHallInterruptDeliverySource
    {
        bool TryTake(
            out KingdomQuestUnderHallInterruptDelivery delivery);
    }

    /// <summary>
    /// Optional owner for the eight non-waitinterrupt UnderHall command
    /// families. This keeps their still-unrecovered side effects separate from
    /// the source-proven waitinterrupt variable handoff.
    /// </summary>
    public interface IKingdomQuestUnderHallExternalCommandSink
    {
        bool TryStep(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Source-equivalent UnderHall command state for the one dataflow that is
    /// proven by the canonical Pine source:
    ///
    ///   InterruptBlock ""
    ///   InterruptArg   ""
    ///   waitinterrupt InterruptBlock "InterruptArg".
    ///   call InterruptBlock.
    ///
    /// All 19 occurrences have that exact adjacent pair, and every ActionBlock
    /// stored by UnderHall interruptset is a top-level block in the same script.
    ///
    /// The class therefore applies only an already-authoritatively-selected
    /// interrupt delivery to those two variables. It does not evaluate
    /// PlayerEliminate/HPLow/Sec/TimeOut, choose BlastCheck ordering, poll game
    /// objects, or synthesize an interrupt.
    /// </summary>
    public sealed class KingdomQuestUnderHallCommandState :
        IKingdomQuestUnderHallCommandSink
    {
        private readonly KingdomQuestPineScriptDocument document;
        private readonly IKingdomQuestUnderHallInterruptDeliverySource
            interruptDeliverySource;
        private readonly IKingdomQuestUnderHallExternalCommandSink
            externalSink;

        public KingdomQuestUnderHallCommandState(
            KingdomQuestPineScriptDocument document,
            IKingdomQuestUnderHallInterruptDeliverySource
                interruptDeliverySource,
            IKingdomQuestUnderHallExternalCommandSink externalSink = null)
        {
            this.document = document;
            this.interruptDeliverySource = interruptDeliverySource;
            this.externalSink = externalSink;
        }

        public bool TryStep(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (plan == null || variables == null)
                return false;

            if (plan.Kind ==
                KingdomQuestUnderHallCommandKind.WaitInterrupt)
                return TryStepWaitInterrupt(
                    plan, variables, out completed);

            return externalSink != null &&
                externalSink.TryStep(
                    plan,
                    variables,
                    canonicalLine,
                    ref nativeState,
                    out completed);
        }

        private bool TryStepWaitInterrupt(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            out bool completed)
        {
            completed = false;
            if (document == null ||
                interruptDeliverySource == null ||
                plan.Arguments == null ||
                plan.Arguments.Count != 2)
                return false;

            KingdomQuestUnderHallInterruptDelivery delivery;
            if (!interruptDeliverySource.TryTake(out delivery))
            {
                // A wait with no selected native interrupt remains active.
                return true;
            }

            if (delivery == null ||
                string.IsNullOrEmpty(delivery.ActionBlock) ||
                delivery.Argument == null ||
                !document.Blocks.ContainsKey(delivery.ActionBlock))
                return false;

            KingdomQuestPineTokenValue blockValue;
            KingdomQuestPineTokenValue argumentValue;
            if (!variables.TryFind(plan.Arguments[0], out blockValue) ||
                !variables.TryFind(plan.Arguments[1], out argumentValue) ||
                blockValue == null ||
                argumentValue == null)
                return false;

            if (!blockValue.TrySetAscii(delivery.ActionBlock) ||
                !argumentValue.TrySetAscii(delivery.Argument))
                return false;

            completed = true;
            return true;
        }
    }
}
