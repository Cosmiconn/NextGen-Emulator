namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit native-owner boundary for Warrior's Code external commands.
    /// The owner receives only a source-locked plan plus existing Pine runtime
    /// state. No command-specific gameplay behavior is supplied here.
    /// </summary>
    public interface IKingdomQuestKQHBatExternalCommandSink
    {
        bool TryStep(
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

            return externalSink.TryStep(
                plan,
                variables,
                currentKingdomQuestHandle,
                ref nativeState,
                out completed);
        }
    }
}
