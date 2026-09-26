#!/usr/bin/env python3
"""Lock original NA2016 Kingdom Quest script/static-regen source presence."""
from pathlib import Path
import csv
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/KINGDOM_QUEST_RUNTIME_SOURCE_MANIFEST.tsv"
KQ_SQL = ROOT / "sql/data/data_kq_source_10_kingdomquest.sql"
MAP_SQL = ROOT / "sql/data/data_kq_source_20_kingdomquestmap.sql"
REGEN_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestRegenSource.cs"
SCENARIOBOOK_SOURCE = ROOT / "docs/KINGDOM_QUEST_SCENARIOBOOK_SOURCE.tsv"
SCENARIOBOOK_PROJECTION = ROOT / "NextGen.Zone/Data/KingdomQuestScenarioBookShelfSource.cs"
PINE_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestPineScriptSource.cs"
PINE_CONTROL_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineControlRuntime.cs"
PINE_KQ_TERMINAL = ROOT / "NextGen.Zone/Data/KingdomQuestPineKqTerminalPlan.cs"
SINGLE_DATA_SOURCE = ROOT / "docs/KINGDOM_QUEST_SINGLEDATA_SOURCE.tsv"
SINGLE_DATA_PROJECTION = ROOT / "NextGen.FiestaLib/Data/KingdomQuestSingleDataInfo.cs"
SCENARIOBOOK_ROWS_SHA256 = "eb63221fb015069f2d5099b12074ef13564cb473adccceae1163ed2bcaf78195"
SINGLE_DATA_ROWS_SHA256 = "6e205eb3d9e179d387d2469634cc7a3e46352e0968902185bb61cdc009b8a228"

SOURCE_ARCHIVE_SHA256 = "b83bf92c7193578a772fcebf4d8b7c8c2a9a642cf0d50d33506f77a75b0e211d"
CANONICAL_ROWS_SHA256 = "e4a9c437d8dd44911bd04def351a6a26cdac1423ee633bcace998b586ebe83e7"

PINE_SCRIPT_KEYS = {
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
        "# ScriptCatalog\tWorld/PineScript.txt",
        "# ScriptCatalogSha256\t8ba15c6d7a5d14f1bd01f94a8403d8a9652868e15e0eee7a7730504c7754440c",
        "# ScriptCatalogSemantics\tScenarioBookShelf::sbs_LoadScripts reads table PineScript/ScriptName; sbs_Read tries ScenarioBookShelf/<key>.ps then LuaScript/<key>.lua; MAKE uses sbs_GetScenarioBook",
        "# KQScriptManagerSemantics\tSeparate loader reads DialogFile from World/PineScript.txt and loads ShineScript text; its 64-entry limit is not the MAKE ScenarioBookShelf capacity",
        "# ArchiveRoot\tServer - Kopie/9Data/Shine",
        "# KQRegenLookup\tZone.exe KQRegenTable::kqrt_Load: MobRegen/KingdomQuest/%s.txt -> MobRegen/Instant/%s.txt",
        "# InstantRegenBasenames\tAdlF,AdlFH,Leviathan,Siren,Tower01,Tower02,Tower03,UrgDragon,WarN",
        "# SingleDataSource\tSingleData.shn",
        "# SingleDataSha256\t8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88",
        "# SingleDataShape\trecords=41; defaultRecordLength=36; columns=SingleDataIDX:string32,SingleDataValue:u16",
        "# KQSingleData\tKQVote_VoteLimitTime=60; KQVote_SuggestCoolTime=300; KQVote_LoginCoolTime=300; KQPlayerList_ResetListCoolTime=5",
        "# KQStartSemantics\tKQElement::kqe_QuestStart drops the prior film, closes all map doors, then calls CinemaComplex::cc_PlayFilm with ScriptLanguage and ScriptInitValue",
        "# KQRegenRuntime\tShineRegenGroup::sa_Step -> PineScriptMobRegenerator::psmr_find -> psmr_Load -> KQRegenTable -> MobHatchery::mh_ScriptBreed",
        "# KQRegenTableSemantics\tcapacity=50; element key=12 bytes plus loaded OptionReader; static groups are consumed lazily by the running scenario, not spawned at KQ START",
        "# UsedStaticRegenRows\t15 files; MobRegenGroup=732; MobRegen=766; all use the 7-column/16-column modern source shape",
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


def load_scenario_book_source():
    text = SCENARIOBOOK_SOURCE.read_text(encoding="utf-8")
    required_headers = (
        "# SourceArchive\tServer.zip",
        "# SourceArchiveSha256\t" + SOURCE_ARCHIVE_SHA256,
        "# ZoneExeSha256\tdb1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5",
        "# ScriptCatalog\tWorld/PineScript.txt",
        "# ScriptCatalogSha256\t8ba15c6d7a5d14f1bd01f94a8403d8a9652868e15e0eee7a7730504c7754440c",
        "# NativeLoad\tScenarioBookShelf::sbs_LoadScripts reads PineScript/ScriptName and calls sbs_Read for every catalog row",
        "# NativeRead\tsbs_Read tries ScenarioBookShelf/<key>.ps first, then LuaScript/<key>.lua; missing both returns false",
        "# NativeInsert\tfor a present file sbs_Read calls virtual ScenarioBook::sb_Load, ignores its bool return, then inserts key and object into the shelf",
        "# NativeMakeOrder\tduplicate handle 0x0982 -> sbs_GetScenarioBook null 0x098C -> KQ container full 0x0983",
        "# Scope\t32 KQ/* PineScript catalog rows; all 32 have an exact source file; 27 are used by supplied KingdomQuest.shn",
        "# CanonicalRowsSha256\t" + SCENARIOBOOK_ROWS_SHA256,
    )
    for header in required_headers:
        if header not in text:
            raise ValueError(
                "ScenarioBookShelf provenance header changed: " + header)

    data_lines = [line for line in text.splitlines()
                  if line and not line.startswith("#")]
    reader = csv.DictReader(data_lines, delimiter="\t")
    expected_columns = [
        "key", "backend", "archive_path", "sha256", "size",
        "used_by_supplied_kq"]
    if reader.fieldnames != expected_columns:
        raise ValueError("ScenarioBookShelf source columns changed")

    rows = list(reader)
    canonical = "".join(
        "\t".join(row[name] for name in expected_columns) + "\n"
        for row in rows)
    if hashlib.sha256(canonical.encode("utf-8")).hexdigest() != (
            SCENARIOBOOK_ROWS_SHA256):
        raise ValueError(
            "ScenarioBookShelf canonical source snapshot changed")
    return rows


def load_single_data_source():
    text = SINGLE_DATA_SOURCE.read_text(encoding="utf-8")
    required_headers = (
        "# SourceArchive\tServer.zip",
        "# ArchivePath\tServer - Kopie/9Data/Shine/SingleData.shn",
        "# SourceSha256\t8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88",
        "# Header\t0",
        "# RecordCount\t41",
        "# DefaultRecordLength\t36",
        "# ColumnCount\t2",
        "# Column0\tSingleDataIDX; type=9; length=32",
        "# Column1\tSingleDataValue; type=2; length=2",
        "# CanonicalRowsSha256\t" + SINGLE_DATA_ROWS_SHA256,
    )
    for header in required_headers:
        if header not in text:
            raise ValueError("SingleData provenance header changed: " + header)

    data_lines = [line for line in text.splitlines()
                  if line and not line.startswith("#")]
    reader = csv.DictReader(data_lines, delimiter="\t")
    expected_columns = ["source_row", "key", "value"]
    if reader.fieldnames != expected_columns:
        raise ValueError("SingleData source columns changed")

    rows = list(reader)
    canonical = "".join(
        "\t".join(row[name] for name in expected_columns) + "\n"
        for row in rows)
    if hashlib.sha256(canonical.encode("utf-8")).hexdigest() != (
            SINGLE_DATA_ROWS_SHA256):
        raise ValueError("SingleData canonical source snapshot changed")
    if len(rows) != 41:
        raise ValueError("SingleData source row count changed")
    if [int(row["source_row"]) for row in rows] != list(range(41)):
        raise ValueError("SingleData source ordinals changed")

    values = {row["key"]: int(row["value"]) for row in rows}
    expected_kq = {
        "KQVote_VoteLimitTime": 60,
        "KQVote_SuggestCoolTime": 300,
        "KQVote_LoginCoolTime": 300,
        "KQPlayerList_ResetListCoolTime": 5,
    }
    if any(values.get(key) != value for key, value in expected_kq.items()):
        raise ValueError("KQ SingleData values changed")
    return rows


def main():
    for path in (MANIFEST, KQ_SQL, MAP_SQL, REGEN_SOURCE,
                 SCENARIOBOOK_SOURCE, SCENARIOBOOK_PROJECTION, PINE_SOURCE,
                 PINE_CONTROL_RUNTIME, PINE_KQ_TERMINAL,
                 SINGLE_DATA_SOURCE, SINGLE_DATA_PROJECTION):
        if not path.is_file():
            print("FAIL: missing", path)
            return 1

    try:
        rows = load_manifest()
        scenario_rows = load_scenario_book_source()
        single_data_rows = load_single_data_source()
    except (ValueError, csv.Error) as exc:
        print("FAIL:", exc)
        return 1

    single_data_projection = SINGLE_DATA_PROJECTION.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestSingleDataInfo",
        "8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88",
        "RecordCount = 41",
        "DefaultRecordLength = 36",
        "ColumnCount = 2",
        "KqVoteVoteLimitTimeSeconds = 60",
        "KqVoteSuggestCoolTimeSeconds = 300",
        "KqVoteLoginCoolTimeSeconds = 300",
        "KqPlayerListResetListCoolTimeSeconds = 5",
    ):
        if token not in single_data_projection:
            print("FAIL: KQ SingleData runtime projection changed", token)
            return 1

    regen_source_text = REGEN_SOURCE.read_text(encoding="utf-8")
    source_tokens = (
        "class KingdomQuestRegenGroupSource",
        "class KingdomQuestRegenMobSource",
        "class KingdomQuestRegenSourceDocument",
        "class KingdomQuestRegenSourceLoader",
        "NativeTableCapacity = 50",
        "NativeElementNameBytes = 12",
        '"MobRegen", "KingdomQuest", sourceKey + ".txt"',
        '"MobRegen", "Instant", sourceKey + ".txt"',
        'GroupTable = "MobRegenGroup"',
        'MobTable = "MobRegen"',
        "fields.Length != 8",
        "fields.Length != 17",
        "RegDelta4 = regDelta4",
        "KingdomQuestRegenSourceBackend.Instant",
    )
    missing_source_tokens = [
        token for token in source_tokens if token not in regen_source_text]
    if missing_source_tokens:
        print("FAIL: KQ regen source loader lost native/source boundary",
              missing_source_tokens)
        return 1
    for forbidden in ("Mobspawn", "MapManager", "new Mob("):
        if forbidden in regen_source_text:
            print("FAIL: KQ regen source loader invented live spawn coupling",
                  forbidden)
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

    scenario_by_key = {row["key"]: row for row in scenario_rows}
    if len(scenario_by_key) != 32 or len(scenario_rows) != 32:
        print("FAIL: ScenarioBookShelf KQ catalog key count changed")
        return 1
    if any(row["backend"] not in ("pine", "lua")
           for row in scenario_rows):
        print("FAIL: ScenarioBookShelf backend marker changed")
        return 1
    if sum(row["backend"] == "pine" for row in scenario_rows) != 9 or \
            sum(row["backend"] == "lua" for row in scenario_rows) != 23:
        print("FAIL: ScenarioBookShelf KQ backend split changed")
        return 1
    if any(row["used_by_supplied_kq"] not in ("0", "1")
           for row in scenario_rows):
        print("FAIL: ScenarioBookShelf used marker changed")
        return 1

    used_scenario_keys = {
        row["key"] for row in scenario_rows
        if row["used_by_supplied_kq"] == "1"
    }
    if used_scenario_keys != scripts or len(used_scenario_keys) != 27:
        print("FAIL: supplied KQ ScriptLanguage set no longer matches "
              "source-backed ScenarioBookShelf rows")
        return 1

    for row in scenario_rows:
        key = row["key"]
        expected_path = (
            "ScenarioBookShelf/" + key + ".ps"
            if row["backend"] == "pine"
            else "LuaScript/" + key + ".lua")
        if row["archive_path"] != expected_path:
            print("FAIL: ScenarioBookShelf source path changed", key)
            return 1
        if not re.match(r"^[0-9a-f]{64}$", row["sha256"]):
            print("FAIL: ScenarioBookShelf source hash malformed", key)
            return 1
        try:
            if int(row["size"]) <= 0:
                raise ValueError()
        except ValueError:
            print("FAIL: ScenarioBookShelf source size malformed", key)
            return 1

    scenario_projection_text = SCENARIOBOOK_PROJECTION.read_text(
        encoding="utf-8")
    projection_tokens = (
        "class KingdomQuestScenarioBookShelfSource",
        "KqCatalogKeyCount = 32",
        "KqUsedBySuppliedDefinitions = 27",
        "ContainsSourceBackedScenarioBook(",
        "StringComparer.Ordinal",
        "ignores that bool return",
        "does not parse or execute",
    )
    for token in projection_tokens:
        if token not in scenario_projection_text:
            print("FAIL: ScenarioBookShelf runtime source projection changed",
                  token)
            return 1

    projected_keys = set(re.findall(
        r'^\s+"(KQ/[^"]+)",\s*$', scenario_projection_text, re.MULTILINE))
    if projected_keys != set(scenario_by_key):
        print("FAIL: ScenarioBookShelf runtime key set differs from "
              "proven KQ catalog")
        return 1

    pine_source_text = PINE_SOURCE.read_text(encoding="utf-8")
    pine_tokens = (
        "class KingdomQuestPineScriptSource",
        "UsedPineScriptCount = 9",
        "390c0e948eb035aee62078327cb63d81ddef54aa9e28c38d642d02c86a9f9e57",
        "KingdomQuestPineNodeKind.Command",
        "KingdomQuestPineNodeKind.If",
        "KingdomQuestPineNodeKind.Infinite",
        "KingdomQuestPineNodeKind.Scope",
        "expected top-level named open",
        "orphan structural token",
        "missing close before EOF",
        "KQ/GordonMaster",
        "KQ/Honeying",
        "KQ/KQHBat1",
        "KQ/KQHBat2",
        "KQ/KQHBat3",
        "KQ/KQHBat4",
        "KQ/KQHBat5",
        "KQ/UnderHall",
        "KQ/UnderHall2",
    )
    for token in pine_tokens:
        if token not in pine_source_text:
            print("FAIL: KQ Pine executable-source projection changed", token)
            return 1

    if "no Pine command is assigned gameplay" not in pine_source_text:
        print("FAIL: KQ Pine source parser lost execution-boundary guard")
        return 1

    pine_control_text = PINE_CONTROL_RUNTIME.read_text(encoding="utf-8")
    pine_control_tokens = (
        "interface IKingdomQuestPineRuntimeHost",
        "class KingdomQuestPineControlRuntime",
        "NativeMaxFrameIndex = 0x1F",
        "NativeBreakExitIndex = 0x270F",
        "PineScriptStack::ProcessStack::ps_Push      0x004D6BE0",
        "PineScriptStack::ProcessStack::ps_Pop       0x004D6C20",
        "PineScriptStack::ProcessStack::ps_ExitBlock 0x004D8C70",
        "PineEventScriptNode::Block::sa_Step         0x004D81B0",
        "PineEventScriptNode::StateInfinite::sa_Step 0x004D82C0",
        "PineEventScriptNode::StateIf::sa_Step       0x004D8400",
        "PineEventScriptNode::StateBreak::sa_Step    0x004DA120",
        "PineEventScriptNode::StateCall::sa_Step     0x004DA180",
        "TryEvaluateCondition(",
        "TryStepCommand(",
        "frame.State = 1",
        "stack[match].State = NativeBreakExitIndex",
        "Native Pine ProcessStack frame capacity exceeded.",
        "This class deliberately executes no Fiesta gameplay command.",
    )
    for token in pine_control_tokens:
        if token not in pine_control_text:
            print("FAIL: KQ Pine native control runtime changed", token)
            return 1

    pine_terminal_text = PINE_KQ_TERMINAL.read_text(encoding="utf-8")
    pine_terminal_tokens = (
        "class KingdomQuestPineKqTerminalPlan",
        "class KingdomQuestPineQuestResultPlan",
        "class KingdomQuestPineEndPlan",
        "NativeHeader = 0x16",
        "NativeCompleteType = 0x12",
        "NativeFailType = 0x13",
        "NativeSuccessTitleCategory = 21",
        "NativeFailTitleCategory = 22",
        "NativeNoKingdomQuestHandle = 0xFFFFFFFFu",
        "NativeClearObjectTypeMask = 0xB0u",
        "index_suc",
        "NC_KQ_FAIL_CMD branch",
        "WorldManagerSession::wms_EndOfKQPacket(handle)",
        "FieldMap::fm_ClearObject(0xB0)",
        "it sends no packets, clears no map objects and mutates no title state",
    )
    for token in pine_terminal_tokens:
        if token not in pine_terminal_text:
            print("FAIL: KQ Pine terminal command projection changed", token)
            return 1

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
    if missing_scripts:
        print("FAIL: a used KQ ScriptLanguage lost its original Lua/Pine source",
              sorted(missing_scripts))
        return 1
    if missing_regen != MISSING_STATIC_REGEN:
        print("FAIL: exact missing KQ static regen set changed",
              sorted(missing_regen))
        return 1

    present_scripts = 0
    present_lua_scripts = 0
    present_pine_scripts = 0
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
            if key in PINE_SCRIPT_KEYS:
                present_pine_scripts += 1
                expected_path = "ScenarioBookShelf/" + key + ".ps"
            else:
                present_lua_scripts += 1
                expected_path = "LuaScript/" + key + ".lua"
        else:
            present_regen += 1
            expected_path = "MobRegen/KingdomQuest/" + key + ".txt"
        if row["archive_path"] != expected_path:
            print("FAIL: source path is not the exact key-derived archive path",
                  key)
            return 1

    if (present_scripts, present_lua_scripts,
            present_pine_scripts, present_regen) != (27, 18, 9, 15):
        print("FAIL: KQ runtime-source presence/backend counts changed",
              present_scripts, present_lua_scripts,
              present_pine_scripts, present_regen)
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
    print("PASS: exact SingleData.shn snapshot locked: 41 rows, KQ vote/list values 60/300/300/5")
    print("PASS: all 27 used KingdomQuest.shn ScriptLanguage keys have original source: 18 Lua + 9 PineScript")
    print("PASS: original World/PineScript.txt KQ shelf is locked to 32 exact keys (9 Pine + 23 Lua), all with source files")
    print("PASS: native sbs_Read file-presence insertion is locked: sb_Load return is ignored before shelf insertion")
    print("PASS: all 27 supplied KQ ScriptLanguage values are proven members of the source-backed ScenarioBookShelf")
    print("PASS: all 9 used Pine ScenarioBooks are canonical-source modeled and structurally parsed without gameplay semantics")
    print("PASS: native Pine Block/IF/INFINITE/CALL/BREAK ProcessStack semantics are executable with 32-frame and 0x270F exit boundaries")
    print("PASS: Pine questresult/endofkq terminal actions are source-modeled as COMPLETE/FAIL title hooks and Z2W END + fm_ClearObject(0xB0)")
    print("PASS: MAKE error precedence is source-locked to duplicate -> script lookup -> 300-slot capacity")
    print("PASS: separate KQScriptManager/DialogFile capacity is not conflated with ScenarioBookShelf")
    print("PASS: 18 used KingdomQuestMap BaseMap keys are covered; 15 static KQ regen files present, 3 explicitly absent")
    print("PASS: Zone KQRegenTable lookup order is locked to KingdomQuest then Instant")
    print("PASS: exact Instant regen basenames are locked; KDArena/KDMine/KDSpring have no static fallback")
    print("PASS: KQ START is source-locked to ScenarioBook film execution; static regen activation is lazy through PineScriptMobRegenerator")
    print("PASS: native KQRegenTable capacity/key boundary is modeled as 50 elements and 12-byte source keys")
    print("PASS: 15 used static regen files are locked to 732 MobRegenGroup + 766 MobRegen modern-schema rows")
    print("PASS: source-faithful regen parser remains detached from live map/mob spawning")
    print("PASS: Lua trees are not treated as KQRegenTable fallback")
    return 0


if __name__ == "__main__":
    sys.exit(main())
