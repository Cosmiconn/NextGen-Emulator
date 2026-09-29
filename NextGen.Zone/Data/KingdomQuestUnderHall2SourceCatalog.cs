using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHall2MapSourceRef
    {
        public ushort MapId { get; private set; }
        public string MapName { get; private set; }
        public string DisplayName { get; private set; }
        public int RegenX { get; private set; }
        public int RegenY { get; private set; }
        public string MapFolderName { get; private set; }

        internal KingdomQuestUnderHall2MapSourceRef(
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

    public sealed class KingdomQuestUnderHall2MobSourceRef
    {
        public ushort MobId { get; private set; }
        public string InxName { get; private set; }
        public string DisplayName { get; private set; }

        internal KingdomQuestUnderHall2MobSourceRef(
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
    /// Exact MapInfo/MobInfo correlations used by the six-family
    /// KQ/UnderHall2 source slice. These values are source identity only.
    ///
    /// In particular Uruga's MapInfo regen point (6293,5477) is not equated
    /// with UnderHall2's explicit link target (5835,6397).
    /// </summary>
    public static class KingdomQuestUnderHall2SourceCatalog
    {
        public const int CorrelatedMapCount = 2;
        public const int CorrelatedMobCount = 13;
        public const ushort ElderineMapId = 9;
        public const ushort UrugaMapId = 17;

        private static readonly Dictionary<string, KingdomQuestUnderHall2MapSourceRef>
            Maps =
                new Dictionary<string, KingdomQuestUnderHall2MapSourceRef>(
                    StringComparer.Ordinal)
                {
                    { "Eld", new KingdomQuestUnderHall2MapSourceRef(
                        ElderineMapId, "Eld", "Elderine",
                        17214, 13445, "Eld") },
                    { "Urg", new KingdomQuestUnderHall2MapSourceRef(
                        UrugaMapId, "Urg", "Uruga",
                        6293, 5477, "Urg") },
                };

        private static readonly Dictionary<string, KingdomQuestUnderHall2MobSourceRef>
            Mobs =
                new Dictionary<string, KingdomQuestUnderHall2MobSourceRef>(
                    StringComparer.Ordinal)
                {
                    { "KQ_GB_Spider", new KingdomQuestUnderHall2MobSourceRef(
                        1158, "KQ_GB_Spider", "Great Spider") },
                    { "KQ_M_Spider", new KingdomQuestUnderHall2MobSourceRef(
                        1159, "KQ_M_Spider", "Mini Spider") },
                    { "KQ_U_Spider01", new KingdomQuestUnderHall2MobSourceRef(
                        1130, "KQ_U_Spider01", "Fighter Spider") },
                    { "KQ_U_Spider02", new KingdomQuestUnderHall2MobSourceRef(
                        1131, "KQ_U_Spider02", "Violent Spider") },
                    { "KQ_U_Spider03", new KingdomQuestUnderHall2MobSourceRef(
                        1132, "KQ_U_Spider03", "Speedy Spider") },
                    { "KQ_U_Spider04", new KingdomQuestUnderHall2MobSourceRef(
                        1137, "KQ_U_Spider04", "Fierce Spider") },
                    { "KQ_U_Spider05", new KingdomQuestUnderHall2MobSourceRef(
                        1134, "KQ_U_Spider05", "Cannibal Spider") },
                    { "KQ_U_AMageBook", new KingdomQuestUnderHall2MobSourceRef(
                        1133, "KQ_U_AMageBook", "Archmage Book") },
                    { "KQ_U_Lvivi", new KingdomQuestUnderHall2MobSourceRef(
                        1135, "KQ_U_Lvivi", "Lightning Vivi") },
                    { "KQ_U_Greenky", new KingdomQuestUnderHall2MobSourceRef(
                        1136, "KQ_U_Greenky", "Merciless Greenky") },
                    { "KQ_U_TombRaider", new KingdomQuestUnderHall2MobSourceRef(
                        1139, "KQ_U_TombRaider", "Madness Grave Robber") },
                    { "KQ_U_Uspider", new KingdomQuestUnderHall2MobSourceRef(
                        1140, "KQ_U_Uspider", "Ultra Spider") },
                    { "Daliy_Check", new KingdomQuestUnderHall2MobSourceRef(
                        50000, "Daliy_Check", "Kingdom Quest Clear ") },
                };

        public static bool TryGetMap(
            string mapName,
            out KingdomQuestUnderHall2MapSourceRef map)
        {
            map = null;
            return !string.IsNullOrEmpty(mapName) &&
                Maps.TryGetValue(mapName, out map);
        }

        public static bool TryGetMob(
            string inxName,
            out KingdomQuestUnderHall2MobSourceRef mob)
        {
            mob = null;
            return !string.IsNullOrEmpty(inxName) &&
                Mobs.TryGetValue(inxName, out mob);
        }

        public static IReadOnlyDictionary<string, KingdomQuestUnderHall2MapSourceRef>
            SnapshotMaps()
        {
            return new Dictionary<string, KingdomQuestUnderHall2MapSourceRef>(
                Maps, StringComparer.Ordinal);
        }

        public static IReadOnlyDictionary<string, KingdomQuestUnderHall2MobSourceRef>
            SnapshotMobs()
        {
            return new Dictionary<string, KingdomQuestUnderHall2MobSourceRef>(
                Mobs, StringComparer.Ordinal);
        }
    }
}
