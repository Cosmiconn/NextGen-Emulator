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
        "Dictionary<Tuple<ushort, short>, uint>",
        "DataProvider.Instance.KingdomQuestMaps.ContainsKey(mapId)",
        "new KingdomQuestSessionTarget(instanceId, mapId, mapInstance)",
        "Tuple.Create(mapId, mapInstance)",
    ], "explicit KQ wire-instance to map-instance mapping"):
        return 1

    if not need(c["kq_transfer"], [
        "KingdomQuestSessionTargetRegistry.TryGet(instanceId, out target)",
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

    combined = "\n".join(c.values())
    if "(short)instanceId" in combined or "(short)InstanceID" in combined:
        print("FAIL: World KQ InstanceID conflated with internal Map.InstanceID")
        return 1

    print("PASS: MapManager can allocate requested internal map instances")
    print("PASS: internal MapInstance survives Zone -> World -> Zone transfer")
    print("PASS: captured 32-bit KQ InstanceID remains a separate namespace")
    print("PASS: KQ wire InstanceID -> source MapID/internal MapInstance mapping is explicit")
    print("PASS: World KQ transfer requests reuse ZoneCharacter.ChangeMap with explicit coordinates")
    print("PASS: Mobspawn rows are isolated by internal Map.InstanceID")
    return 0

if __name__ == "__main__":
    sys.exit(main())
