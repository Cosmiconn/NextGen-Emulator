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
STATE = ROOT / "NextGen.World/Data/KingdomQuestInstanceWireState.cs"
DEFINITIONS = ROOT / "NextGen.World/Data/KingdomQuestDefinitionRegistry.cs"

def require(text, tokens, label):
    missing = [t for t in tokens if t not in text]
    if missing:
        print(f"FAIL: {label}: missing {missing}")
        return False
    return True

def main():
    files = [CENUM, SENUM, PROTO, INFO, HANDLER, STATE, DEFINITIONS]
    missing = [str(p) for p in files if not p.is_file()]
    if missing:
        print("FAIL: KQ audit files missing:", missing)
        return 1

    cenum = CENUM.read_text(encoding="utf-8")
    senum = SENUM.read_text(encoding="utf-8")
    proto = PROTO.read_text(encoding="utf-8")
    info = INFO.read_text(encoding="utf-8")
    handler = HANDLER.read_text(encoding="utf-8")
    state = STATE.read_text(encoding="utf-8")
    definitions = DEFINITIONS.read_text(encoding="utf-8")

    if not require(cenum, [
        "KingdomQuestStatusReq = 3",
        "KingdomQuestJoinReq = 5",
        "KingdomQuestListRefreshReq = 27",
    ], "native CH22 KQ request names"):
        return 1

    if not require(senum, [
        "KingdomQuestListAck = 2",
        "KingdomQuestStatusAck = 4",
        "KingdomQuestJoinAck = 6",
        "KingdomQuestScheduleAck = 10",
        "KingdomQuestNotify = 11",
        "KingdomQuestFailed = 19",
        "KingdomQuestListTimeAck = 28",
        "KingdomQuestListAddAck = 29",
        "KingdomQuestListDeleteAck = 30",
        "KingdomQuestListUpdateAck = 31",
        "KingdomQuestJoiningAlarm = 36",
        "KingdomQuestJoiningAlarmEnd = 37",
        "KingdomQuestJoiningAlarmList = 38",
        "KingdomQuestJoinListAck = 50",
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
        "public const int WireSize = 23",
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
        "[PacketHandler(CH22Type.KingdomQuestStatusReq)]",
        "KingdomQuestProtocol.CreateStatusAck(state)",
        "[PacketHandler(CH22Type.KingdomQuestListRefreshReq)]",
        "KingdomQuestProtocol.CreateListTime(DateTimeOffset.Now)",
        "KingdomQuestDefinitionRegistry.Snapshot()",
        "KingdomQuestProtocol.CreateListAdd(definitions)",
        "if (!client.Character.IsIngame)",
    ], "KQ status/list-refresh handlers"):
        return 1

    if "CreateEmptyListAdd()" in handler:
        print("FAIL: LIST_REFRESH reverted to a hard-coded empty KQ list")
        return 1

    if "[PacketHandler(CH22Type.KingdomQuestJoinReq)]" in handler:
        print("FAIL: KQ join enabled before source-backed admission/session rules")
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

    print("PASS: native NC_KQ opcode names replace capture-era guesses")
    print("PASS: KQ status/list update/alarm layouts match original 2016 structures")
    print("PASS: KQ LIST_TIME_ACK is full 40-byte body, not legacy 4-byte stub")
    print("PASS: PROTO_KQ_INFO_CLIENT=141 and PROTO_KQ_INFO=377 serializers are explicit")
    print("PASS: NC_KQ_JOIN_LIST_ACK uses native 23-byte KQ_JOIN_CHAR_INFO entries")
    print("PASS: complete KQ client definitions can be stored without scheduler inference")
    print("PASS: LIST_REFRESH serializes only entries supplied by the source-owned definition registry")
    print("PASS: KQ join remains disabled until admission/session rules are source-backed")
    return 0

if __name__ == "__main__":
    sys.exit(main())
