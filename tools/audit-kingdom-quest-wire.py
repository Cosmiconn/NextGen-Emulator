#!/usr/bin/env python3
"""Guard original 2016 Kingdom Quest World protocol layouts."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CENUM = ROOT / "NextGen.FiestaLib/PacketTypeClient.cs"
SENUM = ROOT / "NextGen.FiestaLib/PacketTypeServer.cs"
PROTO = ROOT / "NextGen.World/Handlers/KingdomQuestProtocol.cs"
INFO = ROOT / "NextGen.FiestaLib/Data/KingdomQuestProtocolInfo.cs"
CHAR_SAVE_LOCATION = ROOT / "NextGen.FiestaLib/Data/CharacterSaveLocationProtocolInfo.cs"
HANDLER = ROOT / "NextGen.World/Handlers/Handler22.cs"
SERVER_PROTO = ROOT / "NextGen.World/Handlers/KingdomQuestServerProtocol.cs"
STATE = ROOT / "NextGen.World/Data/KingdomQuestInstanceWireState.cs"
DEFINITIONS = ROOT / "NextGen.World/Data/KingdomQuestDefinitionRegistry.cs"
JOIN_LIST_REPLY = ROOT / "NextGen.World/Data/KingdomQuestJoinListReplyRegistry.cs"
PARTICIPANTS = ROOT / "NextGen.World/Data/KingdomQuestParticipantRegistry.cs"
WORLD_CLIENT = ROOT / "NextGen.World/Networking/WorldClient.cs"
SESSION_COORDINATOR = ROOT / "NextGen.World/Data/KingdomQuestSessionCoordinator.cs"
MAKE_ACK_REGISTRY = ROOT / "NextGen.World/Data/KingdomQuestMakeAckRegistry.cs"
WORLD_INTER = ROOT / "NextGen.World/InterServer/InterHandler.cs"
WORLD_ZONE_CONNECTION = ROOT / "NextGen.World/InterServer/ZoneConnection.cs"
ZONE_INTER = ROOT / "NextGen.Zone/InterServer/InterHandler.cs"
MEMBERSHIP = ROOT / "NextGen.World/Data/KingdomQuestMembershipRegistry.cs"
VOTE_STATE = ROOT / "NextGen.World/Data/KingdomQuestVoteState.cs"
CHARACTER = ROOT / "NextGen.Database/Storage/Character.cs"
READ_METHODS = ROOT / "NextGen.Database/DataStore/ReadMethods.cs"
WORLD_SCHEMA = ROOT / "sql/world/schema.sql"
IDENTITY = ROOT / "NextGen.World/Data/KingdomQuestCharacterIdentity.cs"
PACKET_HELPER = ROOT / "NextGen.World/Handlers/PacketHelper.cs"
ADMISSION = ROOT / "NextGen.World/Data/KingdomQuestAdmissionCoordinator.cs"
INTER_HEADER = ROOT / "NextGen.InterLib/Networking/InterHeader.cs"
ZONE_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestZoneRuntime.cs"
SCENARIOBOOK_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestScenarioBookShelfSource.cs"
ZONE_CHARACTER = ROOT / "NextGen.Zone/Game/ZoneCharacter.cs"
RECONNECT_SERVICE = ROOT / "NextGen.World/Data/KingdomQuestReconnectService.cs"
WORLD_HANDLER4 = ROOT / "NextGen.World/Handlers/Handler4.cs"
CLIENT_TRANSFER = ROOT / "NextGen.Util/ClientTransfer.cs"
ZONE_HANDLER6 = ROOT / "NextGen.Zone/Handlers/Handler6.cs"
WORLD_SCHEDULER = ROOT / "NextGen.World/Data/KingdomQuestSourceScheduler.cs"

def require(text, tokens, label):
    missing = [t for t in tokens if t not in text]
    if missing:
        print(f"FAIL: {label}: missing {missing}")
        return False
    return True

def main():
    files = [CENUM, SENUM, PROTO, INFO, CHAR_SAVE_LOCATION, HANDLER, SERVER_PROTO, STATE, DEFINITIONS, JOIN_LIST_REPLY, PARTICIPANTS, WORLD_CLIENT, SESSION_COORDINATOR, MAKE_ACK_REGISTRY, WORLD_INTER, WORLD_ZONE_CONNECTION, ZONE_INTER, CHARACTER, READ_METHODS, WORLD_SCHEMA, MEMBERSHIP, VOTE_STATE, IDENTITY, PACKET_HELPER, ADMISSION, INTER_HEADER, ZONE_RUNTIME, SCENARIOBOOK_SOURCE, ZONE_CHARACTER, RECONNECT_SERVICE, WORLD_HANDLER4, CLIENT_TRANSFER, ZONE_HANDLER6, WORLD_SCHEDULER]
    missing = [str(p) for p in files if not p.is_file()]
    if missing:
        print("FAIL: KQ audit files missing:", missing)
        return 1

    cenum = CENUM.read_text(encoding="utf-8")
    senum = SENUM.read_text(encoding="utf-8")
    proto = PROTO.read_text(encoding="utf-8")
    info = INFO.read_text(encoding="utf-8")
    char_save_location = CHAR_SAVE_LOCATION.read_text(encoding="utf-8")
    handler = HANDLER.read_text(encoding="utf-8")
    server_proto = SERVER_PROTO.read_text(encoding="utf-8")
    state = STATE.read_text(encoding="utf-8")
    definitions = DEFINITIONS.read_text(encoding="utf-8")
    join_list_reply = JOIN_LIST_REPLY.read_text(encoding="utf-8")
    participants = PARTICIPANTS.read_text(encoding="utf-8")
    world_client = WORLD_CLIENT.read_text(encoding="utf-8")
    session_coordinator = SESSION_COORDINATOR.read_text(encoding="utf-8")
    make_ack_registry = MAKE_ACK_REGISTRY.read_text(encoding="utf-8")
    world_inter = WORLD_INTER.read_text(encoding="utf-8")
    world_zone_connection = WORLD_ZONE_CONNECTION.read_text(encoding="utf-8")
    zone_inter = ZONE_INTER.read_text(encoding="utf-8")
    character = CHARACTER.read_text(encoding="utf-8")
    read_methods = READ_METHODS.read_text(encoding="utf-8")
    world_schema = WORLD_SCHEMA.read_text(encoding="utf-8")
    membership = MEMBERSHIP.read_text(encoding="utf-8")
    vote_state = VOTE_STATE.read_text(encoding="utf-8")
    identity = IDENTITY.read_text(encoding="utf-8")
    packet_helper = PACKET_HELPER.read_text(encoding="utf-8")
    admission = ADMISSION.read_text(encoding="utf-8")
    inter_header = INTER_HEADER.read_text(encoding="utf-8")
    zone_runtime = ZONE_RUNTIME.read_text(encoding="utf-8")
    scenario_book_source = SCENARIOBOOK_SOURCE.read_text(encoding="utf-8")
    zone_character = ZONE_CHARACTER.read_text(encoding="utf-8")
    reconnect_service = RECONNECT_SERVICE.read_text(encoding="utf-8")
    world_handler4 = WORLD_HANDLER4.read_text(encoding="utf-8")
    client_transfer = CLIENT_TRANSFER.read_text(encoding="utf-8")
    zone_handler6 = ZONE_HANDLER6.read_text(encoding="utf-8")
    world_scheduler = WORLD_SCHEDULER.read_text(encoding="utf-8")

    if not require(cenum, [
        "KingdomQuestListReq = 1",
        "KingdomQuestStatusReq = 3",
        "KingdomQuestJoinReq = 5",
        "KingdomQuestScheduleReq = 9",
        "KingdomQuestListRefreshReq = 27",
        "KingdomQuestVoteStartReq = 39",
        "KingdomQuestVoteVotingReq = 42",
        "KingdomQuestJoinListReq = 49",
        "KingdomQuestVoteStartCheckReq = 52",
    ], "native CH22 KQ request names"):
        return 1

    if not require(senum, [
        "KingdomQuestListAck = 2",
        "KingdomQuestStatusAck = 4",
        "KingdomQuestJoinAck = 6",
        "KingdomQuestJoinCancelAck = 8",
        "KingdomQuestScheduleAck = 10",
        "KingdomQuestNotify = 11",
        "KingdomQuestFailed = 19",
        "KingdomQuestRestDeadNum = 24",
        "KingdomQuestEntryResponseAck = 26",
        "KingdomQuestListTimeAck = 28",
        "KingdomQuestListAddAck = 29",
        "KingdomQuestListDeleteAck = 30",
        "KingdomQuestListUpdateAck = 31",
        "KingdomQuestMobKillNumber = 34",
        "KingdomQuestJoiningAlarm = 36",
        "KingdomQuestJoiningAlarmEnd = 37",
        "KingdomQuestJoiningAlarmList = 38",
        "KingdomQuestVoteStartAck = 40",
        "KingdomQuestVoteVotingCmd = 41",
        "KingdomQuestVoteVotingAck = 43",
        "KingdomQuestVoteResultSuccess = 44",
        "KingdomQuestVoteResultFail = 45",
        "KingdomQuestVoteCancel = 46",
        "KingdomQuestVoteBanMessage = 47",
        "KingdomQuestVoteBanMessageLogoff = 48",
        "KingdomQuestJoinListAck = 50",
        "KingdomQuestLinkToForceByBan = 51",
        "KingdomQuestVoteStartCheckAck = 53",
        "KingdomQuestTeamSelectAck = 56",
        "KingdomQuestTeamSelectCmd = 57",
        "KingdomQuestTeamTypeCmd = 58",
        "KingdomQuestPlayerDisjoin = 59",
    ], "native SH22 KQ names"):
        return 1

    if not require(info, [
        "public const int WireSize = 141",
        "public new const int WireSize = 377",
        "packet.WriteString(Title ?? string.Empty, 64);",
        "packet.WriteLong(DemandClass);",
        "new KingdomQuestMapProtocolInfo[4]",
        "packet.WriteString(MapBase ?? string.Empty, 12);",
        "packet.WriteString(MapName ?? string.Empty, 12);",
        "packet.WriteString(ScriptLanguage ?? string.Empty, 32);",
        "packet.WriteString(ScriptInitValue ?? string.Empty, 32);",
        "new KingdomQuestXY[2]",
        "public static bool TryRead(Packet packet, out KingdomQuestProtocolInfo value)",
        "KingdomQuestClientInfo.TryReadInto(packet, result)",
        "KingdomQuestMapProtocolInfo.TryRead(packet, out mapLinks[i])",
        "KingdomQuestXY.TryRead(packet, out teamRegen[i])",
        "public static bool TryRead(Packet packet, out KingdomQuestZoneJoinerInfo value)",
        "public static bool TryRead(Packet packet, out KingdomQuestJoinCharacterInfo value)",
        "public const int WireSize = 23",
        "public const int WireSize = 5",
        "packet.WriteUInt(CharacterNumber);",
        "packet.WriteByte(TeamType);",
        "packet.WriteString(Name ?? string.Empty, 20);",
        "packet.WriteByte(Team);",
        "YearFrom1900 = local.Year - 1900",
        "DayOfYearFromZero = local.DayOfYear - 1",
        "packet.WriteInt(YearFrom1900);",
        "packet.WriteInt(DayOfYearFromZero);",
    ], "native PROTO_KQ_INFO client/server serializers"):
        return 1

    for stale in (
        "KingdomQuestInstanceInfo",
        "KingdomQuestRegistrationAck",
        "KingdomQuestInstanceGroup",
        "KingdomQuestRecruitmentUpdate",
        "KingdomQuestInstanceState",
        "KingdomQuestInstanceResync",
        "KingdomQuestRegistrationNotice",
    ):
        if stale in proto or stale in handler:
            print("FAIL: stale guessed KQ semantic name remains in runtime:", stale)
            return 1

    checks = {
        "status ack": [
            "new Packet(SH22Type.KingdomQuestStatusAck)",
            "packet.WriteUInt(state.Handle);",
            "packet.WriteByte(state.Status);",
            "packet.WriteUShort((ushort)state.JoinerNames.Count);",
            "packet.WriteString(state.JoinerNames[i] ?? string.Empty, 20);",
        ],
        "join ack": [
            "new Packet(SH22Type.KingdomQuestJoinAck)",
            "packet.WriteUInt(handle);",
            "packet.WriteUShort(error);",
        ],
        "list time": [
            "new Packet(SH22Type.KingdomQuestListTimeAck)",
            "WriteServerTime(packet, now);",
            "packet.WriteInt((int)unix);",
            "KingdomQuestNativeTime.FromLocalDateTime(now.LocalDateTime).Write(packet);",
        ],
        "list add": [
            "new Packet(SH22Type.KingdomQuestListAddAck)",
            "packet.WriteUShort((ushort)entries.Count);",
            "entries[i].Write(packet);",
        ],
        "list update": [
            "new Packet(SH22Type.KingdomQuestListUpdateAck)",
            "packet.WriteUInt(states[i].Handle);",
            "packet.WriteByte(states[i].Status);",
            "packet.WriteUShort((ushort)states[i].JoinerNames.Count);",
        ],
        "remaining dead count": [
            "new Packet(SH22Type.KingdomQuestRestDeadNum)",
            "packet.WriteByte(number);",
        ],
        "entry response ack": [
            "new Packet(SH22Type.KingdomQuestEntryResponseAck)",
            "packet.WriteByte(reply);",
            "packet.WriteUInt(encodedHandle);",
        ],
        "mob kill count": [
            "new Packet(SH22Type.KingdomQuestMobKillNumber)",
            "packet.WriteUShort(currentMobKill);",
            "packet.WriteUShort(demandMobKill);",
        ],
        "team score info": [
            "new Packet(SH22Type.KingdomQuestScoreInfo)",
            "packet.WriteUInt(redScore);",
            "packet.WriteUInt(blueScore);",
        ],
        "score board info": [
            "new Packet(SH22Type.KingdomQuestScoreBoardInfo)",
            "packet.WriteByte(useRound);",
            "packet.WriteByte(round);",
            "packet.WriteByte(redWinFlag);",
            "packet.WriteByte(redScore);",
            "packet.WriteByte(blueWinFlag);",
            "packet.WriteByte(blueScore);",
        ],
        "winter event score": [
            "new Packet(SH22Type.KingdomQuestWinterEvent2014Score)",
            "packet.WriteByte(redWinFlag);",
            "packet.WriteByte(redScore);",
            "packet.WriteByte(blueWinFlag);",
            "packet.WriteByte(blueScore);",
        ],
        "joining alarm": [
            "new Packet(SH22Type.KingdomQuestJoiningAlarm)",
            "packet.WriteUShort(state.ID);",
            "packet.WriteByte(state.MinLevel);",
            "packet.WriteByte(state.MaxLevel);",
        ],
        "joining alarm end": [
            "new Packet(SH22Type.KingdomQuestJoiningAlarmEnd)",
            "packet.WriteUInt(handle);",
            "packet.WriteUShort(id);",
        ],
        "joining alarm list": [
            "new Packet(SH22Type.KingdomQuestJoiningAlarmList)",
            "packet.WriteUShort((ushort)states.Count);",
            "WriteJoiningAlarmInfo(packet, states[i]);",
        ],
        "join cancel ack": [
            "new Packet(SH22Type.KingdomQuestJoinCancelAck)",
            "packet.WriteUInt(handle);",
            "packet.WriteUShort(error);",
        ],
        "team select ack": [
            "new Packet(SH22Type.KingdomQuestTeamSelectAck)",
            "packet.WriteUShort(error);",
            "packet.WriteByte(teamType);",
        ],
        "team select cmd": [
            "new Packet(SH22Type.KingdomQuestTeamSelectCmd)",
            "packet.WriteString(characterName ?? string.Empty, 20);",
            "packet.WriteByte(teamType);",
        ],
        "team type cmd": [
            "new Packet(SH22Type.KingdomQuestTeamTypeCmd)",
            "packet.WriteByte(teamType);",
        ],
        "player disjoin cmd": [
            "new Packet(SH22Type.KingdomQuestPlayerDisjoin)",
            "packet.WriteUInt(handle);",
            "packet.WriteUInt(characterNumber);",
        ],
        "vote start ack": [
            "new Packet(SH22Type.KingdomQuestVoteStartAck)",
            "packet.WriteUShort(error);",
        ],
        "vote voting cmd": [
            "new Packet(SH22Type.KingdomQuestVoteVotingCmd)",
            "packet.WriteString(starter ?? string.Empty, 20);",
            "packet.WriteString(target ?? string.Empty, 20);",
            "endTime.Write(packet);",
            "packet.WriteByte((byte)data.Length);",
        ],
        "vote voting ack": [
            "new Packet(SH22Type.KingdomQuestVoteVotingAck)",
            "packet.WriteUShort(error);",
        ],
        "vote result success": [
            "new Packet(SH22Type.KingdomQuestVoteResultSuccess)",
            "packet.WriteString(target ?? string.Empty, 20);",
            "packet.WriteByte(voteRate);",
            "packet.WriteByte(yes);",
            "packet.WriteByte(no);",
            "packet.WriteByte(cancel);",
        ],
        "vote result fail": [
            "new Packet(SH22Type.KingdomQuestVoteResultFail)",
            "packet.WriteString(target ?? string.Empty, 20);",
            "packet.WriteByte(yes);",
            "packet.WriteByte(no);",
            "packet.WriteByte(cancel);",
        ],
        "vote cancel": [
            "new Packet(SH22Type.KingdomQuestVoteCancel)",
            "packet.WriteString(target ?? string.Empty, 20);",
        ],
        "vote ban message": [
            "new Packet(SH22Type.KingdomQuestVoteBanMessage)",
            "packet.WriteByte(voteRate);",
        ],
        "force-link after ban": [
            "new Packet(SH22Type.KingdomQuestLinkToForceByBan)",
            "packet.WriteUInt(characterNumber);",
            "packet.WriteString(mapNames[i] ?? string.Empty, 12);",
        ],
        "vote start-check ack": [
            "new Packet(SH22Type.KingdomQuestVoteStartCheckAck)",
            "packet.WriteUShort(error);",
        ],
        "join list ack": [
            "new Packet(SH22Type.KingdomQuestJoinListAck)",
            "packet.WriteUShort(error);",
            "packet.WriteByte((byte)joiners.Count);",
            "joiners[i].Write(packet);",
        ],
    }
    for label, tokens in checks.items():
        if not require(proto, tokens, label):
            return 1

    if "0x4d0bc167" in handler or "new Packet(0x581C)" in handler:
        print("FAIL: legacy truncated KQ list-time stub reintroduced")
        return 1

    if not require(handler, [
        "[PacketHandler(CH22Type.KingdomQuestListReq)]",
        "SnapshotClientVisible()",
        "snapshot[i].Status <= ClientVisibleMaxStatus",
        "ResolveAckBounds(entries, out newStartHandle, out newEndHandle)",
        "KingdomQuestProtocol.CreateListAck(",
        "[PacketHandler(CH22Type.KingdomQuestScheduleReq)]",
        "SnapshotAllScheduled()",
        "KingdomQuestProtocolDefinitionRegistry.Snapshot()",
        "KingdomQuestProtocol.CreateScheduleAck(",
        "[PacketHandler(CH22Type.KingdomQuestStatusReq)]",
        "KingdomQuestProtocol.CreateStatusAck(state)",
        "[PacketHandler(CH22Type.KingdomQuestJoinListReq)]",
        "packet.TryReadUInt(out requestedHandle)",
        "client.KingdomQuestHandle.HasValue",
        "current.Status == KingdomQuestNativeConstants.StatusRunning",
        "effectiveHandle = client.KingdomQuestHandle.Value",
        "KingdomQuestParticipantRegistry.TryGet(",
        "KingdomQuestNativeConstants.JoinListInvalidHandle",
        "client.KingdomQuestJoinListLastRequestTime.HasValue",
        "KingdomQuestNativeConstants.JoinListCooldownSeconds > now",
        "KingdomQuestNativeConstants.JoinListCooldown",
        "client.KingdomQuestJoinListLastRequestTime = now",
        "KingdomQuestNativeConstants.JoinListSuccess",
        "[PacketHandler(CH22Type.KingdomQuestTeamSelectReq)]",
        "packet.TryReadByte(out requestedTeamType)",
        "KingdomQuestNativeConstants.TeamSelectInvalidHandle",
        "KingdomQuestSessionCoordinator.TrySelectUserTeam(",
        "KingdomQuestProtocol.CreateTeamSelectAck(",
        "result.Error != KingdomQuestNativeConstants.TeamSelectSuccess",
        "KingdomQuestProtocol.CreateTeamSelectCmd(",
        "result.OtherCharacterNumbers.Count",
        "other.KingdomQuestHandle.Value != result.Handle",
        "[PacketHandler(CH22Type.KingdomQuestListRefreshReq)]",
        "if (!client.KingdomQuestListTimeSent)",
        "KingdomQuestProtocol.CreateListTime(DateTimeOffset.Now)",
        "KingdomQuestProtocol.CreateListDelete(",
        "KingdomQuestProtocol.CreateListUpdateFromDefinitions(",
        "SendListAdds(client, added.AsReadOnly())",
        "ListAddBatchEntries = 53",
        "if (!client.Character.IsIngame)",
    ], "KQ status/list-refresh handlers"):
        return 1

    if "CreateEmptyListAdd()" in handler:
        print("FAIL: LIST_REFRESH reverted to a hard-coded empty KQ list")
        return 1

    if not require(handler, [
        "[PacketHandler(CH22Type.KingdomQuestJoinReq)]",
        "KingdomQuestAdmissionCoordinator.TryGetPreJoinError(",
        "original PrisonMin is unknown",
        "PlayerDisjoin(client);",
        "KingdomQuestAdmissionCoordinator.TryAddMembership(",
        "KingdomQuestProtocol.CreateJoinAck(handle, error)",
        "BroadcastJoinList(handle)",
        "[PacketHandler(CH22Type.KingdomQuestJoinCancelReq)]",
        "KingdomQuestNativeConstants.JoinCancelSuccess",
        "KingdomQuestNativeConstants.JoinCancelNotJoined",
        "KingdomQuestProtocol.CreateJoinCancelAck(",
        "requestedHandle, error",
    ], "live source-backed JOIN/JOIN_CANCEL handlers"):
        return 1

    precheck_pos = handler.find("KingdomQuestAdmissionCoordinator.TryGetPreJoinError(")
    disjoin_pos = handler.find("PlayerDisjoin(client);", precheck_pos)
    add_pos = handler.find("KingdomQuestAdmissionCoordinator.TryAddMembership(", disjoin_pos)
    if precheck_pos < 0 or disjoin_pos < precheck_pos or add_pos < disjoin_pos:
        print("FAIL: JOIN request no longer preserves prison/same-handle -> PlayerDisjoin -> PlayerJoin order")
        return 1

    if not require(char_save_location, [
        "class CharacterSaveLocationProtocolInfo",
        "WireSize = 48",
        "MapNameSize = 12",
        "packet.WriteUInt(CharacterNumber)",
        "packet.WriteString(MapName ?? string.Empty, MapNameSize)",
        "packet.WriteUInt(KingdomQuestHandle)",
        "packet.WriteString(",
        "KingdomQuestMapName ?? string.Empty, MapNameSize",
        "packet.WriteInt(KingdomQuestX)",
        "packet.WriteInt(KingdomQuestY)",
        "Zone.pdb names the fields chrregnum, coord, kqhandle, map_kq",
    ], "native 48-byte character save-location/KQ persistence boundary"):
        return 1

    if not require(info, [
        "MakeAckSuccess = 0x0981",
        "MakeAckDuplicateHandle = 0x0982",
        "MakeAckTooManyQuest = 0x0983",
        "MakeAckScriptNotFound = 0x098C",
        "JoinSuccess = 0x0991",
        "JoinInvalidHandle = 0x0992",
        "JoinCapacityReached = 0x0993",
        "JoinWrongStatus = 0x0994",
        "JoinLevelRejected = 0x0995",
        "JoinClassRejected = 0x0996",
        "JoinGenderRejected = 0x0997",
        "JoinUnexpectedResult = 0x0998",
        "JoinPrisonRestricted = 0x0999",
        "JoinAlreadyInRequestedKq = 0x099A",
        "JoinCancelSuccess = 0x09A1",
        "JoinCancelNotJoined = 0x09A2",
        "JoinListSuccess = 0x3118",
        "JoinListInvalidHandle = 0x3119",
        "JoinListCooldown = 0x311A",
        "JoinListCooldownSeconds =",
        "KingdomQuestSingleDataInfo.KqPlayerListResetListCoolTimeSeconds",
        "VoteLimitSeconds =",
        "KingdomQuestSingleDataInfo.KqVoteVoteLimitTimeSeconds",
        "VoteSuggestCooldownSeconds =",
        "KingdomQuestSingleDataInfo.KqVoteSuggestCoolTimeSeconds",
        "VoteLoginCooldownSeconds =",
        "KingdomQuestSingleDataInfo.KqVoteLoginCoolTimeSeconds",
        "VoteStartSuccess = 0x3100",
        "VoteStartInvalidHandle = 0x3101",
        "VoteStartWrongStatus = 0x3102",
        "VoteStartAlreadyRunning = 0x3103",
        "VoteStartTargetRejected = 0x3105",
        "VoteStartSelfTarget = 0x3106",
        "VoteStartEmptyContents = 0x3107",
        "VoteStartSuggestCooldown = 0x3108",
        "VoteStartDisabled = 0x3109",
        "VoteVotingSuccess = 0x3110",
        "VoteVotingInvalidJoiner = 0x3111",
        "VoteVotingWrongVoteOrTeam = 0x3112",
        "VoteVotingNotEligible = 0x3113",
        "VoteStartCheckSuccess = 0x3120",
        "VoteStartCheckAlreadyRunning = 0x3121",
        "VoteStartCheckSuggestCooldown = 0x3122",
        "VoteChoiceCancel = 0",
        "VoteChoiceYes = 1",
        "VoteChoiceNo = 2",
        "VoteChoiceMax = 3",
        "TeamSelectSuccess = 0x31F0",
        "TeamSelectInvalidHandle = 0x31F1",
        "TeamSelectWrongStatus = 0x31F2",
        "TeamSelectSameTeam = 0x31F3",
        "TeamSelectTargetTeamLimit = 0x31F4",
        "TeamSelectMemberGap = 0x31F5",
        "TeamSelectMissingTeamData = 0x31F6",
        "TeamSelectWrongDivideType = 0x31F7",
        "StatusMakeRequested = 1",
        "StatusJoining = 2",
        "StatusNoMap = 8",
        "RandomTeamDivideType = 1",
        "UserSelectTeamDivideType = 2",
        "NeutralTeamType = 2",
    ], "native KQ admission/lifecycle constants"):
        return 1

    if not require(character, [
        "public short? PrisonMinutes",
        "KQ JOIN must not silently treat it as zero",
    ], "source-backed nullable prison state"):
        return 1

    if not require(read_methods, [
        'row.IsNull("PrisonMin")',
        '? (short?)null',
        'GetDataTypes.Getshort(row["PrisonMin"])',
    ], "database prison-state load"):
        return 1

    if not require(world_client, [
        'row.IsNull("PrisonMin")',
        '? (short?)null',
        'GetDataTypes.Getshort(row["PrisonMin"])',
    ], "World character-list prison-state load"):
        return 1

    if not require(world_schema, [
        '`PrisonMin` SMALLINT NULL DEFAULT NULL',
        'original value unknown',
    ], "fail-closed prison-state schema"):
        return 1
    if '`PrisonMin` SMALLINT NOT NULL DEFAULT 0' in world_schema:
        print("FAIL: unresolved original nPrisonMin default was guessed as zero")
        return 1

    if not require(character, [
        "public int? KingdomQuestHandle",
        "public string KingdomQuestMapName",
        "public int? KingdomQuestX",
        "public int? KingdomQuestY",
        "public DateTime? KingdomQuestDate",
        "0xFFFFFFFF no-KQ value",
    ], "source-backed Character KQ persistence state"):
        return 1

    for source_text, label in (
        (read_methods, "Zone/shared character KQ DB load"),
        (world_client, "World character-list KQ DB load"),
    ):
        if not require(source_text, [
            'row.IsNull("KQHandle")',
            'GetDataTypes.GetInt(row["KQHandle"])',
            'row.IsNull("KQMap")',
            'row["KQMap"].ToString()',
            'row.IsNull("KQX")',
            'row.IsNull("KQY")',
            'row.IsNull("KQDate")',
            'Convert.ToDateTime(row["KQDate"])',
        ], label):
            return 1

    if not require(world_schema, [
        '`KQHandle` INT NULL DEFAULT NULL',
        '`KQMap` VARCHAR(16) NULL DEFAULT NULL',
        '`KQX` INT NULL DEFAULT NULL',
        '`KQY` INT NULL DEFAULT NULL',
        '`KQDate` DATETIME NULL DEFAULT NULL',
        'p_Char_SaveLocation parameters',
        'GetDate()',
    ], "source-backed KQ persistence schema"):
        return 1

    if not require(zone_runtime, [
        "public static bool TryGetByMap(",
        "current.MapID == mapId",
        "current.MapInstance == mapInstance",
    ], "KQ instance reverse lookup for save-location suffix"):
        return 1

    if not require(zone_character, [
        "KingdomQuestZoneRuntimeRegistry.TryGetByMap(",
        "kingdomQuestHandle = unchecked((int)kingdomQuestState.Handle)",
        "link.MapBase, Map.MapInfo.ShortName",
        "kingdomQuestMapName = link.MapName",
        "Character.KingdomQuestHandle = kingdomQuestHandle",
        '"KQHandle=@kqHandle, KQMap=@kqMap, KQX=@kqX, KQY=@kqY, "',
        '"KQDate=CURRENT_TIMESTAMP "',
        'new MySqlParameter("@kqHandle", Character.KingdomQuestHandle)',
        'new MySqlParameter("@kqMap", Character.KingdomQuestMapName ?? string.Empty)',
    ], "live native KQ save-location suffix persistence"):
        return 1

    if not require(reconnect_service, [
        "class KingdomQuestReconnectService",
        "KingdomQuestReconnectRules.TryIsExistingFromDatabase(",
        "KingdomQuestMembershipRegistry.TryGet(handle, out members)",
        "Name5Equals(member.Name, source.Name)",
        "KingdomQuestSessionTargetRegistry.TryGet(handle, out target)",
        "client.KingdomQuestHandle = handle",
        "existingJoiner.BanRaw == 1",
        "voteBanLogoff = true",
        "for (int i = 0; i < 20; i++)",
        "Encoding.ASCII.GetBytes",
        "never creates membership",
    ], "native IsExisted -> Name5 JoinerInfoUpdateByLogin rebind"):
        return 1

    if "KingdomQuestMembershipRegistry.Set(" in reconnect_service:
        print("FAIL: reconnect service recreates KQ membership from persisted state")
        return 1

    if not require(world_handler4, [
        "KingdomQuestNativeConstants.VoteLoginCooldownSeconds",
        "client.KingdomQuestVoteSuggestCooldownUntil = unchecked(",
        "KingdomQuestReconnectService.TryRestore(",
        "out voteBanLogoff",
        "Handler22.KingdomQuestLoginBan(",
        "Handler22.FlushKingdomQuestVoteBanLogoff(client)",
        "reconnectTarget.MapID",
        "reconnectTarget.MapInstance",
        "character.Character.PositionInfo.XPos = reconnectX",
        "character.Character.PositionInfo.YPos = reconnectY",
        "kingdomQuestReconnect ? (ushort?)targetMap : null",
    ], "World KQ reconnect routing"):
        return 1

    if not require(client_transfer, [
        "HasPositionOverride",
        "MapOverrideID",
        "MapOverrideX",
        "MapOverrideY",
    ], "KQ reconnect transfer metadata"):
        return 1

    if not require(world_zone_connection, [
        "packet.WriteBool(mapOverrideId.HasValue)",
        "packet.WriteUShort(mapOverrideId.Value)",
        "packet.WriteInt(mapOverrideX)",
        "packet.WriteInt(mapOverrideY)",
    ], "World -> Zone KQ reconnect position override"):
        return 1

    if not require(zone_inter, [
        "packet.TryReadBool(out hasPositionOverride)",
        "packet.TryReadUShort(out mapOverrideId)",
        "packet.TryReadInt(out mapOverrideX)",
        "packet.TryReadInt(out mapOverrideY)",
        "hasPositionOverride ? (ushort?)mapOverrideId : null",
    ], "Zone KQ reconnect transfer receiver"):
        return 1

    if not require(zone_handler6, [
        "transfer.HasPositionOverride",
        "(ushort?)transfer.MapOverrideID",
        "(int?)transfer.MapOverrideX",
        "(int?)transfer.MapOverrideY",
    ], "Zone login KQ position override handoff"):
        return 1

    if not require(zone_character, [
        "ushort? mapOverrideId = null",
        "mapOverrideX.HasValue",
        "mapOverrideY.HasValue",
        "Character.PositionInfo.Map = mapOverrideId.Value",
        "Character.PositionInfo.XPos = mapOverrideX.Value",
        "Character.PositionInfo.YPos = mapOverrideY.Value",
    ], "ZoneCharacter KQ reconnect map/instance position application"):
        return 1

    if not require(admission, [
        "class KingdomQuestAdmissionCoordinator",
        "!client.Character.Character.PrisonMinutes.HasValue",
        "alreadyInRequestedKq",
        "TryRemoveCurrentMembership",
        "TryRemoveMembership(",
        "client.KingdomQuestHandle.Value",
        "uint targetCharacterNumber = characterNumber",
        "v => v.CharacterNumber == targetCharacterNumber",
        "KingdomQuestSessionCoordinator.TrySetMembership(",
        "CompleteDisjoin",
        "client.KingdomQuestHandle = null",
        "TryAddMembership",
        "KingdomQuestAdmissionRules.EvaluatePlayerJoin(",
        "KingdomQuestCharacterIdentity.TryGetCharacterNumber(",
        "KingdomQuestAdmissionRules.AssignInitialTeam(",
        "InVoteRaw = 0",
        "BanRaw = 0",
        "VotingCount = 0",
        "client.KingdomQuestHandle = handle",
    ], "atomic native KQ admission membership mutations"):
        return 1

    if not require(handler, [
        "BroadcastPlayerDisjoinToZones",
        "InterHeader.KingdomQuestPlayerDisjoin",
        "BroadcastJoinList(oldHandle)",
        "KingdomQuestAdmissionCoordinator.CompleteDisjoin(",
        "memberClient.KingdomQuestJoinListLastRequestTime = now",
    ], "native PlayerDisjoin broadcast ordering"):
        return 1

    if not require(inter_header, [
        "KingdomQuestPlayerDisjoin = 0x400A",
    ], "emulator transport for native PLAYER_DISJOIN"):
        return 1

    if not require(zone_inter, [
        "[InterPacketHandler(InterHeader.KingdomQuestPlayerDisjoin)]",
        "native.OpCode != 0x583B",
        "native.TryReadUInt(out handle)",
        "native.TryReadUInt(out characterNumber)",
        "KingdomQuestZoneRuntimeRegistry.TryDisjoin(",
    ], "Zone native PLAYER_DISJOIN receiver"):
        return 1

    if not require(zone_runtime, [
        "public static bool TryDisjoin(uint handle, uint characterNumber)",
        "v => v.CharacterNumber == characterNumber",
        "roster.RemoveAt(index)",
        "current.State, current.Definition, roster",
    ], "Zone KQ player-info deletion"):
        return 1

    if not require(membership, [
        "class KingdomQuestMembershipEntry",
        "public uint CharacterNumber",
        "public byte Level",
        "public byte Class",
        "public string Name",
        "public byte TeamType",
        "public uint InVoteRaw",
        "public uint BanRaw",
        "public byte VotingCount",
        "InVoteRaw != 0",
        "BanRaw == 1",
        "InVoteRaw = InVoteRaw",
        "BanRaw = BanRaw",
        "VotingCount = VotingCount",
        "TrySetNativeVoteFields(",
        "current[index].InVoteRaw = inVoteRaw",
        "current[index].BanRaw = banRaw",
        "current[index].VotingCount = votingCount",
        "ToClientInfo()",
        "ToZoneInfo()",
        "never infers one identity from the other",
    ], "combined KQ native membership identity and exact vote fields"):
        return 1

    if not require(vote_state, [
        "class KingdomQuestVoteState",
        "StarterIndex = -1",
        "TargetIndex = -1",
        "TeamType = KingdomQuestNativeConstants.NeutralTeamType",
        "class KingdomQuestVoteCoordinator",
        "public static bool TryPrepareStart(",
        "VoteStartInvalidHandle",
        "VoteStartWrongStatus",
        "VoteStartAlreadyRunning",
        "VoteStartDisabled",
        "VoteStartTargetRejected",
        "target.BanRaw == 1",
        "VoteStartSelfTarget",
        "contentsLength == 0",
        "VoteStartEmptyContents",
        "VoteStartSuggestCooldown",
        "members[i].InVoteRaw = 1",
        "cancelCount++",
        "public static bool TryRecordVote(",
        "VoteVotingInvalidJoiner",
        "VoteVotingWrongVoteOrTeam",
        "VoteVotingNotEligible",
        "members[memberIndex].InVoteRaw = 0",
        "VoteChoiceYes",
        "VoteChoiceNo",
        "public static bool TryResolveExpired(",
        "state.EndTime >= currentTime",
        "rateIndex = target.VotingCount",
        "rateIndex =",
        "target.VotingCount++",
        "yesRate >= requiredRate",
        "members[i].InVoteRaw = 0",
        "target.BanRaw = 1",
        "v.BanRaw != 0",
        "class KingdomQuestVoteCancelPlan",
        "public static bool TryCancelForTargetDisjoin(",
        "target.CharacterNumber != characterNumber",
        "target.BanRaw != 0",
        "members[i].InVoteRaw = 0",
        "ByHandle.Remove(handle)",
    ], "native KQ vote state/result projection"):
        return 1

    if not require(handler, [
        "[PacketHandler(CH22Type.KingdomQuestVoteStartReq)]",
        "packet.TryReadString(out targetName, 20)",
        "packet.TryReadByte(out voteType)",
        "packet.TryReadByte(out contentsLength)",
        "KingdomQuestNativeConstants.VoteLimitSeconds",
        "KingdomQuestNativeConstants.VoteSuggestCooldownSeconds",
        "KingdomQuestVoteCoordinator.TryPrepareStart(",
        "KingdomQuestProtocol.CreateVoteStartAck(error)",
        "KingdomQuestProtocol.CreateVoteVotingCmd(",
        "plan.VoterCharacterNumbers.Count",
        "[PacketHandler(CH22Type.KingdomQuestVoteVotingReq)]",
        "packet.TryReadInt(out choice)",
        "KingdomQuestVoteCoordinator.TryRecordVote(",
        "KingdomQuestProtocol.CreateVoteVotingAck(error)",
        "[PacketHandler(CH22Type.KingdomQuestVoteStartCheckReq)]",
        "KingdomQuestVoteCoordinator.TryGetState(handle, out state)",
        "VoteStartCheckAlreadyRunning",
        "VoteStartCheckSuggestCooldown",
        "internal static void ProcessKingdomQuestVotes(DateTime localNow)",
        "KingdomQuestVoteCoordinator.TryGetBannedMembers(",
        "KingdomQuestProtocol.CreateLinkToForceByBan(",
        "KingdomQuestVoteCoordinator.TryResolveExpired(",
        "KingdomQuestProtocol.CreateVoteResultSuccess(",
        "KingdomQuestProtocol.CreateVoteResultFail(",
        "KingdomQuestProtocol.CreateVoteBanMessage(",
        "KingdomQuestVoteCoordinator.TryCancelForTargetDisjoin(",
        "KingdomQuestProtocol.CreateVoteCancel(plan.TargetName)",
    ], "live native KQ vote transport"):
        return 1

    vote_start = handler.find(
        "[PacketHandler(CH22Type.KingdomQuestVoteStartReq)]")
    vote_start_ack = handler.find(
        "KingdomQuestProtocol.CreateVoteStartAck(error)", vote_start)
    vote_voting_cmd = handler.find(
        "KingdomQuestProtocol.CreateVoteVotingCmd(", vote_start_ack)
    if not (0 <= vote_start < vote_start_ack < vote_voting_cmd):
        print("FAIL: native VOTE_START ACK-before-VOTING_CMD order changed")
        return 1

    vote_process = handler.find(
        "internal static void ProcessKingdomQuestVotes(DateTime localNow)")
    force_link = handler.find(
        "KingdomQuestProtocol.CreateLinkToForceByBan(", vote_process)
    resolve_vote = handler.find(
        "KingdomQuestVoteCoordinator.TryResolveExpired(", vote_process)
    if not (0 <= vote_process < force_link < resolve_vote):
        print("FAIL: VoteProcessing ban-link-before-expiry order changed")
        return 1

    disjoin = handler.find(
        "private static bool PlayerDisjoin(\n            WorldClient client, uint oldHandle, uint characterNumber)")
    cancel_vote = handler.find(
        "KingdomQuestVoteCoordinator.TryCancelForTargetDisjoin(", disjoin)
    remove_member = handler.find(
        "KingdomQuestAdmissionCoordinator.TryRemoveMembership(", disjoin)
    if not (0 <= disjoin < cancel_vote < remove_member):
        print("FAIL: PlayerDisjoin target-vote cancellation order changed")
        return 1

    if not require(world_client, [
        "int? KingdomQuestVoteSuggestCooldownUntil",
        "bool KingdomQuestVoteBanLogoffPending",
    ], "native KQ vote session deadlines/ban-logoff flag"):
        return 1

    if not require(handler, [
        "internal static void KingdomQuestLoginBan(",
        "client.KingdomQuestVoteBanLogoffPending = true",
        "PlayerDisjoin(client, handle, characterNumber)",
        "internal static void FlushKingdomQuestVoteBanLogoff(",
        "KingdomQuestProtocol.CreateVoteBanMessageLogoff()",
        "client.KingdomQuestVoteBanLogoffPending = false",
    ], "native login-ban deferred VOTE_BAN_MSG_LOGOFF path"):
        return 1

    login_ban = handler.find("internal static void KingdomQuestLoginBan(")
    pending_set = handler.find(
        "client.KingdomQuestVoteBanLogoffPending = true", login_ban)
    login_disjoin = handler.find(
        "PlayerDisjoin(client, handle, characterNumber)", pending_set)
    flush = handler.find(
        "internal static void FlushKingdomQuestVoteBanLogoff(", login_disjoin)
    logoff_send = handler.find(
        "KingdomQuestProtocol.CreateVoteBanMessageLogoff()", flush)
    pending_clear = handler.find(
        "client.KingdomQuestVoteBanLogoffPending = false", logoff_send)
    if not (0 <= login_ban < pending_set < login_disjoin <
            flush < logoff_send < pending_clear):
        print("FAIL: login-ban flag/disjoin/logoff ordering changed")
        return 1

    login_cool = world_handler4.find(
        "client.KingdomQuestVoteSuggestCooldownUntil = unchecked(")
    reconnect = world_handler4.find(
        "KingdomQuestReconnectService.TryRestore(", login_cool)
    login_ban_call = world_handler4.find(
        "Handler22.KingdomQuestLoginBan(", reconnect)
    login_logoff_call = world_handler4.find(
        "Handler22.FlushKingdomQuestVoteBanLogoff(client)", login_ban_call)
    if not (0 <= login_cool < reconnect < login_ban_call < login_logoff_call):
        print("FAIL: character-login KQ vote cooldown/ban ordering changed")
        return 1

    if not require(world_scheduler, [
        "Handler22.ProcessKingdomQuestVotes(local)",
    ], "continuous native KQ vote processing"):
        return 1

    if "Math.Round" in vote_state or "Random" in vote_state:
        print("FAIL: KQ vote result gained inferred rounding/randomness")
        return 1

    for forbidden in (
        "Character.ID",
        "GetClientByChar",
        "GetClientByName",
        "Group",
        "Party",
    ):
        if forbidden in membership:
            print("FAIL: KQ membership owner infers native identity/session state:", forbidden)
            return 1

    if not require(session_coordinator, [
        "TryApplyMakeAck",
        "error != KingdomQuestNativeConstants.MakeAckSuccess",
        "StatusNoMap",
        "StatusMakeRequested",
        "StatusJoining",
        "new KingdomQuestJoinCharacterInfo[0]",
        "class KingdomQuestAdmissionRules",
        "TryGetPreJoinError",
        "EvaluatePlayerJoin",
        "JoinHardCapacity",
        "1UL << characterClass",
        "definition.DemandGender & genderBit",
        "AssignInitialTeam",
        "public static bool TrySetMembership",
        "KingdomQuestMembershipRegistry.Set(handle, roster)",
        "KingdomQuestZoneJoinerRegistry.Set(handle, zoneRoster)",
        "team.TeamDivideType != KingdomQuestNativeConstants.UserSelectTeamDivideType",
        "team1Count <= team0Count",
    ], "native KQ admission/MAKE transition rules"):
        return 1

    if not require(world_inter, [
        "[InterPacketHandler(InterHeader.KingdomQuestEnd)]",
        "native.OpCode != 0x5810",
        "KingdomQuestSessionCoordinator.TrySetDone(handle)",
        "zone.SendKingdomQuestDestroy(handle)",
        "KingdomQuestMapAllocationRegistry.Free(handle)",
        "FreeKingdomQuestJoinerSessions(handle)",
        "client.KingdomQuestHandle = null",
    ], "native Z2W END -> World SetDone lifecycle"):
        return 1

    if "SendKingdomQuestEnd(" in world_zone_connection:
        print("FAIL: native Z2W END regressed to a World->Zone sender")
        return 1

    if not require(zone_inter, [
        "public static void SendKingdomQuestEnd(uint handle)",
        "new Packet((ushort)0x5810)",
        "new InterPacket(InterHeader.KingdomQuestEnd)",
        "WorldConnector.Instance.SendPacket(packet)",
    ], "native Zone->World END sender"):
        return 1

    if "[InterPacketHandler(InterHeader.KingdomQuestEnd)]" in zone_inter:
        print("FAIL: native Z2W END is incorrectly consumed as World->Zone")
        return 1

    if not require(session_coordinator, [
        "public static bool TrySetDone(uint handle)",
        "KingdomQuestNativeConstants.StatusDone",
        "The original function has no",
        "prior-status gate",
    ], "native SetDone Status-5 mutation"):
        return 1

    if not require(handler, [
        "internal static void KingdomQuestLogout(WorldClient client)",
        "definition.Status == KingdomQuestNativeConstants.StatusRunning",
        "if (!running)",
        "PlayerDisjoin(client)",
    ], "native Logout InKQStatusRunning gate"):
        return 1

    if not require(world_client, [
        "Handler22.KingdomQuestLogout(this);",
        "ClientManager.Instance.RemoveClient(this);",
    ], "socket logout KQ cleanup order"):
        return 1

    if not require(world_inter, [
        "Handler22.KingdomQuestLogout(client);",
        "client.Character.Loggeout(client);",
        "ClientManager.Instance.RemoveClient(client);",
    ], "Zone-reported logout KQ cleanup order"):
        return 1

    if not require(session_coordinator, [
        "class KingdomQuestTeamSelectResult",
        "public static bool TrySelectUserTeam(",
        "requestedTeamType > 1",
        "TeamSelectInvalidHandle",
        "StatusJoining",
        "TeamSelectWrongStatus",
        "TeamSelectMissingTeamData",
        "UserSelectTeamDivideType",
        "TeamSelectWrongDivideType",
        "TeamSelectSameTeam",
        "int targetAfter = counts[requestedTeamType] + 1",
        "int oldAfter = counts[oldTeamType] - 1",
        "int halfMaxPlayers = protocolDefinition.MaxPlayers >> 1",
        "targetAfter >= halfMaxPlayers",
        "targetAfter - oldAfter > team.MaxMemberGap",
        "TeamSelectTargetTeamLimit",
        "TeamSelectMemberGap",
        "TrySetMembership(handle, updated)",
        "TeamSelectSuccess",
        "NeutralTeamType",
    ], "native USERSELECT TEAM_SELECT decision/mutation"):
        return 1

    if "Math.Abs(targetAfter - oldAfter)" in session_coordinator:
        print("FAIL: TEAM_SELECT member-gap check became symmetric")
        return 1

    team_select_handler = handler.find(
        "[PacketHandler(CH22Type.KingdomQuestTeamSelectReq)]")
    team_select_ack = handler.find(
        "client.SendPacket(ack);", team_select_handler)
    team_select_cmd = handler.find(
        "KingdomQuestProtocol.CreateTeamSelectCmd(", team_select_ack)
    if not (0 <= team_select_handler < team_select_ack < team_select_cmd):
        print("FAIL: TEAM_SELECT ACK/CMD ordering changed")
        return 1

    if not require(make_ack_registry, [
        "KingdomQuestNativeConstants.MakeAckSuccess",
        "IsSuccess(ushort error)",
    ], "proven MAKE_ACK success registry"):
        return 1

    if not require(world_inter, [
        "KingdomQuestMakeAckRegistry.Set(handle, error)",
        "KingdomQuestSessionCoordinator.TryApplyMakeAck(",
    ], "World MAKE_ACK transition"):
        return 1

    if not require(zone_inter, [
        "KingdomQuestZoneRuntimeRegistry.TryMake(",
        "KingdomQuestZoneMakeResult.DuplicateHandle",
        "KingdomQuestNativeConstants.MakeAckDuplicateHandle",
        "KingdomQuestZoneMakeResult.ScriptNotFound",
        "KingdomQuestNativeConstants.MakeAckScriptNotFound",
        "KingdomQuestZoneMakeResult.NativeContainerFull",
        "KingdomQuestNativeConstants.MakeAckTooManyQuest",
        "KingdomQuestZoneMakeResult.Success",
        "KingdomQuestNativeConstants.MakeAckSuccess",
        "Emulator-only map/routing validation has no proven",
    ], "Zone MAKE ACK result routing"):
        return 1

    if not require(scenario_book_source, [
        "class KingdomQuestScenarioBookShelfSource",
        "KqCatalogKeyCount = 32",
        "KqUsedBySuppliedDefinitions = 27",
        "ContainsSourceBackedScenarioBook(",
        "StringComparer.Ordinal",
        "ignores that bool return",
        "does not parse or execute",
    ], "source-backed Zone ScenarioBookShelf MAKE projection"):
        return 1

    if not require(zone_runtime, [
        "enum KingdomQuestZoneMakeResult",
        "RejectedUnmapped = 0",
        "Success = 1",
        "DuplicateHandle = 2",
        "NativeContainerFull = 3",
        "ScriptNotFound = 4",
        "NativeContainerCapacity = 300",
        "ByHandle.ContainsKey(definition.Handle)",
        "return KingdomQuestZoneMakeResult.DuplicateHandle",
        "ContainsSourceBackedScenarioBook(",
        "return KingdomQuestZoneMakeResult.ScriptNotFound",
        "ByHandle.Count >= NativeContainerCapacity",
        "return KingdomQuestZoneMakeResult.NativeContainerFull",
    ], "atomic Zone duplicate/script/capacity MAKE classification"):
        return 1

    duplicate_pos = zone_runtime.find(
        "return KingdomQuestZoneMakeResult.DuplicateHandle")
    script_pos = zone_runtime.find(
        "return KingdomQuestZoneMakeResult.ScriptNotFound")
    capacity_pos = zone_runtime.find(
        "return KingdomQuestZoneMakeResult.NativeContainerFull")
    if not (0 <= duplicate_pos < script_pos < capacity_pos):
        print("FAIL: Zone MAKE native error precedence changed")
        return 1

    if not require(state, [
        "public uint Handle",
        "public byte Status",
        "public ushort ID",
        "public byte MinLevel",
        "public byte MaxLevel",
        "IReadOnlyList<string> JoinerNames",
        "SetJoiners",
    ], "native KQ wire state"):
        return 1

    if not require(definitions, [
        "Dictionary<uint, KingdomQuestClientInfo>",
        "ByHandle[info.Handle] = Clone(info);",
        ".OrderBy(v => v.Handle)",
        "StartTm = CloneTime(source.StartTm)",
        "DemandClass = source.DemandClass",
        "DemandGender = source.DemandGender",
    ], "source-owned KQ definition registry"):
        return 1

    for forbidden in (
        "DateTime.Now",
        "DateTimeOffset.Now",
        "Random",
        "KingdomQuestSessionTargetRegistry.TryCreate",
    ):
        if forbidden in definitions:
            print("FAIL: KQ definition registry invents runtime scheduling/routing:", forbidden)
            return 1

    if "KingdomQuestJoinListReplyRegistry" in handler:
        print("FAIL: recovered JOIN_LIST_ACK errors regressed to external placeholder state")
        return 1

    if not require(world_client, [
        "uint? KingdomQuestHandle",
        "int? KingdomQuestJoinListLastRequestTime",
    ], "native KQ session handle/list cooldown state"):
        return 1

    if not require(identity, [
        "class KingdomQuestCharacterIdentity",
        "TryGetCharacterNumber",
        "character.Character.ID < 0",
        "unchecked((uint)character.Character.ID)",
        "PROTO_NC_CHAR_CHARDATA_REQ",
        "p_Char_Create returns nCharNo = @@IDENTITY",
        "PROTO_AVATARINFORMATION chrregnum",
    ], "source-correlated native CharacterNumber identity"):
        return 1

    if not require(packet_helper, [
        "PROTO_AVATARINFORMATION begins with u32 chrregnum",
        "packet.WriteInt(wchar.Character.ID);",
    ], "existing avatar chrregnum serialization"):
        return 1

    if "KingdomQuestRangeReplyRegistry" in handler:
        print("FAIL: obsolete guessed KQ range-reply workaround is still live")
        return 1

    if not require(world_client, [
        "KingdomQuestListTimeSent",
        "List<KingdomQuestClientInfo> KingdomQuestListSnapshot",
        "KingdomQuestListSnapshot = new List<KingdomQuestClientInfo>()",
    ], "per-session native KQ refresh snapshot"):
        return 1

    if not require(proto, [
        "CreateListUpdateFromDefinitions",
        "packet.WriteUInt(entries[i].Handle);",
        "packet.WriteByte(entries[i].Status);",
        "packet.WriteUShort(entries[i].NumOfJoiner);",
    ], "definition-backed KQ list-update wire"):
        return 1

    if not require(server_proto, [
        "new Packet((ushort)0x580D)",
        "new Packet((ushort)0x580E)",
        "new Packet((ushort)0x580F)",
        "new Packet((ushort)0x5810)",
        "new Packet((ushort)0x5811)",
        "info.Write(packet);",
        "packet.WriteUShort((ushort)joiners.Count);",
        "joiners[i].Write(packet);",
        "packet.WriteUInt(handle);",
        "packet.WriteUShort(error);",
        "KingdomQuestProtocolDefinitionRegistry.TryGet(handle, out info)",
        "KingdomQuestZoneJoinerRegistry.TryGet(handle, out joiners)",
        "packet = CreateMakeRequest(info);",
        "packet = CreateStart(info, joiners);",
    ], "native World/Zone KQ lifecycle wire"):
        return 1

    if "KingdomQuestServerProtocol" in handler:
        print("FAIL: server-only KQ lifecycle packet used by client Handler22")
        return 1

    print("PASS: native NC_KQ opcode names replace capture-era guesses")
    print("PASS: original prison minutes are loaded fail-closed; unresolved creation default is not guessed")
    print("PASS: PROTO_NC_CHARSAVE_LOCATION_CMD is modeled as exact 48-byte normal+KQ location persistence wire")
    print("PASS: Zone save persists the source-backed KQ handle/dynamic-map/XY suffix and DB-side timestamp without inventing native return-location policy")
    print("PASS: reconnect reuses an exact 20-byte Name5 joiner and routes saved KQ map-instance/XY without recreating membership")
    print("PASS: JOIN_CANCEL 0x09A1/0x09A2 is live and removes session-owned current membership before echoing the request Handle")
    print("PASS: KQ status/list update/alarm layouts match original 2016 structures")
    print("PASS: KQ dead-count, entry-response, mob-kill and team-score layouts are explicit")
    print("PASS: native W2Z_MAKE/START/DESTROY and Z2W_END/MAKE_ACK directions are isolated from client traffic")
    print("PASS: Z2W END drives exact World SetDone Status5 -> DESTROY -> FreeMapLink -> FreeJoiner lifecycle")
    print("PASS: native Logout disjoins non-running KQs but preserves Status-4 membership for reconnect")
    print("PASS: W2Z_MAKE/START builders consume only explicit full-definition and Zone-roster state")
    print("PASS: KQ LIST_TIME_ACK is full 40-byte body, not legacy 4-byte stub")
    print("PASS: PROTO_KQ_INFO_CLIENT=141 and PROTO_KQ_INFO=377 serializers/parsers are explicit")
    print("PASS: NC_KQ_JOIN_LIST_ACK uses native 23-byte KQ_JOIN_CHAR_INFO entries")
    print("PASS: JOIN_LIST errors 0x3118/0x3119/0x311A and exact 5-second SingleData cooldown are live")
    print("PASS: emulator Character.ID is source-correlated to native chrregnum/CharacterNumber through existing avatar serialization")
    print("PASS: join-cancel/team-select/team-type/disjoin wire layouts are source-level named")
    print("PASS: vote/start/result/ban wire layouts are source-level modeled without vote policy")
    print("PASS: JOIN_LIST_REQ uses recovered native errors/cooldown directly; no external Error placeholder is consulted")
    print("PASS: TEAM_SELECT 0x31F0..0x31F7 is live with native USERSELECT gates, directed gap arithmetic and ACK-before-peer-CMD order")
    print("PASS: TEAM_SELECT request bytes outside native team 0/1 remain fail-closed instead of indexing an unsafe native counter")
    print("PASS: LIST_REQ ignores request bounds and exposes only Status 0..4; SCHEDULE_REQ exposes the full scheduler array")
    print("PASS: LIST_REFRESH keeps a per-session visible snapshot and emits native delete/update/add deltas in original order")
    print("PASS: LIST_ADD refresh batches use the original/capture-correlated 53-entry threshold")
    print("PASS: complete KQ client definitions can be stored without scheduler inference")
    print("PASS: LIST_REFRESH serializes only entries supplied by the source-owned definition registry")
    print("PASS: JOIN_ACK 0x0991..0x099A is live for characters with known original PrisonMin; unknown provenance remains fail-closed")
    print("PASS: successful JOIN/CANCEL preserves native PlayerDisjoin/PlayerJoin list-broadcast ordering and propagates PLAYER_DISJOIN to Zone")
    print("PASS: Z2W_MAKE_ACK 0x0981 success now drives the proven World Status 1/2 -> 2 transition; non-success drives Status 8")
    print("PASS: Zone MAKE_ACK now preserves native duplicate 0x0982 -> script 0x098C -> capacity 0x0983 precedence")
    print("PASS: all 27 supplied KQ ScriptLanguage values resolve in the exact source-backed ScenarioBookShelf projection")
    print("PASS: native Zone KingdomQuestContainer capacity is locked to 300 fixed KQElement slots and 0x0983 is live after script lookup")
    print("PASS: PDB names lock TeamDivideType 1=RANDOM and 2=USERSELECT; PlayerJoin type-2 initial assignment remains source-modeled")
    print("PASS: one combined membership owner carries CharacterNumber plus client identity fields into both native roster projections")
    print("PASS: native KQ_JOINER_BF +0x20 bInVote / +0x24 bBan / +0x28 nVotingCount are preserved at exact widths")
    print("PASS: native KQ vote bookkeeping models 0x3100/0x3110 errors, eligibility counters, threshold clamp, result ban and audience")
    print("PASS: VOTE_START/VOTING/START_CHECK transport is live with exact SingleData 60s/300s timing and ACK-before-command ordering")
    print("PASS: VoteProcessing preserves existing-ban force-link before expiry/result, and passing targets receive BAN_MSG before next-tick force link")
    print("PASS: PlayerDisjoin cancels only an active unbanned vote target before normal membership removal")
    print("PASS: character login initializes the shared vote deadline from KQVote_LoginCoolTime=300 before reconnect checks")
    print("PASS: CheckCharBannedInLogin exact bBan==1 is represented as pending flag -> PlayerDisjoin -> deferred 0x5830 send/clear")
    return 0

if __name__ == "__main__":
    sys.exit(main())
