using System;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Resolves the native KingdomQuestMap MapBase identity to the emulator's
    /// already source-backed MapInfo catalog.
    ///
    /// The supplied NA2016 KingdomQuest.shn snapshot has exactly one active
    /// MapLink per definition. This is a provenance-locked corpus property, not
    /// a claim that the native protocol is globally single-map.
    /// </summary>
    public static class KingdomQuestMapRouteResolver
    {
        public static bool TryResolveScheduledMap(ushort kqId, out MapInfo mapInfo)
        {
            mapInfo = null;
            DataProvider provider = DataProvider.Instance;
            if (provider == null ||
                provider.KingdomQuestSourceDefinitions == null ||
                provider.KingdomQuestSourceMaps == null)
                return false;

            KingdomQuestSourceDefinition source = null;
            for (int i = 0; i < provider.KingdomQuestSourceDefinitions.Count; i++)
            {
                KingdomQuestSourceDefinition candidate =
                    provider.KingdomQuestSourceDefinitions[i];
                if (candidate.ID >= 0 && (ushort)candidate.ID == kqId)
                {
                    // Native AllocMapLink uses the first matching source row.
                    source = candidate;
                    break;
                }
            }

            if (source == null || source.MapLinkColumns == null ||
                source.MapLinkColumns.Count != 4)
                return false;

            int sourceMapIndex = -1;
            for (int i = 0; i < source.MapLinkColumns.Count; i++)
            {
                short candidate = source.MapLinkColumns[i];
                if (candidate == -1)
                    continue;
                if (candidate < 0 || sourceMapIndex >= 0)
                    return false;
                sourceMapIndex = candidate;
            }

            if (sourceMapIndex < 0 ||
                sourceMapIndex >= provider.KingdomQuestSourceMaps.Count)
                return false;

            KingdomQuestMapSourceRow sourceMap =
                provider.KingdomQuestSourceMaps[sourceMapIndex];
            if (sourceMap.SourceRow != (uint)sourceMapIndex)
                return false;

            return TryResolveBaseMap(sourceMap.BaseMap, out mapInfo);
        }

        public static bool TryResolveAllocatedMap(
            KingdomQuestProtocolInfo definition,
            out MapInfo mapInfo,
            out KingdomQuestMapProtocolInfo mapLink)
        {
            mapInfo = null;
            mapLink = null;
            if (definition == null ||
                definition.MapLink == null ||
                definition.MapLink.Length != 4)
                return false;

            for (int i = 0; i < definition.MapLink.Length; i++)
            {
                KingdomQuestMapProtocolInfo candidate = definition.MapLink[i];
                if (candidate == null)
                    return false;

                bool populated =
                    !string.IsNullOrEmpty(candidate.MapBase) ||
                    !string.IsNullOrEmpty(candidate.MapName);
                if (!populated)
                    continue;

                if (mapLink != null ||
                    string.IsNullOrEmpty(candidate.MapBase) ||
                    string.IsNullOrEmpty(candidate.MapName))
                    return false;

                mapLink = candidate;
            }

            return mapLink != null &&
                TryResolveBaseMap(mapLink.MapBase, out mapInfo);
        }

        private static bool TryResolveBaseMap(string mapBase, out MapInfo mapInfo)
        {
            mapInfo = null;
            if (string.IsNullOrEmpty(mapBase) ||
                DataProvider.Instance == null ||
                DataProvider.Instance.KingdomQuestMaps == null)
                return false;

            foreach (MapInfo candidate in
                DataProvider.Instance.KingdomQuestMaps.Values)
            {
                if (!string.Equals(
                        candidate.ShortName, mapBase, StringComparison.Ordinal))
                    continue;

                if (mapInfo != null)
                {
                    mapInfo = null;
                    return false;
                }
                mapInfo = candidate;
            }

            return mapInfo != null;
        }
    }
}
