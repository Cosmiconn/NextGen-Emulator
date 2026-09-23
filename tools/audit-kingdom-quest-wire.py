#!/usr/bin/env python3
"""Static guard for captured NA2016 Kingdom Quest World packet layouts."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ENUM = ROOT / "NextGen.FiestaLib/PacketTypeServer.cs"
PROTO = ROOT / "NextGen.World/Handlers/KingdomQuestProtocol.cs"
HANDLER = ROOT / "NextGen.World/Handlers/Handler22.cs"
STATE = ROOT / "NextGen.World/Data/KingdomQuestInstanceWireState.cs"

def require(text, tokens, label):
    missing = [t for t in tokens if t not in text]
    if missing:
        print(f"FAIL: {label}: missing {missing}")
        return False
    return True

def main():
    files = [ENUM, PROTO, HANDLER, STATE]
    missing = [str(p) for p in files if not p.is_file()]
    if missing:
        print("FAIL: KQ audit files missing:", missing)
        return 1

    enum = ENUM.read_text(encoding="utf-8")
    proto = PROTO.read_text(encoding="utf-8")
    handler = HANDLER.read_text(encoding="utf-8")
    state = STATE.read_text(encoding="utf-8")

    if not require(enum, [
        "KingdomQuestInstanceInfo = 4",
        "KingdomQuestRegistrationAck = 6",
        "KingdomQuestFailed = 19",
        "KingdomQuestList = 29",
        "KingdomQuestInstanceGroup = 30",
        "KingdomQuestRecruitmentUpdate = 31",
        "KingdomQuestInstanceState = 37",
        "KingdomQuestInstanceResync = 38",
        "KingdomQuestRegistrationNotice = 50",
    ], "SH22 captured opcode family"):
        return 1

    builders = {
        "CreateInstanceInfo": [
            "new Packet(SH22Type.KingdomQuestInstanceInfo)",
            "packet.WriteUInt(instanceId);",
            "packet.WriteUShort(value);",
        ],
        "CreateRegistrationAck": [
            "new Packet(SH22Type.KingdomQuestRegistrationAck)",
            "packet.WriteUInt(instanceId);",
            "packet.WriteUShort(value);",
        ],
        "CreateInstanceGroup": [
            "new Packet(SH22Type.KingdomQuestInstanceGroup)",
            "packet.WriteUShort((ushort)instanceIds.Count);",
            "packet.WriteUInt(instanceIds[i]);",
        ],
        "CreateRecruitmentUpdate": [
            "new Packet(SH22Type.KingdomQuestRecruitmentUpdate)",
            "packet.WriteUShort(firstValue);",
            "packet.WriteUInt(instanceId);",
            "packet.WriteUShort(lastValue);",
        ],
        "CreateInstanceState": [
            "new Packet(SH22Type.KingdomQuestInstanceState)",
            "packet.WriteUInt(instanceId);",
            "packet.WriteUShort(stateValue);",
        ],
        "CreateInstanceResync": [
            "new Packet(SH22Type.KingdomQuestInstanceResync)",
            "packet.WriteUShort((ushort)states.Count);",
            "packet.WriteUInt(states[i].InstanceID);",
            "packet.WriteUShort(states[i].StateValue);",
            "packet.WriteUShort(states[i].TypeValue);",
        ],
        "CreateFailed": ["new Packet(SH22Type.KingdomQuestFailed)"],
    }
    for name, tokens in builders.items():
        start = proto.find(" " + name + "(")
        if start < 0:
            print("FAIL: builder missing:", name)
            return 1
        next_start = proto.find("\n        internal static Packet ", start + 1)
        block = proto[start: next_start if next_start >= 0 else len(proto)]
        if not require(block, tokens, name):
            return 1

    if "new Packet(0x581D)" in handler:
        print("FAIL: raw SH22/29 KingdomQuestList opcode reintroduced")
        return 1
    if not require(handler, [
        "new Packet(SH22Type.KingdomQuestList)",
        "p3.WriteUShort(0);",
    ], "empty-list guard before definition import"):
        return 1

    if not require(state, [
        "Dictionary<uint, KingdomQuestInstanceWireState>",
        "OrderBy(v => v.InstanceID)",
        "StateValue",
        "TypeValue",
        "InstanceInfoValue",
        "SetInstanceInfoValue",
    ], "instance wire-state registry"):
        return 1

    if not require(handler, [
        "[PacketHandler(CH22Type.GetKQInstanceInfo)]",
        "packet.TryReadUInt(out instanceId)",
        "KingdomQuestInstanceRegistry.TryGet(instanceId, out state)",
        "!state.InstanceInfoValue.HasValue",
        "KingdomQuestProtocol.CreateInstanceInfo(",
        "instanceId, state.InstanceInfoValue.Value",
    ], "captured CH22/3 instance-detail handler"):
        return 1

    # The captured registration path also emits SH22/50, whose 26-byte body is
    # not fully decoded yet. Do not enable a partial type-5 handler that would
    # falsely claim registration parity.
    if "[PacketHandler(CH22Type.RegisterForKQInstance)]" in handler:
        print("FAIL: KQ registration enabled before SH22/50 body is source-proven")
        return 1

    print("PASS: CH22/3 replies only from an explicitly populated instance-info value")
    print("PASS: CH22/5 registration remains disabled until SH22/50 is decoded")
    print("PASS: captured KQ World wire primitives are explicit and definition-neutral")
    print("PASS: live KQ list remains empty until authoritative definition/schedule import")
    return 0

if __name__ == "__main__":
    sys.exit(main())
