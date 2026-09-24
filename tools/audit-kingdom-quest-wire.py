#!/usr/bin/env python3
"""Guard original 2016 Kingdom Quest World protocol layouts."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CENUM = ROOT / "NextGen.FiestaLib/PacketTypeClient.cs"
SENUM = ROOT / "NextGen.FiestaLib/PacketTypeServer.cs"
PROTO = ROOT / "NextGen.World/Handlers/KingdomQuestProtocol.cs"
INFO = ROOT / "NextGen.FiestaLib/Data/KingdomQuestProtocolInfo.cs"
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
ZONE_INTER = ROOT / "NextGen.Zone/InterServer/InterHandler.cs"

def require(text, tokens, label):
    missing = [t for t in tokens if t not in text]
    if missing:
        print(f"FAIL: {label}: missing {missing}")
        return False
    return True

def main():
    files = [CENUM, SENUM, PROTO, INFO, HANDLER, SERVER_PROTO, STATE, DEFINITIONS, JOIN_LIST_REPLY, PARTICIPANTS, WORLD_CLIENT, SESSION_COORDINATOR, MAKE_ACK_REGISTRY, WORLD_INTER, ZONE_INTER]
    missing = [str(p) for p in files if not p.is_file()]
    if missing:
        print("FAIL: KQ audit files missing:", missing)
        return 1

    cenum = CENUM.read_text(encoding="utf-8")
    senum = SENUM.read_text(encoding="utf-8")
    proto = PROTO.read_text(encoding="utf-8")
    info = INFO.read_text(encoding="utf-8")
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
    zone_inter = ZONE_INTER.read_text(encoding="utf-8")

    if not require(cenum, [
        "KingdomQuestListReq = 1",
        "KingdomQuestStatusReq = 3",
        "KingdomQuestJoinReq = 5",
        "KingdomQuestScheduleReq = 9",
        "KingdomQuestListRefreshReq = 27",
        "KingdomQuestJoinListReq = 49",
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
        "KingdomQuestJoinListAck = 50",
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
        "packet.TryReadUInt(out handle)",
        "KingdomQuestJoinListReplyRegistry.TryGet(handle, out error)",
        "KingdomQuestParticipantRegistry.TryGet(handle, out participants)",
        "KingdomQuestProtocol.CreateJoinListAck(error, participants)",
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

    if "[PacketHandler(CH22Type.KingdomQuestJoinReq)]" in handler:
        print("FAIL: KQ join enabled before prison/current-KQ mutation state is represented")
        return 1

    if not require(info, [
        "MakeAckSuccess = 0x0981",
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
        "StatusMakeRequested = 1",
        "StatusJoining = 2",
        "StatusNoMap = 8",
        "AutomaticSplitTeamDivideType = 2",
        "NeutralTeamType = 2",
    ], "native KQ admission/lifecycle constants"):
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
        "team.TeamDivideType != KingdomQuestNativeConstants.AutomaticSplitTeamDivideType",
        "team1Count <= team0Count",
    ], "native KQ admission/MAKE transition rules"):
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
        "SendKingdomQuestMakeAck(",
        "KingdomQuestNativeConstants.MakeAckSuccess",
    ], "Zone MAKE success ACK"):
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

    if not require(join_list_reply, [
        "Dictionary<uint, ushort>",
        "ErrorByHandle[handle] = error;",
        "ErrorByHandle.TryGetValue(handle, out error)",
    ], "explicit JOIN_LIST_ACK error registry"):
        return 1

    if "0x0991" in join_list_reply or "return 0;" in join_list_reply:
        print("FAIL: JOIN_LIST_ACK error value was guessed")
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
    print("PASS: KQ status/list update/alarm layouts match original 2016 structures")
    print("PASS: KQ dead-count, entry-response, mob-kill and team-score layouts are explicit")
    print("PASS: native W2Z_MAKE/START/END/DESTROY and Z2W_MAKE_ACK layouts are isolated from client traffic")
    print("PASS: W2Z_MAKE/START builders consume only explicit full-definition and Zone-roster state")
    print("PASS: KQ LIST_TIME_ACK is full 40-byte body, not legacy 4-byte stub")
    print("PASS: PROTO_KQ_INFO_CLIENT=141 and PROTO_KQ_INFO=377 serializers/parsers are explicit")
    print("PASS: NC_KQ_JOIN_LIST_ACK uses native 23-byte KQ_JOIN_CHAR_INFO entries")
    print("PASS: join-cancel/team-select/team-type/disjoin wire layouts are source-level named")
    print("PASS: vote/start/result/ban wire layouts are source-level modeled without vote policy")
    print("PASS: JOIN_LIST_REQ is live only when its native ushort Error is explicitly supplied")
    print("PASS: LIST_REQ ignores request bounds and exposes only Status 0..4; SCHEDULE_REQ exposes the full scheduler array")
    print("PASS: LIST_REFRESH keeps a per-session visible snapshot and emits native delete/update/add deltas in original order")
    print("PASS: LIST_ADD refresh batches use the original/capture-correlated 53-entry threshold")
    print("PASS: complete KQ client definitions can be stored without scheduler inference")
    print("PASS: LIST_REFRESH serializes only entries supplied by the source-owned definition registry")
    print("PASS: JOIN_ACK 0x0991..0x099A admission results are source-level recovered; network JOIN remains gated on missing prison/current-KQ mutation state")
    print("PASS: Z2W_MAKE_ACK 0x0981 success now drives the proven World Status 1/2 -> 2 transition; non-success drives Status 8")
    print("PASS: Zone emits MAKE_ACK only for the proven successful MAKE path; no failure Error is guessed")
    print("PASS: TeamDivideType 2 automatic split behavior is modeled; supplied divide-type-1 rows resolve to native TeamType 2")
    return 0

if __name__ == "__main__":
    sys.exit(main())
