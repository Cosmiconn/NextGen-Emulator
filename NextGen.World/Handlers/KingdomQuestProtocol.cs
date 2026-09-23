using System;
using System.Collections.Generic;
using System.Text;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.FiestaLib.Data;
using NextGen.World.Data;

namespace NextGen.World.Handlers
{
    /// <summary>
    /// Native-layout KQ packet builders. These layouts are from the original
    /// 2016 protocol/PDB structures and are cross-correlated with the project
    /// captures. Gameplay state is supplied by the caller.
    /// </summary>
    internal static class KingdomQuestProtocol
    {
        internal static Packet CreateStatusAck(KingdomQuestInstanceWireState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            var packet = new Packet(SH22Type.KingdomQuestStatusAck);
            packet.WriteUInt(state.Handle);
            packet.WriteByte(state.Status);
            packet.WriteUShort((ushort)state.JoinerNames.Count);
            for (int i = 0; i < state.JoinerNames.Count; i++)
                packet.WriteString(state.JoinerNames[i] ?? string.Empty, 20);
            return packet;
        }

        internal static Packet CreateJoinAck(uint handle, ushort error)
        {
            var packet = new Packet(SH22Type.KingdomQuestJoinAck);
            packet.WriteUInt(handle);
            packet.WriteUShort(error);
            return packet;
        }

        internal static Packet CreateListTime(DateTimeOffset now)
        {
            var packet = new Packet(SH22Type.KingdomQuestListTimeAck);
            WriteServerTime(packet, now);
            return packet;
        }

        internal static Packet CreateListAdd(
            IReadOnlyList<KingdomQuestClientInfo> entries)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (entries.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("entries");

            var packet = new Packet(SH22Type.KingdomQuestListAddAck);
            packet.WriteUShort((ushort)entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null) throw new ArgumentException("KQ list entry is null.", "entries");
                entries[i].Write(packet);
            }
            return packet;
        }

        internal static Packet CreateEmptyListAdd()
        {
            return CreateListAdd(Array.Empty<KingdomQuestClientInfo>());
        }

        internal static Packet CreateListAck(DateTimeOffset now,
            uint newStartHandle, uint newEndHandle,
            IReadOnlyList<KingdomQuestClientInfo> entries)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (entries.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("entries");

            var packet = new Packet(SH22Type.KingdomQuestListAck);
            WriteServerTime(packet, now);
            packet.WriteUInt(newStartHandle);
            packet.WriteUInt(newEndHandle);
            packet.WriteUShort((ushort)entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null) throw new ArgumentException("KQ list entry is null.", "entries");
                entries[i].Write(packet);
            }
            return packet;
        }

        internal static Packet CreateScheduleAck(uint newStartHandle,
            uint newEndHandle, IReadOnlyList<KingdomQuestClientInfo> entries)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (entries.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("entries");

            var packet = new Packet(SH22Type.KingdomQuestScheduleAck);
            packet.WriteUInt(newStartHandle);
            packet.WriteUInt(newEndHandle);
            packet.WriteUShort((ushort)entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null) throw new ArgumentException("KQ schedule entry is null.", "entries");
                entries[i].Write(packet);
            }
            return packet;
        }

        internal static Packet CreateListDelete(IReadOnlyList<uint> handles)
        {
            if (handles == null) throw new ArgumentNullException("handles");
            if (handles.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("handles");

            var packet = new Packet(SH22Type.KingdomQuestListDeleteAck);
            packet.WriteUShort((ushort)handles.Count);
            for (int i = 0; i < handles.Count; i++)
                packet.WriteUInt(handles[i]);
            return packet;
        }

        internal static Packet CreateListUpdate(
            IReadOnlyList<KingdomQuestInstanceWireState> states)
        {
            if (states == null) throw new ArgumentNullException("states");
            if (states.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("states");

            var packet = new Packet(SH22Type.KingdomQuestListUpdateAck);
            packet.WriteUShort((ushort)states.Count);
            for (int i = 0; i < states.Count; i++)
            {
                packet.WriteUInt(states[i].Handle);
                packet.WriteByte(states[i].Status);
                packet.WriteUShort((ushort)states[i].JoinerNames.Count);
            }
            return packet;
        }

        internal static Packet CreateNotify(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message ?? string.Empty);
            if (data.Length > byte.MaxValue) throw new ArgumentOutOfRangeException("message");

            var packet = new Packet(SH22Type.KingdomQuestNotify);
            packet.WriteByte((byte)data.Length);
            packet.WriteBytes(data);
            return packet;
        }

        internal static Packet CreateJoiningAlarm(
            KingdomQuestInstanceWireState state, string message)
        {
            if (state == null) throw new ArgumentNullException("state");
            byte[] data = Encoding.ASCII.GetBytes(message ?? string.Empty);
            if (data.Length > byte.MaxValue) throw new ArgumentOutOfRangeException("message");

            var packet = new Packet(SH22Type.KingdomQuestJoiningAlarm);
            WriteJoiningAlarmInfo(packet, state);
            packet.WriteByte((byte)data.Length);
            packet.WriteBytes(data);
            return packet;
        }

        internal static Packet CreateJoiningAlarmEnd(uint handle, ushort id)
        {
            var packet = new Packet(SH22Type.KingdomQuestJoiningAlarmEnd);
            packet.WriteUInt(handle);
            packet.WriteUShort(id);
            return packet;
        }

        internal static Packet CreateJoiningAlarmList(
            IReadOnlyList<KingdomQuestInstanceWireState> states)
        {
            if (states == null) throw new ArgumentNullException("states");
            if (states.Count > ushort.MaxValue) throw new ArgumentOutOfRangeException("states");

            var packet = new Packet(SH22Type.KingdomQuestJoiningAlarmList);
            packet.WriteUShort((ushort)states.Count);
            for (int i = 0; i < states.Count; i++)
                WriteJoiningAlarmInfo(packet, states[i]);
            return packet;
        }

        internal static Packet CreateJoinCancelAck(uint handle, ushort error)
        {
            var packet = new Packet(SH22Type.KingdomQuestJoinCancelAck);
            packet.WriteUInt(handle);
            packet.WriteUShort(error);
            return packet;
        }

        internal static Packet CreateTeamSelectAck(ushort error, byte teamType)
        {
            var packet = new Packet(SH22Type.KingdomQuestTeamSelectAck);
            packet.WriteUShort(error);
            packet.WriteByte(teamType);
            return packet;
        }

        internal static Packet CreateTeamSelectCmd(string characterName, byte teamType)
        {
            var packet = new Packet(SH22Type.KingdomQuestTeamSelectCmd);
            packet.WriteString(characterName ?? string.Empty, 20);
            packet.WriteByte(teamType);
            return packet;
        }

        internal static Packet CreateTeamTypeCmd(byte teamType)
        {
            var packet = new Packet(SH22Type.KingdomQuestTeamTypeCmd);
            packet.WriteByte(teamType);
            return packet;
        }

        internal static Packet CreatePlayerDisjoin(uint handle, uint characterNumber)
        {
            var packet = new Packet(SH22Type.KingdomQuestPlayerDisjoin);
            packet.WriteUInt(handle);
            packet.WriteUInt(characterNumber);
            return packet;
        }

        internal static Packet CreateJoinListAck(ushort error,
            IReadOnlyList<KingdomQuestJoinCharacterInfo> joiners)
        {
            if (joiners == null) throw new ArgumentNullException("joiners");
            if (joiners.Count > byte.MaxValue) throw new ArgumentOutOfRangeException("joiners");

            var packet = new Packet(SH22Type.KingdomQuestJoinListAck);
            packet.WriteUShort(error);
            packet.WriteByte((byte)joiners.Count);
            for (int i = 0; i < joiners.Count; i++)
            {
                if (joiners[i] == null)
                    throw new ArgumentException("KQ join-list entry is null.", "joiners");
                joiners[i].Write(packet);
            }
            return packet;
        }

        internal static Packet CreateFailed()
        {
            return new Packet(SH22Type.KingdomQuestFailed);
        }

        private static void WriteJoiningAlarmInfo(
            Packet packet, KingdomQuestInstanceWireState state)
        {
            packet.WriteUInt(state.Handle);
            packet.WriteUShort(state.ID);
            packet.WriteByte(state.MinLevel);
            packet.WriteByte(state.MaxLevel);
        }

        private static void WriteServerTime(Packet packet, DateTimeOffset now)
        {
            long unix = now.ToUnixTimeSeconds();
            if (unix < int.MinValue || unix > int.MaxValue)
                throw new ArgumentOutOfRangeException("now");

            packet.WriteInt((int)unix);
            KingdomQuestNativeTime.FromLocalDateTime(now.LocalDateTime).Write(packet);
        }
    }
}
