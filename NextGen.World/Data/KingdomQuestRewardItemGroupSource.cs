using System;
using System.Collections.Generic;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact KQ-used ItemGroupClassifier keys recovered from the supplied
    /// ItemInfoServer.shn.
    ///
    /// Zone.exe ItemGroupClassifier::igc_Load calls igc_Store only for
    /// ItemInfoServer DropGroupA (record offset 0x39) and DropGroupB
    /// (record offset 0x61). This is intentionally only the KQ-used group-only
    /// key subset; candidate CardDeck rows are not modeled here.
    /// </summary>
    public static class KingdomQuestRewardItemGroupSource
    {
        public const string ItemInfoServerSha256 =
            "d8cf2b411783822908e6ecbfc833b4ae104d2f3ece1b3cc2250aafa5aa714c10";
        public const string ItemInfoSha256 =
            "7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494";

        public const int KqUsedItemRewardHandles = 247;
        public const int DirectItemHandles = 161;
        public const int GroupResolvedHandles = 82;
        public const int NativeMissHandles = 4;
        public const int DirectArguments = 95;
        public const int GroupOnlyArguments = 33;
        public const int NativeMissArguments = 3;
        public const int DirectAndGroupNameOverlap = 59;
        public const int UsedGroupCandidateRows = 791;
        public const int UsedGroupCandidateUniqueItemIds = 790;

        private static readonly string[] GroupOnlyKeys =
        {
            "BestProduct",
            "GordonMasterNewReward",
            "HenneathNewReward",
            "HighBeast",
            "HighCarcass",
            "HighDust",
            "HighKylin",
            "HighProduct",
            "LostMiniNewReward",
            "MaraNewReward",
            "NamedArmor7",
            "NamedWeapon13",
            "NamedWeapon14",
            "NamedWeapon15",
            "NamedWeapon4",
            "NamedWeapon8",
            "NamedWeapon9",
            "NorProduct",
            "P_KQHBAT1",
            "P_KQHBAT2",
            "P_KQHBAT3",
            "P_KQHBAT4",
            "P_KQHBAT5",
            "RareWeapon03",
            "SlimeJelly",
            "SpUpsource13",
            "SpUpsource14",
            "SpUpsource15",
            "Upsource13",
            "Upsource14",
            "Upsource15",
            "Weapon3",
            "Weapon6",
        };

        private static readonly string[] NativeMissKeys =
        {
            "BestHighProduct",
            "GiantHoneyingNewReward",
            "NamedOP3Armor6",
        };

        public static ISet<string> CreateGroupOnlyKeySet()
        {
            return new HashSet<string>(
                GroupOnlyKeys, StringComparer.Ordinal);
        }

        public static ISet<string> CreateNativeMissKeySet()
        {
            return new HashSet<string>(
                NativeMissKeys, StringComparer.Ordinal);
        }
    }
}
