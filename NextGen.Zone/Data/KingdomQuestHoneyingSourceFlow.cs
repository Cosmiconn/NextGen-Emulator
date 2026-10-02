using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestHoneyingExternalKind : byte
    {
        Broadcast = 1,
        ChatWin = 2,
        DoorBuild = 3,
        DoorClose = 4,
        DoorOpen = 5,
        EffectObject = 6,
        LinkTo = 7,
        MobRegen = 8,
        NpcShout = 9,
        QuestMobKill = 10,
        Reward = 11,
        SummonMob = 12,
        Vanish = 13,
    }

    public sealed class KingdomQuestHoneyingExternalSourceSite
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestHoneyingExternalKind Kind { get; private set; }
        public string CommandText { get; private set; }

        internal KingdomQuestHoneyingExternalSourceSite(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestHoneyingExternalKind kind,
            string commandText)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            Kind = kind;
            CommandText = commandText ?? string.Empty;
        }
    }

    public static class KingdomQuestHoneyingSourceFlow
    {
        public const string ScriptLanguage = "KQ/Honeying";
        public const int SourceUsedFamilyCount = 13;
        public const int SourceUsedOccurrenceCount = 40;

        private static readonly Dictionary<int, KingdomQuestHoneyingExternalSourceSite>
            Sites =
                new Dictionary<int, KingdomQuestHoneyingExternalSourceSite>
                {
                { 12, Site(
                    12, "main",
                    KingdomQuestHoneyingExternalKind.DoorBuild,
                    "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\".") },
                { 13, Site(
                    13, "main",
                    KingdomQuestHoneyingExternalKind.DoorBuild,
                    "doorbuild Door2 \"KQ_SlimeGate\" 6692 3944    6 1000 \"Normal\".") },
                { 14, Site(
                    14, "main",
                    KingdomQuestHoneyingExternalKind.DoorBuild,
                    "doorbuild Door3 \"KQ_SlimeGate\" 5894 6098   88 1000 \"Normal\".") },
                { 15, Site(
                    15, "main",
                    KingdomQuestHoneyingExternalKind.DoorClose,
                    "doorclose Door1 \"CloseGate01\".") },
                { 16, Site(
                    16, "main",
                    KingdomQuestHoneyingExternalKind.DoorClose,
                    "doorclose Door2 \"CloseGate02\".") },
                { 17, Site(
                    17, "main",
                    KingdomQuestHoneyingExternalKind.DoorClose,
                    "doorclose Door3 \"CloseGate03\".") },
                { 18, Site(
                    18, "main",
                    KingdomQuestHoneyingExternalKind.EffectObject,
                    "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000.") },
                { 19, Site(
                    19, "main",
                    KingdomQuestHoneyingExternalKind.EffectObject,
                    "effectobj EffDoor2 Door2 \"KQ_SlimeGate\" 3600000 1000.") },
                { 20, Site(
                    20, "main",
                    KingdomQuestHoneyingExternalKind.EffectObject,
                    "effectobj EffDoor3 Door3 \"KQ_SlimeGate\" 3600000 1000.") },
                { 61, Site(
                    61, "FirstMobEleminate",
                    KingdomQuestHoneyingExternalKind.DoorOpen,
                    "dooropen Door1 \"CloseGate01\".") },
                { 62, Site(
                    62, "FirstMobEleminate",
                    KingdomQuestHoneyingExternalKind.Vanish,
                    "vanish EffDoor1.") },
                { 97, Site(
                    97, "SecondMobEleminate",
                    KingdomQuestHoneyingExternalKind.DoorOpen,
                    "dooropen Door2 \"CloseGate02\".") },
                { 98, Site(
                    98, "SecondMobEleminate",
                    KingdomQuestHoneyingExternalKind.Vanish,
                    "vanish EffDoor2.") },
                { 130, Site(
                    130, "ThirdMobEleminate",
                    KingdomQuestHoneyingExternalKind.DoorOpen,
                    "dooropen Door3 \"CloseGate03\".") },
                { 131, Site(
                    131, "ThirdMobEleminate",
                    KingdomQuestHoneyingExternalKind.Vanish,
                    "vanish EffDoor3.") },
                { 139, Site(
                    139, "TopFloor",
                    KingdomQuestHoneyingExternalKind.MobRegen,
                    "mobregen Boss \"KQ_H_GHoneying\" 7081 5972 90 1000 \"Normal\".") },
                { 140, Site(
                    140, "TopFloor",
                    KingdomQuestHoneyingExternalKind.ChatWin,
                    "chatwin \"KQ_H_GHoneying\" \"Honeying01\".") },
                { 142, Site(
                    142, "TopFloor",
                    KingdomQuestHoneyingExternalKind.ChatWin,
                    "chatwin \"KQ_H_GHoneying\" \"Honeying02\".") },
                { 160, Site(
                    160, "Summon1",
                    KingdomQuestHoneyingExternalKind.NpcShout,
                    "npcshout Boss \"Summon01\".") },
                { 161, Site(
                    161, "Summon1",
                    KingdomQuestHoneyingExternalKind.SummonMob,
                    "summonmob Boss \"KQ_H_Honeying\" 2.") },
                { 164, Site(
                    164, "Summon2",
                    KingdomQuestHoneyingExternalKind.NpcShout,
                    "npcshout Boss \"Summon01\".") },
                { 165, Site(
                    165, "Summon2",
                    KingdomQuestHoneyingExternalKind.SummonMob,
                    "summonmob Boss \"KQ_H_Honeying\" 4.") },
                { 168, Site(
                    168, "Summon3",
                    KingdomQuestHoneyingExternalKind.NpcShout,
                    "npcshout Boss \"Summon01\".") },
                { 169, Site(
                    169, "Summon3",
                    KingdomQuestHoneyingExternalKind.SummonMob,
                    "summonmob Boss \"KQ_H_Honeying\" 8.") },
                { 172, Site(
                    172, "Summon4",
                    KingdomQuestHoneyingExternalKind.NpcShout,
                    "npcshout Boss \"Summon01\".") },
                { 173, Site(
                    173, "Summon4",
                    KingdomQuestHoneyingExternalKind.SummonMob,
                    "summonmob Boss \"KQ_H_Honeying\" 10.") },
                { 174, Site(
                    174, "Summon4",
                    KingdomQuestHoneyingExternalKind.SummonMob,
                    "summonmob Boss \"KQ_H_Honeying\" 10.") },
                { 177, Site(
                    177, "Dead",
                    KingdomQuestHoneyingExternalKind.NpcShout,
                    "npcshout Boss \"KQ_H_GHoneyingDead\".") },
                { 181, Site(
                    181, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.Reward,
                    "reward KingdomQuest.") },
                { 182, Site(
                    182, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.QuestMobKill,
                    "questmobkill 2668 \"Daliy_Check\" 1.") },
                { 183, Site(
                    183, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn30\".") },
                { 185, Site(
                    185, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn20\".") },
                { 187, Site(
                    187, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn10\".") },
                { 189, Site(
                    189, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn5\".") },
                { 191, Site(
                    191, "QuestSuccess",
                    KingdomQuestHoneyingExternalKind.LinkTo,
                    "linkto all \"Eld\" \"Eld\" 17214 13445.") },
                { 197, Site(
                    197, "QuestFail",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn30\".") },
                { 199, Site(
                    199, "QuestFail",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn20\".") },
                { 201, Site(
                    201, "QuestFail",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn10\".") },
                { 203, Site(
                    203, "QuestFail",
                    KingdomQuestHoneyingExternalKind.Broadcast,
                    "broadcast all \"KQReturn5\".") },
                { 205, Site(
                    205, "QuestFail",
                    KingdomQuestHoneyingExternalKind.LinkTo,
                    "linkto all \"Eld\" \"Eld\" 17214 13445.") },
                };

        public static bool TryResolve(
            string scriptLanguage,
            int canonicalLine,
            string commandText,
            out KingdomQuestHoneyingExternalSourceSite site)
        {
            site = null;
            if (!string.Equals(
                    scriptLanguage,
                    ScriptLanguage,
                    StringComparison.Ordinal) ||
                !Sites.TryGetValue(canonicalLine, out site) ||
                site == null ||
                !string.Equals(
                    commandText == null ? string.Empty : commandText.Trim(),
                    site.CommandText,
                    StringComparison.Ordinal))
            {
                site = null;
                return false;
            }

            return true;
        }

        private static KingdomQuestHoneyingExternalSourceSite Site(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestHoneyingExternalKind kind,
            string commandText)
        {
            return new KingdomQuestHoneyingExternalSourceSite(
                canonicalLine,
                topLevelBlock,
                kind,
                commandText);
        }
    }
}
