using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHallMapSourceRef
    {
        public ushort MapId { get; private set; }
        public string MapName { get; private set; }
        public string DisplayName { get; private set; }
        public int RegenX { get; private set; }
        public int RegenY { get; private set; }
        public string MapFolderName { get; private set; }

        internal KingdomQuestUnderHallMapSourceRef(
            ushort mapId,
            string mapName,
            string displayName,
            int regenX,
            int regenY,
            string mapFolderName)
        {
            MapId = mapId;
            MapName = mapName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RegenX = regenX;
            RegenY = regenY;
            MapFolderName = mapFolderName ?? string.Empty;
        }
    }

    public sealed class KingdomQuestUnderHallMobSourceRef
    {
        public ushort MobId { get; private set; }
        public string InxName { get; private set; }
        public string DisplayName { get; private set; }

        internal KingdomQuestUnderHallMobSourceRef(
            ushort mobId,
            string inxName,
            string displayName)
        {
            MobId = mobId;
            InxName = inxName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    /// <summary>
    /// Source-only correlations for identifiers that occur in the exact
    /// KQ/UnderHall Pine command forms.
    ///
    /// These are direct projections of the checked-in original MapInfo.shn and
    /// MobInfo.shn rows. They do not assert how a Pine command consumes the
    /// correlated row; native link/spawn/regen side effects remain separate.
    /// </summary>
    public static class KingdomQuestUnderHallSourceCatalog
    {
        public const ushort ElderineMapId = 9;
        public const int CorrelatedMobCount = 11;

        private static readonly KingdomQuestUnderHallMapSourceRef Elderine =
            new KingdomQuestUnderHallMapSourceRef(
                ElderineMapId,
                "Eld",
                "Elderine",
                17214,
                13445,
                "Eld");

        private static readonly Dictionary<string, KingdomQuestUnderHallMobSourceRef>
            Mobs =
                new Dictionary<string, KingdomQuestUnderHallMobSourceRef>(
                    StringComparer.Ordinal)
                {
                    { "KQ_BossRobo",
                        new KingdomQuestUnderHallMobSourceRef(
                            1068, "KQ_BossRobo", "Millennium Robo") },
                    { "KQ_DesertWolf",
                        new KingdomQuestUnderHallMobSourceRef(
                            1051, "KQ_DesertWolf", "Carnival Wolf") },
                    { "KQ_FireViVi",
                        new KingdomQuestUnderHallMobSourceRef(
                            1066, "KQ_FireViVi", "Angry Fire ViVi") },
                    { "KQ_GiantMushRoom",
                        new KingdomQuestUnderHallMobSourceRef(
                            1046, "KQ_GiantMushRoom", "Giant Mushroom") },
                    { "KQ_RapidBoar",
                        new KingdomQuestUnderHallMobSourceRef(
                            1058, "KQ_RapidBoar", "Rapid Boar") },
                    { "KQ_SkelArcher",
                        new KingdomQuestUnderHallMobSourceRef(
                            1056, "KQ_SkelArcher", "Skeleton Archer") },
                    { "KQ_SkelKnight",
                        new KingdomQuestUnderHallMobSourceRef(
                            1060, "KQ_SkelKnight", "Dark Skeleton Knight") },
                    { "KQ_SkelWarrior",
                        new KingdomQuestUnderHallMobSourceRef(
                            1059, "KQ_SkelWarrior",
                            "Powerful Skeleton Warrior") },
                    { "KQ_Skeleton",
                        new KingdomQuestUnderHallMobSourceRef(
                            1054, "KQ_Skeleton", "Brave Skeleton") },
                    { "KQ_WildKebing",
                        new KingdomQuestUnderHallMobSourceRef(
                            1055, "KQ_WildKebing", "Cave Kebing") },
                    { "KQ_Zombie",
                        new KingdomQuestUnderHallMobSourceRef(
                            1065, "KQ_Zombie", "Madness Zombie") },
                };

        public static KingdomQuestUnderHallMapSourceRef GetElderine()
        {
            return Elderine;
        }

        public static bool TryGetMob(
            string inxName,
            out KingdomQuestUnderHallMobSourceRef mob)
        {
            mob = null;
            return !string.IsNullOrEmpty(inxName) &&
                Mobs.TryGetValue(inxName, out mob);
        }

        public static IReadOnlyDictionary<string, KingdomQuestUnderHallMobSourceRef>
            SnapshotMobs()
        {
            return new Dictionary<string, KingdomQuestUnderHallMobSourceRef>(
                Mobs, StringComparer.Ordinal);
        }
    }
}
