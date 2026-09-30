using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHall2ChatWinPlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string FirstToken { get; private set; }
        public string SecondToken { get; private set; }

        internal KingdomQuestUnderHall2ChatWinPlan(
            int canonicalLine,
            string topLevelBlock,
            string firstToken,
            string secondToken)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            FirstToken = firstToken ?? string.Empty;
            SecondToken = secondToken ?? string.Empty;
        }
    }

    /// <summary>
    /// Exact source boundary for the four KQ/UnderHall2 chatwin commands.
    ///
    /// The original source proves two quoted tokens and their exact site/order,
    /// but their native gameplay meaning is not independently recovered.
    /// They therefore remain neutral FirstToken/SecondToken fields and cross
    /// only an explicit external owner boundary.
    /// </summary>
    public static class KingdomQuestUnderHall2ChatWinSource
    {
        public const int SourceUsedOccurrenceCount = 4;
        public const string TopLevelBlock = "TwelveTwo";

        private sealed class Expected
        {
            public string CommandText;
            public string FirstToken;
            public string SecondToken;

            public Expected(
                string commandText,
                string firstToken,
                string secondToken)
            {
                CommandText = commandText;
                FirstToken = firstToken;
                SecondToken = secondToken;
            }
        }

        private static readonly Dictionary<int, Expected> Sites =
            new Dictionary<int, Expected>
            {
                { 394, new Expected(
                    "chatwin \"KQ_GB_Spider\" \"Spider01\".",
                    "KQ_GB_Spider", "Spider01") },
                { 396, new Expected(
                    "chatwin \"KQ_GB_Spider\" \"Spider02\".",
                    "KQ_GB_Spider", "Spider02") },
                { 398, new Expected(
                    "chatwin \"RouTownChiefRoumenus\" \"Roumenus01\".",
                    "RouTownChiefRoumenus", "Roumenus01") },
                { 400, new Expected(
                    "chatwin \"RouTownChiefRoumenus\" \"Roumenus02\".",
                    "RouTownChiefRoumenus", "Roumenus02") },
            };

        public static bool TryBuild(
            int canonicalLine,
            string commandText,
            out KingdomQuestUnderHall2ChatWinPlan plan)
        {
            plan = null;
            Expected expected;
            if (!Sites.TryGetValue(canonicalLine, out expected) ||
                expected == null ||
                !string.Equals(
                    commandText == null ? string.Empty : commandText.Trim(),
                    expected.CommandText,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestUnderHall2ChatWinPlan(
                canonicalLine,
                TopLevelBlock,
                expected.FirstToken,
                expected.SecondToken);
            return true;
        }
    }
}
