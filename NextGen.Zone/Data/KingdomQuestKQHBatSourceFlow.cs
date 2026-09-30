using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestKQHBatExternalKind : byte
    {
        AbStateSet = 1,
        BattleStart = 2,
        BattleStop = 3,
        Broadcast = 4,
        ChatWin = 5,
        IndividualReward = 6,
        ItemDrop = 7,
        ItemErase = 8,
        LinkTo = 9,
        Revival = 10,
        SendQuestResult = 11,
    }

    public sealed class KingdomQuestKQHBatExternalSourceSite
    {
        public string ScriptLanguage { get; private set; }
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestKQHBatExternalKind Kind { get; private set; }
        public string CommandText { get; private set; }

        internal KingdomQuestKQHBatExternalSourceSite(
            string scriptLanguage,
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestKQHBatExternalKind kind,
            string commandText)
        {
            ScriptLanguage = scriptLanguage ?? string.Empty;
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            Kind = kind;
            CommandText = commandText ?? string.Empty;
        }
    }

    /// <summary>
    /// Exact source-flow projection for the five Warrior's Code Pine KQs.
    ///
    /// The five scripts contain the same 41 external-command line/block sites.
    /// Only the two linkto commands differ by stage. This class preserves that
    /// shared structure without assigning gameplay meaning to any of the 11
    /// still-unrecovered command families.
    /// </summary>
    public static class KingdomQuestKQHBatSourceFlow
    {
        public const int ScriptCount = 5;
        public const int FamilyCount = 11;
        public const int OccurrencePerScript = 41;
        public const int TotalOccurrenceCount = 205;

        private sealed class Template
        {
            public int CanonicalLine;
            public string TopLevelBlock;
            public KingdomQuestKQHBatExternalKind Kind;
            public string CommandText;
        }

        private static readonly HashSet<string> ScriptLanguages =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "KQ/KQHBat1",
                "KQ/KQHBat2",
                "KQ/KQHBat3",
                "KQ/KQHBat4",
                "KQ/KQHBat5",
            };

        private static readonly Dictionary<int, Template> CommonSites =
            new Dictionary<int, Template>
            {
                { 14, SiteTemplate(
                    14, "main",
                    KingdomQuestKQHBatExternalKind.BattleStop,
                    "battlestop PK.") },
                { 22, SiteTemplate(
                    22, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"Intro0\".") },
                { 24, SiteTemplate(
                    24, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"Intro1\".") },
                { 26, SiteTemplate(
                    26, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"Intro2\".") },
                { 28, SiteTemplate(
                    28, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"Intro3\".") },
                { 30, SiteTemplate(
                    30, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"Intro4\".") },
                { 32, SiteTemplate(
                    32, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"Intro5\".") },
                { 34, SiteTemplate(
                    34, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"Intro6\".") },
                { 36, SiteTemplate(
                    36, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"DualStart\".") },
                { 37, SiteTemplate(
                    37, "main",
                    KingdomQuestKQHBatExternalKind.BattleStart,
                    "battlestart PK.") },
                { 49, SiteTemplate(
                    49, "main",
                    KingdomQuestKQHBatExternalKind.ItemErase,
                    "itemerase all Maul.") },
                { 50, SiteTemplate(
                    50, "main",
                    KingdomQuestKQHBatExternalKind.ItemErase,
                    "itemerase all \"KQ_Ice01\".") },
                { 51, SiteTemplate(
                    51, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"DualStop\".") },
                { 52, SiteTemplate(
                    52, "main",
                    KingdomQuestKQHBatExternalKind.BattleStop,
                    "battlestop PK.") },
                { 53, SiteTemplate(
                    53, "main",
                    KingdomQuestKQHBatExternalKind.Revival,
                    "revival all.") },
                { 67, SiteTemplate(
                    67, "main",
                    KingdomQuestKQHBatExternalKind.SendQuestResult,
                    "sendquestresult Suc PlayerHandle.") },
                { 68, SiteTemplate(
                    68, "main",
                    KingdomQuestKQHBatExternalKind.IndividualReward,
                    "invidualreward PlayerHandle \"HERO_\" % InitFlag % \"_\" % Count.") },
                { 72, SiteTemplate(
                    72, "main",
                    KingdomQuestKQHBatExternalKind.SendQuestResult,
                    "sendquestresult Fail PlayerHandle.") },
                { 73, SiteTemplate(
                    73, "main",
                    KingdomQuestKQHBatExternalKind.IndividualReward,
                    "invidualreward PlayerHandle \"HERO_\" % InitFlag % \"_3\".") },
                { 78, SiteTemplate(
                    78, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"DualResult\" Winner0 Winner1 Winner2.") },
                { 79, SiteTemplate(
                    79, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"DualResult1\".") },
                { 80, SiteTemplate(
                    80, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"DualResult2\".") },
                { 81, SiteTemplate(
                    81, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"EldSpeGuard01\" \"DualResult3\".") },
                { 82, SiteTemplate(
                    82, "main",
                    KingdomQuestKQHBatExternalKind.ChatWin,
                    "chatwin \"RouTownChiefRoumenus\" \"DualResult4\".") },
                { 83, SiteTemplate(
                    83, "main",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn30\".") },
                { 85, SiteTemplate(
                    85, "main",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn20\".") },
                { 87, SiteTemplate(
                    87, "main",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn10\".") },
                { 89, SiteTemplate(
                    89, "main",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn5\".") },
                { 101, SiteTemplate(
                    101, "PickUpMaul",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all @CharName(LooterHandle) % \" has obtained Invincible Hammer.\".") },
                { 102, SiteTemplate(
                    102, "PickUpMaul",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"Everyone else will be immobilized for 3 seconds.\".") },
                { 105, SiteTemplate(
                    105, "PickUpMaul",
                    KingdomQuestKQHBatExternalKind.AbStateSet,
                    "abstateset all \"StaCommonStun01\" 1 3000 LooterHandle.") },
                { 110, SiteTemplate(
                    110, "RegenMaul",
                    KingdomQuestKQHBatExternalKind.ItemErase,
                    "itemerase all Maul.") },
                { 120, SiteTemplate(
                    120, "LooterDead",
                    KingdomQuestKQHBatExternalKind.ItemErase,
                    "itemerase all Maul.") },
                { 121, SiteTemplate(
                    121, "LooterDead",
                    KingdomQuestKQHBatExternalKind.ItemDrop,
                    "itemdrop InterruptArg Maul 1000000.") },
                { 139, SiteTemplate(
                    139, "QuestEnd",
                    KingdomQuestKQHBatExternalKind.BattleStop,
                    "battlestop PK.") },
                { 140, SiteTemplate(
                    140, "QuestEnd",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn30\".") },
                { 142, SiteTemplate(
                    142, "QuestEnd",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn20\".") },
                { 144, SiteTemplate(
                    144, "QuestEnd",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn10\".") },
                { 146, SiteTemplate(
                    146, "QuestEnd",
                    KingdomQuestKQHBatExternalKind.Broadcast,
                    "broadcast all \"KQReturn5\".") },
            };

        private static readonly Dictionary<string, string> LinkCommands =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "KQ/KQHBat1", "linkto all \"Eld\" \"Eld\" 17214 13445." },
                { "KQ/KQHBat2", "linkto all \"Eld\" \"Eld\" 17214 13445." },
                { "KQ/KQHBat3", "linkto all \"Urg\" \"Urg\" 6293 5477." },
                { "KQ/KQHBat4", "linkto all \"Urg_Alruin\" \"Urg_Alruin\" 6120 10286." },
                { "KQ/KQHBat5", "linkto all \"Adl\" \"Adl\" 11674 9329." },
            };

        public static bool IsSupportedScript(string scriptLanguage)
        {
            return !string.IsNullOrEmpty(scriptLanguage) &&
                ScriptLanguages.Contains(scriptLanguage);
        }

        public static bool TryResolve(
            string scriptLanguage,
            int canonicalLine,
            string commandText,
            out KingdomQuestKQHBatExternalSourceSite site)
        {
            site = null;
            if (!IsSupportedScript(scriptLanguage) ||
                string.IsNullOrWhiteSpace(commandText))
                return false;

            Template template;
            string expectedCommand;
            string topLevelBlock;
            KingdomQuestKQHBatExternalKind kind;

            if (canonicalLine == 91 || canonicalLine == 148)
            {
                if (!LinkCommands.TryGetValue(
                        scriptLanguage, out expectedCommand))
                    return false;
                topLevelBlock = canonicalLine == 91 ? "main" : "QuestEnd";
                kind = KingdomQuestKQHBatExternalKind.LinkTo;
            }
            else
            {
                if (!CommonSites.TryGetValue(canonicalLine, out template) ||
                    template == null)
                    return false;
                expectedCommand = template.CommandText;
                topLevelBlock = template.TopLevelBlock;
                kind = template.Kind;
            }

            if (!string.Equals(
                    commandText.Trim(),
                    expectedCommand,
                    StringComparison.Ordinal))
                return false;

            site = new KingdomQuestKQHBatExternalSourceSite(
                scriptLanguage,
                canonicalLine,
                topLevelBlock,
                kind,
                expectedCommand);
            return true;
        }

        private static Template SiteTemplate(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestKQHBatExternalKind kind,
            string commandText)
        {
            return new Template
            {
                CanonicalLine = canonicalLine,
                TopLevelBlock = topLevelBlock ?? string.Empty,
                Kind = kind,
                CommandText = commandText ?? string.Empty,
            };
        }
    }
}
