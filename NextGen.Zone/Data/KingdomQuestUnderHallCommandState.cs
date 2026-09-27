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
        public KingdomQuestPineInterruptSetPlan SelectedPlan
        {
            get;
            private set;
        }
        public string Argument { get; private set; }

        public KingdomQuestUnderHallInterruptDelivery(
            KingdomQuestPineInterruptSetPlan selectedPlan,
            string argument)
        {
            SelectedPlan = selectedPlan;
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
            KingdomQuestPineInterruptRegistryState activeInterrupts,
            out KingdomQuestUnderHallInterruptDelivery delivery);
    }

    /// <summary>
    /// Optional owner for the seven non-waitinterrupt/non-waitlogin UnderHall
    /// command families. It receives only source-resolved plans; raw Pine argument
    /// interpretation remains inside the checked source boundary.
    /// </summary>
    public interface IKingdomQuestUnderHallExternalCommandSink
    {
        bool TryBroadcast(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryLinkTo(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryMobRegen(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineTokenValue runtimeHandleToken,
            ref int nativeState,
            out bool completed);

        bool TryQuestMobKill(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryReward(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryScriptFile(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TrySummonMob(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineTokenValue runtimeHandleToken,
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
        private readonly KingdomQuestPineInterruptRegistryState
            activeInterrupts;
        private readonly IKingdomQuestUnderHallExternalCommandSink
            externalSink;

        public KingdomQuestUnderHallCommandState(
            KingdomQuestPineScriptDocument document,
            IKingdomQuestUnderHallInterruptDeliverySource
                interruptDeliverySource,
            IKingdomQuestUnderHallExternalCommandSink externalSink = null)
            : this(
                document,
                interruptDeliverySource,
                null,
                externalSink)
        {
        }

        public KingdomQuestUnderHallCommandState(
            KingdomQuestPineScriptDocument document,
            IKingdomQuestUnderHallInterruptDeliverySource
                interruptDeliverySource,
            KingdomQuestPineInterruptRegistryState activeInterrupts,
            IKingdomQuestUnderHallExternalCommandSink externalSink = null)
        {
            this.document = document;
            this.interruptDeliverySource = interruptDeliverySource;
            this.activeInterrupts = activeInterrupts;
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

            KingdomQuestUnderHallExternalPlan externalPlan;
            KingdomQuestUnderHallExternalSourceSite sourceSite;
            if (!KingdomQuestUnderHallExternalPlanBuilder.TryBuild(
                    plan, out externalPlan) ||
                externalPlan == null ||
                !KingdomQuestUnderHallSourceFlow.TryResolve(
                    canonicalLine,
                    externalPlan.Kind,
                    out sourceSite) ||
                sourceSite == null)
                return false;

            return TryStepExternal(
                externalPlan,
                sourceSite,
                variables,
                ref nativeState,
                out completed);
        }

        private bool TryStepExternal(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (plan == null ||
                sourceSite == null ||
                externalSink == null)
                return false;

            switch (plan.Kind)
            {
                case KingdomQuestUnderHallExternalPlanKind.Broadcast:
                    return externalSink.TryBroadcast(
                        plan, sourceSite, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.LinkTo:
                    return externalSink.TryLinkTo(
                        plan, sourceSite, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.MobRegen:
                    KingdomQuestPineTokenValue regenHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out regenHandleToken))
                        return false;
                    return externalSink.TryMobRegen(
                        plan, sourceSite, regenHandleToken,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.QuestMobKill:
                    return externalSink.TryQuestMobKill(
                        plan, sourceSite, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.Reward:
                    return externalSink.TryReward(
                        plan, sourceSite, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.ScriptFile:
                    return externalSink.TryScriptFile(
                        plan, sourceSite, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.SummonMob:
                    KingdomQuestPineTokenValue summonHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out summonHandleToken))
                        return false;
                    return externalSink.TrySummonMob(
                        plan, sourceSite, summonHandleToken,
                        ref nativeState, out completed);

                default:
                    return false;
            }
        }

        private static bool TryResolveRuntimeHandleToken(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestPineTokenValue token)
        {
            token = null;
            return plan != null &&
                variables != null &&
                !string.IsNullOrEmpty(plan.RuntimeHandleIdentifier) &&
                variables.TryFind(plan.RuntimeHandleIdentifier, out token) &&
                token != null;
        }

        private bool TryStepWaitInterrupt(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            out bool completed)
        {
            completed = false;
            if (document == null ||
                interruptDeliverySource == null ||
                activeInterrupts == null ||
                plan.Arguments == null ||
                plan.Arguments.Count != 2)
                return false;

            KingdomQuestUnderHallInterruptDelivery delivery;
            if (!interruptDeliverySource.TryTake(
                    activeInterrupts, out delivery))
            {
                // A wait with no selected native interrupt remains active.
                return true;
            }

            if (delivery == null ||
                delivery.SelectedPlan == null ||
                !activeInterrupts.ContainsReference(delivery.SelectedPlan) ||
                string.IsNullOrEmpty(delivery.SelectedPlan.ActionBlock) ||
                delivery.Argument == null ||
                !document.Blocks.ContainsKey(
                    delivery.SelectedPlan.ActionBlock))
                return false;

            KingdomQuestPineTokenValue blockValue;
            KingdomQuestPineTokenValue argumentValue;
            if (!variables.TryFind(plan.Arguments[0], out blockValue) ||
                !variables.TryFind(plan.Arguments[1], out argumentValue) ||
                blockValue == null ||
                argumentValue == null)
                return false;

            if (!blockValue.TrySetAscii(
                    delivery.SelectedPlan.ActionBlock) ||
                !argumentValue.TrySetAscii(delivery.Argument))
                return false;

            completed = true;
            return true;
        }
    }
}
