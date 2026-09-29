using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineKqExternalKind : byte
    {
        Broadcast = 1,
        LinkTo = 2,
        MobRegen = 3,
        QuestMobKill = 4,
        Reward = 5,
        SummonMob = 6,
    }

    public sealed class KingdomQuestPineKqExternalArgument
    {
        public string Text { get; private set; }
        public bool Quoted { get; private set; }

        internal KingdomQuestPineKqExternalArgument(
            string text,
            bool quoted)
        {
            Text = text ?? string.Empty;
            Quoted = quoted;
        }
    }

    public sealed class KingdomQuestPineKqExternalSyntaxPlan
    {
        public KingdomQuestPineKqExternalKind Kind { get; private set; }
        public IReadOnlyList<KingdomQuestPineKqExternalArgument> Arguments
        {
            get;
            private set;
        }

        internal KingdomQuestPineKqExternalSyntaxPlan(
            KingdomQuestPineKqExternalKind kind,
            List<KingdomQuestPineKqExternalArgument> arguments)
        {
            Kind = kind;
            Arguments =
                (arguments ?? new List<KingdomQuestPineKqExternalArgument>())
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Shared source-syntax parser for the six Pine KQ command families used by
    /// both KQ/UnderHall and KQ/UnderHall2.
    ///
    /// It preserves token text and quoting, checks only each native command
    /// family's source shape, and deliberately assigns no map/mob/reward/link
    /// gameplay meaning. Script-specific exact values remain owned by the
    /// hash-locked source projections.
    /// </summary>
    public static class KingdomQuestPineKqExternalSyntax
    {
        public const int FamilyCount = 6;

        public static bool TryParse(
            string commandText,
            out KingdomQuestPineKqExternalSyntaxPlan plan)
        {
            plan = null;
            List<KingdomQuestPineKqExternalArgument> tokens;
            if (!TryLex(commandText, out tokens) || tokens.Count == 0)
                return false;

            string verb = tokens[0].Text.ToLowerInvariant();
            KingdomQuestPineKqExternalKind kind;
            switch (verb)
            {
                case "broadcast":
                    kind = KingdomQuestPineKqExternalKind.Broadcast;
                    if (tokens.Count != 3 ||
                        tokens[1].Quoted ||
                        !tokens[2].Quoted)
                        return false;
                    break;

                case "linkto":
                    kind = KingdomQuestPineKqExternalKind.LinkTo;
                    if (tokens.Count != 6 ||
                        tokens[1].Quoted ||
                        !tokens[2].Quoted ||
                        !tokens[3].Quoted ||
                        tokens[4].Quoted ||
                        tokens[5].Quoted)
                        return false;
                    break;

                case "mobregen":
                    kind = KingdomQuestPineKqExternalKind.MobRegen;
                    if (tokens.Count != 8 ||
                        tokens[1].Quoted ||
                        !tokens[2].Quoted ||
                        tokens[3].Quoted ||
                        tokens[4].Quoted ||
                        tokens[5].Quoted ||
                        tokens[6].Quoted ||
                        !tokens[7].Quoted)
                        return false;
                    break;

                case "questmobkill":
                    kind = KingdomQuestPineKqExternalKind.QuestMobKill;
                    if (tokens.Count != 4 ||
                        tokens[1].Quoted ||
                        !tokens[2].Quoted ||
                        tokens[3].Quoted)
                        return false;
                    break;

                case "reward":
                    kind = KingdomQuestPineKqExternalKind.Reward;
                    if (tokens.Count != 2 || tokens[1].Quoted)
                        return false;
                    break;

                case "summonmob":
                    kind = KingdomQuestPineKqExternalKind.SummonMob;
                    if (tokens.Count != 4 ||
                        tokens[1].Quoted ||
                        !tokens[2].Quoted ||
                        tokens[3].Quoted)
                        return false;
                    break;

                default:
                    return false;
            }

            tokens.RemoveAt(0);
            plan = new KingdomQuestPineKqExternalSyntaxPlan(kind, tokens);
            return true;
        }

        private static bool TryLex(
            string commandText,
            out List<KingdomQuestPineKqExternalArgument> tokens)
        {
            tokens = new List<KingdomQuestPineKqExternalArgument>();
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            string text = commandText.Trim();
            if (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();

            int offset = 0;
            while (offset < text.Length)
            {
                while (offset < text.Length &&
                    char.IsWhiteSpace(text[offset]))
                    offset++;

                if (offset >= text.Length)
                    break;

                if (text[offset] == '"')
                {
                    int start = ++offset;
                    while (offset < text.Length &&
                        text[offset] != '"')
                        offset++;
                    if (offset >= text.Length)
                        return false;

                    tokens.Add(new KingdomQuestPineKqExternalArgument(
                        text.Substring(start, offset - start), true));
                    offset++;
                    continue;
                }

                int tokenStart = offset;
                while (offset < text.Length &&
                    !char.IsWhiteSpace(text[offset]))
                    offset++;

                tokens.Add(new KingdomQuestPineKqExternalArgument(
                    text.Substring(tokenStart, offset - tokenStart), false));
            }

            return tokens.Count != 0;
        }
    }
}
