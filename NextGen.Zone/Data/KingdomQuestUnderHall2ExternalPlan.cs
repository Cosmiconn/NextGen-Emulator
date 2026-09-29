using System;
using System.Collections.Generic;
using System.Globalization;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHall2ExternalPlan
    {
        public KingdomQuestUnderHall2ExternalKind Kind { get; private set; }
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string SourceToken { get; private set; }
        public string TargetToken { get; private set; }
        public string RuntimeHandleIdentifier { get; private set; }
        public KingdomQuestUnderHall2MapSourceRef Map { get; private set; }
        public KingdomQuestUnderHall2MobSourceRef Mob { get; private set; }
        public int Count { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int RawNumeric1 { get; private set; }
        public int RawNumeric2 { get; private set; }
        public string RawText1 { get; private set; }

        internal KingdomQuestUnderHall2ExternalPlan(
            KingdomQuestUnderHall2ExternalKind kind,
            int canonicalLine,
            string topLevelBlock,
            string sourceToken,
            string targetToken,
            string runtimeHandleIdentifier,
            KingdomQuestUnderHall2MapSourceRef map,
            KingdomQuestUnderHall2MobSourceRef mob,
            int count,
            int x,
            int y,
            int rawNumeric1,
            int rawNumeric2,
            string rawText1)
        {
            Kind = kind;
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            SourceToken = sourceToken ?? string.Empty;
            TargetToken = targetToken ?? string.Empty;
            RuntimeHandleIdentifier = runtimeHandleIdentifier ?? string.Empty;
            Map = map;
            Mob = mob;
            Count = count;
            X = x;
            Y = y;
            RawNumeric1 = rawNumeric1;
            RawNumeric2 = rawNumeric2;
            RawText1 = rawText1 ?? string.Empty;
        }
    }

    /// <summary>
    /// Typed, mutation-free projection of all 74 six-family external
    /// KQ/UnderHall2 source sites.
    ///
    /// The shared KingdomQuestPineKqExternalSyntax parser owns only lexical
    /// command shape. This builder then requires the exact UnderHall2
    /// canonical line/block, original MapInfo/MobInfo identity and literal
    /// operands. It performs no link, spawn, quest, reward or notice side
    /// effect.
    /// </summary>
    public static class KingdomQuestUnderHall2ExternalPlanBuilder
    {
        public const int SourceUsedOccurrenceCount = 74;
        public const int BroadcastOccurrenceCount = 12;
        public const int LinkToOccurrenceCount = 3;
        public const int MobRegenOccurrenceCount = 1;
        public const int QuestMobKillOccurrenceCount = 2;
        public const int RewardOccurrenceCount = 2;
        public const int SummonMobOccurrenceCount = 54;

        public const string RuntimeHandleIdentifier = "KQ_GB_Spider";
        public const int MobRegenCanonicalLine = 402;
        public const string MobRegenBlock = "TwelveTwo";

        private static readonly HashSet<string> BroadcastKeys =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "KQReturn5",
                "KQReturn10",
                "KQReturn20",
                "KQReturn30",
            };

        private sealed class LinkExpectation
        {
            public string Block;
            public string Map;
            public int X;
            public int Y;

            public LinkExpectation(string block, string map, int x, int y)
            {
                Block = block;
                Map = map;
                X = x;
                Y = y;
            }
        }

        private sealed class SummonExpectation
        {
            public string Block;
            public string Mob;
            public int Count;

            public SummonExpectation(string block, string mob, int count)
            {
                Block = block;
                Mob = mob;
                Count = count;
            }
        }

        private static readonly Dictionary<int, LinkExpectation> Links =
            new Dictionary<int, LinkExpectation>
        {
            { 553, new LinkExpectation(
                "QuestSuc", "Eld", 17214, 13445) },
            { 571, new LinkExpectation(
                "QuestSuc2", "Urg", 5835, 6397) },
            { 585, new LinkExpectation(
                "QuestFail", "Urg", 5835, 6397) },
        };

        private static readonly Dictionary<int, SummonExpectation> Summons =
            new Dictionary<int, SummonExpectation>
        {
            { 424, new SummonExpectation(
                "Summon1", "KQ_M_Spider", 5) },
            { 426, new SummonExpectation(
                "Summon1", "KQ_M_Spider", 5) },
            { 428, new SummonExpectation(
                "Summon1", "KQ_M_Spider", 5) },
            { 430, new SummonExpectation(
                "Summon1", "KQ_U_Spider01", 2) },
            { 433, new SummonExpectation(
                "Summon2", "KQ_M_Spider", 5) },
            { 435, new SummonExpectation(
                "Summon2", "KQ_M_Spider", 6) },
            { 437, new SummonExpectation(
                "Summon2", "KQ_M_Spider", 7) },
            { 439, new SummonExpectation(
                "Summon2", "KQ_U_Spider02", 2) },
            { 442, new SummonExpectation(
                "Summon3", "KQ_M_Spider", 6) },
            { 444, new SummonExpectation(
                "Summon3", "KQ_M_Spider", 6) },
            { 446, new SummonExpectation(
                "Summon3", "KQ_M_Spider", 6) },
            { 448, new SummonExpectation(
                "Summon3", "KQ_U_Spider03", 2) },
            { 451, new SummonExpectation(
                "Summon4", "KQ_M_Spider", 4) },
            { 453, new SummonExpectation(
                "Summon4", "KQ_M_Spider", 5) },
            { 455, new SummonExpectation(
                "Summon4", "KQ_M_Spider", 6) },
            { 457, new SummonExpectation(
                "Summon4", "KQ_M_Spider", 7) },
            { 459, new SummonExpectation(
                "Summon4", "KQ_U_Spider04", 2) },
            { 462, new SummonExpectation(
                "Summon5", "KQ_M_Spider", 5) },
            { 464, new SummonExpectation(
                "Summon5", "KQ_M_Spider", 5) },
            { 466, new SummonExpectation(
                "Summon5", "KQ_M_Spider", 5) },
            { 468, new SummonExpectation(
                "Summon5", "KQ_M_Spider", 5) },
            { 470, new SummonExpectation(
                "Summon5", "KQ_M_Spider", 5) },
            { 472, new SummonExpectation(
                "Summon5", "KQ_U_Spider05", 2) },
            { 475, new SummonExpectation(
                "Summon6", "KQ_M_Spider", 6) },
            { 477, new SummonExpectation(
                "Summon6", "KQ_M_Spider", 6) },
            { 479, new SummonExpectation(
                "Summon6", "KQ_M_Spider", 6) },
            { 481, new SummonExpectation(
                "Summon6", "KQ_M_Spider", 6) },
            { 483, new SummonExpectation(
                "Summon6", "KQ_M_Spider", 6) },
            { 485, new SummonExpectation(
                "Summon6", "KQ_U_AMageBook", 2) },
            { 488, new SummonExpectation(
                "Summon7", "KQ_M_Spider", 7) },
            { 490, new SummonExpectation(
                "Summon7", "KQ_M_Spider", 7) },
            { 492, new SummonExpectation(
                "Summon7", "KQ_M_Spider", 7) },
            { 494, new SummonExpectation(
                "Summon7", "KQ_M_Spider", 7) },
            { 496, new SummonExpectation(
                "Summon7", "KQ_M_Spider", 7) },
            { 498, new SummonExpectation(
                "Summon7", "KQ_U_Lvivi", 2) },
            { 501, new SummonExpectation(
                "Summon8", "KQ_M_Spider", 8) },
            { 503, new SummonExpectation(
                "Summon8", "KQ_M_Spider", 8) },
            { 505, new SummonExpectation(
                "Summon8", "KQ_M_Spider", 8) },
            { 507, new SummonExpectation(
                "Summon8", "KQ_M_Spider", 8) },
            { 509, new SummonExpectation(
                "Summon8", "KQ_M_Spider", 8) },
            { 511, new SummonExpectation(
                "Summon8", "KQ_U_Greenky", 2) },
            { 514, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 516, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 518, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 520, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 522, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 524, new SummonExpectation(
                "Summon9", "KQ_M_Spider", 7) },
            { 526, new SummonExpectation(
                "Summon9", "KQ_U_TombRaider", 2) },
            { 529, new SummonExpectation(
                "Summon10", "KQ_M_Spider", 9) },
            { 531, new SummonExpectation(
                "Summon10", "KQ_M_Spider", 9) },
            { 533, new SummonExpectation(
                "Summon10", "KQ_M_Spider", 9) },
            { 535, new SummonExpectation(
                "Summon10", "KQ_M_Spider", 9) },
            { 537, new SummonExpectation(
                "Summon10", "KQ_M_Spider", 9) },
            { 539, new SummonExpectation(
                "Summon10", "KQ_U_Uspider", 2) },
        };

        public static bool TryBuild(
            int canonicalLine,
            string commandText,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            KingdomQuestPineKqExternalSyntaxPlan syntax;
            if (!KingdomQuestPineKqExternalSyntax.TryParse(
                    commandText, out syntax) ||
                syntax == null)
                return false;

            KingdomQuestUnderHall2ExternalKind kind =
                (KingdomQuestUnderHall2ExternalKind)syntax.Kind;
            KingdomQuestUnderHall2ExternalSourceSite sourceSite;
            if (!KingdomQuestUnderHall2SourceFlow.TryResolve(
                    canonicalLine, kind, out sourceSite) ||
                sourceSite == null)
                return false;

            switch (kind)
            {
                case KingdomQuestUnderHall2ExternalKind.Broadcast:
                    return TryBroadcast(syntax, sourceSite, out plan);
                case KingdomQuestUnderHall2ExternalKind.LinkTo:
                    return TryLinkTo(syntax, sourceSite, out plan);
                case KingdomQuestUnderHall2ExternalKind.MobRegen:
                    return TryMobRegen(syntax, sourceSite, out plan);
                case KingdomQuestUnderHall2ExternalKind.QuestMobKill:
                    return TryQuestMobKill(syntax, sourceSite, out plan);
                case KingdomQuestUnderHall2ExternalKind.Reward:
                    return TryReward(syntax, sourceSite, out plan);
                case KingdomQuestUnderHall2ExternalKind.SummonMob:
                    return TrySummonMob(syntax, sourceSite, out plan);
                default:
                    return false;
            }
        }

        private static bool TryBroadcast(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 2 ||
                !string.Equals(
                    source.Arguments[0].Text, "all",
                    StringComparison.Ordinal) ||
                !BroadcastKeys.Contains(source.Arguments[1].Text))
                return false;

            plan = Build(
                KingdomQuestUnderHall2ExternalKind.Broadcast,
                sourceSite,
                source.Arguments[1].Text,
                "all");
            return true;
        }

        private static bool TryLinkTo(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 5 ||
                !string.Equals(
                    source.Arguments[0].Text, "all",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.Arguments[1].Text,
                    source.Arguments[2].Text,
                    StringComparison.Ordinal))
                return false;

            LinkExpectation expected;
            KingdomQuestUnderHall2MapSourceRef map;
            int x;
            int y;
            if (!Links.TryGetValue(sourceSite.CanonicalLine, out expected) ||
                expected == null ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    expected.Block,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.Arguments[1].Text,
                    expected.Map,
                    StringComparison.Ordinal) ||
                !KingdomQuestUnderHall2SourceCatalog.TryGetMap(
                    expected.Map, out map) ||
                map == null ||
                !TryInt(source.Arguments[3].Text, out x) ||
                !TryInt(source.Arguments[4].Text, out y) ||
                x != expected.X ||
                y != expected.Y)
                return false;

            plan = new KingdomQuestUnderHall2ExternalPlan(
                KingdomQuestUnderHall2ExternalKind.LinkTo,
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.Arguments[1].Text,
                source.Arguments[0].Text,
                string.Empty,
                map,
                null,
                0,
                x,
                y,
                0,
                0,
                source.Arguments[2].Text);
            return true;
        }

        private static bool TryMobRegen(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            KingdomQuestUnderHall2MobSourceRef mob;
            int x;
            int y;
            int raw1;
            int raw2;
            if (sourceSite.CanonicalLine != MobRegenCanonicalLine ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    MobRegenBlock,
                    StringComparison.Ordinal) ||
                source.Arguments.Count != 7 ||
                !string.Equals(
                    source.Arguments[0].Text,
                    RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.Arguments[1].Text,
                    RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                !KingdomQuestUnderHall2SourceCatalog.TryGetMob(
                    source.Arguments[1].Text, out mob) ||
                mob == null ||
                mob.MobId != 1158 ||
                !TryInt(source.Arguments[2].Text, out x) ||
                !TryInt(source.Arguments[3].Text, out y) ||
                !TryInt(source.Arguments[4].Text, out raw1) ||
                !TryInt(source.Arguments[5].Text, out raw2) ||
                x != 2350 ||
                y != 2550 ||
                raw1 != 90 ||
                raw2 != 1000 ||
                !string.Equals(
                    source.Arguments[6].Text,
                    "Normal",
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestUnderHall2ExternalPlan(
                KingdomQuestUnderHall2ExternalKind.MobRegen,
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.Arguments[1].Text,
                string.Empty,
                source.Arguments[0].Text,
                null,
                mob,
                0,
                x,
                y,
                raw1,
                raw2,
                source.Arguments[6].Text);
            return true;
        }

        private static bool TryQuestMobKill(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            KingdomQuestUnderHall2MobSourceRef mob;
            int questId;
            int count;
            bool expectedSite =
                (sourceSite.CanonicalLine == 544 &&
                    string.Equals(
                        sourceSite.TopLevelBlock,
                        "QuestSuc",
                        StringComparison.Ordinal)) ||
                (sourceSite.CanonicalLine == 560 &&
                    string.Equals(
                        sourceSite.TopLevelBlock,
                        "QuestSuc2",
                        StringComparison.Ordinal));
            if (!expectedSite ||
                source.Arguments.Count != 3 ||
                !TryInt(source.Arguments[0].Text, out questId) ||
                questId != 2668 ||
                !string.Equals(
                    source.Arguments[1].Text,
                    "Daliy_Check",
                    StringComparison.Ordinal) ||
                !KingdomQuestUnderHall2SourceCatalog.TryGetMob(
                    source.Arguments[1].Text, out mob) ||
                mob == null ||
                mob.MobId != 50000 ||
                !TryInt(source.Arguments[2].Text, out count) ||
                count != 1)
                return false;

            plan = new KingdomQuestUnderHall2ExternalPlan(
                KingdomQuestUnderHall2ExternalKind.QuestMobKill,
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.Arguments[1].Text,
                string.Empty,
                string.Empty,
                null,
                mob,
                count,
                0,
                0,
                questId,
                0,
                string.Empty);
            return true;
        }

        private static bool TryReward(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            bool expectedSite =
                (sourceSite.CanonicalLine == 543 &&
                    string.Equals(
                        sourceSite.TopLevelBlock,
                        "QuestSuc",
                        StringComparison.Ordinal)) ||
                (sourceSite.CanonicalLine == 559 &&
                    string.Equals(
                        sourceSite.TopLevelBlock,
                        "QuestSuc2",
                        StringComparison.Ordinal));
            if (!expectedSite ||
                source.Arguments.Count != 1 ||
                !string.Equals(
                    source.Arguments[0].Text,
                    "KingdomQuest",
                    StringComparison.Ordinal))
                return false;

            plan = Build(
                KingdomQuestUnderHall2ExternalKind.Reward,
                sourceSite,
                "KingdomQuest",
                string.Empty);
            return true;
        }

        private static bool TrySummonMob(
            KingdomQuestPineKqExternalSyntaxPlan source,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            out KingdomQuestUnderHall2ExternalPlan plan)
        {
            plan = null;
            SummonExpectation expected;
            KingdomQuestUnderHall2MobSourceRef mob;
            int count;
            if (source.Arguments.Count != 3 ||
                !Summons.TryGetValue(
                    sourceSite.CanonicalLine, out expected) ||
                expected == null ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    expected.Block,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.Arguments[0].Text,
                    RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.Arguments[1].Text,
                    expected.Mob,
                    StringComparison.Ordinal) ||
                !KingdomQuestUnderHall2SourceCatalog.TryGetMob(
                    source.Arguments[1].Text, out mob) ||
                mob == null ||
                !TryInt(source.Arguments[2].Text, out count) ||
                count != expected.Count)
                return false;

            plan = new KingdomQuestUnderHall2ExternalPlan(
                KingdomQuestUnderHall2ExternalKind.SummonMob,
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.Arguments[1].Text,
                string.Empty,
                source.Arguments[0].Text,
                null,
                mob,
                count,
                0,
                0,
                0,
                0,
                string.Empty);
            return true;
        }

        private static KingdomQuestUnderHall2ExternalPlan Build(
            KingdomQuestUnderHall2ExternalKind kind,
            KingdomQuestUnderHall2ExternalSourceSite sourceSite,
            string sourceToken,
            string targetToken)
        {
            return new KingdomQuestUnderHall2ExternalPlan(
                kind,
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                sourceToken,
                targetToken,
                string.Empty,
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                string.Empty);
        }

        private static bool TryInt(string value, out int result)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}
