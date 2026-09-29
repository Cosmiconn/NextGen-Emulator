using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHall2ExternalKind : byte
    {
        Broadcast = 1,
        LinkTo = 2,
        MobRegen = 3,
        QuestMobKill = 4,
        Reward = 5,
        SummonMob = 6,
    }

    public sealed class KingdomQuestUnderHall2ExternalSourceSite
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestUnderHall2ExternalKind Kind { get; private set; }

        internal KingdomQuestUnderHall2ExternalSourceSite(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestUnderHall2ExternalKind kind)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            Kind = kind;
        }
    }

    /// <summary>
    /// Exact top-level block routing for the 74 occurrences of the six
    /// UnderHall-compatible external command families in the canonical,
    /// hash-locked KQ/UnderHall2 Pine source.
    ///
    /// Exact command text is independently locked by
    /// docs/KINGDOM_QUEST_UNDERHALL2_EXTERNAL_SOURCE.tsv. This class is only a
    /// source-routing boundary; it does not assign link/spawn/reward/quest
    /// gameplay semantics.
    /// </summary>
    public static class KingdomQuestUnderHall2SourceFlow
    {
        public const string ScriptLanguage = "KQ/UnderHall2";
        public const int ExternalOccurrenceCount = 74;
        public const int BroadcastOccurrenceCount = 12;
        public const int LinkToOccurrenceCount = 3;
        public const int MobRegenOccurrenceCount = 1;
        public const int QuestMobKillOccurrenceCount = 2;
        public const int RewardOccurrenceCount = 2;
        public const int SummonMobOccurrenceCount = 54;

        private static readonly Dictionary<int, KingdomQuestUnderHall2ExternalSourceSite>
            Sites = new Dictionary<int, KingdomQuestUnderHall2ExternalSourceSite>
        {
            { 402, Site(402, "TwelveTwo", KingdomQuestUnderHall2ExternalKind.MobRegen) },
            { 424, Site(424, "Summon1", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 426, Site(426, "Summon1", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 428, Site(428, "Summon1", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 430, Site(430, "Summon1", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 433, Site(433, "Summon2", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 435, Site(435, "Summon2", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 437, Site(437, "Summon2", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 439, Site(439, "Summon2", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 442, Site(442, "Summon3", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 444, Site(444, "Summon3", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 446, Site(446, "Summon3", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 448, Site(448, "Summon3", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 451, Site(451, "Summon4", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 453, Site(453, "Summon4", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 455, Site(455, "Summon4", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 457, Site(457, "Summon4", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 459, Site(459, "Summon4", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 462, Site(462, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 464, Site(464, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 466, Site(466, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 468, Site(468, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 470, Site(470, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 472, Site(472, "Summon5", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 475, Site(475, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 477, Site(477, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 479, Site(479, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 481, Site(481, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 483, Site(483, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 485, Site(485, "Summon6", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 488, Site(488, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 490, Site(490, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 492, Site(492, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 494, Site(494, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 496, Site(496, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 498, Site(498, "Summon7", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 501, Site(501, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 503, Site(503, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 505, Site(505, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 507, Site(507, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 509, Site(509, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 511, Site(511, "Summon8", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 514, Site(514, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 516, Site(516, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 518, Site(518, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 520, Site(520, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 522, Site(522, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 524, Site(524, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 526, Site(526, "Summon9", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 529, Site(529, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 531, Site(531, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 533, Site(533, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 535, Site(535, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 537, Site(537, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 539, Site(539, "Summon10", KingdomQuestUnderHall2ExternalKind.SummonMob) },
            { 543, Site(543, "QuestSuc", KingdomQuestUnderHall2ExternalKind.Reward) },
            { 544, Site(544, "QuestSuc", KingdomQuestUnderHall2ExternalKind.QuestMobKill) },
            { 545, Site(545, "QuestSuc", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 547, Site(547, "QuestSuc", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 549, Site(549, "QuestSuc", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 551, Site(551, "QuestSuc", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 553, Site(553, "QuestSuc", KingdomQuestUnderHall2ExternalKind.LinkTo) },
            { 559, Site(559, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.Reward) },
            { 560, Site(560, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.QuestMobKill) },
            { 563, Site(563, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 565, Site(565, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 567, Site(567, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 569, Site(569, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 571, Site(571, "QuestSuc2", KingdomQuestUnderHall2ExternalKind.LinkTo) },
            { 577, Site(577, "QuestFail", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 579, Site(579, "QuestFail", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 581, Site(581, "QuestFail", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 583, Site(583, "QuestFail", KingdomQuestUnderHall2ExternalKind.Broadcast) },
            { 585, Site(585, "QuestFail", KingdomQuestUnderHall2ExternalKind.LinkTo) },
        };

        public static bool TryResolve(
            int canonicalLine,
            KingdomQuestUnderHall2ExternalKind kind,
            out KingdomQuestUnderHall2ExternalSourceSite site)
        {
            site = null;
            KingdomQuestUnderHall2ExternalSourceSite candidate;
            if (!Sites.TryGetValue(canonicalLine, out candidate) ||
                candidate == null ||
                candidate.Kind != kind)
                return false;

            site = candidate;
            return true;
        }

        public static IReadOnlyDictionary<int, KingdomQuestUnderHall2ExternalSourceSite>
            Snapshot()
        {
            return new Dictionary<int, KingdomQuestUnderHall2ExternalSourceSite>(Sites);
        }

        private static KingdomQuestUnderHall2ExternalSourceSite Site(
            int canonicalLine,
            string block,
            KingdomQuestUnderHall2ExternalKind kind)
        {
            return new KingdomQuestUnderHall2ExternalSourceSite(
                canonicalLine, block, kind);
        }
    }
}
