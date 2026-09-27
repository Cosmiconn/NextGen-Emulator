using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Already-selected native ScriptInterruptManager result for one Pine
    /// waitinterrupt command.
    ///
    /// The producer owns BlastCheck ordering and all gameplay predicates.
    /// The runtime only validates that SelectedPlan is still present in the
    /// active registry and copies its source-backed ActionBlock plus the opaque
    /// native argument into the two source variables.
    /// </summary>
    public sealed class KingdomQuestPineWaitInterruptDelivery
    {
        public KingdomQuestPineInterruptSetPlan SelectedPlan
        {
            get;
            private set;
        }

        public string Argument { get; private set; }

        public KingdomQuestPineWaitInterruptDelivery(
            KingdomQuestPineInterruptSetPlan selectedPlan,
            string argument)
        {
            SelectedPlan = selectedPlan;
            Argument = argument;
        }
    }

    /// <summary>
    /// Explicit native owner for ScriptInterruptManager candidate selection.
    /// false means no selected interrupt is currently available and the Pine
    /// wait frame remains active.
    /// </summary>
    public interface IKingdomQuestPineWaitInterruptSource
    {
        bool TryTake(
            KingdomQuestPineInterruptRegistryState activeInterrupts,
            out KingdomQuestPineWaitInterruptDelivery delivery);
    }

    /// <summary>
    /// Source-form boundary for all waitinterrupt commands in the exact nine
    /// supplied KQ Pine scripts.
    ///
    /// CI locks all 55 occurrences to the same form:
    ///   waitinterrupt InterruptBlock "InterruptArg".
    /// immediately followed by:
    ///   call InterruptBlock.
    ///
    /// This type assigns no event, HP, elimination, timer, ordering or argument
    /// semantics. Those remain native-provider responsibilities.
    /// </summary>
    public static class KingdomQuestPineWaitInterrupt
    {
        public const int UsedCommandCount = 55;
        public const string UsedBlockVariable = "InterruptBlock";
        public const string UsedArgumentVariable = "InterruptArg";

        public static bool TryParseUsed(
            string commandText,
            out string blockVariable,
            out string argumentVariable)
        {
            blockVariable = null;
            argumentVariable = null;

            if (!KingdomQuestPineInterruptPlan.TryParseWaitInterrupt(
                    commandText,
                    out blockVariable,
                    out argumentVariable))
                return false;

            return string.Equals(
                    blockVariable,
                    UsedBlockVariable,
                    StringComparison.Ordinal) &&
                string.Equals(
                    argumentVariable,
                    UsedArgumentVariable,
                    StringComparison.Ordinal);
        }
    }
}
