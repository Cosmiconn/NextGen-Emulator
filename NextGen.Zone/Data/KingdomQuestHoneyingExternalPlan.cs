namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestHoneyingExternalPlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestHoneyingExternalKind Kind { get; private set; }
        public string CommandText { get; private set; }

        internal KingdomQuestHoneyingExternalPlan(
            KingdomQuestHoneyingExternalSourceSite source)
        {
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock;
            Kind = source.Kind;
            CommandText = source.CommandText;
        }
    }

    public static class KingdomQuestHoneyingExternalPlanBuilder
    {
        public static bool TryBuild(
            string scriptLanguage,
            int canonicalLine,
            string commandText,
            out KingdomQuestHoneyingExternalPlan plan)
        {
            plan = null;
            KingdomQuestHoneyingExternalSourceSite source;
            if (!KingdomQuestHoneyingSourceFlow.TryResolve(
                    scriptLanguage,
                    canonicalLine,
                    commandText,
                    out source) ||
                source == null)
                return false;

            plan = new KingdomQuestHoneyingExternalPlan(source);
            return true;
        }
    }
}
