using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Optional owner for the six gameplay-side-effect UnderHall command
    /// families. It receives only source-resolved plans; raw Pine argument
    /// interpretation remains inside the checked source boundary.
    /// </summary>
    public interface IKingdomQuestUnderHallExternalCommandSink
    {
        bool TryBroadcast(
            KingdomQuestUnderHallBroadcastNativePlan plan,
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
            KingdomQuestUnderHallQuestMobKillNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryReward(
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
    /// Source-routed state for the six remaining UnderHall external commands.
    ///
    /// waitlogin and waitinterrupt are intentionally absent: the generic Pine
    /// control runtime owns all 9/9 waitlogin and 55/55 waitinterrupt sites,
    /// including native BlastCheck selection/removal semantics. scriptfile is
    /// likewise owned by the generic one-step runtime.
    ///
    /// This class therefore performs no interrupt delivery, player polling or
    /// script-file state mutation. It only resolves exact UnderHall source
    /// sites and forwards only the strongest recovered plan available to the
    /// native side-effect owner. Broadcast and questmobkill therefore never
    /// expose their raw Pine operands beyond this boundary.
    /// </summary>
    public sealed class KingdomQuestUnderHallCommandState :
        IKingdomQuestUnderHallCommandSink
    {
        private readonly IKingdomQuestUnderHallExternalCommandSink
            externalSink;

        public KingdomQuestUnderHallCommandState(
            IKingdomQuestUnderHallExternalCommandSink externalSink = null)
        {
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
                    KingdomQuestUnderHallBroadcastNativePlan broadcastPlan;
                    if (!KingdomQuestUnderHallBroadcastNative.TryBuild(
                            plan, sourceSite, out broadcastPlan) ||
                        broadcastPlan == null)
                        return false;
                    return externalSink.TryBroadcast(
                        broadcastPlan, variables,
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
                    KingdomQuestUnderHallQuestMobKillNativePlan
                        questMobKillPlan;
                    if (!KingdomQuestUnderHallQuestMobKillNative.TryBuild(
                            plan,
                            sourceSite,
                            DataProvider.Instance,
                            out questMobKillPlan) ||
                        questMobKillPlan == null)
                        return false;
                    return externalSink.TryQuestMobKill(
                        questMobKillPlan, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.Reward:
                    return externalSink.TryReward(
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
    }
}
