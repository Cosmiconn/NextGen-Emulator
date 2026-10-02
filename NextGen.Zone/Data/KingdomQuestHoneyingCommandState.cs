namespace NextGen.Zone.Data
{
    public interface IKingdomQuestHoneyingExternalCommandSink
    {
        bool TryStep(
            KingdomQuestHoneyingExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

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

            return externalSink.TryStep(
                plan,
                variables,
                currentKingdomQuestHandle,
                ref nativeState,
                out completed);
        }
    }
}
