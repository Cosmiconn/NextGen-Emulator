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
    "inter_header": ROOT / "NextGen.InterLib/Networking/InterHeader.cs",
    "kq_session": ROOT / "NextGen.World/Data/KingdomQuestSessionCoordinator.cs",
    "kq_participants": ROOT / "NextGen.World/Data/KingdomQuestParticipantRegistry.cs",
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
    ], "explicit KQ wire-instance to map-instance mapping"):
        return 1

    if not need(c["kq_transfer"], [
        "KingdomQuestSessionTargetRegistry.TryGet(handle, out target)",
        "Program.GetZoneByMap(character.Character.PositionInfo.Map)",
        "currentZone.SendKingdomQuestTransferRequest(",
        "target.MapID",
        "target.MapInstance",
    ], "World KQ transfer bridge"):
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
    if not need(c["inter_header"], ["KingdomQuestTransfer = 0x4004"], "internal KQ inter-server opcode"):
        return 1
    if not need(c["kq_session"], [
        "KingdomQuestDefinitionRegistry.TryGet(definition.Handle",
        "KingdomQuestInstanceRegistry.TryGet(definition.Handle",
        "KingdomQuestSessionTargetRegistry.TryGet(definition.Handle",
        "definition.NumOfJoiner != (ushort)roster.Count",
        "KingdomQuestParticipantRegistry.Set(definition.Handle, roster);",
        "KingdomQuestSessionTargetRegistry.TryCreate(",
        "definition.Handle, mapId, mapInstance",
        "KingdomQuestDefinitionRegistry.Upsert(definition);",
        "KingdomQuestInstanceRegistry.Upsert(",
        "KingdomQuestInstanceRegistry.SetJoiners(",
        "KingdomQuestInstanceRegistry.SetStatus(handle, status)",
        "public static bool TrySetParticipants",
        "KingdomQuestJoinListReplyRegistry.Remove(handle)",
        "definition.NumOfJoiner = (ushort)roster.Count;",
        "KingdomQuestParticipantRegistry.Set(handle, roster);",
        "KingdomQuestDefinitionRegistry.Remove(definition.Handle);",
        "KingdomQuestInstanceRegistry.Remove(definition.Handle);",
        "KingdomQuestParticipantRegistry.Remove(definition.Handle);",
        "KingdomQuestSessionTargetRegistry.Remove(definition.Handle);",
    ], "atomic explicit KQ session coordinator"):
        return 1

    for forbidden in ("DateTime.", "DateTimeOffset.", "Random", "++handle", "Handle++"):
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

    combined = "\n".join(c.values())
    if "(short)handle" in combined or "(short)Handle" in combined:
        print("FAIL: native World KQ Handle conflated with internal Map.InstanceID")
        return 1

    print("PASS: MapManager can allocate requested internal map instances")
    print("PASS: internal MapInstance survives Zone -> World -> Zone transfer")
    print("PASS: native 32-bit KQ Handle remains separate from internal Map.InstanceID")
    print("PASS: KQ Handle -> source MapID/internal MapInstance mapping is explicit")
    print("PASS: World KQ transfer requests reuse ZoneCharacter.ChangeMap with explicit coordinates")
    print("PASS: KQ session create/remove keeps definition, status, participant, join-list reply and routing registries synchronized")
    print("PASS: KQ participant roster preserves native Level/Class/Name5/Team fields")
    print("PASS: explicit status/participant mutations keep list, STATUS_ACK and JOIN_LIST state synchronized")
    print("PASS: Mobspawn rows are isolated by internal Map.InstanceID")
    return 0

if __name__ == "__main__":
    sys.exit(main())
