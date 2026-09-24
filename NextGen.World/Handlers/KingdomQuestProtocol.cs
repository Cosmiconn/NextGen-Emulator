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
        internal static Packet CreateCharacterKqMap(
            KingdomQuestMapContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var packet = new Packet((ushort)0x101A);
            packet.WriteUInt(context.Handle);
            packet.WriteString(context.MapName ?? string.Empty, 12);
            packet.WriteInt(context.X);
            packet.WriteInt(context.Y);
            packet.WriteUInt(context.NativeDate);
            return packet;
        }

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

        internal static Packet CreateRestDeadNum(byte number)
        {
            var packet = new Packet(SH22Type.KingdomQuestRestDeadNum);
            packet.WriteByte(number);
            return packet;
        }

        internal static Packet CreateEntryResponseAck(byte reply, uint encodedHandle)
        {
            var packet = new Packet(SH22Type.KingdomQuestEntryResponseAck);
            packet.WriteByte(reply);
            packet.WriteUInt(encodedHandle);
            return packet;
        }

        internal static Packet CreateMobKillNumber(ushort currentMobKill,
            ushort demandMobKill)
        {
            var packet = new Packet(SH22Type.KingdomQuestMobKillNumber);
            packet.WriteUShort(currentMobKill);
            packet.WriteUShort(demandMobKill);
            return packet;
        }

        internal static Packet CreateScoreInfo(uint redScore, uint blueScore)
        {
            var packet = new Packet(SH22Type.KingdomQuestScoreInfo);
            packet.WriteUInt(redScore);
            packet.WriteUInt(blueScore);
            return packet;
        }

        internal static Packet CreateScoreBoardInfo(byte useRound, byte round,
            byte redWinFlag, byte redScore, byte blueWinFlag, byte blueScore)
        {
            var packet = new Packet(SH22Type.KingdomQuestScoreBoardInfo);
            packet.WriteByte(useRound);
            packet.WriteByte(round);
            packet.WriteByte(redWinFlag);
            packet.WriteByte(redScore);
            packet.WriteByte(blueWinFlag);
            packet.WriteByte(blueScore);
            return packet;
        }

        internal static Packet CreateWinterEventScore(byte redWinFlag,
            byte redScore, byte blueWinFlag, byte blueScore)
        {
            var packet = new Packet(SH22Type.KingdomQuestWinterEvent2014Score);
            packet.WriteByte(redWinFlag);
            packet.WriteByte(redScore);
            packet.WriteByte(blueWinFlag);
            packet.WriteByte(blueScore);
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

        internal static Packet CreateVoteStartAck(ushort error)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteStartAck);
            packet.WriteUShort(error);
            return packet;
        }

        internal static Packet CreateVoteVotingCmd(string starter, string target,
            byte voteType, KingdomQuestNativeTime endTime, string contents)
        {
            if (endTime == null) throw new ArgumentNullException("endTime");
            byte[] data = Encoding.ASCII.GetBytes(contents ?? string.Empty);
            if (data.Length > byte.MaxValue) throw new ArgumentOutOfRangeException("contents");

            var packet = new Packet(SH22Type.KingdomQuestVoteVotingCmd);
            packet.WriteString(starter ?? string.Empty, 20);
            packet.WriteString(target ?? string.Empty, 20);
            packet.WriteByte(voteType);
            endTime.Write(packet);
            packet.WriteByte((byte)data.Length);
            packet.WriteBytes(data);
            return packet;
        }

        internal static Packet CreateVoteVotingAck(ushort error)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteVotingAck);
            packet.WriteUShort(error);
            return packet;
        }

        internal static Packet CreateVoteResultSuccess(string target,
            byte voteRate, byte yes, byte no, byte cancel)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteResultSuccess);
            packet.WriteString(target ?? string.Empty, 20);
            packet.WriteByte(voteRate);
            packet.WriteByte(yes);
            packet.WriteByte(no);
            packet.WriteByte(cancel);
            return packet;
        }

        internal static Packet CreateVoteResultFail(string target,
            byte yes, byte no, byte cancel)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteResultFail);
            packet.WriteString(target ?? string.Empty, 20);
            packet.WriteByte(yes);
            packet.WriteByte(no);
            packet.WriteByte(cancel);
            return packet;
        }

        internal static Packet CreateVoteCancel(string target)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteCancel);
            packet.WriteString(target ?? string.Empty, 20);
            return packet;
        }

        internal static Packet CreateVoteBanMessage(
            byte voteRate, byte yes, byte no, byte cancel)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteBanMessage);
            packet.WriteByte(voteRate);
            packet.WriteByte(yes);
            packet.WriteByte(no);
            packet.WriteByte(cancel);
            return packet;
        }

        internal static Packet CreateVoteBanMessageLogoff()
        {
            return new Packet(SH22Type.KingdomQuestVoteBanMessageLogoff);
        }

        internal static Packet CreateLinkToForceByBan(
            uint characterNumber, IReadOnlyList<string> mapNames)
        {
            if (mapNames == null) throw new ArgumentNullException("mapNames");
            if (mapNames.Count != 4) throw new ArgumentOutOfRangeException("mapNames");

            var packet = new Packet(SH22Type.KingdomQuestLinkToForceByBan);
            packet.WriteUInt(characterNumber);
            for (int i = 0; i < 4; i++)
                packet.WriteString(mapNames[i] ?? string.Empty, 12);
            return packet;
        }

        internal static Packet CreateVoteStartCheckAck(ushort error)
        {
            var packet = new Packet(SH22Type.KingdomQuestVoteStartCheckAck);
            packet.WriteUShort(error);
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
