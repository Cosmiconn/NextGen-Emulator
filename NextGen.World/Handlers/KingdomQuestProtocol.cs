using System;
using System.Collections.Generic;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.World.Data;

namespace NextGen.World.Handlers
{
    /// <summary>
    /// Packet builders for KQ layouts proven by the project captures.
    /// No gameplay meaning is assigned to fields whose semantics are still
    /// unresolved; callers must provide the raw captured/source-correlated
    /// values explicitly.
    /// </summary>
    internal static class KingdomQuestProtocol
    {
        internal static Packet CreateInstanceInfo(uint instanceId, ushort value)
        {
            var packet = new Packet(SH22Type.KingdomQuestInstanceInfo);
            packet.WriteUInt(instanceId);
            packet.WriteUShort(value);
            return packet;
        }

        internal static Packet CreateRegistrationAck(uint instanceId, ushort value)
        {
            var packet = new Packet(SH22Type.KingdomQuestRegistrationAck);
            packet.WriteUInt(instanceId);
            packet.WriteUShort(value);
            return packet;
        }

        internal static Packet CreateInstanceGroup(IReadOnlyList<uint> instanceIds)
        {
            if (instanceIds == null) throw new ArgumentNullException("instanceIds");
            if (instanceIds.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("instanceIds");

            var packet = new Packet(SH22Type.KingdomQuestInstanceGroup);
            packet.WriteUShort((ushort)instanceIds.Count);
            for (int i = 0; i < instanceIds.Count; i++)
                packet.WriteUInt(instanceIds[i]);
            return packet;
        }

        internal static Packet CreateRecruitmentUpdate(
            ushort firstValue, uint instanceId, ushort lastValue)
        {
            var packet = new Packet(SH22Type.KingdomQuestRecruitmentUpdate);
            packet.WriteUShort(firstValue);
            packet.WriteUInt(instanceId);
            packet.WriteUShort(lastValue);
            return packet;
        }

        internal static Packet CreateInstanceState(uint instanceId, ushort stateValue)
        {
            var packet = new Packet(SH22Type.KingdomQuestInstanceState);
            packet.WriteUInt(instanceId);
            packet.WriteUShort(stateValue);
            return packet;
        }

        internal static Packet CreateInstanceResync(
            IReadOnlyList<KingdomQuestInstanceWireState> states)
        {
            if (states == null) throw new ArgumentNullException("states");
            if (states.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("states");

            var packet = new Packet(SH22Type.KingdomQuestInstanceResync);
            packet.WriteUShort((ushort)states.Count);
            for (int i = 0; i < states.Count; i++)
            {
                packet.WriteUInt(states[i].InstanceID);
                packet.WriteUShort(states[i].StateValue);
                packet.WriteUShort(states[i].TypeValue);
            }
            return packet;
        }

        internal static Packet CreateFailed()
        {
            return new Packet(SH22Type.KingdomQuestFailed);
        }
    }
}
