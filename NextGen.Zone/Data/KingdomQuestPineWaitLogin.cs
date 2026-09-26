namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit native owner for Pine waitlogin execution.
    ///
    /// The control runtime resolves the exact source target variable and passes
    /// its native token storage plus persistent command-frame state. The source
    /// decides when the command completes and what token bytes are written.
    /// No player-count meaning, login predicate or polling cadence is invented.
    /// </summary>
    public interface IKingdomQuestPineWaitLoginSource
    {
        bool TryStep(
            string targetIdentifier,
            KingdomQuestPineTokenValue destination,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Source-form parser for the waitlogin command used exactly once by each
    /// of the nine supplied Pine KQ scripts.
    /// </summary>
    public static class KingdomQuestPineWaitLogin
    {
        public const int UsedCommandCount = 9;

        public static bool TryParseUsed(
            string commandText,
            out string targetIdentifier)
        {
            targetIdentifier = null;
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            string text = commandText.Trim();
            if (text.EndsWith(".", System.StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();

            const string Prefix = "waitlogin ";
            if (!text.StartsWith(
                    Prefix,
                    System.StringComparison.OrdinalIgnoreCase))
                return false;

            string target = text.Substring(Prefix.Length).Trim();
            if (target.Length == 0 ||
                target.IndexOfAny(new[] { ' ', '\t', '"', '\'' }) >= 0)
                return false;

            string identifier;
            if (!KingdomQuestPineBasicExpression.TrySimpleIdentifier(
                    target, out identifier) ||
                string.IsNullOrEmpty(identifier))
                return false;

            targetIdentifier = identifier;
            return true;
        }
    }
}
