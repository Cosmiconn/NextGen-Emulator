using System;
using System.Collections.Generic;
using System.Globalization;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHallCommandKind : byte
    {
        Broadcast = 1,
        LinkTo = 2,
        MobRegen = 3,
        QuestMobKill = 4,
        Reward = 5,
        ScriptFile = 6,
        SummonMob = 7,
        WaitInterrupt = 8,
        WaitLogin = 9,
    }

    public sealed class KingdomQuestUnderHallCommandSourcePlan
    {
        public KingdomQuestUnderHallCommandKind Kind { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; }

        internal KingdomQuestUnderHallCommandSourcePlan(
            KingdomQuestUnderHallCommandKind kind,
            List<string> arguments)
        {
            Kind = kind;
            Arguments = (arguments ?? new List<string>()).AsReadOnly();
        }
    }

    /// <summary>
    /// Exact source-form parser for the nine unrecovered command families used
    /// by KQ/UnderHall in the hash-locked canonical Pine corpus.
    ///
    /// This type deliberately models source syntax/arguments only. It does not
    /// assign gameplay meaning or execute any map, mob, reward, quest, script,
    /// interrupt, login or transport side effect.
    /// </summary>
    public static class KingdomQuestUnderHallCommandSource
    {
        private static readonly HashSet<string> BroadcastMessages =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "KQReturn5",
                "KQReturn10",
                "KQReturn20",
                "KQReturn30",
            };

        private static readonly Dictionary<string, int> SummonCounts =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "KQ_DesertWolf", 3 },
                { "KQ_FireViVi", 6 },
                { "KQ_GiantMushRoom", 2 },
                { "KQ_RapidBoar", 5 },
                { "KQ_SkelArcher", 4 },
                { "KQ_SkelKnight", 2 },
                { "KQ_Skeleton", 5 },
                { "KQ_WildKebing", 5 },
                { "KQ_Zombie", 5 },
            };

        public const int SourceUsedFamilyCount = 9;
        public const int BroadcastFormCount = 4;
        public const int SummonDistinctFormCount = 11;

        public static bool TryParse(
            string commandText,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            List<Token> tokens;
            if (!TryLex(commandText, out tokens) || tokens.Count == 0)
                return false;

            string verb = tokens[0].Text.ToLowerInvariant();
            switch (verb)
            {
                case "broadcast":
                    return TryBroadcast(tokens, out plan);
                case "linkto":
                    return TryLinkTo(tokens, out plan);
                case "mobregen":
                    return TryMobRegen(tokens, out plan);
                case "questmobkill":
                    return TryQuestMobKill(tokens, out plan);
                case "reward":
                    return TryReward(tokens, out plan);
                case "scriptfile":
                    return TryScriptFile(tokens, out plan);
                case "summonmob":
                    return TrySummonMob(tokens, out plan);
                case "waitinterrupt":
                    return TryWaitInterrupt(tokens, out plan);
                case "waitlogin":
                    return TryWaitLogin(tokens, out plan);
                default:
                    return false;
            }
        }

        private static bool TryBroadcast(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 3 ||
                !Eq(t[1], "all", false) ||
                !t[2].Quoted ||
                !BroadcastMessages.Contains(t[2].Text))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.Broadcast,
                t[1].Text, t[2].Text);
            return true;
        }

        private static bool TryLinkTo(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 6 ||
                !Eq(t[1], "all", false) ||
                !Eq(t[2], "Eld", true) ||
                !Eq(t[3], "Eld", true) ||
                !Eq(t[4], "17214", false) ||
                !Eq(t[5], "13445", false))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.LinkTo,
                "all", "Eld", "Eld", "17214", "13445");
            return true;
        }

        private static bool TryMobRegen(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 8 ||
                !Eq(t[1], "KQ_BossRobo", false) ||
                !Eq(t[2], "KQ_BossRobo", true) ||
                !Eq(t[3], "2300", false) ||
                !Eq(t[4], "2500", false) ||
                !Eq(t[5], "90", false) ||
                !Eq(t[6], "1000", false) ||
                !Eq(t[7], "Normal", true))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.MobRegen,
                "KQ_BossRobo", "KQ_BossRobo",
                "2300", "2500", "90", "1000", "Normal");
            return true;
        }

        private static bool TryQuestMobKill(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 4 ||
                !Eq(t[1], "2668", false) ||
                !Eq(t[2], "Daliy_Check", true) ||
                !Eq(t[3], "1", false))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.QuestMobKill,
                "2668", "Daliy_Check", "1");
            return true;
        }

        private static bool TryReward(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 2 ||
                !Eq(t[1], "KingdomQuest", false))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.Reward,
                "KingdomQuest");
            return true;
        }

        private static bool TryScriptFile(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 2 ||
                !Eq(t[1], "KQUnderHall", true))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.ScriptFile,
                "KQUnderHall");
            return true;
        }

        private static bool TrySummonMob(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 4 ||
                !Eq(t[1], "KQ_BossRobo", false) ||
                !t[2].Quoted ||
                t[3].Quoted)
                return false;

            int count;
            if (!int.TryParse(
                    t[3].Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out count))
                return false;

            if (string.Equals(
                    t[2].Text, "KQ_SkelWarrior",
                    StringComparison.Ordinal))
            {
                if (count != 3 && count != 5)
                    return false;
            }
            else
            {
                int expected;
                if (!SummonCounts.TryGetValue(
                        t[2].Text, out expected) ||
                    count != expected)
                    return false;
            }

            plan = Build(
                KingdomQuestUnderHallCommandKind.SummonMob,
                "KQ_BossRobo",
                t[2].Text,
                count.ToString(CultureInfo.InvariantCulture));
            return true;
        }

        private static bool TryWaitInterrupt(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 3 ||
                !Eq(t[1], "InterruptBlock", false) ||
                !Eq(t[2], "InterruptArg", true))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.WaitInterrupt,
                "InterruptBlock", "InterruptArg");
            return true;
        }

        private static bool TryWaitLogin(
            List<Token> t,
            out KingdomQuestUnderHallCommandSourcePlan plan)
        {
            plan = null;
            if (t.Count != 2 ||
                !Eq(t[1], "Wait", false))
                return false;

            plan = Build(
                KingdomQuestUnderHallCommandKind.WaitLogin,
                "Wait");
            return true;
        }

        private static KingdomQuestUnderHallCommandSourcePlan Build(
            KingdomQuestUnderHallCommandKind kind,
            params string[] arguments)
        {
            return new KingdomQuestUnderHallCommandSourcePlan(
                kind, new List<string>(arguments));
        }

        private sealed class Token
        {
            public string Text;
            public bool Quoted;
        }

        private static bool Eq(
            Token token,
            string value,
            bool quoted)
        {
            return token != null &&
                token.Quoted == quoted &&
                string.Equals(
                    token.Text, value, StringComparison.Ordinal);
        }

        private static bool TryLex(
            string commandText,
            out List<Token> tokens)
        {
            tokens = new List<Token>();
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

                    tokens.Add(new Token
                    {
                        Text = text.Substring(start, offset - start),
                        Quoted = true,
                    });
                    offset++;
                    continue;
                }

                int tokenStart = offset;
                while (offset < text.Length &&
                    !char.IsWhiteSpace(text[offset]))
                    offset++;

                tokens.Add(new Token
                {
                    Text = text.Substring(tokenStart, offset - tokenStart),
                    Quoted = false,
                });
            }

            return tokens.Count != 0;
        }
    }
}
