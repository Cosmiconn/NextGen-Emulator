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

        private static bool TryGetActiveKqClient(
            uint characterNumber, uint handle, out WorldClient client)
        {
            client = null;
            if (characterNumber > int.MaxValue ||
                ClientManager.Instance == null)
                return false;

            WorldClient candidate =
                ClientManager.Instance.GetClientByCharID((int)characterNumber);
            if (candidate == null ||
                !candidate.KingdomQuestHandle.HasValue ||
                candidate.KingdomQuestHandle.Value != handle)
                return false;

            uint resolvedCharacterNumber;
            if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    candidate.Character, out resolvedCharacterNumber) ||
                resolvedCharacterNumber != characterNumber)
                return false;

            client = candidate;
            return true;
        }

        private static void SendVoteCancel(
            uint handle, KingdomQuestVoteCancelPlan plan)
        {
            if (plan == null)
                return;

            using (Packet cancel =
                KingdomQuestProtocol.CreateVoteCancel(plan.TargetName))
            {
                for (int i = 0; i < plan.AudienceCharacterNumbers.Count; i++)
                {
                    WorldClient audience;
                    if (TryGetActiveKqClient(
                            plan.AudienceCharacterNumbers[i],
                            handle, out audience))
                        audience.SendPacket(cancel);
                }
            }
        }

        private static bool PlayerDisjoin(WorldClient client)
        {
            if (client == null || !client.KingdomQuestHandle.HasValue)
                return false;

            uint oldHandle = client.KingdomQuestHandle.Value;
            uint characterNumber;
            if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    client.Character, out characterNumber))
                return false;

            // Native PlayerDisjoin cancels an active vote only when the
            // leaving joiner is the vote target and is not already banned.
            // VOTE_CANCEL is emitted before normal membership deletion.
            KingdomQuestVoteCancelPlan cancelPlan;
            if (!KingdomQuestVoteCoordinator.TryCancelForTargetDisjoin(
                    oldHandle, characterNumber, out cancelPlan))
                return false;
            SendVoteCancel(oldHandle, cancelPlan);

            uint removedHandle;
            uint removedCharacterNumber;
            if (!KingdomQuestAdmissionCoordinator.TryRemoveCurrentMembership(
                    client, out removedHandle, out removedCharacterNumber) ||
                removedHandle != oldHandle ||
                removedCharacterNumber != characterNumber)
                return false;

            // Native PlayerDisjoin order after vote cancellation: Zone
            // broadcast, JOIN_LIST broadcast, then session nKQHandle=FFFFFFFF.
            BroadcastPlayerDisjoinToZones(oldHandle, characterNumber);
            BroadcastJoinList(oldHandle);
            KingdomQuestAdmissionCoordinator.CompleteDisjoin(
                client, oldHandle);
            return true;
        }

        /// <summary>
        /// Mirrors the KQ branch inside CWMClientSession::Logout.
        /// Native World calls PlayerDisjoin only when InKQStatusRunning
        /// returns false. Status 4 therefore deliberately preserves the
        /// KQ_JOINER_BF entry for reconnect.
        /// </summary>
        internal static void KingdomQuestLogout(WorldClient client)
        {
            if (client == null || !client.KingdomQuestHandle.HasValue)
                return;

            KingdomQuestProtocolInfo definition;
            bool running =
                KingdomQuestProtocolDefinitionRegistry.TryGet(
                    client.KingdomQuestHandle.Value, out definition) &&
                definition.Status == KingdomQuestNativeConstants.StatusRunning;

            if (!running)
                PlayerDisjoin(client);
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

        [PacketHandler(CH22Type.KingdomQuestVoteStartReq)]
        public static void KingdomQuestVoteStart(
            WorldClient client, Packet packet)
        {
            string targetName;
            byte voteType;
            byte contentsLength;
            string contents;
            if (client == null || packet == null ||
                !packet.TryReadString(out targetName, 20) ||
                !packet.TryReadByte(out voteType) ||
                !packet.TryReadByte(out contentsLength) ||
                !packet.TryReadString(out contents, contentsLength) ||
                packet.Remaining != 0)
                return;

            if (!client.KingdomQuestHandle.HasValue)
            {
                using (Packet invalid =
                    KingdomQuestProtocol.CreateVoteStartAck(
                        KingdomQuestNativeConstants.VoteStartInvalidHandle))
                    client.SendPacket(invalid);
                return;
            }

            uint characterNumber;
            if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    client.Character, out characterNumber))
                return;

            uint handle = client.KingdomQuestHandle.Value;
            WorldClient targetClient =
                ClientManager.Instance == null
                    ? null
                    : ClientManager.Instance.GetClientByCharname(targetName);
            bool targetSessionAvailable =
                targetClient != null &&
                targetClient.KingdomQuestHandle.HasValue &&
                targetClient.KingdomQuestHandle.Value == handle;

            DateTime localNow = DateTime.Now;
            int currentTime =
                KingdomQuestSourceScheduler.ToNativeTime32(localNow);
            bool suggestCooldownActive =
                client.KingdomQuestVoteSuggestCooldownUntil.HasValue &&
                client.KingdomQuestVoteSuggestCooldownUntil.Value >
                    currentTime;
            int voteEndTime = unchecked(
                currentTime + KingdomQuestNativeConstants.VoteLimitSeconds);

            ushort error;
            KingdomQuestVoteStartPlan plan;
            if (!KingdomQuestVoteCoordinator.TryPrepareStart(
                    handle,
                    characterNumber,
                    targetName,
                    voteType,
                    contentsLength,
                    targetSessionAvailable,
                    suggestCooldownActive,
                    voteEndTime,
                    out error,
                    out plan))
            {
                Log.WriteLine(LogLevel.Warn,
                    "KQ VOTE_START fail-closed for CharacterNumber {0}.",
                    characterNumber);
                return;
            }

            // Native sends VOTE_START_ACK before broadcasting VOTE_VOTING_CMD.
            using (Packet ack =
                KingdomQuestProtocol.CreateVoteStartAck(error))
                client.SendPacket(ack);

            if (error != KingdomQuestNativeConstants.VoteStartSuccess ||
                plan == null)
                return;

            client.KingdomQuestVoteSuggestCooldownUntil = unchecked(
                currentTime +
                KingdomQuestNativeConstants.VoteSuggestCooldownSeconds);

            KingdomQuestNativeTime endTm =
                KingdomQuestNativeTime.FromLocalDateTime(
                    localNow.AddSeconds(
                        KingdomQuestNativeConstants.VoteLimitSeconds));
            using (Packet command =
                KingdomQuestProtocol.CreateVoteVotingCmd(
                    plan.StarterName,
                    plan.TargetName,
                    plan.VoteType,
                    endTm,
                    contents))
            {
                for (int i = 0; i < plan.VoterCharacterNumbers.Count; i++)
                {
                    WorldClient voter;
                    if (TryGetActiveKqClient(
                            plan.VoterCharacterNumbers[i],
                            handle, out voter))
                        voter.SendPacket(command);
                }
            }
        }

        [PacketHandler(CH22Type.KingdomQuestVoteVotingReq)]
        public static void KingdomQuestVoteVoting(
            WorldClient client, Packet packet)
        {
            int choice;
            if (client == null || packet == null ||
                !packet.TryReadInt(out choice) ||
                packet.Remaining != 0)
                return;

            if (!client.KingdomQuestHandle.HasValue)
            {
                using (Packet invalid =
                    KingdomQuestProtocol.CreateVoteVotingAck(
                        KingdomQuestNativeConstants.VoteVotingInvalidJoiner))
                    client.SendPacket(invalid);
                return;
            }

            uint characterNumber;
            if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    client.Character, out characterNumber))
                return;

            ushort error;
            if (!KingdomQuestVoteCoordinator.TryRecordVote(
                    client.KingdomQuestHandle.Value,
                    characterNumber,
                    choice,
                    out error))
            {
                Log.WriteLine(LogLevel.Warn,
                    "KQ VOTE_VOTING fail-closed for CharacterNumber {0}.",
                    characterNumber);
                return;
            }

            using (Packet ack =
                KingdomQuestProtocol.CreateVoteVotingAck(error))
                client.SendPacket(ack);
        }

        [PacketHandler(CH22Type.KingdomQuestVoteStartCheckReq)]
        public static void KingdomQuestVoteStartCheck(
            WorldClient client, Packet packet)
        {
            if (client == null || !client.KingdomQuestHandle.HasValue)
                return;

            uint handle = client.KingdomQuestHandle.Value;
            KingdomQuestProtocolInfo definition;
            if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                    handle, out definition) ||
                definition.Status != KingdomQuestNativeConstants.StatusRunning)
                return;

            int currentTime =
                KingdomQuestSourceScheduler.ToNativeTime32(DateTime.Now);
            KingdomQuestVoteState state;
            if (!KingdomQuestVoteCoordinator.TryGetState(handle, out state))
                return;

            ushort error =
                state.IsActive
                    ? KingdomQuestNativeConstants.VoteStartCheckAlreadyRunning
                    : (client.KingdomQuestVoteSuggestCooldownUntil.HasValue &&
                       client.KingdomQuestVoteSuggestCooldownUntil.Value >
                           currentTime
                        ? KingdomQuestNativeConstants.VoteStartCheckSuggestCooldown
                        : KingdomQuestNativeConstants.VoteStartCheckSuccess);

            using (Packet ack =
                KingdomQuestProtocol.CreateVoteStartCheckAck(error))
                client.SendPacket(ack);
        }

        /// <summary>
        /// Native CKQServer::VoteProcessing order is important: already-banned
        /// joiners receive LINK_TO_FORCE_BY_BAN before this tick resolves an
        /// expired vote. A newly-banned target therefore receives its force
        /// link on the following processing tick.
        /// </summary>
        internal static void ProcessKingdomQuestVotes(DateTime localNow)
        {
            int currentTime =
                KingdomQuestSourceScheduler.ToNativeTime32(localNow);
            IReadOnlyList<KingdomQuestProtocolInfo> definitions =
                KingdomQuestProtocolDefinitionRegistry.Snapshot();

            for (int i = 0; i < definitions.Count; i++)
            {
                KingdomQuestProtocolInfo definition = definitions[i];
                if (definition.Status !=
                        KingdomQuestNativeConstants.StatusRunning)
                    continue;

                IReadOnlyList<KingdomQuestMembershipEntry> banned;
                if (KingdomQuestVoteCoordinator.TryGetBannedMembers(
                        definition.Handle, out banned) &&
                    definition.MapLink != null &&
                    definition.MapLink.Length == 4)
                {
                    var mapNames = new List<string>(4);
                    bool completeMapLinks = true;
                    for (int mapIndex = 0; mapIndex < 4; mapIndex++)
                    {
                        if (definition.MapLink[mapIndex] == null)
                        {
                            completeMapLinks = false;
                            break;
                        }
                        mapNames.Add(
                            definition.MapLink[mapIndex].MapName ??
                            string.Empty);
                    }

                    if (completeMapLinks)
                    {
                        for (int bannedIndex = 0;
                            bannedIndex < banned.Count;
                            bannedIndex++)
                        {
                            WorldClient bannedClient;
                            if (!TryGetActiveKqClient(
                                    banned[bannedIndex].CharacterNumber,
                                    definition.Handle,
                                    out bannedClient))
                                continue;

                            using (Packet force =
                                KingdomQuestProtocol.CreateLinkToForceByBan(
                                    banned[bannedIndex].CharacterNumber,
                                    mapNames.AsReadOnly()))
                                bannedClient.SendPacket(force);
                        }
                    }
                }

                KingdomQuestVoteResolution resolution;
                if (!KingdomQuestVoteCoordinator.TryResolveExpired(
                        definition.Handle,
                        currentTime,
                        out resolution))
                    continue;

                using (Packet result =
                    resolution.Passed
                        ? KingdomQuestProtocol.CreateVoteResultSuccess(
                            resolution.TargetName,
                            resolution.RequiredRate,
                            resolution.YesCount,
                            resolution.NoCount,
                            resolution.CancelCount)
                        : KingdomQuestProtocol.CreateVoteResultFail(
                            resolution.TargetName,
                            resolution.YesCount,
                            resolution.NoCount,
                            resolution.CancelCount))
                {
                    for (int audienceIndex = 0;
                        audienceIndex <
                            resolution.AudienceCharacterNumbers.Count;
                        audienceIndex++)
                    {
                        WorldClient audience;
                        if (TryGetActiveKqClient(
                                resolution.AudienceCharacterNumbers[
                                    audienceIndex],
                                definition.Handle,
                                out audience))
                            audience.SendPacket(result);
                    }
                }

                if (resolution.Passed)
                {
                    WorldClient target;
                    if (TryGetActiveKqClient(
                            resolution.TargetCharacterNumber,
                            definition.Handle,
                            out target))
                    {
                        using (Packet ban =
                            KingdomQuestProtocol.CreateVoteBanMessage(
                                resolution.RequiredRate,
                                resolution.YesCount,
                                resolution.NoCount,
                                resolution.CancelCount))
                            target.SendPacket(ban);
                    }
                }
            }
        }

        [PacketHandler(CH22Type.KingdomQuestTeamSelectReq)]
        public static void KingdomQuestTeamSelect(
            WorldClient client, Packet packet)
        {
            byte requestedTeamType;
            if (!packet.TryReadByte(out requestedTeamType) || client == null)
                return;

            if (!client.KingdomQuestHandle.HasValue)
            {
                using (Packet invalid =
                    KingdomQuestProtocol.CreateTeamSelectAck(
                        KingdomQuestNativeConstants.TeamSelectInvalidHandle,
                        KingdomQuestNativeConstants.NeutralTeamType))
                    client.SendPacket(invalid);
                return;
            }

            uint characterNumber;
            if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    client.Character, out characterNumber))
                return;

            KingdomQuestTeamSelectResult result;
            if (!KingdomQuestSessionCoordinator.TrySelectUserTeam(
                    client.KingdomQuestHandle.Value,
                    characterNumber,
                    requestedTeamType,
                    out result))
            {
                Log.WriteLine(LogLevel.Warn,
                    "KQ TEAM_SELECT fail-closed for CharacterNumber {0}.",
                    characterNumber);
                return;
            }

            // Native success mutates first, ACKs the requester, then sends
            // TEAM_SELECT_CMD to the other represented KQ sessions.
            using (Packet ack = KingdomQuestProtocol.CreateTeamSelectAck(
                result.Error, result.AckTeamType))
                client.SendPacket(ack);

            if (result.Error != KingdomQuestNativeConstants.TeamSelectSuccess)
                return;

            using (Packet command = KingdomQuestProtocol.CreateTeamSelectCmd(
                result.CharacterName, result.AckTeamType))
            {
                for (int i = 0;
                    i < result.OtherCharacterNumbers.Count;
                    i++)
                {
                    uint otherNumber = result.OtherCharacterNumbers[i];
                    if (otherNumber > int.MaxValue ||
                        ClientManager.Instance == null)
                        continue;

                    WorldClient other =
                        ClientManager.Instance.GetClientByCharID(
                            (int)otherNumber);
                    if (other == null ||
                        !other.KingdomQuestHandle.HasValue ||
                        other.KingdomQuestHandle.Value != result.Handle)
                        continue;

                    other.SendPacket(command);
                }
            }
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
