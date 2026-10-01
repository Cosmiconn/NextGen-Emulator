namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit native-owner boundary for Warrior's Code external commands.
    /// The owner receives only a source-locked plan plus existing Pine runtime
    /// state. No command-specific gameplay behavior is supplied here.
    /// </summary>
    public interface IKingdomQuestKQHBatExternalCommandSink
    {
        bool TryAbStateSet(
            KingdomQuestKQHBatAbStateSetNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryBattlePk(
            KingdomQuestKQHBatBattlePkNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TrySendQuestResult(
            KingdomQuestKQHBatSendQuestResultNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryIndividualReward(
            KingdomQuestKQHBatIndividualRewardNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryRevivalAll(
            KingdomQuestKQHBatRevivalAllNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryLinkToAll(
            KingdomQuestKQHBatLinkToAllNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryItemEraseAll(
            KingdomQuestKQHBatItemEraseAllNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryItemDrop(
            KingdomQuestKQHBatItemDropNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryBroadcastAll(
            KingdomQuestKQHBatBroadcastAllNativePlan plan,
            ref int nativeState,
            out bool completed);

        bool TryChatWindow(
            KingdomQuestKQHBatChatWindowNativePlan plan,
            ref int nativeState,
            out bool completed);
    }

    public sealed class KingdomQuestKQHBatCommandState :
        IKingdomQuestKQHBatCommandSink
    {
        private readonly IKingdomQuestKQHBatExternalCommandSink externalSink;

        public KingdomQuestKQHBatCommandState(
            IKingdomQuestKQHBatExternalCommandSink externalSink = null)
        {
            this.externalSink = externalSink;
        }

        public bool TryStep(
            KingdomQuestKQHBatExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (plan == null ||
                variables == null ||
                externalSink == null ||
                !KingdomQuestKQHBatSourceFlow.IsSupportedScript(
                    plan.ScriptLanguage))
                return false;

            switch (plan.Kind)
            {
                case KingdomQuestKQHBatExternalKind.AbStateSet:
                    KingdomQuestKQHBatAbStateSetNativePlan abStatePlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder.TryBuildAbStateSet(
                            plan, variables, out abStatePlan) ||
                        abStatePlan == null)
                        return false;
                    return externalSink.TryAbStateSet(
                        abStatePlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.BattleStart:
                case KingdomQuestKQHBatExternalKind.BattleStop:
                    KingdomQuestKQHBatBattlePkNativePlan battlePlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder.TryBuildBattlePk(
                            plan, out battlePlan) ||
                        battlePlan == null)
                        return false;
                    return externalSink.TryBattlePk(
                        battlePlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.SendQuestResult:
                    KingdomQuestKQHBatSendQuestResultNativePlan resultPlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder
                            .TryBuildSendQuestResult(
                                plan, variables, out resultPlan) ||
                        resultPlan == null)
                        return false;
                    return externalSink.TrySendQuestResult(
                        resultPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.IndividualReward:
                    KingdomQuestKQHBatIndividualRewardNativePlan rewardPlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder
                            .TryBuildIndividualReward(
                                plan, variables, out rewardPlan) ||
                        rewardPlan == null)
                        return false;
                    return externalSink.TryIndividualReward(
                        rewardPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.Revival:
                    KingdomQuestKQHBatRevivalAllNativePlan revivalPlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder.TryBuildRevivalAll(
                            plan, out revivalPlan) ||
                        revivalPlan == null)
                        return false;
                    return externalSink.TryRevivalAll(
                        revivalPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.LinkTo:
                    KingdomQuestKQHBatLinkToAllNativePlan linkPlan;
                    if (!KingdomQuestKQHBatNativePlanBuilder.TryBuildLinkToAll(
                            plan, out linkPlan) ||
                        linkPlan == null)
                        return false;
                    return externalSink.TryLinkToAll(
                        linkPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.ItemErase:
                    KingdomQuestKQHBatItemEraseAllNativePlan erasePlan;
                    if (!KingdomQuestKQHBatItemNativePlanBuilder
                            .TryBuildItemEraseAll(
                                plan, variables, out erasePlan) ||
                        erasePlan == null)
                        return false;
                    return externalSink.TryItemEraseAll(
                        erasePlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.ItemDrop:
                    KingdomQuestKQHBatItemDropNativePlan dropPlan;
                    if (!KingdomQuestKQHBatItemNativePlanBuilder
                            .TryBuildItemDrop(
                                plan, variables, out dropPlan) ||
                        dropPlan == null)
                        return false;
                    return externalSink.TryItemDrop(
                        dropPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.Broadcast:
                    KingdomQuestKQHBatBroadcastAllNativePlan broadcastPlan;
                    if (!KingdomQuestKQHBatTextNativePlanBuilder
                            .TryBuildBroadcastAll(
                                plan, variables, out broadcastPlan) ||
                        broadcastPlan == null)
                        return false;
                    return externalSink.TryBroadcastAll(
                        broadcastPlan,
                        ref nativeState,
                        out completed);

                case KingdomQuestKQHBatExternalKind.ChatWin:
                    KingdomQuestKQHBatChatWindowNativePlan chatPlan;
                    if (!KingdomQuestKQHBatTextNativePlanBuilder
                            .TryBuildChatWindow(
                                plan, variables, out chatPlan) ||
                        chatPlan == null)
                        return false;
                    return externalSink.TryChatWindow(
                        chatPlan,
                        ref nativeState,
                        out completed);

                default:
                    return false;
            }
        }
    }
}
