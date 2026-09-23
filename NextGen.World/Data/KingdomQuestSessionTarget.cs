using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.World.Data
{
    /// <summary>
    /// Explicit server-internal routing target for one live KQ wire instance.
    /// The captured 32-bit InstanceID is intentionally kept separate from the
    /// Zone map's internal short InstanceID.
    /// </summary>
    public sealed class KingdomQuestSessionTarget
    {
        public uint InstanceID { get; private set; }
        public ushort MapID { get; private set; }
        public short MapInstance { get; private set; }

        internal KingdomQuestSessionTarget(uint instanceId, ushort mapId, short mapInstance)
        {
            InstanceID = instanceId;
            MapID = mapId;
            MapInstance = mapInstance;
        }
    }

    /// <summary>
    /// Thread-safe explicit mapping from captured World KQ instance IDs to
    /// source-backed map IDs and server-internal map instances.
    ///
    /// This registry allocates nothing and infers nothing. A later
    /// source-backed scheduler/session creator must provide all three values.
    /// </summary>
    public static class KingdomQuestSessionTargetRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestSessionTarget> ByInstance =
            new Dictionary<uint, KingdomQuestSessionTarget>();
        private static readonly Dictionary<Tuple<ushort, short>, uint> ByMapInstance =
            new Dictionary<Tuple<ushort, short>, uint>();

        public static bool TryCreate(uint instanceId, ushort mapId, short mapInstance,
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
                if (ByInstance.ContainsKey(instanceId) || ByMapInstance.ContainsKey(key))
                    return false;

                target = new KingdomQuestSessionTarget(instanceId, mapId, mapInstance);
                ByInstance.Add(instanceId, target);
                ByMapInstance.Add(key, instanceId);
                return true;
            }
        }

        public static bool TryGet(uint instanceId, out KingdomQuestSessionTarget target)
        {
            lock (Sync)
            {
                KingdomQuestSessionTarget current;
                if (!ByInstance.TryGetValue(instanceId, out current))
                {
                    target = null;
                    return false;
                }

                target = new KingdomQuestSessionTarget(
                    current.InstanceID, current.MapID, current.MapInstance);
                return true;
            }
        }

        public static bool Remove(uint instanceId)
        {
            lock (Sync)
            {
                KingdomQuestSessionTarget current;
                if (!ByInstance.TryGetValue(instanceId, out current))
                    return false;

                ByInstance.Remove(instanceId);
                ByMapInstance.Remove(Tuple.Create(current.MapID, current.MapInstance));
                return true;
            }
        }

        public static IReadOnlyList<KingdomQuestSessionTarget> Snapshot()
        {
            lock (Sync)
                return ByInstance.Values
                    .OrderBy(v => v.InstanceID)
                    .Select(v => new KingdomQuestSessionTarget(
                        v.InstanceID, v.MapID, v.MapInstance))
                    .ToList()
                    .AsReadOnly();
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ByInstance.Clear();
                ByMapInstance.Clear();
            }
        }
    }
}
