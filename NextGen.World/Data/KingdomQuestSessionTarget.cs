using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Explicit server-internal routing target for one live native KQ Handle.
    /// The 32-bit World Handle is intentionally kept separate from the Zone
    /// map's internal short InstanceID.
    /// </summary>
    public sealed class KingdomQuestSessionTarget
    {
        public uint Handle { get; private set; }
        public ushort MapID { get; private set; }
        public short MapInstance { get; private set; }
        public byte NativeMapIndex { get; private set; }
        public string NativeMapBase { get; private set; }
        public string NativeMapName { get; private set; }

        internal KingdomQuestSessionTarget(uint handle, ushort mapId, short mapInstance)
            : this(handle, mapId, mapInstance, 0, string.Empty, string.Empty)
        {
        }

        internal KingdomQuestSessionTarget(uint handle, ushort mapId, short mapInstance,
            byte nativeMapIndex, string nativeMapBase, string nativeMapName)
        {
            Handle = handle;
            MapID = mapId;
            MapInstance = mapInstance;
            NativeMapIndex = nativeMapIndex;
            NativeMapBase = nativeMapBase ?? string.Empty;
            NativeMapName = nativeMapName ?? string.Empty;
        }
    }

    /// <summary>
    /// Thread-safe explicit mapping from native World KQ Handles to
    /// source-backed map IDs and server-internal map instances.
    ///
    /// This registry allocates nothing and infers nothing. A later
    /// source-backed scheduler/session creator must provide all three values.
    /// </summary>
    public static class KingdomQuestSessionTargetRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestSessionTarget> ByHandle =
            new Dictionary<uint, KingdomQuestSessionTarget>();
        private static readonly Dictionary<Tuple<ushort, short>, uint> ByMapInstance =
            new Dictionary<Tuple<ushort, short>, uint>();
        private static readonly Dictionary<ushort, int> NextInternalInstanceByMap =
            new Dictionary<ushort, int>();

        public static bool TryCreate(uint handle, ushort mapId, short mapInstance,
            out KingdomQuestSessionTarget target)
        {
            target = null;
            if (mapInstance < 0 ||
                DataProvider.Instance == null ||
                DataProvider.Instance.KingdomQuestMaps == null ||
                !DataProvider.Instance.KingdomQuestMaps.ContainsKey(mapId))
                return false;

            var key = Tuple.Create(mapId, mapInstance);
            lock (Sync)
            {
                if (ByHandle.ContainsKey(handle) || ByMapInstance.ContainsKey(key))
                    return false;

                target = new KingdomQuestSessionTarget(handle, mapId, mapInstance);
                ByHandle.Add(handle, target);
                ByMapInstance.Add(key, handle);
                return true;
            }
        }

        public static bool TryAllocateNative(
            uint handle,
            KingdomQuestProtocolInfo definition,
            out KingdomQuestSessionTarget target)
        {
            target = null;
            if (definition == null || definition.Handle != handle)
                return false;

            MapInfo mapInfo;
            KingdomQuestMapProtocolInfo mapLink;
            if (!KingdomQuestMapRouteResolver.TryResolveAllocatedMap(
                    definition, out mapInfo, out mapLink))
                return false;

            lock (Sync)
            {
                if (ByHandle.ContainsKey(handle))
                    return false;

                int next;
                if (!NextInternalInstanceByMap.TryGetValue(mapInfo.ID, out next) ||
                    next < 1)
                    next = 1;

                // Instance 0 is the emulator's loaded/base map. Dynamic KQ
                // instances live in a separate monotonically allocated range.
                // This counter is intentionally unrelated to Handle/MapIndex.
                while (next <= short.MaxValue &&
                    ByMapInstance.ContainsKey(
                        Tuple.Create(mapInfo.ID, (short)next)))
                    next++;

                if (next > short.MaxValue)
                    return false;

                short mapInstance = (short)next;
                var key = Tuple.Create(mapInfo.ID, mapInstance);
                target = new KingdomQuestSessionTarget(
                    handle,
                    mapInfo.ID,
                    mapInstance,
                    mapLink.MapIndex,
                    mapLink.MapBase,
                    mapLink.MapName);
                ByHandle.Add(handle, target);
                ByMapInstance.Add(key, handle);
                NextInternalInstanceByMap[mapInfo.ID] = next + 1;
                return true;
            }
        }

        public static bool TryGet(uint handle, out KingdomQuestSessionTarget target)
        {
            lock (Sync)
            {
                KingdomQuestSessionTarget current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    target = null;
                    return false;
                }

                target = new KingdomQuestSessionTarget(
                    current.Handle, current.MapID, current.MapInstance,
                    current.NativeMapIndex, current.NativeMapBase, current.NativeMapName);
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
            {
                KingdomQuestSessionTarget current;
                if (!ByHandle.TryGetValue(handle, out current))
                    return false;

                ByHandle.Remove(handle);
                ByMapInstance.Remove(Tuple.Create(current.MapID, current.MapInstance));
                return true;
            }
        }

        public static IReadOnlyList<KingdomQuestSessionTarget> Snapshot()
        {
            lock (Sync)
                return ByHandle.Values
                    .OrderBy(v => v.Handle)
                    .Select(v => new KingdomQuestSessionTarget(
                        v.Handle, v.MapID, v.MapInstance,
                        v.NativeMapIndex, v.NativeMapBase, v.NativeMapName))
                    .ToList()
                    .AsReadOnly();
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ByHandle.Clear();
                ByMapInstance.Clear();
                NextInternalInstanceByMap.Clear();
            }
        }
    }
}
