using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Source projection of the KQ subset of the original Zone
    /// ScenarioBookShelf after sbs_LoadScripts has processed World/PineScript.txt.
    ///
    /// Zone.exe ScenarioBookShelf::sbs_Read tries the .ps path first and the
    /// .lua path second. When either file exists it allocates the matching
    /// ScenarioBook, calls its virtual sb_Load method, ignores that bool return,
    /// and still inserts the key/object into the shelf. Only absence of both
    /// files makes sbs_Read return false.
    ///
    /// This class therefore models MAKE-time sbs_GetScenarioBook membership
    /// for the exact supplied source corpus only. It does not parse or execute
    /// PineScript/Lua and does not model CinemaComplex film execution.
    /// </summary>
    public static class KingdomQuestScenarioBookShelfSource
    {
        public const string SourceArchiveSha256 =
            "b83bf92c7193578a772fcebf4d8b7c8c2a9a642cf0d50d33506f77a75b0e211d";
        public const string ZoneExeSha256 =
            "db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5";
        public const string ScriptCatalogSha256 =
            "8ba15c6d7a5d14f1bd01f94a8403d8a9652868e15e0eee7a7730504c7754440c";
        public const int KqCatalogKeyCount = 32;
        public const int KqUsedBySuppliedDefinitions = 27;

        private static readonly string[] KqCatalogKeys =
        {
            "KQ/KingSlime/KingSlime",
            "KQ/MaraPirate/MaraPirate",
            "KQ/UnderHall",
            "KQ/GoldHill/GoldHill",
            "KQ/MiniDragon/MiniDragon",
            "KQ/Kingkong/Kingkong",
            "KQ/Honeying",
            "KQ/UnderHall2",
            "KQ/GordonMaster",
            "KQ/HMiniDragon/HMiniDragon",
            "KQ/KQHBat1",
            "KQ/KQHBat2",
            "KQ/KQHBat3",
            "KQ/KQHBat4",
            "KQ/LegendOfBijou/LegendOfBijou",
            "KQ/AntiHenis/AntiHenis",
            "KQ/KQHBat5",
            "KQ/KDMine/KDMine",
            "KQ/KDEgg/KDEgg",
            "KQ/KDSpring/KDSpring",
            "KQ/KDArena/KDArena1",
            "KQ/KDArena/KDArena2",
            "KQ/KDArena/KDArena3",
            "KQ/KDArena/KDArena4",
            "KQ/KDArena/KDArena5",
            "KQ/KDArena/KDArena6",
            "KQ/EmperorSlime/EmperorSlime",
            "KQ/KDSoccer/KDSoccer",
            "KQ/KDWater/KDWater",
            "KQ/KDSoccer_W/KDSoccer_W",
            "KQ/KDFargels/KDFargels",
            "KQ/KDCake/KDCake",
        };

        private static readonly HashSet<string> KqCatalogKeySet =
            new HashSet<string>(KqCatalogKeys, StringComparer.Ordinal);

        public static IReadOnlyList<string> Keys
        {
            get { return Array.AsReadOnly(KqCatalogKeys); }
        }

        public static bool ContainsSourceBackedScenarioBook(
            string scriptLanguage)
        {
            return !string.IsNullOrEmpty(scriptLanguage) &&
                KqCatalogKeySet.Contains(scriptLanguage);
        }
    }
}
