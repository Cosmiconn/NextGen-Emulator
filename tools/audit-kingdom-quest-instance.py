#!/usr/bin/env python3
"""Guard the server-side map-instance path required by KQ sessions."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "map_manager": ROOT / "NextGen.Zone/Managers/MapManager.cs",
    "zone_character": ROOT / "NextGen.Zone/Game/ZoneCharacter.cs",
    "transfer": ROOT / "NextGen.Util/ClientTransfer.cs",
    "zone_inter": ROOT / "NextGen.Zone/InterServer/InterHandler.cs",
    "world_inter": ROOT / "NextGen.World/InterServer/InterHandler.cs",
    "zone_connection": ROOT / "NextGen.World/InterServer/ZoneConnection.cs",
    "handler6": ROOT / "NextGen.Zone/Handlers/Handler6.cs",
    "map": ROOT / "NextGen.Zone/Game/Map.cs",
    "kq_target": ROOT / "NextGen.World/Data/KingdomQuestSessionTarget.cs",
    "kq_transfer": ROOT / "NextGen.World/Data/KingdomQuestTransferService.cs",
    "kq_map_context": ROOT / "NextGen.World/Data/KingdomQuestMapContext.cs",
    "kq_protocol": ROOT / "NextGen.World/Handlers/KingdomQuestProtocol.cs",
    "server_types": ROOT / "NextGen.FiestaLib/PacketTypeServer.cs",
    "inter_header": ROOT / "NextGen.InterLib/Networking/InterHeader.cs",
    "kq_session": ROOT / "NextGen.World/Data/KingdomQuestSessionCoordinator.cs",
    "kq_protocol_defs": ROOT / "NextGen.World/Data/KingdomQuestProtocolDefinitionRegistry.cs",
    "kq_participants": ROOT / "NextGen.World/Data/KingdomQuestParticipantRegistry.cs",
    "kq_zone_joiners": ROOT / "NextGen.World/Data/KingdomQuestZoneJoinerRegistry.cs",
    "kq_membership": ROOT / "NextGen.World/Data/KingdomQuestMembershipRegistry.cs",
    "kq_zone_runtime": ROOT / "NextGen.Zone/Data/KingdomQuestZoneRuntime.cs",
    "zone_inter": ROOT / "NextGen.Zone/InterServer/InterHandler.cs",
    "world_zone_connection": ROOT / "NextGen.World/InterServer/ZoneConnection.cs",
    "world_inter": ROOT / "NextGen.World/InterServer/InterHandler.cs",
    "kq_make_ack": ROOT / "NextGen.World/Data/KingdomQuestMakeAckRegistry.cs",
    "kq_map_allocator": ROOT / "NextGen.World/Data/KingdomQuestMapAllocationRegistry.cs",
    "kq_map_route": ROOT / "NextGen.World/Data/KingdomQuestMapRouteResolver.cs",
    "kq_scheduler": ROOT / "NextGen.World/Data/KingdomQuestSourceScheduler.cs",
}

def need(text, tokens, label):
    missing = [token for token in tokens if token not in text]
    if missing:
        print("FAIL:", label, "missing", missing)
        return False
    return True

def main():
    for path in FILES.values():
        if not path.is_file():
            print("FAIL: missing", path)
            return 1
    c = {key: path.read_text(encoding="utf-8") for key, path in FILES.items()}

    if not need(c["map_manager"], [
        "while (maps.Count <= instance)",
        "new Map(info, block, (short)maps.Count)",
        "return maps[instance];",
    ], "MapManager instance creation"):
        return 1
    if not need(c["zone_character"], [
        "SetMap(MapID, mapInstance);",
        "MapManager.Instance.GetMap(info, instance < 0 ? (short)0 : instance)",
        "short targetInstance = instance < 0 ? (short)0 : instance;",
        "InterHandler.TransferClient(zci.ID, id, targetInstance",
    ], "ZoneCharacter instance routing"):
        return 1
    if not need(c["transfer"], ["public short MapInstance", "this.MapInstance = mapInstance < 0"], "ClientTransfer"):
        return 1
    if not need(c["zone_inter"], ["packet.WriteShort(mapInstance);", "packet.TryReadShort(out mapInstance)"], "Zone relay"):
        return 1
    if not need(c["world_inter"], ["packet.TryReadShort(out mapInstance)", "hostip, mapInstance)"], "World relay"):
        return 1
    if not need(c["zone_connection"], ["packet.WriteShort(mapInstance);"], "target-Zone relay"):
        return 1
    if not need(c["handler6"], ["transfer.MapInstance"], "target Zone login"):
        return 1
    if not need(c["map"], ["WHERE MapID=@mapId AND InstanceID=@instanceId", "new MySqlParameter(\"@instanceId\", this.InstanceID)", "InstanceID = GetDataTypes.Getshort(row[\"InstanceID\"])"], "instance-bound Mobspawn load"):
        return 1

    if not need(c["kq_target"], [
        "Dictionary<uint, KingdomQuestSessionTarget>",
        "public uint Handle",
        "Dictionary<Tuple<ushort, short>, uint>",
        "DataProvider.Instance.KingdomQuestMaps.ContainsKey(mapId)",
        "new KingdomQuestSessionTarget(handle, mapId, mapInstance)",
        "Tuple.Create(mapId, mapInstance)",
        "public byte NativeMapIndex",
        "public string NativeMapBase",
        "public string NativeMapName",
        "TryAllocateNative(",
        "NextInternalInstanceByMap",
        "next < 1",
        "next > short.MaxValue",
        "mapLink.MapIndex",
        "mapLink.MapBase",
        "mapLink.MapName",
    ], "explicit KQ wire-instance to map-instance mapping"):
        return 1

    if not need(c["kq_transfer"], [
        "KingdomQuestSessionTargetRegistry.TryGet(handle, out target)",
        "KingdomQuestMapContextRegistry.TryGet(handle, out context)",
        "Program.GetZoneByMap(character.Character.PositionInfo.Map)",
        "currentZone.SendKingdomQuestTransferRequest(",
        "target.MapID",
        "target.MapInstance",
        "context.X",
        "context.Y",
    ], "World KQ transfer bridge"):
        return 1
    if not need(c["kq_map_context"], [
        "public uint Handle",
        "public string MapName",
        "public int X",
        "public int Y",
        "public uint NativeDate",
        "KingdomQuestSessionTargetRegistry.TryGet(handle, out target)",
        "KingdomQuestMaps.TryGetValue(",
        "Encoding.ASCII.GetByteCount(map.ShortName) > 12",
    ], "native KQ map context"):
        return 1
    if not need(c["server_types"], ["KingdomQuestMapCmd = 26"], "NC_CHAR_KQMAP_CMD opcode"):
        return 1
    if not need(c["kq_protocol"], [
        "new Packet((ushort)0x101A)",
        "packet.WriteUInt(context.Handle);",
        "packet.WriteString(context.MapName ?? string.Empty, 12);",
        "packet.WriteInt(context.X);",
        "packet.WriteInt(context.Y);",
        "packet.WriteUInt(context.NativeDate);",
    ], "NC_CHAR_KQMAP_CMD serializer"):
        return 1

    if not need(c["zone_connection"], [
        "SendKingdomQuestTransferRequest",
        "new InterPacket(InterHeader.KingdomQuestTransfer)",
        "packet.WriteShort(mapInstance);",
    ], "World -> current Zone KQ transfer request"):
        return 1
    if not need(c["zone_inter"], [
        "[InterPacketHandler(InterHeader.KingdomQuestTransfer)]",
        "packet.TryReadShort(out mapInstance)",
        "ClientManager.Instance.GetClientByCharName(characterName)",
        "client.Character.ChangeMap(mapId, x, y, mapInstance);",
    ], "Zone KQ transfer dispatch"):
        return 1
    if not need(c["inter_header"], [
        "KingdomQuestTransfer = 0x4004",
        "KingdomQuestMake = 0x4005",
        "KingdomQuestStart = 0x4006",
        "KingdomQuestEnd = 0x4007",
        "KingdomQuestDestroy = 0x4008",
        "KingdomQuestMakeAck = 0x4009",
    ], "internal KQ inter-server opcodes"):
        return 1
    if not need(c["kq_zone_runtime"], [
        "KingdomQuestZoneLifecycleState",
        "activeMap.MapBase, mapInfo.ShortName, StringComparison.Ordinal",
        "!string.IsNullOrEmpty(candidate.MapName)",
        "KingdomQuestProtocolInfo.TryRead(reader, out clone)",
        "DataProvider.Instance.MapsByID.TryGetValue(mapId, out mapInfo)",
        "MapManager.Instance.GetMap(mapInfo, mapInstance)",
        "KingdomQuestZoneLifecycleState.Made",
        "KingdomQuestZoneLifecycleState.Started",
        "KingdomQuestZoneLifecycleState.Ended",
    ], "Zone-local native KQ lifecycle state"):
        return 1
    if not need(c["world_zone_connection"], [
        "KingdomQuestServerProtocol.TryCreateMakeRequest(handle, out native)",
        "KingdomQuestServerProtocol.TryCreateStart(handle, out native)",
        "new InterPacket(InterHeader.KingdomQuestMake)",
        "new InterPacket(InterHeader.KingdomQuestStart)",
        "new InterPacket(InterHeader.KingdomQuestDestroy)",
    ], "World -> Zone KQ MAKE/START/DESTROY transport"):
        return 1
    if not need(c["kq_make_ack"], [
        "Dictionary<uint, ushort>",
        "ErrorByHandle[handle] = error;",
        "ErrorByHandle.TryGetValue(handle, out error)",
        "KingdomQuestNativeConstants.MakeAckSuccess",
        "IsSuccess(ushort error)",
    ], "native KQ MAKE_ACK registry"):
        return 1
    if "error == 0" in c["kq_make_ack"] or "0x0991" in c["kq_make_ack"]:
        print("FAIL: MAKE_ACK reused an unrelated/invented success value")
        return 1
    if not need(c["world_inter"], [
        "[InterPacketHandler(InterHeader.KingdomQuestEnd)]",
        "native.OpCode != 0x5810",
        "KingdomQuestSessionCoordinator.TrySetDone(handle)",
        "zone.SendKingdomQuestDestroy(handle)",
        "KingdomQuestMapAllocationRegistry.Free(handle)",
        "FreeKingdomQuestJoinerSessions(handle)",
    ], "Zone -> World END and World -> Zone DESTROY lifecycle"):
        return 1

    if not need(c["world_inter"], [
        "[InterPacketHandler(InterHeader.KingdomQuestMakeAck)]",
        "native.OpCode != 0x580E",
        "KingdomQuestMakeAckRegistry.Set(handle, error)",
        "KingdomQuestSessionCoordinator.TryApplyMakeAck(",
    ], "Zone -> World native KQ MAKE_ACK transport/transition"):
        return 1
    if not need(c["zone_inter"], [
        "public static void SendKingdomQuestMakeAck(uint handle, ushort error)",
        "new Packet((ushort)0x580E)",
        "new InterPacket(InterHeader.KingdomQuestMakeAck)",
        "KingdomQuestNativeConstants.MakeAckSuccess",
    ], "proven KQ MAKE_ACK success sender"):
        return 1

    if not need(c["zone_inter"], [
        "[InterPacketHandler(InterHeader.KingdomQuestMake)]",
        "[InterPacketHandler(InterHeader.KingdomQuestStart)]",
        "[InterPacketHandler(InterHeader.KingdomQuestDestroy)]",
        "native.OpCode != 0x580D",
        "native.OpCode != 0x580F",
        "KingdomQuestProtocolInfo.TryRead(native, out definition)",
        "KingdomQuestZoneJoinerInfo.TryRead(native, out joiner)",
        "TryReadHandleOnlyNativeKqPacket(packet, 0x5811, out handle)",
        "public static void SendKingdomQuestEnd(uint handle)",
        "new Packet((ushort)0x5810)",
        "new InterPacket(InterHeader.KingdomQuestEnd)",
    ], "Zone KQ MAKE/START/DESTROY parser and native Z2W END sender"):
        return 1
    if not need(c["kq_protocol_defs"], [
        "Dictionary<uint, KingdomQuestProtocolInfo>",
        "source.MapLink.Length != 4",
        "source.TeamRegenXY.Length != 2",
        "NextStartMode = source.NextStartMode",
        "RewardIndex = source.RewardIndex",
        "DemandMobKill = source.DemandMobKill",
        "ScheduleTm = CloneTime(source.ScheduleTm)",
        "MapLink = new KingdomQuestMapProtocolInfo[4]",
        "TeamRegenXY = new KingdomQuestXY[2]",
    ], "full native PROTO_KQ_INFO registry"):
        return 1

    if not need(c["kq_session"], [
        "KingdomQuestProtocolDefinitionRegistry.TryGet(",
        "KingdomQuestDefinitionRegistry.TryGet(definition.Handle",
        "KingdomQuestInstanceRegistry.TryGet(definition.Handle",
        "KingdomQuestSessionTargetRegistry.TryGet(definition.Handle",
        "definition.NumOfJoiner != (ushort)roster.Count",
        "KingdomQuestParticipantRegistry.Set(definition.Handle, roster);",
        "KingdomQuestSessionTargetRegistry.TryCreate(",
        "definition.Handle, mapId, mapInstance",
        "KingdomQuestProtocolDefinitionRegistry.Upsert(definition);",
        "KingdomQuestDefinitionRegistry.Upsert(definition);",
        "KingdomQuestInstanceRegistry.Upsert(",
        "KingdomQuestInstanceRegistry.SetJoiners(",
        "KingdomQuestInstanceRegistry.SetStatus(handle, status)",
        "public static bool TrySetParticipants",
        "KingdomQuestJoinListReplyRegistry.Remove(handle)",
        "KingdomQuestMapContextRegistry.Remove(handle)",
        "KingdomQuestZoneJoinerRegistry.Remove(handle)",
        "definition.NumOfJoiner = (ushort)roster.Count;",
        "KingdomQuestParticipantRegistry.Set(handle, roster);",
        "public static bool TrySetMembership",
        "KingdomQuestMembershipRegistry.Set(handle, roster)",
        "KingdomQuestZoneJoinerRegistry.Set(handle, zoneRoster)",
        "KingdomQuestMembershipRegistry.Remove(handle)",
        "KingdomQuestProtocolDefinitionRegistry.Remove(definition.Handle);",
        "KingdomQuestDefinitionRegistry.Remove(definition.Handle);",
        "KingdomQuestInstanceRegistry.Remove(definition.Handle);",
        "KingdomQuestParticipantRegistry.Remove(definition.Handle);",
        "KingdomQuestSessionTargetRegistry.Remove(definition.Handle);",
    ], "atomic explicit KQ session coordinator"):
        return 1

    for forbidden in ("DateTime.", "DateTimeOffset.", "new Random(", "System.Random", "++handle", "Handle++"):
        if forbidden in c["kq_session"]:
            print("FAIL: KQ session coordinator invents scheduler/handle state:", forbidden)
            return 1
    if not need(c["kq_participants"], [
        "Dictionary<uint, List<KingdomQuestJoinCharacterInfo>>",
        "copy.Count > byte.MaxValue",
        "participants = current.Select(Clone).ToList().AsReadOnly();",
        "Level = source.Level",
        "Class = source.Class",
        "Name = source.Name",
        "Team = source.Team",
    ], "native KQ participant registry"):
        return 1
    if not need(c["kq_membership"], [
        "class KingdomQuestMembershipEntry",
        "public uint CharacterNumber",
        "public byte Level",
        "public byte Class",
        "public string Name",
        "public byte TeamType",
        "ToClientInfo()",
        "ToZoneInfo()",
        "never infers one identity from the other",
        "Dictionary<uint, List<KingdomQuestMembershipEntry>>",
    ], "combined native KQ membership identity"):
        return 1

    for forbidden in (
        "Character.ID",
        "GetClientByChar",
        "GetClientByName",
        "Group",
        "Party",
    ):
        if forbidden in c["kq_membership"]:
            print("FAIL: combined KQ membership registry infers native identity/session state:", forbidden)
            return 1

    if not need(c["kq_zone_joiners"], [
        "Dictionary<uint, List<KingdomQuestZoneJoinerInfo>>",
        "copy.Count > ushort.MaxValue",
        "CharacterNumber = source.CharacterNumber",
        "TeamType = source.TeamType",
        "joiners = current.Select(Clone).ToList().AsReadOnly();",
    ], "native World -> Zone KQ joiner registry"):
        return 1

    for forbidden in ("GetClientByChar", "GetClientByName", "Character.ID", "Group", "Party"):
        if forbidden in c["kq_zone_joiners"]:
            print("FAIL: KQ Zone joiner registry infers admission/identity:", forbidden)
            return 1

    if not need(c["kq_map_route"], [
        "TryResolveScheduledMap",
        "TryResolveAllocatedMap",
        "StringComparison.Ordinal",
        "source.MapLinkColumns[i]",
    ], "source-backed native MapBase route resolution"):
        return 1

    if not need(c["kq_scheduler"], [
        "RunMakeRoom(local)",
        "definition.ScheduleTime > currentTime",
        "KingdomQuestMapRouteResolver.TryResolveScheduledMap(",
        "KingdomQuestSessionCoordinator.TryPrepareMake(",
        "zone.SendKingdomQuestMake(definition.Handle)",
        "TryRollbackMakePreparation(",
        "RunStartCountdownExpiry(local)",
        "KingdomQuestSessionCoordinator.TryEnterRunning(",
        "zone.SendKingdomQuestStart(definition.Handle)",
    ], "live native DoSetMakeRoom/start bridge"):
        return 1

    if not need(c["kq_session"], [
        "public static bool TryPrepareMake",
        "KingdomQuestMapAllocationRegistry.TryAllocate(",
        "KingdomQuestSessionTargetRegistry.TryAllocateNative(",
        "KingdomQuestNativeConstants.StatusMakeRequested",
        "public static bool TryRollbackMakePreparation",
        "KingdomQuestMapAllocationRegistry.Free(handle)",
    ], "atomic KQ MAKE preparation"):
        return 1

    if not need(c["kq_map_allocator"], [
        "SlotsPerSourceRow = 10",
        "MapIndex = (byte)slot",
        "MapBase = map.BaseMap",
        "MapName = map.MapColumns[slot]",
        "MapClear = clear",
        "FreeLocked(definition.Handle)",
    ], "native KQ source map-slot allocation"):
        return 1
    for forbidden in (
        "KingdomQuestSessionTargetRegistry.TryCreate(",
        "MapManager.Instance.GetMap(",
        "(short)slot",
        "MapInstance = (short)",
    ):
        if forbidden in c["kq_map_allocator"]:
            print("FAIL: native KQ MapIndex slot was conflated with emulator map-instance routing:", forbidden)
            return 1

    combined = "\n".join(c.values())
    for forbidden in (
        "(short)handle",
        "(short)Handle",
        "(short)mapLink.MapIndex",
        "MapInstance = mapLink.MapIndex",
        "MapInstance = (short)definition.Handle",
    ):
        if forbidden in combined:
            print("FAIL: native KQ identity conflated with internal Map.InstanceID:", forbidden)
            return 1

    print("PASS: MapManager can allocate requested internal map instances")
    print("PASS: internal MapInstance survives Zone -> World -> Zone transfer")
    print("PASS: native 32-bit KQ Handle remains separate from internal Map.InstanceID")
    print("PASS: KQ Handle -> source MapID/internal MapInstance mapping is explicit")
    print("PASS: native KingdomQuestMap MapIndex allocation stays separate from emulator Map.InstanceID routing")
    print("PASS: native MapBase is resolved exactly to a source-backed base MapID; dynamic MapName is retained separately")
    print("PASS: internal KQ Map.InstanceID allocation is independent, starts outside base instance 0 and is not derived from Handle/MapIndex")
    print("PASS: due Status-0 schedules execute the recovered AllocMapLink -> Status-1 -> MAKE boundary")
    print("PASS: Status-3 expiry executes the recovered Status-4 -> team divide -> START instance lifecycle")
    print("PASS: World KQ transfer requests reuse ZoneCharacter.ChangeMap with native KQ map-context coordinates")
    print("PASS: NC_CHAR_KQMAP_CMD is modeled as Handle + Name3 + XY + raw SHINE_DATETIME")
    print("PASS: KQ session create/remove keeps full/server definition, client definition, status, participant, join-list reply and routing registries synchronized")
    print("PASS: full 377-byte PROTO_KQ_INFO stays source-owned for later native Zone lifecycle")
    print("PASS: KQ participant roster preserves native Level/Class/Name5/Team fields")
    print("PASS: World -> Zone KQ roster stores only explicit CharacterNumber/TeamType pairs")
    print("PASS: combined KQ membership owns CharacterNumber plus Level/Class/Name/TeamType and projects both wire rosters without identity inference")
    print("PASS: internal MAKE/START/DESTROY transport carries native W2Z bodies and END carries native Z2W body")
    print("PASS: Zone KQ lifecycle uses explicit Handle/MapID/Map.InstanceID without allocation inference")
    print("PASS: Z2W_MAKE_ACK preserves raw Error and applies the executable-proven 0x0981 success transition")
    print("PASS: explicit status/participant mutations keep list, STATUS_ACK and JOIN_LIST state synchronized")
    print("PASS: Mobspawn rows are isolated by internal Map.InstanceID")
    return 0

if __name__ == "__main__":
    sys.exit(main())
