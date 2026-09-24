using System;
using System.Collections.Generic;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.World.Networking;
using NextGen.World.Data;
using NextGen.InterLib.Networking;
using NextGen.Util;

namespace NextGen.World.Handlers
{
    public class Handler22
    {
        // WorldManager.exe CParserClient::fc_NC_KQ_LIST_REQ compares Status
        // as an unsigned byte and includes exactly values 0..4.
        private const byte ClientVisibleMaxStatus = 4;

        // Ack_NC_KQ_LIST_REFRESH flushes LIST_ADD after the accumulated
        // (2 + 141*N) body reaches 0x1D26. That happens at N=53 and matches
        // the project's observed 53/53/8 capture batches.
        private const int ListAddBatchEntries = 53;

        private static IReadOnlyList<KingdomQuestClientInfo> SnapshotClientVisible()
        {
            IReadOnlyList<KingdomQuestClientInfo> snapshot =
                KingdomQuestDefinitionRegistry.Snapshot();
            var visible = new List<KingdomQuestClientInfo>();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].Status <= ClientVisibleMaxStatus)
                    visible.Add(snapshot[i]);
            }
            return visible.AsReadOnly();
        }

        private static IReadOnlyList<KingdomQuestClientInfo> SnapshotAllScheduled()
        {
            IReadOnlyList<KingdomQuestProtocolInfo> snapshot =
                KingdomQuestProtocolDefinitionRegistry.Snapshot();
            var entries = new List<KingdomQuestClientInfo>(snapshot.Count);
            for (int i = 0; i < snapshot.Count; i++)
                entries.Add(snapshot[i]);
            return entries.AsReadOnly();
        }

        private static void ResolveAckBounds(
            IReadOnlyList<KingdomQuestClientInfo> entries,
            out uint newStartHandle, out uint newEndHandle)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            if (entries.Count == 0)
            {
                // The original empty branch writes NewStartHandle=-1 and
                // NumOfKQ=0 but leaves NewEndHandle as uninitialized stack
                // bytes. Never reproduce that memory disclosure.
                newStartHandle = uint.MaxValue;
                newEndHandle = 0;
                return;
            }

            newStartHandle = entries[0].Handle;
            newEndHandle = entries[entries.Count - 1].Handle;
        }

        private static Dictionary<uint, KingdomQuestClientInfo> IndexByHandle(
            IEnumerable<KingdomQuestClientInfo> entries)
        {
            var result = new Dictionary<uint, KingdomQuestClientInfo>();
            foreach (KingdomQuestClientInfo entry in entries)
                result[entry.Handle] = entry;
            return result;
        }

        private static void SendListAdds(WorldClient client,
            IReadOnlyList<KingdomQuestClientInfo> additions)
        {
            for (int offset = 0; offset < additions.Count; offset += ListAddBatchEntries)
            {
                int count = Math.Min(ListAddBatchEntries, additions.Count - offset);
                var batch = new List<KingdomQuestClientInfo>(count);
                for (int i = 0; i < count; i++)
                    batch.Add(additions[offset + i]);

                using (Packet add = KingdomQuestProtocol.CreateListAdd(batch.AsReadOnly()))
                    client.SendPacket(add);
            }
        }

        [PacketHandler(CH22Type.KingdomQuestListReq)]
        public static void KingdomQuestList(WorldClient client, Packet packet)
        {
            uint ignoredStartHandle;
            uint ignoredEndHandle;
            if (!packet.TryReadUInt(out ignoredStartHandle) ||
                !packet.TryReadUInt(out ignoredEndHandle))
                return;

            // Original WorldManager validates the two request fields but never
            // reads either one for selection. It sends every Status <= 4 entry.
            IReadOnlyList<KingdomQuestClientInfo> entries = SnapshotClientVisible();
            uint newStartHandle;
            uint newEndHandle;
            ResolveAckBounds(entries, out newStartHandle, out newEndHandle);

            using (Packet response = KingdomQuestProtocol.CreateListAck(
                DateTimeOffset.Now, newStartHandle, newEndHandle, entries))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestScheduleReq)]
        public static void KingdomQuestSchedule(WorldClient client, Packet packet)
        {
            uint ignoredStartHandle;
            uint ignoredEndHandle;
            if (!packet.TryReadUInt(out ignoredStartHandle) ||
                !packet.TryReadUInt(out ignoredEndHandle))
                return;

            // Original WorldManager likewise ignores the request bounds here,
            // but unlike LIST_REQ it serializes the entire scheduler array.
            IReadOnlyList<KingdomQuestClientInfo> entries = SnapshotAllScheduled();
            uint newStartHandle;
            uint newEndHandle;
            ResolveAckBounds(entries, out newStartHandle, out newEndHandle);

            using (Packet response = KingdomQuestProtocol.CreateScheduleAck(
                newStartHandle, newEndHandle, entries))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestStatusReq)]
        public static void KingdomQuestStatus(WorldClient client, Packet packet)
        {
            uint handle;
            if (!packet.TryReadUInt(out handle))
                return;

            KingdomQuestInstanceWireState state;
            if (!KingdomQuestInstanceRegistry.TryGet(handle, out state))
            {
                Log.WriteLine(LogLevel.Debug,
                    "KQ status requested for unresolved handle {0}.", handle);
                return;
            }

            using (Packet response = KingdomQuestProtocol.CreateStatusAck(state))
                client.SendPacket(response);
        }

        private static void BroadcastJoinList(uint handle)
        {
            IReadOnlyList<KingdomQuestMembershipEntry> members;
            IReadOnlyList<KingdomQuestJoinCharacterInfo> participants;
            if (!KingdomQuestMembershipRegistry.TryGet(handle, out members) ||
                !KingdomQuestParticipantRegistry.TryGet(handle, out participants))
                return;

            int now = KingdomQuestSourceScheduler.ToNativeTime32(DateTime.Now);
            using (Packet response = KingdomQuestProtocol.CreateJoinListAck(
                KingdomQuestNativeConstants.JoinListSuccess, participants))
            {
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].CharacterNumber > int.MaxValue)
                        continue;

                    WorldClient memberClient = ClientManager.Instance.GetClientByCharID(
                        (int)members[i].CharacterNumber);
                    if (memberClient == null)
                        continue;

                    memberClient.KingdomQuestJoinListLastRequestTime = now;
                    memberClient.SendPacket(response);
                }
            }
        }

        private static void BroadcastPlayerDisjoinToZones(
            uint handle, uint characterNumber)
        {
            if (Managers.ZoneManager.Instance == null)
                return;

            using (Packet native = KingdomQuestProtocol.CreatePlayerDisjoin(
                handle, characterNumber))
            using (var packet =
                new InterPacket(InterHeader.KingdomQuestPlayerDisjoin))
            {
                byte[] body = native.ToNormalArray();
                packet.WriteInt(body.Length);
                packet.WriteBytes(body);
                Managers.ZoneManager.Instance.Broadcast(packet);
            }
        }

        private static bool PlayerDisjoin(WorldClient client)
        {
            uint oldHandle;
            uint characterNumber;
            if (!KingdomQuestAdmissionCoordinator.TryRemoveCurrentMembership(
                    client, out oldHandle, out characterNumber))
                return false;

            // Native PlayerDisjoin order: Zone broadcast, JOIN_LIST broadcast,
            // then session nKQHandle = 0xFFFFFFFF.
            BroadcastPlayerDisjoinToZones(oldHandle, characterNumber);
            BroadcastJoinList(oldHandle);
            KingdomQuestAdmissionCoordinator.CompleteDisjoin(
                client, oldHandle);
            return true;
        }

        [PacketHandler(CH22Type.KingdomQuestJoinReq)]
        public static void KingdomQuestJoin(WorldClient client, Packet packet)
        {
            uint handle;
            if (!packet.TryReadUInt(out handle))
                return;

            ushort preJoinError;
            if (!KingdomQuestAdmissionCoordinator.TryGetPreJoinError(
                    client, handle, out preJoinError))
            {
                // The original server always has PROTO_NC_CHAR_BASE_CMD.
                // A nullable PrisonMin is our provenance sentinel only; do not
                // turn unknown original state into a native success/error.
                Log.WriteLine(LogLevel.Warn,
                    "KQ JOIN blocked for {0}: original PrisonMin is unknown.",
                    client.Character == null ? "<no-character>" :
                        client.Character.Character.Name);
                return;
            }

            if (preJoinError != 0)
            {
                using (Packet rejected =
                    KingdomQuestProtocol.CreateJoinAck(handle, preJoinError))
                    client.SendPacket(rejected);
                return;
            }

            // fc_NC_KQ_JOIN_REQ always executes PlayerDisjoin before
            // PlayerJoin when the prison/same-handle prechecks pass.
            PlayerDisjoin(client);

            ushort error;
            if (!KingdomQuestAdmissionCoordinator.TryAddMembership(
                    client, handle, out error))
            {
                Log.WriteLine(LogLevel.Error,
                    "KQ JOIN fail-closed for handle {0}: authoritative membership state is incomplete.",
                    handle);
                return;
            }

            // Native PlayerJoin broadcasts the new join list before returning
            // to fc_NC_KQ_JOIN_REQ, which sends JOIN_ACK afterwards.
            if (error == KingdomQuestNativeConstants.JoinSuccess)
                BroadcastJoinList(handle);

            using (Packet response =
                KingdomQuestProtocol.CreateJoinAck(handle, error))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestJoinCancelReq)]
        public static void KingdomQuestJoinCancel(WorldClient client, Packet packet)
        {
            uint requestedHandle;
            if (!packet.TryReadUInt(out requestedHandle))
                return;

            bool removed = PlayerDisjoin(client);
            ushort error = removed
                ? KingdomQuestNativeConstants.JoinCancelSuccess
                : KingdomQuestNativeConstants.JoinCancelNotJoined;

            // Original ACK echoes the request Handle even though
            // PlayerDisjoin selects the session-owned current Handle.
            using (Packet response =
                KingdomQuestProtocol.CreateJoinCancelAck(
                    requestedHandle, error))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestJoinListReq)]
        public static void KingdomQuestJoinList(WorldClient client, Packet packet)
        {
            uint requestedHandle;
            if (!packet.TryReadUInt(out requestedHandle))
                return;

            // Original Recv_NC_KQ_JOIN_LIST_REQ substitutes the session-owned
            // KQ Handle only while that KQ is Status 4; otherwise it uses the
            // request Handle unchanged.
            uint effectiveHandle = requestedHandle;
            if (client.KingdomQuestHandle.HasValue)
            {
                KingdomQuestProtocolInfo current;
                if (KingdomQuestProtocolDefinitionRegistry.TryGet(
                        client.KingdomQuestHandle.Value, out current) &&
                    current.Status == KingdomQuestNativeConstants.StatusRunning)
                    effectiveHandle = client.KingdomQuestHandle.Value;
            }

            IReadOnlyList<KingdomQuestJoinCharacterInfo> participants;
            if (!KingdomQuestParticipantRegistry.TryGet(
                    effectiveHandle, out participants))
            {
                using (Packet invalid = KingdomQuestProtocol.CreateJoinListAck(
                    KingdomQuestNativeConstants.JoinListInvalidHandle,
                    new KingdomQuestJoinCharacterInfo[0]))
                    client.SendPacket(invalid);
                return;
            }

            int now = KingdomQuestSourceScheduler.ToNativeTime32(DateTime.Now);
            if (client.KingdomQuestJoinListLastRequestTime.HasValue &&
                (long)client.KingdomQuestJoinListLastRequestTime.Value +
                    KingdomQuestNativeConstants.JoinListCooldownSeconds > now)
            {
                using (Packet cooldown = KingdomQuestProtocol.CreateJoinListAck(
                    KingdomQuestNativeConstants.JoinListCooldown,
                    new KingdomQuestJoinCharacterInfo[0]))
                    client.SendPacket(cooldown);
                return;
            }

            client.KingdomQuestJoinListLastRequestTime = now;
            using (Packet response = KingdomQuestProtocol.CreateJoinListAck(
                KingdomQuestNativeConstants.JoinListSuccess, participants))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestListRefreshReq)]
        public static void KingdomQuestListRefresh(WorldClient client, Packet packet)
        {
            if (!client.KingdomQuestListTimeSent)
            {
                using (Packet time = KingdomQuestProtocol.CreateListTime(DateTimeOffset.Now))
                    client.SendPacket(time);
                client.KingdomQuestListTimeSent = true;
            }

            IReadOnlyList<KingdomQuestClientInfo> current = SnapshotClientVisible();
            List<KingdomQuestClientInfo> previous = client.KingdomQuestListSnapshot;
            Dictionary<uint, KingdomQuestClientInfo> currentByHandle =
                IndexByHandle(current);
            Dictionary<uint, KingdomQuestClientInfo> previousByHandle =
                IndexByHandle(previous);

            var deleted = new List<uint>();
            var updated = new List<KingdomQuestClientInfo>();
            for (int i = 0; i < previous.Count; i++)
            {
                KingdomQuestClientInfo now;
                if (!currentByHandle.TryGetValue(previous[i].Handle, out now))
                {
                    deleted.Add(previous[i].Handle);
                    continue;
                }

                if (previous[i].Status != now.Status ||
                    previous[i].NumOfJoiner != now.NumOfJoiner)
                    updated.Add(now);
            }

            if (deleted.Count != 0)
            {
                using (Packet remove =
                    KingdomQuestProtocol.CreateListDelete(deleted.AsReadOnly()))
                    client.SendPacket(remove);
            }

            if (updated.Count != 0)
            {
                using (Packet update =
                    KingdomQuestProtocol.CreateListUpdateFromDefinitions(
                        updated.AsReadOnly()))
                    client.SendPacket(update);
            }

            var added = new List<KingdomQuestClientInfo>();
            for (int i = 0; i < current.Count; i++)
            {
                if (!previousByHandle.ContainsKey(current[i].Handle))
                    added.Add(current[i]);
            }
            SendListAdds(client, added.AsReadOnly());

            previous.Clear();
            for (int i = 0; i < current.Count; i++)
                previous.Add(current[i]);

            // The client also emits LIST_REFRESH during initial World entry.
            // Keep unrelated bootstrap work one-time; later refreshes must not
            // replay login callbacks.
            if (!client.Character.IsIngame)
            {
                using (var friends = new Packet(21, 7))
                {
                    friends.WriteByte((byte)client.Character.Friends.Count);
                    client.Character.WriteFriendData(friends);
                    client.SendPacket(friends);
                }
                using (var timePacket = new Packet(SH2Type.UnkTimePacket))
                {
                    timePacket.WriteShort(256);
                    client.SendPacket(timePacket);
                }

                client.Character.IsIngame = true;
                client.Character.OneIngameLoginLoad();
                MasterManager.Instance.SendMasterList(client);
                Managers.CharacterManager.InvokdeIngame(client.Character);
                client.Character.OnGotIngame();
            }
        }
    }
}
