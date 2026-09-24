using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Game;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestZoneLifecycleState : byte
    {
        Made = 1,
        Started = 2,
        Ended = 3,
    }

    public sealed class KingdomQuestZoneRuntimeState
    {
        public uint Handle { get; private set; }
        public ushort MapID { get; private set; }
        public short MapInstance { get; private set; }
        public KingdomQuestZoneLifecycleState State { get; private set; }
        public KingdomQuestProtocolInfo Definition { get; private set; }
        public IReadOnlyList<KingdomQuestZoneJoinerInfo> Joiners { get; private set; }

        internal KingdomQuestZoneRuntimeState(uint handle, ushort mapId, short mapInstance,
            KingdomQuestZoneLifecycleState state, KingdomQuestProtocolInfo definition,
            IEnumerable<KingdomQuestZoneJoinerInfo> joiners)
        {
            Handle = handle;
            MapID = mapId;
            MapInstance = mapInstance;
            State = state;
            Definition = CloneDefinition(definition);
            Joiners = CloneJoiners(joiners);
        }

        internal KingdomQuestZoneRuntimeState Clone()
        {
            return new KingdomQuestZoneRuntimeState(
                Handle, MapID, MapInstance, State, Definition, Joiners);
        }

        private static KingdomQuestProtocolInfo CloneDefinition(
            KingdomQuestProtocolInfo source)
        {
            if (source == null) throw new ArgumentNullException("source");
            using (var packet = new Packet((ushort)0x580D))
            {
                source.Write(packet);
                using (var reader = new Packet(packet.ToNormalArray()))
                {
                    KingdomQuestProtocolInfo clone;
                    if (!KingdomQuestProtocolInfo.TryRead(reader, out clone) ||
                        reader.Remaining != 0)
                        throw new InvalidOperationException(
                            "Failed to clone native PROTO_KQ_INFO.");
                    return clone;
                }
            }
        }

        private static IReadOnlyList<KingdomQuestZoneJoinerInfo> CloneJoiners(
            IEnumerable<KingdomQuestZoneJoinerInfo> source)
        {
            if (source == null)
                return new List<KingdomQuestZoneJoinerInfo>().AsReadOnly();

            return source.Select(v =>
            {
                if (v == null) throw new ArgumentException(
                    "KQ Zone joiner entry is null.", "source");
                return new KingdomQuestZoneJoinerInfo
                {
                    CharacterNumber = v.CharacterNumber,
                    TeamType = v.TeamType,
                };
            }).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Zone-local KQ state driven only by validated native W2Z lifecycle bodies.
    /// It does not allocate Handles, choose maps/instances, assign teams or
    /// synthesize MAKE_ACK errors.
    /// </summary>
    public static class KingdomQuestZoneRuntimeRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestZoneRuntimeState> ByHandle =
            new Dictionary<uint, KingdomQuestZoneRuntimeState>();

        public static bool TryMake(KingdomQuestProtocolInfo definition,
            ushort mapId, short mapInstance)
        {
            if (definition == null || mapInstance < 0 ||
                DataProvider.Instance == null || DataProvider.Instance.MapsByID == null ||
                MapManager.Instance == null)
                return false;

            MapInfo mapInfo;
            if (!DataProvider.Instance.MapsByID.TryGetValue(mapId, out mapInfo))
                return false;

            lock (Sync)
            {
                if (ByHandle.ContainsKey(definition.Handle))
                    return false;

                Map map = MapManager.Instance.GetMap(mapInfo, mapInstance);
                if (map == null || map.MapID != mapId || map.InstanceID != mapInstance)
                    return false;

                ByHandle.Add(definition.Handle, new KingdomQuestZoneRuntimeState(
                    definition.Handle, mapId, mapInstance,
                    KingdomQuestZoneLifecycleState.Made, definition, null));
                return true;
            }
        }

        public static bool TryStart(KingdomQuestProtocolInfo definition,
            IEnumerable<KingdomQuestZoneJoinerInfo> joiners)
        {
            if (definition == null || joiners == null)
                return false;

            List<KingdomQuestZoneJoinerInfo> roster = joiners.ToList();
            lock (Sync)
            {
                KingdomQuestZoneRuntimeState current;
                if (!ByHandle.TryGetValue(definition.Handle, out current))
                    return false;

                ByHandle[definition.Handle] = new KingdomQuestZoneRuntimeState(
                    current.Handle, current.MapID, current.MapInstance,
                    KingdomQuestZoneLifecycleState.Started, definition, roster);
                return true;
            }
        }

        public static bool TryEnd(uint handle)
        {
            lock (Sync)
            {
                KingdomQuestZoneRuntimeState current;
                if (!ByHandle.TryGetValue(handle, out current))
                    return false;

                ByHandle[handle] = new KingdomQuestZoneRuntimeState(
                    current.Handle, current.MapID, current.MapInstance,
                    KingdomQuestZoneLifecycleState.Ended,
                    current.Definition, current.Joiners);
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ByHandle.Remove(handle);
        }

        public static bool TryGet(uint handle, out KingdomQuestZoneRuntimeState state)
        {
            lock (Sync)
            {
                KingdomQuestZoneRuntimeState current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    state = null;
                    return false;
                }

                state = current.Clone();
                return true;
            }
        }

        public static void Clear()
        {
            lock (Sync)
                ByHandle.Clear();
        }
    }
}
