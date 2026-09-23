using System;
using System.Collections.Generic;
using System.Text;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
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
            long unix = now.ToUnixTimeSeconds();
            if (unix < int.MinValue || unix > int.MaxValue)
                throw new ArgumentOutOfRangeException("now");

            DateTime local = now.LocalDateTime;
            var packet = new Packet(SH22Type.KingdomQuestListTimeAck);
            packet.WriteInt((int)unix);
            WriteTm(packet, local);
            return packet;
        }

        internal static Packet CreateEmptyListAdd()
        {
            var packet = new Packet(SH22Type.KingdomQuestListAddAck);
            packet.WriteUShort(0);
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

        private static void WriteTm(Packet packet, DateTime local)
        {
            packet.WriteInt(local.Second);
            packet.WriteInt(local.Minute);
            packet.WriteInt(local.Hour);
            packet.WriteInt(local.Day);
            packet.WriteInt(local.Month - 1);
            packet.WriteInt(local.Year - 1900);
            packet.WriteInt((int)local.DayOfWeek);
            packet.WriteInt(local.DayOfYear - 1);
            packet.WriteInt(TimeZoneInfo.Local.IsDaylightSavingTime(local) ? 1 : 0);
        }
    }
}
