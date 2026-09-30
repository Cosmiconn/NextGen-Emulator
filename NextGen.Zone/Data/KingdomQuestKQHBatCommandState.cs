namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit native-owner boundary for Warrior's Code external commands.
    /// The owner receives only a source-locked plan plus existing Pine runtime
    /// state. No command-specific gameplay behavior is supplied here.
    /// </summary>
    public interface IKingdomQuestKQHBatExternalCommandSink
    {
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

        bool TryUnresolved(
            KingdomQuestKQHBatExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
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
