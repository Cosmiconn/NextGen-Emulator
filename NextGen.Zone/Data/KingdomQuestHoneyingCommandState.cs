namespace NextGen.Zone.Data
{
    public interface IKingdomQuestHoneyingExternalCommandSink :
        IKingdomQuestPineSharedExternalOwner
    {
        bool TryBroadcast(
            KingdomQuestHoneyingBroadcastNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryChatWindow(
            KingdomQuestHoneyingChatWindowNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryQuestMobKill(
            KingdomQuestHoneyingQuestMobKillNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryReward(
            KingdomQuestHoneyingRewardNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryNpcShout(
            KingdomQuestHoneyingNpcShoutNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryDoorBuild(
            KingdomQuestHoneyingDoorBuildNativePlan plan,
            KingdomQuestPineVariableStack variables,
            ref int nativeState,
            out bool completed);

        bool TryDoorAction(
            KingdomQuestHoneyingDoorNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryUnresolved(
            KingdomQuestHoneyingExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Upgrades the eleven Honeying families whose native Pine command
    /// semantics are recovered to their strongest immutable plans. The two
    /// remaining Honeying-specific families stay on an explicit unresolved
    /// owner method.
    /// </summary>
    public sealed class KingdomQuestHoneyingCommandState :
        IKingdomQuestHoneyingCommandSink
    {
        private readonly IKingdomQuestHoneyingExternalCommandSink externalSink;

        public KingdomQuestHoneyingCommandState(
            IKingdomQuestHoneyingExternalCommandSink externalSink = null)
        {
            this.externalSink = externalSink;
        }

        public bool TryStep(
            KingdomQuestHoneyingExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (plan == null ||
                variables == null ||
                externalSink == null)
                return false;

            switch (plan.Kind)
            {
                case KingdomQuestHoneyingExternalKind.DoorBuild:
                    KingdomQuestHoneyingDoorBuildNativePlan buildPlan;
                    if (!KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(plan, out buildPlan)) return false;
                    return externalSink.TryDoorBuild(buildPlan, variables, ref nativeState, out completed);

                case KingdomQuestHoneyingExternalKind.DoorOpen:
                case KingdomQuestHoneyingExternalKind.DoorClose:
                    KingdomQuestHoneyingDoorNativePlan doorPlan;
                    if (!KingdomQuestHoneyingDoorNativePlan.TryBuild(plan, variables, out doorPlan))
                        return false;
                    return externalSink.TryDoorAction(doorPlan, ref nativeState, out completed);

                case KingdomQuestHoneyingExternalKind.NpcShout:
                    KingdomQuestHoneyingNpcShoutNativePlan shoutPlan;
                    if (!KingdomQuestHoneyingNpcShoutNativePlan.TryBuild(
                            plan, variables, out shoutPlan))
                        return false;
                    return externalSink.TryNpcShout(
                        shoutPlan, ref nativeState, out completed);

                case KingdomQuestHoneyingExternalKind.Broadcast:
                    KingdomQuestHoneyingBroadcastNativePlan broadcastPlan;
                    if (!KingdomQuestHoneyingCommonNative.TryBuildBroadcast(
                            plan, out broadcastPlan) ||
                        broadcastPlan == null)
                        return false;
                    return externalSink.TryBroadcast(
                        broadcastPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestHoneyingExternalKind.ChatWin:
                    KingdomQuestHoneyingChatWindowNativePlan chatWindowPlan;
                    if (!KingdomQuestHoneyingCommonNative.TryBuildChatWindow(
                            plan,
                            DataProvider.Instance,
                            out chatWindowPlan) ||
                        chatWindowPlan == null)
                        return false;
                    return externalSink.TryChatWindow(
                        chatWindowPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestHoneyingExternalKind.LinkTo:
                    KingdomQuestPineLinkToOwnerPlan linkPlan;
                    if (!KingdomQuestHoneyingCommonNative.TryBuildLinkTo(
                            plan, out linkPlan) ||
                        linkPlan == null)
                        return false;
                    return externalSink.TryLinkTo(
                        linkPlan,
                        variables,
                        ref nativeState,
                        out completed);

                case KingdomQuestHoneyingExternalKind.MobRegen:
                    KingdomQuestPineTokenValue regenHandle;
                    KingdomQuestPineMobRegenOwnerPlan regenPlan;
                    if (!variables.TryFind(
                            KingdomQuestHoneyingCommonNative
                                .RuntimeHandleIdentifier,
                            out regenHandle) ||
                        regenHandle == null ||
                        !KingdomQuestHoneyingCommonNative.TryBuildMobRegen(
                            plan,
                            regenHandle,
                            DataProvider.Instance,
                            out regenPlan) ||
                        regenPlan == null)
                        return false;
                    return externalSink.TryMobRegen(
                        regenPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestHoneyingExternalKind.QuestMobKill:
                    KingdomQuestHoneyingQuestMobKillNativePlan questMobKillPlan;
                    if (!KingdomQuestHoneyingCommonNative.TryBuildQuestMobKill(
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

                case KingdomQuestHoneyingExternalKind.Reward:
                    KingdomQuestHoneyingRewardNativePlan rewardPlan;
                    if (!currentKingdomQuestHandle.HasValue ||
                        !KingdomQuestHoneyingCommonNative.TryBuildReward(
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

                case KingdomQuestHoneyingExternalKind.SummonMob:
                    KingdomQuestPineTokenValue summonHandle;
                    KingdomQuestPineSummonMobOwnerPlan summonPlan;
                    if (!variables.TryFind(
                            KingdomQuestHoneyingCommonNative
                                .RuntimeHandleIdentifier,
                            out summonHandle) ||
                        summonHandle == null ||
                        !KingdomQuestHoneyingCommonNative.TryBuildSummonMob(
                            plan,
                            summonHandle,
                            DataProvider.Instance,
                            out summonPlan) ||
                        summonPlan == null)
                        return false;
                    return externalSink.TrySummonMob(
                        summonPlan,
                        ref nativeState,
                        out completed);

                default:
                    return externalSink.TryUnresolved(
                        plan,
                        variables,
                        currentKingdomQuestHandle,
                        ref nativeState,
                        out completed);
            }
        }
    }
}
