#!/usr/bin/env python3
"""Lock original NA2016 Kingdom Quest Lua/static-regen source presence."""
from pathlib import Path
import csv
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/KINGDOM_QUEST_RUNTIME_SOURCE_MANIFEST.tsv"
KQ_SQL = ROOT / "sql/data/data_kq_source_10_kingdomquest.sql"
MAP_SQL = ROOT / "sql/data/data_kq_source_20_kingdomquestmap.sql"

SOURCE_ARCHIVE_SHA256 = "b83bf92c7193578a772fcebf4d8b7c8c2a9a642cf0d50d33506f77a75b0e211d"
CANONICAL_ROWS_SHA256 = "1a79fea8ea4136fee23bc51e051f8bb61e636a1a86a6265b572abb3409c450e6"

MISSING_SCRIPTS = {
    "KQ/GordonMaster",
    "KQ/Honeying",
    "KQ/KQHBat1",
    "KQ/KQHBat2",
    "KQ/KQHBat3",
    "KQ/KQHBat4",
    "KQ/KQHBat5",
    "KQ/UnderHall",
    "KQ/UnderHall2",
}
MISSING_STATIC_REGEN = {"KDArena", "KDMine", "KDSpring"}
INSTANT_REGEN_BASENAMES = {
    "AdlF", "AdlFH", "Leviathan", "Siren", "Tower01", "Tower02",
    "Tower03", "UrgDragon", "WarN",
}


def data_rows(path):
    return [line.strip() for line in path.read_text(encoding="utf-8").splitlines()
            if line.lstrip().startswith("(")]


def split_row_fields(line):
    text = line.strip().rstrip(",;")
    if text.startswith("(") and text.endswith(")"):
        text = text[1:-1]
    fields = []
    current = []
    quoted = False
    escaped = False
    for ch in text:
        if escaped:
            current.append(ch)
            escaped = False
            continue
        if ch == "\\":
            current.append(ch)
            escaped = True
            continue
        if ch == "'":
            current.append(ch)
            quoted = not quoted
            continue
        if ch == "," and not quoted:
            fields.append("".join(current).strip())
            current = []
            continue
        current.append(ch)
    fields.append("".join(current).strip())
    return fields


def unquote_sql(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == "'" and value[-1] == "'":
        return value[1:-1].replace("\\'", "'").replace("\\\\", "\\")
    return value


def load_manifest():
    text = MANIFEST.read_text(encoding="utf-8")
    required_headers = (
        "# SourceArchive\tServer.zip",
        "# SourceArchiveSha256\t" + SOURCE_ARCHIVE_SHA256,
        "# ArchiveRoot\tServer - Kopie/9Data/Shine",
        "# KQRegenLookup\tZone.exe KQRegenTable::kqrt_Load: MobRegen/KingdomQuest/%s.txt -> MobRegen/Instant/%s.txt",
        "# InstantRegenBasenames\tAdlF,AdlFH,Leviathan,Siren,Tower01,Tower02,Tower03,UrgDragon,WarN",
        "# Semantics\tloader-proven static fallback; KDArena/KDMine/KDSpring are absent in both static paths; Lua is not a KQRegenTable fallback",
    )
    for header in required_headers:
        if header not in text:
            raise ValueError("manifest provenance header changed: " + header)

    data_lines = [line for line in text.splitlines()
                  if line and not line.startswith("#")]
    reader = csv.DictReader(data_lines, delimiter="\t")
    expected_columns = [
        "kind", "key", "present", "archive_path", "sha256", "size"]
    if reader.fieldnames != expected_columns:
        raise ValueError("manifest columns changed")

    rows = list(reader)
    canonical = "".join(
        "\t".join(row[name] for name in expected_columns) + "\n"
        for row in rows)
    if hashlib.sha256(canonical.encode("utf-8")).hexdigest() != CANONICAL_ROWS_SHA256:
        raise ValueError("manifest canonical source snapshot changed")
    return rows


def main():
    for path in (MANIFEST, KQ_SQL, MAP_SQL):
        if not path.is_file():
            print("FAIL: missing", path)
            return 1

    try:
        rows = load_manifest()
    except (ValueError, csv.Error) as exc:
        print("FAIL:", exc)
        return 1

    map_rows = {}
    for line in data_rows(MAP_SQL):
        fields = split_row_fields(line)
        if len(fields) != 23:
            print("FAIL: KingdomQuestMap source row width changed")
            return 1
        map_rows[int(fields[0])] = unquote_sql(fields[2])

    scripts = set()
    used_base_maps = set()
    for line in data_rows(KQ_SQL):
        fields = split_row_fields(line)
        if len(fields) != 36:
            print("FAIL: KingdomQuest source row width changed")
            return 1
        scripts.add(unquote_sql(fields[31]))
        source_map_row = int(fields[27])
        if source_map_row not in map_rows:
            print("FAIL: KQ MapLink references unknown KingdomQuestMap row",
                  source_map_row)
            return 1
        used_base_maps.add(map_rows[source_map_row])

    script_list = [row for row in rows if row["kind"] == "script"]
    regen_list = [row for row in rows if row["kind"] == "regen"]
    script_rows = {row["key"]: row for row in script_list}
    regen_rows = {row["key"]: row for row in regen_list}
    if len(script_rows) != len(script_list):
        print("FAIL: duplicate script key in KQ runtime-source manifest")
        return 1
    if len(regen_rows) != len(regen_list):
        print("FAIL: duplicate regen key in KQ runtime-source manifest")
        return 1
    if any(row["kind"] not in ("script", "regen") for row in rows):
        print("FAIL: unknown KQ runtime-source manifest kind")
        return 1

    if set(script_rows) != scripts or len(scripts) != 27:
        print("FAIL: manifest ScriptLanguage key set differs from exact KingdomQuest.shn corpus")
        return 1
    if set(regen_rows) != used_base_maps or len(used_base_maps) != 18:
        print("FAIL: manifest regen key set differs from exact used KingdomQuestMap BaseMap corpus")
        return 1

    missing_scripts = {
        key for key, row in script_rows.items() if row["present"] == "0"}
    missing_regen = {
        key for key, row in regen_rows.items() if row["present"] == "0"}
    if missing_scripts != MISSING_SCRIPTS:
        print("FAIL: exact missing KQ Lua entrypoint set changed",
              sorted(missing_scripts))
        return 1
    if missing_regen != MISSING_STATIC_REGEN:
        print("FAIL: exact missing KQ static regen set changed",
              sorted(missing_regen))
        return 1

    present_scripts = 0
    present_regen = 0
    sha_rx = re.compile(r"^[0-9a-f]{64}$")
    for row in rows:
        key = row["key"]
        if row["present"] not in ("0", "1"):
            print("FAIL: invalid presence marker", row)
            return 1
        if row["present"] == "0":
            if row["archive_path"] or row["sha256"] or row["size"]:
                print("FAIL: absent source row carries invented path/hash/size",
                      row)
                return 1
            continue

        if not sha_rx.match(row["sha256"]):
            print("FAIL: malformed source SHA-256", key)
            return 1
        try:
            size = int(row["size"])
        except ValueError:
            print("FAIL: malformed source size", key)
            return 1
        if size <= 0:
            print("FAIL: non-positive source size", key)
            return 1

        if row["kind"] == "script":
            present_scripts += 1
            expected_path = "LuaScript/" + key + ".lua"
        else:
            present_regen += 1
            expected_path = "MobRegen/KingdomQuest/" + key + ".txt"
        if row["archive_path"] != expected_path:
            print("FAIL: source path is not the exact key-derived archive path",
                  key)
            return 1

    if (present_scripts, present_regen) != (18, 15):
        print("FAIL: KQ runtime-source presence counts changed",
              present_scripts, present_regen)
        return 1

    # Zone.exe KQRegenTable::kqrt_Load tries the KingdomQuest directory first
    # and the Instant directory second. The exact supplied Instant directory
    # has nine basenames and none can satisfy these three missing KQ names.
    if MISSING_STATIC_REGEN & INSTANT_REGEN_BASENAMES:
        print("FAIL: a missing KQ static regen unexpectedly gained an Instant fallback")
        return 1

    # Lua trees are a separate runtime source family. The recovered
    # KQRegenTable loader never consults them as a replacement for static
    # MobRegen input.
    print("PASS: Server.zip provenance locked", SOURCE_ARCHIVE_SHA256)
    print("PASS: 27 exact KingdomQuest.shn ScriptLanguage keys are covered; 18 entrypoints present, 9 explicitly absent")
    print("PASS: 18 used KingdomQuestMap BaseMap keys are covered; 15 static KQ regen files present, 3 explicitly absent")
    print("PASS: Zone KQRegenTable lookup order is locked to KingdomQuest then Instant")
    print("PASS: exact Instant regen basenames are locked; KDArena/KDMine/KDSpring have no static fallback")
    print("PASS: Lua trees are not treated as KQRegenTable fallback")
    return 0


if __name__ == "__main__":
    sys.exit(main())
