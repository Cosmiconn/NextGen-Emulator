using System;
using System.Collections.Generic;
using System.Linq;

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

        internal KingdomQuestSessionTarget(uint handle, ushort mapId, short mapInstance)
        {
            Handle = handle;
            MapID = mapId;
            MapInstance = mapInstance;
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
                    current.Handle, current.MapID, current.MapInstance);
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
                        v.Handle, v.MapID, v.MapInstance))
                    .ToList()
                    .AsReadOnly();
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ByHandle.Clear();
                ByMapInstance.Clear();
            }
        }
    }
}
