namespace NextGen.Zone.Data
{
    /// <summary>
    /// Immutable exact-source plan for one Warrior's Code external command.
    /// It intentionally carries source identity only; native side-effect
    /// semantics for these 11 families remain unrecovered and external.
    /// </summary>
    public sealed class KingdomQuestKQHBatExternalPlan
    {
        public string ScriptLanguage { get; private set; }
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestKQHBatExternalKind Kind { get; private set; }
        public string CommandText { get; private set; }

        internal KingdomQuestKQHBatExternalPlan(
            KingdomQuestKQHBatExternalSourceSite source)
        {
            ScriptLanguage = source.ScriptLanguage;
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock;
            Kind = source.Kind;
            CommandText = source.CommandText;
        }
    }

    public static class KingdomQuestKQHBatExternalPlanBuilder
    {
        public static bool TryBuild(
            string scriptLanguage,
            int canonicalLine,
            string commandText,
            out KingdomQuestKQHBatExternalPlan plan)
        {
            plan = null;
            KingdomQuestKQHBatExternalSourceSite source;
            if (!KingdomQuestKQHBatSourceFlow.TryResolve(
                    scriptLanguage,
                    canonicalLine,
                    commandText,
                    out source) ||
                source == null)
                return false;

            plan = new KingdomQuestKQHBatExternalPlan(source);
            return true;
        }
    }
}
