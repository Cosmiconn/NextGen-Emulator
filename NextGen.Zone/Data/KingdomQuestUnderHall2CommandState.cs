namespace NextGen.Zone.Data
{
    /// <summary>
    /// External owner boundary for the six typed UnderHall2 command families.
    /// Every method receives a source/native-resolved immutable plan.
    /// </summary>
    public interface IKingdomQuestUnderHall2ExternalCommandSink
    {
        bool TryBroadcast(
            KingdomQuestUnderHall2BroadcastNativePlan plan,
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
            KingdomQuestUnderHall2QuestMobKillNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryReward(
            KingdomQuestUnderHall2RewardNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TrySummonMob(
            KingdomQuestPineSummonMobOwnerPlan plan,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Exact source/native plan router for KQ/UnderHall2.
    ///
    /// This class turns canonical source text into the strongest recovered plan
    /// for each family and then stops at an explicit external owner. It does
    /// not transfer players, create mobs, mutate quests, persist rewards or send
    /// packets.
    /// </summary>
    public sealed class KingdomQuestUnderHall2CommandState :
        IKingdomQuestUnderHall2CommandSink
    {
        private readonly IKingdomQuestUnderHall2ExternalCommandSink
            externalSink;

        public KingdomQuestUnderHall2CommandState(
            IKingdomQuestUnderHall2ExternalCommandSink externalSink = null)
        {
            this.externalSink = externalSink;
        }

        public bool TryStep(
            string commandText,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (variables == null || externalSink == null)
                return false;

            KingdomQuestUnderHall2ExternalPlan plan;
            if (!KingdomQuestUnderHall2ExternalPlanBuilder.TryBuild(
                    canonicalLine, commandText, out plan) ||
                plan == null)
                return false;

            switch (plan.Kind)
            {
                case KingdomQuestUnderHall2ExternalKind.Broadcast:
                    KingdomQuestUnderHall2BroadcastNativePlan broadcastPlan;
                    if (!KingdomQuestUnderHall2BroadcastNative.TryBuild(
                            plan, out broadcastPlan) ||
                        broadcastPlan == null)
                        return false;
                    return externalSink.TryBroadcast(
                        broadcastPlan,
                        variables,
                        ref nativeState,
                        out completed);

                case KingdomQuestUnderHall2ExternalKind.LinkTo:
                    KingdomQuestPineLinkToOwnerPlan linkToPlan;
                    if (!KingdomQuestUnderHall2OwnerPlanBuilder.TryBuildLinkTo(
                            plan, out linkToPlan) ||
                        linkToPlan == null)
                        return false;
                    return externalSink.TryLinkTo(
                        linkToPlan,
                        variables,
                        ref nativeState,
                        out completed);

                case KingdomQuestUnderHall2ExternalKind.MobRegen:
                    KingdomQuestPineTokenValue regenHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out regenHandleToken))
                        return false;
                    KingdomQuestPineMobRegenOwnerPlan mobRegenPlan;
                    if (!KingdomQuestUnderHall2OwnerPlanBuilder.TryBuildMobRegen(
                            plan,
                            regenHandleToken,
                            DataProvider.Instance,
                            out mobRegenPlan) ||
                        mobRegenPlan == null)
                        return false;
                    return externalSink.TryMobRegen(
                        mobRegenPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestUnderHall2ExternalKind.QuestMobKill:
                    KingdomQuestUnderHall2QuestMobKillNativePlan
                        questMobKillPlan;
                    if (!KingdomQuestUnderHall2CommonNative.TryBuildQuestMobKill(
                            plan,
                            DataProvider.Instance,
                            out questMobKillPlan) ||
                        questMobKillPlan == null)
                        return false;
                    return externalSink.TryQuestMobKill(
                        questMobKillPlan,
                        variables,
                        ref nativeState,
                        out completed);

                case KingdomQuestUnderHall2ExternalKind.Reward:
                    KingdomQuestUnderHall2RewardNativePlan rewardPlan;
                    if (!currentKingdomQuestHandle.HasValue ||
                        !KingdomQuestUnderHall2CommonNative.TryBuildReward(
                            plan,
                            currentKingdomQuestHandle.Value,
                            out rewardPlan) ||
                        rewardPlan == null)
                        return false;
                    return externalSink.TryReward(
                        rewardPlan,
                        variables,
                        ref nativeState,
                        out completed);

                case KingdomQuestUnderHall2ExternalKind.SummonMob:
                    KingdomQuestPineTokenValue summonHandleToken;
                    if (!TryResolveRuntimeHandleToken(
                            plan, variables, out summonHandleToken))
                        return false;
                    KingdomQuestPineSummonMobOwnerPlan summonMobPlan;
                    if (!KingdomQuestUnderHall2OwnerPlanBuilder.TryBuildSummonMob(
                            plan,
                            summonHandleToken,
                            DataProvider.Instance,
                            out summonMobPlan) ||
                        summonMobPlan == null)
                        return false;
                    return externalSink.TrySummonMob(
                        summonMobPlan,
                        ref nativeState,
                        out completed);

                default:
                    return false;
            }
        }

        private static bool TryResolveRuntimeHandleToken(
            KingdomQuestUnderHall2ExternalPlan plan,
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
