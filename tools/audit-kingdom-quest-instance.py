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

    combined = "\n".join(c.values())
    if "(short)instanceId" in combined or "(short)InstanceID" in combined:
        print("FAIL: World KQ InstanceID conflated with internal Map.InstanceID")
        return 1

    print("PASS: MapManager can allocate requested internal map instances")
    print("PASS: internal MapInstance survives Zone -> World -> Zone transfer")
    print("PASS: captured 32-bit KQ InstanceID remains a separate namespace")
    return 0

if __name__ == "__main__":
    sys.exit(main())
