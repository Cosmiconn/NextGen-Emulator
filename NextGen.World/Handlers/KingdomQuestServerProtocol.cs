using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.World.Data;

namespace NextGen.World.Handlers
{
    /// <summary>
    /// Original NC_KQ World <-> Zone packet layouts.
    ///
    /// These builders are deliberately separate from Handler22/client traffic.
    /// They model native server-side transport only; the emulator InterServer
    /// bridge carries their exact native bodies between World and Zone.
    /// </summary>
    internal static class KingdomQuestServerProtocol
    {
        internal static bool TryCreateMakeRequest(uint handle, out Packet packet)
        {
            KingdomQuestProtocolInfo info;
            if (!KingdomQuestProtocolDefinitionRegistry.TryGet(handle, out info))
            {
                packet = null;
                return false;
            }

            packet = CreateMakeRequest(info);
            return true;
        }

        internal static bool TryCreateStart(uint handle, out Packet packet)
        {
            KingdomQuestProtocolInfo info;
            IReadOnlyList<KingdomQuestZoneJoinerInfo> joiners;
            if (!KingdomQuestProtocolDefinitionRegistry.TryGet(handle, out info) ||
                !KingdomQuestZoneJoinerRegistry.TryGet(handle, out joiners))
            {
                packet = null;
                return false;
            }

            packet = CreateStart(info, joiners);
            return true;
        }

        internal static Packet CreateMakeRequest(KingdomQuestProtocolInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");

            var packet = new Packet((ushort)0x580D);
            info.Write(packet);
            return packet;
        }

        internal static Packet CreateMakeAck(uint handle, ushort error)
        {
            var packet = new Packet((ushort)0x580E);
            packet.WriteUInt(handle);
            packet.WriteUShort(error);
            return packet;
        }

        internal static Packet CreateStart(KingdomQuestProtocolInfo info,
            IReadOnlyList<KingdomQuestZoneJoinerInfo> joiners)
        {
            if (info == null) throw new ArgumentNullException("info");
            if (joiners == null) throw new ArgumentNullException("joiners");
            if (joiners.Count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("joiners");

            var packet = new Packet((ushort)0x580F);
            info.Write(packet);
            packet.WriteUShort((ushort)joiners.Count);
            for (int i = 0; i < joiners.Count; i++)
            {
                if (joiners[i] == null)
                    throw new ArgumentException("KQ Zone joiner entry is null.", "joiners");
                joiners[i].Write(packet);
            }
            return packet;
        }

        internal static Packet CreateEnd(uint handle)
        {
            var packet = new Packet((ushort)0x5810);
            packet.WriteUInt(handle);
            return packet;
        }

        internal static Packet CreateDestroy(uint handle)
        {
            var packet = new Packet((ushort)0x5811);
            packet.WriteUInt(handle);
            return packet;
        }
    }
}
