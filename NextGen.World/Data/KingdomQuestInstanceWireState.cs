using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.World.Data
{
    /// <summary>
    /// Live World-side KQ state for the client-visible KQ packet family.
    /// Field names mirror the original 2016 protocol structures:
    /// KQ_UPDATE_ITEMS, KQ_JOINING_ALARM_INFO and KQ_STATUS_ACK.
    /// </summary>
    public sealed class KingdomQuestInstanceWireState
    {
        private readonly List<string> joinerNames;

        public uint Handle { get; private set; }
        public byte Status { get; private set; }
        public ushort ID { get; private set; }
        public byte MinLevel { get; private set; }
        public byte MaxLevel { get; private set; }
        public IReadOnlyList<string> JoinerNames { get { return joinerNames.AsReadOnly(); } }

        public KingdomQuestInstanceWireState(uint handle, byte status, ushort id,
            byte minLevel, byte maxLevel, IEnumerable<string> names = null)
        {
            Handle = handle;
            Status = status;
            ID = id;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
            joinerNames = names == null ? new List<string>() : new List<string>(names);
            if (joinerNames.Count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("names");
        }
    }

    /// <summary>
    /// Thread-safe registry. It does not allocate handles, schedule KQs or
    /// infer status values; a source-backed session owner supplies them.
    /// </summary>
    public static class KingdomQuestInstanceRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestInstanceWireState> Instances =
            new Dictionary<uint, KingdomQuestInstanceWireState>();

        public static void Upsert(uint handle, byte status, ushort id,
            byte minLevel, byte maxLevel)
        {
            lock (Sync)
            {
                KingdomQuestInstanceWireState current;
                IEnumerable<string> names = Instances.TryGetValue(handle, out current)
                    ? current.JoinerNames
                    : null;
                Instances[handle] = new KingdomQuestInstanceWireState(
                    handle, status, id, minLevel, maxLevel, names);
            }
        }

        public static bool SetStatus(uint handle, byte status)
        {
            lock (Sync)
            {
                KingdomQuestInstanceWireState current;
                if (!Instances.TryGetValue(handle, out current))
                    return false;

                Instances[handle] = new KingdomQuestInstanceWireState(
                    current.Handle, status, current.ID,
                    current.MinLevel, current.MaxLevel, current.JoinerNames);
                return true;
            }
        }

        public static bool SetJoiners(uint handle, IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException("names");
            lock (Sync)
            {
                KingdomQuestInstanceWireState current;
                if (!Instances.TryGetValue(handle, out current))
                    return false;

                Instances[handle] = new KingdomQuestInstanceWireState(
                    current.Handle, current.Status, current.ID,
                    current.MinLevel, current.MaxLevel, names);
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return Instances.Remove(handle);
        }

        public static bool TryGet(uint handle, out KingdomQuestInstanceWireState state)
        {
            lock (Sync)
            {
                KingdomQuestInstanceWireState current;
                if (!Instances.TryGetValue(handle, out current))
                {
                    state = null;
                    return false;
                }

                state = new KingdomQuestInstanceWireState(
                    current.Handle, current.Status, current.ID,
                    current.MinLevel, current.MaxLevel, current.JoinerNames);
                return true;
            }
        }

        public static IReadOnlyList<KingdomQuestInstanceWireState> Snapshot()
        {
            lock (Sync)
                return Instances.Values
                    .OrderBy(v => v.Handle)
                    .Select(v => new KingdomQuestInstanceWireState(
                        v.Handle, v.Status, v.ID, v.MinLevel, v.MaxLevel,
                        v.JoinerNames))
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
