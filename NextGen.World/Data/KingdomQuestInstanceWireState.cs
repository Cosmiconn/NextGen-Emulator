using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.World.Data
{
    /// <summary>
    /// Wire-level state carried by the captured SH22 type-37/type-38 packets.
    /// The two ushort fields deliberately keep neutral names until their
    /// source-level KQ definition fields are correlated.
    /// </summary>
    public sealed class KingdomQuestInstanceWireState
    {
        public uint InstanceID { get; private set; }
        public ushort StateValue { get; private set; }
        public ushort TypeValue { get; private set; }

        public KingdomQuestInstanceWireState(uint instanceId, ushort stateValue, ushort typeValue)
        {
            InstanceID = instanceId;
            StateValue = stateValue;
            TypeValue = typeValue;
        }
    }

    /// <summary>
    /// Thread-safe registry for live KQ instance wire state. This class contains
    /// no scheduler or guessed lifecycle rules; authoritative KQ definitions
    /// will create/update entries later.
    /// </summary>
    public static class KingdomQuestInstanceRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestInstanceWireState> Instances =
            new Dictionary<uint, KingdomQuestInstanceWireState>();

        public static void Upsert(uint instanceId, ushort stateValue, ushort typeValue)
        {
            lock (Sync)
                Instances[instanceId] = new KingdomQuestInstanceWireState(instanceId, stateValue, typeValue);
        }

        public static bool Remove(uint instanceId)
        {
            lock (Sync)
                return Instances.Remove(instanceId);
        }

        public static bool TryGet(uint instanceId, out KingdomQuestInstanceWireState state)
        {
            lock (Sync)
            {
                KingdomQuestInstanceWireState current;
                if (!Instances.TryGetValue(instanceId, out current))
                {
                    state = null;
                    return false;
                }
                state = new KingdomQuestInstanceWireState(
                    current.InstanceID, current.StateValue, current.TypeValue);
                return true;
            }
        }

        public static IReadOnlyList<KingdomQuestInstanceWireState> Snapshot()
        {
            lock (Sync)
                return Instances.Values
                    .OrderBy(v => v.InstanceID)
                    .Select(v => new KingdomQuestInstanceWireState(
                        v.InstanceID, v.StateValue, v.TypeValue))
                    .ToList()
                    .AsReadOnly();
        }

        public static void Clear()
        {
            lock (Sync)
                Instances.Clear();
        }
    }
}
