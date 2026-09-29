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
            KingdomQuestPineLinkToOwnerPlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryMobRegen(
            KingdomQuestPineMobRegenOwnerPlan plan,
            ref int nativeState,
            out bool completed);

        bool TryQuestMobKill(
            KingdomQuestUnderHallQuestMobKillNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryReward(
            KingdomQuestUnderHallRewardNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TrySummonMob(
            KingdomQuestPineSummonMobOwnerPlan plan,
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
    /// native side-effect owner. All six UnderHall families therefore cross
    /// this boundary only as immutable native/source-resolved owner plans;
    /// linkto/mobregen/summonmob use the same script-neutral plan types as
    /// UnderHall2. No external owner receives raw Pine operands.
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
            uint? currentKingdomQuestHandle,
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
                currentKingdomQuestHandle,
                ref nativeState,
                out completed);
        }

        private bool TryStepExternal(
            KingdomQuestUnderHallExternalPlan plan,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
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
                    KingdomQuestPineLinkToOwnerPlan linkToPlan;
                    if (!KingdomQuestUnderHallOwnerPlanBuilder.TryBuildLinkTo(
                            plan, sourceSite, out linkToPlan) ||
                        linkToPlan == null)
                        return false;
                    return externalSink.TryLinkTo(
                        linkToPlan, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.MobRegen:
                    KingdomQuestPineTokenValue regenHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out regenHandleToken))
                        return false;
                    KingdomQuestPineMobRegenOwnerPlan mobRegenPlan;
                    if (!KingdomQuestUnderHallOwnerPlanBuilder.TryBuildMobRegen(
                            plan,
                            sourceSite,
                            regenHandleToken,
                            DataProvider.Instance,
                            out mobRegenPlan) ||
                        mobRegenPlan == null)
                        return false;
                    return externalSink.TryMobRegen(
                        mobRegenPlan,
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
                    KingdomQuestUnderHallRewardNativePlan rewardPlan;
                    if (!currentKingdomQuestHandle.HasValue ||
                        !KingdomQuestUnderHallRewardNative.TryBuild(
                            plan,
                            sourceSite,
                            currentKingdomQuestHandle.Value,
                            out rewardPlan) ||
                        rewardPlan == null)
                        return false;
                    return externalSink.TryReward(
                        rewardPlan, variables,
                        ref nativeState, out completed);

                case KingdomQuestUnderHallExternalPlanKind.SummonMob:
                    KingdomQuestPineTokenValue summonHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out summonHandleToken))
                        return false;
                    KingdomQuestPineSummonMobOwnerPlan summonMobPlan;
                    if (!KingdomQuestUnderHallOwnerPlanBuilder.TryBuildSummonMob(
                            plan,
                            sourceSite,
                            summonHandleToken,
                            DataProvider.Instance,
                            out summonMobPlan) ||
                        summonMobPlan == null)
                        return false;
                    return externalSink.TrySummonMob(
                        summonMobPlan,
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
