#!/usr/bin/env python3
"""Lock original NA2016 Kingdom Quest script/static-regen source presence."""
from pathlib import Path
import base64
import csv
import gzip
import hashlib
import re
import sys
from collections import Counter

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/KINGDOM_QUEST_RUNTIME_SOURCE_MANIFEST.tsv"
KQ_SQL = ROOT / "sql/data/data_kq_source_10_kingdomquest.sql"
MAP_SQL = ROOT / "sql/data/data_kq_source_20_kingdomquestmap.sql"
MAP_INFO_SQL = ROOT / "sql/data/mapinfo.sql"
MOB_INFO_SQL = ROOT / "sql/data/data_mobinfo.sql"
REGEN_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestRegenSource.cs"
SCENARIOBOOK_SOURCE = ROOT / "docs/KINGDOM_QUEST_SCENARIOBOOK_SOURCE.tsv"
SCENARIOBOOK_PROJECTION = ROOT / "NextGen.Zone/Data/KingdomQuestScenarioBookShelfSource.cs"
PINE_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestPineScriptSource.cs"
PINE_CONTROL_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineControlRuntime.cs"
PINE_VARIABLE_STACK = ROOT / "NextGen.Zone/Data/KingdomQuestPineVariableStack.cs"
PINE_BASIC_EXPRESSION = ROOT / "NextGen.Zone/Data/KingdomQuestPineBasicExpression.cs"
PINE_REMOVE_FIRST = ROOT / "NextGen.Zone/Data/KingdomQuestPineRemoveFirst.cs"
PINE_RANDOM_EXPRESSION = ROOT / "NextGen.Zone/Data/KingdomQuestPineRandomExpression.cs"
PINE_DISTANCE_EXPRESSION = ROOT / "NextGen.Zone/Data/KingdomQuestPineDistanceExpression.cs"
PINE_CONDITION_EXPRESSION = ROOT / "NextGen.Zone/Data/KingdomQuestPineConditionExpression.cs"
PINE_CHAR_NAME_EXPRESSION = ROOT / "NextGen.Zone/Data/KingdomQuestPineCharNameExpression.cs"
PINE_USED_EXPRESSION_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineUsedExpressionRuntime.cs"
PINE_USED_COMMAND_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineUsedCommandRuntime.cs"
PINE_LOCAL_COMMAND_STATE = ROOT / "NextGen.Zone/Data/KingdomQuestPineLocalCommandState.cs"
UNDERHALL_COMMAND_SOURCE = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallCommandSource.cs"
UNDERHALL_COMMAND_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallCommandRuntime.cs"
UNDERHALL_COMMAND_STATE = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallCommandState.cs"
UNDERHALL_EXTERNAL_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallExternalPlan.cs"
UNDERHALL_SOURCE_FLOW = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallSourceFlow.cs"
UNDERHALL_TIMED_INTERRUPT_DUE = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallTimedInterruptDue.cs"
UNDERHALL_INTERRUPT_CANDIDATE = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallInterruptCandidate.cs"
UNDERHALL_EVENT_PREDICATE_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallEventPredicatePlan.cs"
UNDERHALL_SOURCE_CATALOG = ROOT / "NextGen.Zone/Data/KingdomQuestUnderHallSourceCatalog.cs"
SCENARIO_START_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestScenarioStartPlan.cs"
PINE_SCENARIO_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineScenarioRuntime.cs"
LUA_SCENARIO_SCOPE = ROOT / "NextGen.Zone/Data/KingdomQuestLuaScenarioScope.cs"
ZONE_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestZoneRuntime.cs"
PINE_KQ_TERMINAL = ROOT / "NextGen.Zone/Data/KingdomQuestPineKqTerminalPlan.cs"
PINE_NATIVE_OBJECT_CLEAR = ROOT / "NextGen.Zone/Data/KingdomQuestPineNativeObjectClear.cs"
PINE_KQ_TITLE_HOOK = ROOT / "NextGen.Zone/Data/KingdomQuestPineKqTitleHookNative.cs"
PINE_KQ_REWARD_COMMAND = ROOT / "NextGen.Zone/Data/KingdomQuestPineKqRewardCommandNative.cs"
PINE_REGEN_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestPineRegenGroupPlan.cs"
PINE_REGEN_RUNTIME_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestPineRegenRuntimePlan.cs"
PINE_REGEN_NATIVE_LAYOUT = ROOT / "NextGen.Zone/Data/KingdomQuestPineRegenNativeLayout.cs"
PINE_TIMING_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestPineTimingPlan.cs"
PINE_INTERRUPT_PLAN = ROOT / "NextGen.Zone/Data/KingdomQuestPineInterruptPlan.cs"
PINE_INTERRUPT_RUNTIME = ROOT / "NextGen.Zone/Data/KingdomQuestPineInterruptRuntime.cs"
PINE_WAITLOGIN = ROOT / "NextGen.Zone/Data/KingdomQuestPineWaitLogin.cs"
PINE_WAITINTERRUPT = ROOT / "NextGen.Zone/Data/KingdomQuestPineWaitInterrupt.cs"
PINE_SCRIPTFILE = ROOT / "NextGen.Zone/Data/KingdomQuestPineScriptFile.cs"
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
    for path in (MANIFEST, KQ_SQL, MAP_SQL, MAP_INFO_SQL, MOB_INFO_SQL, REGEN_SOURCE,
                 SCENARIOBOOK_SOURCE, SCENARIOBOOK_PROJECTION, PINE_SOURCE,
                 PINE_CONTROL_RUNTIME, PINE_VARIABLE_STACK,
                 PINE_BASIC_EXPRESSION, PINE_REMOVE_FIRST,
                 PINE_RANDOM_EXPRESSION, PINE_DISTANCE_EXPRESSION,
                 PINE_CONDITION_EXPRESSION, PINE_CHAR_NAME_EXPRESSION,
                 PINE_USED_EXPRESSION_RUNTIME, PINE_USED_COMMAND_RUNTIME,
                 PINE_LOCAL_COMMAND_STATE, UNDERHALL_COMMAND_SOURCE,
                 UNDERHALL_COMMAND_RUNTIME, UNDERHALL_COMMAND_STATE,
                 UNDERHALL_EXTERNAL_PLAN, UNDERHALL_SOURCE_FLOW,
                 UNDERHALL_TIMED_INTERRUPT_DUE, UNDERHALL_INTERRUPT_CANDIDATE,
                 UNDERHALL_EVENT_PREDICATE_PLAN, UNDERHALL_SOURCE_CATALOG,
                 SCENARIO_START_PLAN, PINE_SCENARIO_RUNTIME,
                 LUA_SCENARIO_SCOPE, ZONE_RUNTIME,
                 PINE_KQ_TERMINAL, PINE_REGEN_PLAN, PINE_REGEN_RUNTIME_PLAN,
                 PINE_REGEN_NATIVE_LAYOUT, PINE_TIMING_PLAN, PINE_INTERRUPT_PLAN, PINE_INTERRUPT_RUNTIME,
                 PINE_WAITLOGIN,
                 PINE_WAITINTERRUPT, PINE_SCRIPTFILE,
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
        "KingdomQuestPineNodeKind.VariableDeclaration",
        "KingdomQuestPineNodeKind.Assignment",
        "class KingdomQuestPineVariableDeclarationSource",
        "ParseVariableDeclarations(",
        "TryAssignment(",
        "unterminated var declaration",
        "new PineMeta(355, 16, 249, 8, 5, 0, 7, 8",
        "new PineMeta(151, 10, 92, 3, 2, 1, 1, 8",
        "new PineMeta(405, 45, 251, 1, 19, 0, 1, 0",
        "new PineMeta(588, 57, 401, 1, 22, 0, 1, 0",
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

    compressed_match = re.search(
        r'private const string CompressedCanonicalSource\s*=\s*"([^"]+)";',
        pine_source_text)
    if compressed_match is None:
        print("FAIL: KQ Pine canonical bundle literal missing")
        return 1
    try:
        pine_bundle = gzip.decompress(
            base64.b64decode(compressed_match.group(1))).decode("ascii")
    except (ValueError, OSError, UnicodeDecodeError) as exc:
        print("FAIL: KQ Pine canonical bundle decode failed", exc)
        return 1
    if hashlib.sha256(pine_bundle.encode("ascii")).hexdigest() != (
            "390c0e948eb035aee62078327cb63d81ddef54aa9e28c38d642d02c86a9f9e57"):
        print("FAIL: KQ Pine canonical bundle decoded hash changed")
        return 1

    def is_assignment_line(value):
        quoted = False
        depth = 0
        for i, ch in enumerate(value):
            if ch == '"':
                quoted = not quoted
                continue
            if quoted:
                continue
            if ch == '(':
                depth += 1
                continue
            if ch == ')':
                if depth > 0:
                    depth -= 1
                continue
            if ch != '=' or depth != 0:
                continue
            before = value[i - 1] if i > 0 else ""
            after = value[i + 1] if i + 1 < len(value) else ""
            if before in ("=", "!") or after in ("=", "!"):
                return False
            return i > 0
        return False

    pine_command_verbs = Counter()
    pine_command_verbs_by_script = {}
    pine_command_lines_by_script = {}
    current_command_script = None
    in_var_declaration = False
    for raw_line in pine_bundle.splitlines():
        line = raw_line.strip()
        if not line:
            continue
        if line.startswith("@@ "):
            current_command_script = line[3:].strip()
            pine_command_verbs_by_script.setdefault(
                current_command_script, Counter())
            pine_command_lines_by_script.setdefault(
                current_command_script, {})
            in_var_declaration = False
            continue
        lower = line.lower()

        if in_var_declaration:
            if line.endswith("."):
                in_var_declaration = False
            continue

        if lower.startswith("var "):
            if not line.endswith("."):
                in_var_declaration = True
            continue

        if (lower == "close" or lower == "open" or
                lower == "then open" or lower == "else" or
                lower == "else open" or lower == "infinite" or
                lower.startswith("open [") or lower.startswith("if ")):
            continue

        if is_assignment_line(line):
            continue

        verb = line.split(None, 1)[0].rstrip(".").lower()
        pine_command_verbs[verb] += 1
        if current_command_script is None:
            print("FAIL: KQ Pine command appeared before script marker")
            return 1
        pine_command_verbs_by_script[current_command_script][verb] += 1
        pine_command_lines_by_script[current_command_script].setdefault(
            verb, []).append(line)

    pine_meta_command_counts = [
        int(value) for value in re.findall(
            r"new PineMeta\(\d+,\s*\d+,\s*(\d+),",
            pine_source_text)
    ]
    if len(pine_meta_command_counts) != 9:
        print("FAIL: KQ Pine metadata command-count extraction changed")
        return 1
    if sum(pine_command_verbs.values()) != sum(pine_meta_command_counts):
        print("FAIL: KQ Pine command inventory does not match parsed node count",
              sum(pine_command_verbs.values()),
              sum(pine_meta_command_counts))
        return 1

    expected_pine_command_verbs = {
        "abstatereset": 2,
        "abstateset": 8,
        "battlestart": 5,
        "battlestop": 15,
        "break": 71,
        "broadcast": 87,
        "call": 139,
        "chatwin": 83,
        "doorbuild": 6,
        "doorclose": 7,
        "dooropen": 7,
        "effectobj": 3,
        "endofkq": 19,
        "exchange2mob": 1,
        "interruptclear": 65,
        "interrupterase": 14,
        "interruptset": 225,
        "invensearch": 3,
        "invidualreward": 10,
        "itemdrop": 9,
        "itemerase": 22,
        "itemowner": 2,
        "linkto": 19,
        "mobattr": 4,
        "mobregen": 5,
        "npcchat": 20,
        "npcshout": 5,
        "npcstand": 2,
        "pause": 202,
        "questmobkill": 5,
        "questresult": 9,
        "regengroup": 243,
        "revival": 5,
        "reward": 5,
        "scriptfile": 19,
        "sendquestresult": 10,
        "suicide": 1,
        "summonmob": 72,
        "teleport": 1,
        "timelimit": 14,
        "vanish": 3,
        "waitinterrupt": 55,
        "waitlogin": 9,
        "whoclickme": 3,
    }
    if pine_command_verbs != Counter(expected_pine_command_verbs):
        print("FAIL: KQ Pine exact command verb inventory changed",
              dict(sorted(pine_command_verbs.items())))
        return 1

    pine_command_inventory = ", ".join(
        f"{verb}={pine_command_verbs[verb]}"
        for verb in sorted(pine_command_verbs))
    print("PASS: KQ Pine command verb inventory:", pine_command_inventory)

    unrecovered_pine_verbs = {
        "abstatereset", "abstateset", "battlestart", "battlestop",
        "broadcast", "chatwin", "doorbuild", "doorclose", "dooropen",
        "effectobj", "exchange2mob", "invensearch", "invidualreward",
        "itemdrop", "itemerase", "itemowner", "linkto", "mobattr",
        "mobregen", "npcchat", "npcshout", "npcstand", "questmobkill",
        "revival", "reward", "sendquestresult", "suicide",
        "summonmob", "teleport", "vanish",
        "whoclickme",
    }
    if set(pine_command_verbs_by_script) != PINE_SCRIPT_KEYS:
        print("FAIL: KQ Pine per-script command key set changed",
              sorted(pine_command_verbs_by_script))
        return 1
    expected_unresolved_by_script = {
        "KQ/GordonMaster": {
            "abstatereset": 2, "abstateset": 3, "broadcast": 9,
            "chatwin": 7, "doorbuild": 3, "doorclose": 4, "dooropen": 4,
            "exchange2mob": 1, "invensearch": 3, "itemdrop": 4,
            "itemerase": 2, "itemowner": 2, "linkto": 2, "mobattr": 4,
            "mobregen": 2, "npcchat": 20, "npcstand": 2,
            "questmobkill": 1, "reward": 1,
            "suicide": 1, "summonmob": 1, "teleport": 1,
            "whoclickme": 3,
        },
        "KQ/Honeying": {
            "broadcast": 8, "chatwin": 2, "doorbuild": 3,
            "doorclose": 3, "dooropen": 3, "effectobj": 3, "linkto": 2,
            "mobregen": 1, "npcshout": 5, "questmobkill": 1, "reward": 1,
            "summonmob": 5, "vanish": 3,
        },
        "KQ/KQHBat1": {
            "abstateset": 1, "battlestart": 1, "battlestop": 3,
            "broadcast": 10, "chatwin": 14, "invidualreward": 2,
            "itemdrop": 1, "itemerase": 4, "linkto": 2, "revival": 1,
            "sendquestresult": 2,
        },
        "KQ/KQHBat2": {
            "abstateset": 1, "battlestart": 1, "battlestop": 3,
            "broadcast": 10, "chatwin": 14, "invidualreward": 2,
            "itemdrop": 1, "itemerase": 4, "linkto": 2, "revival": 1,
            "sendquestresult": 2,
        },
        "KQ/KQHBat3": {
            "abstateset": 1, "battlestart": 1, "battlestop": 3,
            "broadcast": 10, "chatwin": 14, "invidualreward": 2,
            "itemdrop": 1, "itemerase": 4, "linkto": 2, "revival": 1,
            "sendquestresult": 2,
        },
        "KQ/KQHBat4": {
            "abstateset": 1, "battlestart": 1, "battlestop": 3,
            "broadcast": 10, "chatwin": 14, "invidualreward": 2,
            "itemdrop": 1, "itemerase": 4, "linkto": 2, "revival": 1,
            "sendquestresult": 2,
        },
        "KQ/KQHBat5": {
            "abstateset": 1, "battlestart": 1, "battlestop": 3,
            "broadcast": 10, "chatwin": 14, "invidualreward": 2,
            "itemdrop": 1, "itemerase": 4, "linkto": 2, "revival": 1,
            "sendquestresult": 2,
        },
        "KQ/UnderHall": {
            "broadcast": 8, "linkto": 2, "mobregen": 1,
            "questmobkill": 1, "reward": 1, "summonmob": 12,
        },
        "KQ/UnderHall2": {
            "broadcast": 12, "chatwin": 4, "linkto": 3, "mobregen": 1,
            "questmobkill": 2, "reward": 2, "summonmob": 54,
        },
    }

    for key in sorted(pine_command_verbs_by_script):
        counts = pine_command_verbs_by_script[key]
        unresolved = {
            verb: counts[verb]
            for verb in sorted(unrecovered_pine_verbs)
            if counts[verb]
        }
        if unresolved != expected_unresolved_by_script[key]:
            print("FAIL: KQ Pine per-script unresolved verb shape changed",
                  key, unresolved)
            return 1
    print("PASS: KQ Pine per-script unresolved verb matrix is source-locked")

    expected_underhall_forms = {
        "broadcast": {
            'broadcast all "KQReturn10".',
            'broadcast all "KQReturn20".',
            'broadcast all "KQReturn30".',
            'broadcast all "KQReturn5".',
        },
        "linkto": {
            'linkto all "Eld" "Eld" 17214 13445.',
        },
        "mobregen": {
            'mobregen KQ_BossRobo "KQ_BossRobo" 2300 2500 90 1000 "Normal".',
        },
        "questmobkill": {
            'questmobkill 2668 "Daliy_Check" 1.',
        },
        "reward": {
            'reward KingdomQuest.',
        },
        "summonmob": {
            'summonmob KQ_BossRobo "KQ_DesertWolf" 3.',
            'summonmob KQ_BossRobo "KQ_FireViVi" 6.',
            'summonmob KQ_BossRobo "KQ_GiantMushRoom" 2.',
            'summonmob KQ_BossRobo "KQ_RapidBoar" 5.',
            'summonmob KQ_BossRobo "KQ_SkelArcher" 4.',
            'summonmob KQ_BossRobo "KQ_SkelKnight" 2.',
            'summonmob KQ_BossRobo "KQ_SkelWarrior" 3.',
            'summonmob KQ_BossRobo "KQ_SkelWarrior" 5.',
            'summonmob KQ_BossRobo "KQ_Skeleton" 5.',
            'summonmob KQ_BossRobo "KQ_WildKebing" 5.',
            'summonmob KQ_BossRobo "KQ_Zombie" 5.',
        },
        "waitinterrupt": {
            'waitinterrupt InterruptBlock "InterruptArg".',
        },
        "waitlogin": {
            'waitlogin Wait.',
        },
    }
    for verb, expected_forms in expected_underhall_forms.items():
        forms = set(
            pine_command_lines_by_script["KQ/UnderHall"].get(verb, []))
        if forms != expected_forms:
            print("FAIL: KQ UnderHall source command forms changed",
                  verb, sorted(forms))
            return 1
    print("PASS: KQ UnderHall unresolved command forms are source-locked")

    underhall_interrupt_state_lines = []
    current_source_script = None
    for raw_line in pine_bundle.splitlines():
        line = raw_line.strip()
        if line.startswith("@@ "):
            current_source_script = line[3:].strip()
            continue
        if (current_source_script == "KQ/UnderHall" and
                ("InterruptBlock" in line or "InterruptArg" in line)):
            underhall_interrupt_state_lines.append(line)
    if (underhall_interrupt_state_lines.count('InterruptBlock  ""') != 1 or
            underhall_interrupt_state_lines.count('InterruptArg    ""') != 1 or
            underhall_interrupt_state_lines.count(
                'waitinterrupt InterruptBlock "InterruptArg".') != 19 or
            underhall_interrupt_state_lines.count(
                'call InterruptBlock.') != 19):
        print("FAIL: KQ UnderHall waitinterrupt/dynamic-call source shape changed",
              underhall_interrupt_state_lines)
        return 1

    underhall_source_lines = []
    current_source_script = None
    for raw_line in pine_bundle.splitlines():
        line = raw_line.strip()
        if line.startswith("@@ "):
            current_source_script = line[3:].strip()
            continue
        if current_source_script == "KQ/UnderHall" and line:
            underhall_source_lines.append(line)
    for index, line in enumerate(underhall_source_lines[:-1]):
        if line == 'waitinterrupt InterruptBlock "InterruptArg".':
            if underhall_source_lines[index + 1] != 'call InterruptBlock.':
                print("FAIL: KQ UnderHall waitinterrupt no longer feeds dynamic CALL",
                      index, underhall_source_lines[index:index + 2])
                return 1
    print("PASS: KQ UnderHall has 19 waitinterrupt -> dynamic call InterruptBlock pairs")

    underhall_interrupt_arg_lines = [
        line for line in underhall_source_lines
        if "InterruptArg" in line
    ]
    if (len(underhall_interrupt_arg_lines) != 20 or
            underhall_interrupt_arg_lines.count('InterruptArg    ""') != 1 or
            underhall_interrupt_arg_lines.count(
                'waitinterrupt InterruptBlock "InterruptArg".') != 19):
        print("FAIL: KQ UnderHall InterruptArg gained a source consumer",
              underhall_interrupt_arg_lines)
        return 1
    print("PASS: KQ UnderHall InterruptArg is only declared and written by waitinterrupt")

    underhall_external_verbs = {
        "broadcast", "linkto", "mobregen", "questmobkill",
        "reward", "summonmob",
    }
    underhall_external_sequence = []
    for index, line in enumerate(underhall_source_lines):
        verb = line.split(None, 1)[0].lower().rstrip(".")
        if verb in underhall_external_verbs:
            underhall_external_sequence.append((index + 1, line))
    if len(underhall_external_sequence) != 25:
        print("FAIL: KQ UnderHall external command occurrence count changed",
              underhall_external_sequence)
        return 1
    print("INFO: KQ UnderHall external command sequence:",
          " || ".join(
              "{0}:{1}".format(line_no, line)
              for line_no, line in underhall_external_sequence))

    underhall_external_blocks = []
    current_top_block = None
    source_depth = 0
    for line_no, line in enumerate(underhall_source_lines, 1):
        lower = line.lower()
        if lower.startswith("open [") and line.endswith("]"):
            if source_depth == 0:
                current_top_block = line[line.index("[") + 1:-1]
            source_depth += 1
            continue
        if lower in ("open", "then open", "else open"):
            source_depth += 1
            continue
        if lower == "close":
            source_depth -= 1
            if source_depth < 0:
                print("FAIL: KQ UnderHall source depth became negative", line_no)
                return 1
            if source_depth == 0:
                current_top_block = None
            continue

        verb = line.split(None, 1)[0].lower().rstrip(".")
        if verb in underhall_external_verbs:
            if source_depth <= 0 or not current_top_block:
                print("FAIL: KQ UnderHall external command lost top-level block",
                      line_no, line)
                return 1
            underhall_external_blocks.append(
                (line_no, current_top_block, line))

    if source_depth != 0 or len(underhall_external_blocks) != 25:
        print("FAIL: KQ UnderHall external block mapping changed",
              source_depth, underhall_external_blocks)
        return 1
    print("INFO: KQ UnderHall external command blocks:",
          " || ".join(
              "{0}:{1}:{2}".format(line_no, block, line)
              for line_no, block, line in underhall_external_blocks))

    expected_underhall_external_blocks = [
        (339, "Nineteenth", 'mobregen KQ_BossRobo "KQ_BossRobo" 2300 2500 90 1000 "Normal".'),
        (355, "Summon1", 'summonmob KQ_BossRobo "KQ_DesertWolf" 3.'),
        (356, "Summon1", 'summonmob KQ_BossRobo "KQ_GiantMushRoom" 2.'),
        (359, "Summon2", 'summonmob KQ_BossRobo "KQ_Skeleton" 5.'),
        (362, "Summon3", 'summonmob KQ_BossRobo "KQ_SkelWarrior" 3.'),
        (363, "Summon3", 'summonmob KQ_BossRobo "KQ_SkelKnight" 2.'),
        (364, "Summon3", 'summonmob KQ_BossRobo "KQ_SkelArcher" 4.'),
        (367, "Summon4", 'summonmob KQ_BossRobo "KQ_Skeleton" 5.'),
        (368, "Summon4", 'summonmob KQ_BossRobo "KQ_WildKebing" 5.'),
        (369, "Summon4", 'summonmob KQ_BossRobo "KQ_Zombie" 5.'),
        (372, "Summon5", 'summonmob KQ_BossRobo "KQ_SkelWarrior" 5.'),
        (373, "Summon5", 'summonmob KQ_BossRobo "KQ_RapidBoar" 5.'),
        (374, "Summon5", 'summonmob KQ_BossRobo "KQ_FireViVi" 6.'),
        (378, "QuestSuc", 'reward KingdomQuest.'),
        (379, "QuestSuc", 'questmobkill 2668 "Daliy_Check" 1.'),
        (380, "QuestSuc", 'broadcast all "KQReturn30".'),
        (382, "QuestSuc", 'broadcast all "KQReturn20".'),
        (384, "QuestSuc", 'broadcast all "KQReturn10".'),
        (386, "QuestSuc", 'broadcast all "KQReturn5".'),
        (388, "QuestSuc", 'linkto all "Eld" "Eld" 17214 13445.'),
        (394, "QuestFail", 'broadcast all "KQReturn30".'),
        (396, "QuestFail", 'broadcast all "KQReturn20".'),
        (398, "QuestFail", 'broadcast all "KQReturn10".'),
        (400, "QuestFail", 'broadcast all "KQReturn5".'),
        (402, "QuestFail", 'linkto all "Eld" "Eld" 17214 13445.'),
    ]
    if underhall_external_blocks != expected_underhall_external_blocks:
        print("FAIL: KQ UnderHall exact external block routing changed",
              underhall_external_blocks)
        return 1
    print("PASS: KQ UnderHall exact external block routing is source-locked")

    underhall_waitlogin_context = []
    for index, line in enumerate(underhall_source_lines):
        if line == "waitlogin Wait.":
            start = max(0, index - 4)
            end = min(len(underhall_source_lines), index + 5)
            underhall_waitlogin_context = underhall_source_lines[start:end]
            break
    expected_underhall_waitlogin_context = [
        'var Wait            ""',
        'InterruptBlock  ""',
        'InterruptArg    ""',
        'KQ_BossRobo 0.',
        'waitlogin Wait.',
        'if Wait == 0',
        'then open',
        'call "QuestFail".',
        'close',
    ]
    if underhall_waitlogin_context != expected_underhall_waitlogin_context:
        print("FAIL: KQ UnderHall waitlogin source context changed",
              underhall_waitlogin_context)
        return 1
    print("PASS: KQ UnderHall waitlogin feeds exact Wait == 0 -> QuestFail gate")

    pine_source_lines_by_script = {}
    current_waitlogin_script = None
    for raw_line in pine_bundle.splitlines():
        line = raw_line.strip()
        if line.startswith("@@ "):
            current_waitlogin_script = line[3:].strip()
            pine_source_lines_by_script[current_waitlogin_script] = []
            continue
        if current_waitlogin_script is not None and line:
            pine_source_lines_by_script[current_waitlogin_script].append(line)

    waitlogin_targets = {}
    for key in sorted(PINE_SCRIPT_KEYS):
        lines = pine_source_lines_by_script.get(key, [])
        matches = [
            (index, line)
            for index, line in enumerate(lines)
            if line.lower().startswith("waitlogin ")
        ]
        if len(matches) != 1:
            print("FAIL: KQ Pine waitlogin count changed", key, matches)
            return 1

        index, line = matches[0]
        target = line[len("waitlogin "):].rstrip(".").strip()
        if not target or index + 1 >= len(lines):
            print("FAIL: KQ Pine waitlogin target/context malformed", key, line)
            return 1

        expected_numeric = "if {0} == 0".format(target)
        expected_token = "if {0} === 0".format(target)
        if lines[index + 1] not in (expected_numeric, expected_token):
            print("FAIL: KQ Pine waitlogin no longer feeds immediate zero gate",
                  key, lines[index:index + 2])
            return 1
        waitlogin_targets[key] = target

    if len(waitlogin_targets) != 9:
        print("FAIL: KQ Pine waitlogin script coverage changed",
              waitlogin_targets)
        return 1
    print("PASS: all 9 Pine KQs route one waitlogin target directly into a zero gate")

    waitinterrupt_count = 0
    for key in sorted(PINE_SCRIPT_KEYS):
        lines = pine_source_lines_by_script.get(key, [])
        for index, line in enumerate(lines):
            if not line.lower().startswith("waitinterrupt "):
                continue
            if (line != 'waitinterrupt InterruptBlock "InterruptArg".' or
                    index + 1 >= len(lines) or
                    lines[index + 1] != 'call InterruptBlock.'):
                print("FAIL: KQ Pine waitinterrupt dataflow changed",
                      key, lines[index:index + 2])
                return 1
            waitinterrupt_count += 1
    if waitinterrupt_count != 55:
        print("FAIL: KQ Pine waitinterrupt occurrence count changed",
              waitinterrupt_count)
        return 1
    print("PASS: all 55 Pine waitinterrupt commands feed dynamic call InterruptBlock")

    underhall_interrupt_type_counts = Counter()
    for line in pine_command_lines_by_script["KQ/UnderHall"].get(
            "interruptset", []):
        parts = line.split()
        if len(parts) < 2:
            print("FAIL: malformed UnderHall interruptset source", line)
            return 1
        underhall_interrupt_type_counts[parts[1]] += 1
    expected_underhall_interrupt_types = {
        "HPLow": 5,
        "PlayerEliminate": 19,
        "Sec": 18,
        "TimeOut": 19,
    }
    if dict(underhall_interrupt_type_counts) != expected_underhall_interrupt_types:
        print("FAIL: KQ UnderHall interrupt type shape changed",
              dict(underhall_interrupt_type_counts))
        return 1
    print("PASS: KQ UnderHall interrupt types are locked:",
          ",".join(
              "{0}={1}".format(kind, underhall_interrupt_type_counts[kind])
              for kind in sorted(underhall_interrupt_type_counts)))

    underhall_interrupt_lines = (
        pine_command_lines_by_script["KQ/UnderHall"].get(
            "interruptset", []))

    expected_underhall_hp_low_forms = [
        'interruptset HPLow "" 1 KQ_BossRobo 800 "Summon1".',
        'interruptset HPLow "" 1 KQ_BossRobo 600 "Summon2".',
        'interruptset HPLow "" 1 KQ_BossRobo 400 "Summon3".',
        'interruptset HPLow "" 1 KQ_BossRobo 200 "Summon4".',
        'interruptset HPLow "" 1 KQ_BossRobo 100 "Summon5".',
    ]
    actual_hp_low_forms = [
        line for line in underhall_interrupt_lines
        if len(line.split()) >= 2 and line.split()[1] == "HPLow"
    ]
    if actual_hp_low_forms != expected_underhall_hp_low_forms:
        print("FAIL: KQ UnderHall HPLow forms changed", actual_hp_low_forms)
        return 1

    actual_player_eliminate_forms = [
        line for line in underhall_interrupt_lines
        if len(line.split()) >= 2 and line.split()[1] == "PlayerEliminate"
    ]
    expected_player_eliminate = 'interruptset PlayerEliminate "" 1 "QuestFail".'
    if (len(actual_player_eliminate_forms) != 19 or
            any(line != expected_player_eliminate
                for line in actual_player_eliminate_forms)):
        print("FAIL: KQ UnderHall PlayerEliminate forms changed",
              actual_player_eliminate_forms)
        return 1
    print("PASS: KQ UnderHall event interrupt forms are source-locked")

    if any(
            len(line.split()) < 4 or line.split()[3] != "1"
            for line in underhall_interrupt_lines):
        print("FAIL: KQ UnderHall interrupt RepeatCount is no longer exact-one")
        return 1
    print("PASS: all 61 UnderHall interrupt registrations use RepeatCount=1")

    underhall_catalog_text = UNDERHALL_SOURCE_CATALOG.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestUnderHallSourceCatalog",
        "ElderineMapId = 9",
        "CorrelatedMobCount = 11",
        '"Eld"',
        '"Elderine"',
        "17214",
        "13445",
        "They do not assert how a Pine command consumes",
    ):
        if token not in underhall_catalog_text:
            print("FAIL: KQ UnderHall source catalog changed", token)
            return 1

    mapinfo_rows = {}
    for row in data_rows(MAP_INFO_SQL):
        fields = split_row_fields(row)
        if len(fields) != 10:
            print("FAIL: MapInfo source row width changed")
            return 1
        mapinfo_rows[int(fields[0])] = fields
    elderine = mapinfo_rows.get(9)
    if (elderine is None or
            unquote_sql(elderine[1]) != "Eld" or
            unquote_sql(elderine[2]) != "Elderine" or
            int(elderine[4]) != 17214 or
            int(elderine[5]) != 13445 or
            unquote_sql(elderine[7]) != "Eld"):
        print("FAIL: UnderHall Elderine source correlation changed",
              elderine)
        return 1

    expected_underhall_mobs = {
        "KQ_BossRobo": (1068, "Millennium Robo"),
        "KQ_DesertWolf": (1051, "Carnival Wolf"),
        "KQ_FireViVi": (1066, "Angry Fire ViVi"),
        "KQ_GiantMushRoom": (1046, "Giant Mushroom"),
        "KQ_RapidBoar": (1058, "Rapid Boar"),
        "KQ_SkelArcher": (1056, "Skeleton Archer"),
        "KQ_SkelKnight": (1060, "Dark Skeleton Knight"),
        "KQ_SkelWarrior": (1059, "Powerful Skeleton Warrior"),
        "KQ_Skeleton": (1054, "Brave Skeleton"),
        "KQ_WildKebing": (1055, "Cave Kebing"),
        "KQ_Zombie": (1065, "Madness Zombie"),
    }
    mobinfo_by_inx = {}
    for row in data_rows(MOB_INFO_SQL):
        fields = split_row_fields(row)
        if len(fields) != 15:
            print("FAIL: MobInfo source row width changed")
            return 1
        mobinfo_by_inx[unquote_sql(fields[1])] = fields

    for inx_name, expected in expected_underhall_mobs.items():
        fields = mobinfo_by_inx.get(inx_name)
        if (fields is None or int(fields[0]) != expected[0] or
                unquote_sql(fields[2]) != expected[1]):
            print("FAIL: UnderHall MobInfo source correlation changed",
                  inx_name, fields)
            return 1
        for token in (
                str(expected[0]), '"' + inx_name + '"',
                '"' + expected[1] + '"'):
            if token not in underhall_catalog_text:
                print("FAIL: UnderHall source catalog lost MobInfo row",
                      inx_name, token)
                return 1

    underhall_runtime_text = UNDERHALL_COMMAND_RUNTIME.read_text(
        encoding="utf-8")
    for token in (
        "interface IKingdomQuestUnderHallCommandSink",
        "class KingdomQuestUnderHallCommandRuntime",
        'ScriptLanguage = "KQ/UnderHall"',
        "SourceUsedFamilyCount = 6",
        "SourceUsedOccurrenceCount = 25",
        "SourceDistinctFormCount = 19",
        "KingdomQuestUnderHallCommandSource.TryParse(",
        "KingdomQuestPineVariableStack variables",
        "ref int nativeState",
        "out bool completed",
        "SourceUsedVerbs.Contains(verb)",
    ):
        if token not in underhall_runtime_text:
            print("FAIL: KQ UnderHall command runtime composition changed",
                  token)
            return 1
    underhall_runtime_verbs = {
        "broadcast", "linkto", "mobregen",
        "questmobkill", "reward", "summonmob",
    }
    for verb in sorted(underhall_runtime_verbs):
        if ('"' + verb + '"') not in underhall_runtime_text:
            print("FAIL: KQ UnderHall runtime lost external verb", verb)
            return 1
    for shared_verb in ("scriptfile", "waitinterrupt", "waitlogin"):
        if ('"' + shared_verb + '",') in underhall_runtime_text:
            print("FAIL: KQ UnderHall runtime duplicated shared Pine verb",
                  shared_verb)
            return 1
    for forbidden in (
            "MapManager.Instance", "ChangeMap(", "SendPacket(",
            "Program.DatabaseManager", "System.Random", "DateTime.Now"):
        if forbidden in underhall_runtime_text:
            print("FAIL: KQ UnderHall runtime invented a side effect",
                  forbidden)
            return 1

    underhall_source_flow_text = UNDERHALL_SOURCE_FLOW.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestUnderHallExternalSourceSite",
        "class KingdomQuestUnderHallSourceFlow",
        'SuccessBlock = "QuestSuc"',
        'FailureBlock = "QuestFail"',
        "ExternalOccurrenceCount = 25",
        "SuccessRewardOccurrenceCount = 1",
        "FailureRewardOccurrenceCount = 0",
        "{ 378, Site(378, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Reward) }",
        "{ 388, Site(388, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.LinkTo) }",
        "{ 402, Site(402, FailureBlock, KingdomQuestUnderHallExternalPlanKind.LinkTo) }",
        "candidate.Kind != kind",
    ):
        if token not in underhall_source_flow_text:
            print("FAIL: KQ UnderHall source-flow routing changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "ChangeMap(", "SendPacket(",
            "Program.DatabaseManager", "System.Random", "DateTime.Now",
            "Environment.TickCount", "AnchorMob"):
        if forbidden in underhall_source_flow_text:
            print("FAIL: KQ UnderHall source-flow routing invented a side effect",
                  forbidden)
            return 1

    underhall_external_plan_text = UNDERHALL_EXTERNAL_PLAN.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestUnderHallExternalPlanKind",
        "class KingdomQuestUnderHallExternalPlan",
        "class KingdomQuestUnderHallExternalPlanBuilder",
        "SourceUsedOccurrenceCount = 25",
        "SourceDistinctFormCount = 19",
        "KingdomQuestUnderHallSourceCatalog.GetElderine()",
        "KingdomQuestUnderHallSourceCatalog.TryGetMob(",
        "RuntimeHandleIdentifier",
        "source.Arguments[0]",
        "source.Arguments[1]",
        "KingdomQuestUnderHallCommandKind.WaitInterrupt",
        "return false;",
        "RawNumeric1",
        "RawNumeric2",
    ):
        if token not in underhall_external_plan_text:
            print("FAIL: KQ UnderHall external source plan changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "ChangeMap(", "SendPacket(",
            "Program.DatabaseManager", "System.Random", "DateTime.Now",
            "Environment.TickCount"):
        if forbidden in underhall_external_plan_text:
            print("FAIL: KQ UnderHall external plan invented a side effect",
                  forbidden)
            return 1

    underhall_state_text = UNDERHALL_COMMAND_STATE.read_text(
        encoding="utf-8")
    for token in (
        "interface IKingdomQuestUnderHallExternalCommandSink",
        "class KingdomQuestUnderHallCommandState",
        "IKingdomQuestUnderHallCommandSink",
        "KingdomQuestUnderHallExternalPlanBuilder.TryBuild(",
        "KingdomQuestUnderHallSourceFlow.TryResolve(",
        "KingdomQuestUnderHallExternalSourceSite sourceSite",
        "private bool TryStepExternal(",
        "externalSink.TryBroadcast(",
        "externalSink.TryLinkTo(",
        "externalSink.TryMobRegen(",
        "TryResolveRuntimeHandleToken(",
        "plan.RuntimeHandleIdentifier",
        "variables.TryFind(plan.RuntimeHandleIdentifier, out token)",
        "externalSink.TryQuestMobKill(",
        "externalSink.TryReward(",
        "externalSink.TrySummonMob(",
        "waitlogin and waitinterrupt are intentionally absent",
        "55/55 waitinterrupt sites",
        "performs no interrupt delivery",
    ):
        if token not in underhall_state_text:
            print("FAIL: KQ UnderHall external command state changed", token)
            return 1
    for forbidden in (
            "KingdomQuestUnderHallInterruptDelivery",
            "IKingdomQuestUnderHallInterruptDeliverySource",
            "KingdomQuestUnderHallCommandKind.WaitInterrupt",
            "TryStepWaitInterrupt(",
            "ContainsReference(",
            "document.Blocks",
            "MapManager.Instance", "ChangeMap(", "SendPacket(",
            "Program.DatabaseManager", "System.Random", "DateTime.Now",
            "Environment.TickCount"):
        if forbidden in underhall_state_text:
            print("FAIL: KQ UnderHall state duplicated shared/runtime semantics",
                  forbidden)
            return 1

    underhall_event_plan_text = UNDERHALL_EVENT_PREDICATE_PLAN.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestUnderHallEventPredicateKind",
        "class KingdomQuestUnderHallEventPredicatePlan",
        "class KingdomQuestUnderHallEventPredicatePlanBuilder",
        "HPLowCount = 5",
        "PlayerEliminateCount = 19",
        '"KQ_BossRobo"',
        "case 800:",
        '"Summon1"',
        "case 100:",
        '"Summon5"',
        "mob.MobId != 1068",
        "KingdomQuestUnderHallSourceFlow.FailureBlock",
        "RawThreshold deliberately preserves",
    ):
        if token not in underhall_event_plan_text:
            print("FAIL: KQ UnderHall event predicate plan changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager",
            "System.Random", "DateTime.Now", "Environment.TickCount"):
        if forbidden in underhall_event_plan_text:
            print("FAIL: KQ UnderHall event plan invented gameplay semantics",
                  forbidden)
            return 1

    underhall_candidate_text = UNDERHALL_INTERRUPT_CANDIDATE.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestUnderHallInterruptCandidateResult",
        "interface IKingdomQuestUnderHallEventPredicateSource",
        "class KingdomQuestUnderHallInterruptCandidate",
        "HPLowCount = 5",
        "PlayerEliminateCount = 19",
        "SecondIntervalCount = 18",
        "TimeOutCount = 19",
        "KingdomQuestUnderHallTimedInterruptDue.Evaluate(",
        "KingdomQuestUnderHallEventPredicatePlanBuilder.TryBuild(",
        "eventSource.TryEvaluateHPLow(",
        "eventSource.TryEvaluatePlayerEliminate(",
        "KingdomQuestUnderHallEventPredicatePlan eventPlan",
        "does not iterate/select manager entries",
        "choose BlastCheck order",
    ):
        if token not in underhall_candidate_text:
            print("FAIL: KQ UnderHall interrupt-candidate boundary changed", token)
            return 1
    for forbidden in (
            "RemoveAt(", ".Clear(", "AdvanceIntervalDeadline(",
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager",
            "System.Random", "DateTime.Now", "Environment.TickCount"):
        if forbidden in underhall_candidate_text:
            print("FAIL: KQ UnderHall interrupt candidate invented mutation/order",
                  forbidden)
            return 1

    underhall_timed_due_text = UNDERHALL_TIMED_INTERRUPT_DUE.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestUnderHallTimedInterruptEvaluation",
        "class KingdomQuestUnderHallTimedInterruptDue",
        "UnderHallSecondIntervalCount = 18",
        "UnderHallTimeOutCount = 19",
        "UnderHallTimedInterruptCount =",
        "KingdomQuestPineInterruptKind.SecondInterval",
        "plan.InitialIntervalDeadlineTick.HasValue",
        "KingdomQuestPineInterruptPlan.IsIntervalDue(",
        "KingdomQuestPineInterruptKind.TimeOut",
        "timeLimit == null || !timeLimit.NativeActive",
        "KingdomQuestPineInterruptPlan.IsTimeLimitExpired(",
        "does NOT select one candidate",
        "decrement RepeatCount",
        "decide BlastCheck iteration/order",
    ):
        if token not in underhall_timed_due_text:
            print("FAIL: KQ UnderHall timed-interrupt evaluator changed", token)
            return 1
    for forbidden in (
            "RemoveAt(", ".Clear(", "AdvanceIntervalDeadline(",
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager",
            "System.Random", "DateTime.Now", "Environment.TickCount"):
        if forbidden in underhall_timed_due_text:
            print("FAIL: KQ UnderHall timed evaluator invented mutation/order",
                  forbidden)
            return 1

    pine_top_blocks = {}
    current_pine_key = None
    pine_depth = 0
    for raw_line in pine_bundle.splitlines():
        line = raw_line.strip()
        if not line:
            continue
        if line.startswith("@@ "):
            if current_pine_key is not None and pine_depth != 0:
                print("FAIL: KQ Pine canonical block depth did not return to zero",
                      current_pine_key, pine_depth)
                return 1
            current_pine_key = line[3:]
            pine_top_blocks[current_pine_key] = set()
            pine_depth = 0
            continue

        lower = line.lower()
        if lower.startswith("open [") and line.endswith("]"):
            if current_pine_key is None:
                print("FAIL: KQ Pine named block appeared before script marker")
                return 1
            if pine_depth == 0:
                pine_top_blocks[current_pine_key].add(
                    line[line.index("[") + 1:-1])
            pine_depth += 1
            continue

        if lower in ("open", "then open", "else open"):
            pine_depth += 1
            continue

        if lower == "close":
            pine_depth -= 1
            if pine_depth < 0:
                print("FAIL: KQ Pine canonical block depth became negative",
                      current_pine_key)
                return 1

    if current_pine_key is not None and pine_depth != 0:
        print("FAIL: final KQ Pine canonical block depth did not return to zero",
              current_pine_key, pine_depth)
        return 1

    common_pine_top_blocks = set.intersection(
        *(set(pine_top_blocks[key]) for key in sorted(PINE_SCRIPT_KEYS)))
    print("INFO: KQ Pine common top-level blocks:",
          ", ".join(sorted(common_pine_top_blocks)) or "<none>")
    print("INFO: KQ Pine scripts with top-level main:",
          sum("main" in pine_top_blocks[key] for key in PINE_SCRIPT_KEYS),
          "of", len(PINE_SCRIPT_KEYS))

    underhall_interruptset_forms = sorted(set(
        pine_command_lines_by_script["KQ/UnderHall"].get(
            "interruptset", [])))
    if not underhall_interruptset_forms:
        print("FAIL: KQ UnderHall interruptset source disappeared")
        return 1

    underhall_interrupt_action_blocks = []
    for line in underhall_interruptset_forms:
        action_match = re.search(r'"([^"]*)"\.\s*$', line)
        name_match = re.match(
            r'^interruptset\s+\S+\s+"([^"]*)"\s+', line)
        if action_match is None:
            print("FAIL: UnderHall interruptset action block shape changed", line)
            return 1
        if name_match is None or name_match.group(1) != "":
            print("FAIL: UnderHall interrupt name is no longer empty", line)
            return 1
        underhall_interrupt_action_blocks.append(action_match.group(1))

    if any(
            block not in pine_top_blocks["KQ/UnderHall"]
            for block in underhall_interrupt_action_blocks):
        print("FAIL: UnderHall interrupt action block lost top-level target",
              sorted(set(underhall_interrupt_action_blocks)),
              sorted(pine_top_blocks["KQ/UnderHall"]))
        return 1
    print("PASS: all UnderHall interrupt ActionBlocks resolve to top-level blocks:",
          ", ".join(sorted(set(underhall_interrupt_action_blocks))))
    print("INFO: KQ UnderHall interruptset forms:",
          " || ".join(underhall_interruptset_forms))

    pine_init_pairs = set()
    pine_init_non_top_level = set()
    for line in data_rows(KQ_SQL):
        fields = split_row_fields(line)
        if len(fields) != 36:
            print("FAIL: KingdomQuest source row width changed while checking Pine init")
            return 1
        script_key = unquote_sql(fields[31])
        if script_key not in PINE_SCRIPT_KEYS:
            continue
        init_value = unquote_sql(fields[32])
        pine_init_pairs.add((script_key, init_value))
        if (script_key not in pine_top_blocks or
                init_value not in pine_top_blocks[script_key]):
            pine_init_non_top_level.add((script_key, init_value))

    if len(pine_init_pairs) != 9:
        print("FAIL: KQ Pine ScriptInitValue pair count changed",
              sorted(pine_init_pairs))
        return 1

    if ("KQ/UnderHall", "10") not in pine_init_non_top_level:
        print("FAIL: KQ Pine ScriptInitValue/top-level counterexample changed")
        return 1

    print("PASS: KQ Pine ScriptInitValue is not treated as a top-level block key; "
          "locked non-top-level pairs:",
          ", ".join(
              f"{script}={init}"
              for script, init in sorted(pine_init_non_top_level)))

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
        "TryResolveIdentifier(",
        "TryCalculateExpression(",
        "TryStepCommand(",
        "StateVarDeclear::sa_Step (0x004DA040)",
        "StateAssignment::sa_Step (0x004DB110)",
        "variables.TryPush(",
        "variables.TryFind(",
        "frame.State = 1",
        "stack[match].State = NativeBreakExitIndex",
        "Native Pine ProcessStack frame capacity exceeded.",
        "This class deliberately executes no Fiesta gameplay command.",
    )
    for token in pine_control_tokens:
        if token not in pine_control_text:
            print("FAIL: KQ Pine native control runtime changed", token)
            return 1

    pine_variable_text = PINE_VARIABLE_STACK.read_text(encoding="utf-8")
    pine_variable_tokens = (
        "class KingdomQuestPineVariableStack",
        "class KingdomQuestPineVariableStackEntry",
        "class KingdomQuestPineTokenValue",
        "NativeTokenBytes = 0x100",
        "NativeEntryStrideBytes = 0x200",
        "NativeCountOffset = 0x10000",
        "NativeCapacity = 0x7F",
        "vs_FindVariable 0x004D69F0",
        "vs_Push 0x004D6A90",
        "for (int i = entries.Count - 1; i >= 0; i--)",
        "entries.Count >= NativeCapacity",
        "entry.NameToken.Text",
        "valueToken = entry.ValueToken",
    )
    for token in pine_variable_tokens:
        if token not in pine_variable_text:
            print("FAIL: KQ Pine native VariableStack changed", token)
            return 1

    pine_expression_text = PINE_BASIC_EXPRESSION.read_text(
        encoding="utf-8")
    pine_expression_tokens = (
        "class KingdomQuestPineBasicExpression",
        "enum KingdomQuestPineExpressionResolution",
        "String::sa_Load                         0x004DB330",
        "PineScriptToken::pst_RemoveQuatator 0x004D6310",
        "Number::sa_Calculate / String::sa_Calculate 0x004D6710",
        "source.Substring(1, source.Length - 2)",
        "Identify::sa_Calculate                    0x004D6650",
        "PineScriptToken::pst_GetNumber           0x004D6360",
        "PineScriptToken::operator+               0x004D7390",
        "PineScriptToken::operator-               0x004D74B0",
        "MergeNativeNumberSuffix(",
        "GetNativeNumberSuffix(",
        "result.ToString(CultureInfo.InvariantCulture)",
        "KingdomQuestPineRemoveFirst.TryCalculateUsed(",
        "Other system functions, dynamic",
    )
    for token in pine_expression_tokens:
        if token not in pine_expression_text:
            print("FAIL: KQ Pine basic expression runtime changed", token)
            return 1

    for token in (
        "KingdomQuestPineBasicExpression.TryCalculate(",
        "KingdomQuestPineBasicExpression.TrySimpleIdentifier(",
        "return host.TryCalculateExpression(",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine control/basic-expression bridge changed",
                  token)
            return 1

    pine_remove_first_text = PINE_REMOVE_FIRST.read_text(
        encoding="utf-8")
    pine_remove_first_tokens = (
        "class KingdomQuestPineRemoveFirst",
        "UsedCallCount = 7",
        "SysFuncShineRemoveFisrt::sfb_Calculate",
        "0x004E4B70",
        "variables.TryFind(variableName, out source)",
        "sourceBytes[sourceIndex] == delimiter",
        "source.TrySetAscii(",
        "resultBytes[resultIndex] = 0",
        "while (sourceIndex < sourceBytes.Length &&",
        "sourceBytes[sourceIndex] == delimiter)",
        "KingdomQuestPineRemoveFirst.TryCalculateUsed(",
    )
    for token in pine_remove_first_tokens:
        if token not in pine_remove_first_text and \
                token not in pine_expression_text:
            print("FAIL: KQ Pine RemoveFirst runtime changed", token)
            return 1

    pine_random_text = PINE_RANDOM_EXPRESSION.read_text(encoding="utf-8")
    pine_random_tokens = (
        "class KingdomQuestPineRandomExpression",
        "UsedCallCount = 1",
        "UsedMinimum = 0",
        "UsedMaximum = 99",
        "SysFuncRand::sfb_Calculate",
        "0x004D77D0",
        "0x006594B2",
        'const string prefix = "@Random("',
        "MsvcCrtRand random",
        "int nativeRand = random.Next()",
        "minimum + (nativeRand % width)",
        "int width = maximum - minimum + 1",
        "minimum + (nativeRand % width)",
        "does not seed",
    )
    for token in pine_random_tokens:
        if token not in pine_random_text:
            print("FAIL: KQ Pine @Random projection changed", token)
            return 1

    pine_distance_text = PINE_DISTANCE_EXPRESSION.read_text(
        encoding="utf-8")
    pine_distance_tokens = (
        "interface IKingdomQuestPineNativeObjectCoordinateResolver",
        "class KingdomQuestPineNativeDistance",
        "class KingdomQuestPineDistanceExpression",
        "UsedCallCount = 2",
        "SysFuncShineDistance::sfb_Calculate",
        "0x004E5F40",
        "0x0054FD10",
        "DirectDistanceTable::ddt_Distance",
        "0x004012D0",
        "DirectDistanceTable::ddt_Initialize",
        "0x0045FC80",
        "NativeCoordinateLimit = 0x400",
        "x = HalfTowardZero(x)",
        "y = HalfTowardZero(y)",
        "scale += scale",
        "(int)Math.Sqrt((double)squared)",
        "(value + 1) >> 1",
        'const string prefix = "@DistanceBetween("',
        "variables.TryFind(leftIdentifier, out leftValue)",
        "GetNativeNumberSuffix(",
        "objectResolver.TryResolveObject(",
        "unchecked(leftX - rightX)",
        "unchecked(leftY - rightY)",
    )
    for token in pine_distance_tokens:
        if token not in pine_distance_text:
            print("FAIL: KQ Pine @DistanceBetween projection changed", token)
            return 1
    for forbidden in (".MapObjectID", "MapManager.Instance", ".GetMap(", ".Objects["):
        if forbidden in pine_distance_text:
            print("FAIL: KQ Pine native object handle was conflated with emulator map identity",
                  forbidden)
            return 1

    pine_condition_text = PINE_CONDITION_EXPRESSION.read_text(
        encoding="utf-8")
    pine_condition_tokens = (
        "enum KingdomQuestPineConditionOperator",
        "class KingdomQuestPineConditionExpression",
        "UsedConditionCount = 26",
        "UsedTokenEqualCount = 11",
        "UsedNumericLessCount = 5",
        "UsedNumericEqualCount = 4",
        "UsedNumericGreaterCount = 3",
        "UsedTokenNotEqualCount = 3",
        "CompareOperator::sa_Load                    0x004DB1B0",
        "CompareOperator::co_Equal                  0x004DAA50",
        "CompareOperator::co_Excremation            0x004DABB0",
        "CompareOperator::co_NotEqual               0x004DAC50",
        "Condition::sa_Calculate                    0x004D87A0",
        'case "===":',
        'case "=!=":',
        "NumericEqual = 1",
        "NumericNotEqual = 2",
        "NumericLess = 3",
        "NumericGreater = 4",
        "NumericLessOrEqual = 5",
        "NumericGreaterOrEqual = 6",
        "TokenEqual = 7",
        "TokenNotEqual = 8",
        "GetNativeNumberSuffix(",
        "StringComparison.Ordinal",
    )
    for token in pine_condition_tokens:
        if token not in pine_condition_text:
            print("FAIL: KQ Pine native condition projection changed", token)
            return 1

    for token in (
        "KingdomQuestPineConditionExpression.TryParse(",
        "condition.LeftExpression",
        "condition.RightExpression",
        "KingdomQuestPineConditionExpression.TryEvaluate(",
        "host.TryEvaluateCondition(node.Text, out value)",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine condition/control bridge changed", token)
            return 1

    pine_char_name_text = PINE_CHAR_NAME_EXPRESSION.read_text(
        encoding="utf-8")
    pine_char_name_tokens = (
        "interface IKingdomQuestPineNativeObjectNameResolver",
        "class KingdomQuestPineCharNameExpression",
        "UsedCallCount = 10",
        "UsedPlayerHandleCallCount = 5",
        "UsedLooterHandleCallCount = 5",
        "NativeNamePayloadBytes = 20",
        "NativeNameBufferBytes = 0x15",
        "SysFuncShineCharName::sfb_Calculate",
        "0x004E45A0",
        "0x0054FD10",
        "ShineObject::so_CharName",
        "offset +0x56C",
        "ushort nativeHandle = unchecked((ushort)nativeNumber)",
        "objectResolver.TryResolveCharName(",
        "destination.TrySetAscii(string.Empty)",
        "encoded.Length > NativeNamePayloadBytes",
        'const string prefix = "@CharName("',
    )
    for token in pine_char_name_tokens:
        if token not in pine_char_name_text:
            print("FAIL: KQ Pine @CharName projection changed", token)
            return 1
    for forbidden in (".MapObjectID", "MapManager.Instance", ".Character.Name"):
        if forbidden in pine_char_name_text:
            print("FAIL: KQ Pine native char-name handle was conflated with emulator identity",
                  forbidden)
            return 1

    pine_used_expression_text = PINE_USED_EXPRESSION_RUNTIME.read_text(
        encoding="utf-8")
    pine_used_expression_tokens = (
        "class KingdomQuestPineUsedExpressionContext",
        "class KingdomQuestPineUsedExpressionRuntime",
        "MsvcCrtRand Random",
        "IKingdomQuestPineNativeObjectCoordinateResolver CoordinateResolver",
        "IKingdomQuestPineNativeObjectNameResolver NameResolver",
        "KingdomQuestPineRandomExpression.TryParseUsedExpression(",
        "context == null || context.Random == null",
        "KingdomQuestPineDistanceExpression.TryParseUsedExpression(",
        "context == null || context.CoordinateResolver == null",
        "KingdomQuestPineCharNameExpression.TryParseUsedExpression(",
        "context == null || context.NameResolver == null",
        "return KingdomQuestPineExpressionResolution.Unsupported",
        "silently falling through to a generic host implementation",
    )
    for token in pine_used_expression_tokens:
        if token not in pine_used_expression_text:
            print("FAIL: KQ Pine used-expression dispatcher changed", token)
            return 1
    for forbidden in (
            "new MsvcCrtRand(", "System.Random", "MapManager.Instance",
            ".MapObjectID"):
        if forbidden in pine_used_expression_text:
            print("FAIL: KQ Pine used-expression dispatcher invented a native dependency",
                  forbidden)
            return 1

    for token in (
        "KingdomQuestPineUsedExpressionContext expressionContext",
        "KingdomQuestPineUsedExpressionRuntime.TryCalculate(",
        "expressionContext,",
        "return host.TryCalculateExpression(",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine used-expression/control bridge changed", token)
            return 1

    pine_waitlogin_text = PINE_WAITLOGIN.read_text(encoding="utf-8")
    for token in (
        "interface IKingdomQuestPineWaitLoginSource",
        "class KingdomQuestPineWaitLogin",
        "UsedCommandCount = 9",
        "TryParseUsed(",
        "KingdomQuestPineBasicExpression.TrySimpleIdentifier(",
        "No player-count meaning, login predicate or polling cadence is invented.",
    ):
        if token not in pine_waitlogin_text:
            print("FAIL: KQ Pine waitlogin handoff changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "ClientManager.Instance", "GetCharacters",
            "SendPacket(", "Program.DatabaseManager", "DateTime.Now",
            "Environment.TickCount"):
        if forbidden in pine_waitlogin_text:
            print("FAIL: KQ Pine waitlogin handoff invented login semantics",
                  forbidden)
            return 1

    for token in (
        'IsCommandVerb(node.Text, "waitlogin")',
        "private void StepWaitLogin(",
        "KingdomQuestPineWaitLogin.TryParseUsed(",
        "variables.TryFind(targetIdentifier, out destination)",
        "commandContext.WaitLoginSource == null",
        "commandContext.WaitLoginSource.TryStep(",
        "ref state",
        "frame.State = state",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine waitlogin/control bridge changed", token)
            return 1

    pine_waitinterrupt_text = PINE_WAITINTERRUPT.read_text(encoding="utf-8")
    for token in (
        "class KingdomQuestPineWaitInterruptDelivery",
        "interface IKingdomQuestPineWaitInterruptSource",
        "class KingdomQuestPineWaitInterrupt",
        "UsedCommandCount = 55",
        'UsedBlockVariable = "InterruptBlock"',
        'UsedArgumentVariable = "InterruptArg"',
        "KingdomQuestPineInterruptPlan.TryParseWaitInterrupt(",
        "The producer owns BlastCheck ordering",
    ):
        if token not in pine_waitinterrupt_text:
            print("FAIL: KQ Pine waitinterrupt handoff changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "ClientManager.Instance", "GetCharacters",
            "SendPacket(", "Program.DatabaseManager", "System.Random",
            "DateTime.Now", "Environment.TickCount"):
        if forbidden in pine_waitinterrupt_text:
            print("FAIL: KQ Pine waitinterrupt handoff invented event semantics",
                  forbidden)
            return 1

    for token in (
        'IsCommandVerb(node.Text, "waitinterrupt")',
        "private void StepWaitInterrupt(",
        "KingdomQuestPineWaitInterrupt.TryParseUsed(",
        "commandContext.WaitInterruptSource == null",
        "commandContext.InterruptRegistry == null",
        "commandContext.WaitInterruptSource.TryTake(",
        "document.Blocks.TryGetValue(",
        "variables.TryFind(blockIdentifier, out blockValue)",
        "variables.TryFind(argumentIdentifier, out argumentValue)",
        "delivery.SelectedPlan.ActionBlock",
        "TryUsedIdentifierCall(node.Text, out identifier)",
        "StepIdentifierCall(frame, node, identifier)",
        'identifier, "InterruptBlock", StringComparison.Ordinal',
        "Dynamic Pine CALL target not found",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine waitinterrupt/control bridge changed", token)
            return 1

    pine_interrupt_runtime_text = PINE_INTERRUPT_RUNTIME.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestPineInterruptRuntimeEntry",
        "RemainingRepeatCount",
        "NextIntervalDeadlineTick",
        "HPLowObjectHandle",
        "HPLowThresholdPermille",
        "CachedHPLowMaxHp",
        "interface IKingdomQuestUnderHallNativeInterruptView",
        "interface IKingdomQuestPineNativeObjectHealthResolver",
        "interface IKingdomQuestUnderHallQualifyingPlayerCountSource",
        "class KingdomQuestUnderHallNativeInterruptView",
        "objectHealthResolver.TryResolveObjectHealth(",
        "qualifyingPlayerCountSource.TryGetQualifyingPlayerCount(",
        "class KingdomQuestUnderHallNativeInterruptDeliverySource",
        "NativeHpScale = 1000u",
        "KingdomQuestPineInterruptPlan.IsIntervalDue(",
        "KingdomQuestPineInterruptPlan.AdvanceIntervalDeadline(",
        "KingdomQuestPineInterruptPlan.IsTimeLimitExpired(",
        "activeInterrupts.RuntimeEntries",
        "entry.RemainingRepeatCount--",
        "activeInterrupts.RemoveRuntimeEntry(entry)",
        "entry.CachedHPLowMaxHp = maximumHp",
        "due = playerCount == 0",
        "if (!exists)",
        "string.Empty",
    ):
        if token not in pine_interrupt_runtime_text:
            print("FAIL: native KQ Pine interrupt runtime changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "ClientManager.Instance", "Program.Randomizer",
            "System.Random", "DateTime.Now", "Environment.TickCount",
            "Program.DatabaseManager", "SendPacket(", "MapObjectID"):
        if forbidden in pine_interrupt_runtime_text:
            print("FAIL: native KQ Pine interrupt runtime invented an owner",
                  forbidden)
            return 1

    if "commandContext.InterruptRegistry.ContainsReference(" in pine_control_text:
        print("FAIL: waitinterrupt incorrectly requires fired entry to remain registered")
        return 1
    for token in (
        "KingdomQuestPineUsedCommandRuntime.TryStep(",
        "commandContext,",
        "variables,",
    ):
        if token not in pine_control_text:
            print("FAIL: Pine command runtime lost VariableStack registration context",
                  token)
            return 1

    pine_scriptfile_text = PINE_SCRIPTFILE.read_text(encoding="utf-8")
    for token in (
        "class KingdomQuestPineScriptFileSource",
        "class KingdomQuestPineScriptFilePlan",
        "class KingdomQuestPineScriptFile",
        "UsedCommandCount = 19",
        "UsedClearCount = 5",
        "UsedSourceCount = 9",
        "NativeKqScriptManagerCapacity = 64",
        "NativeStepAddress = 0x004EBDE0u",
        "NativeManagerLookupAddress = 0x0064CE70u",
        "NativeProcessStackScriptOffset = 0x10130u",
        '"KQGordonMaster"',
        '"KQHoneying"',
        '"KQHBat1"',
        '"KQHBat2"',
        '"KQHBat3"',
        '"KQHBat4"',
        '"KQHBat5"',
        '"KQUnderHall"',
        '"KQUnderHall2"',
        "ClearsCurrentScript = clearsCurrentScript",
        "Sources.TryGetValue(key, out source)",
    ):
        if token not in pine_scriptfile_text:
            print("FAIL: native KQ Pine scriptfile source/runtime changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager",
            "System.Random", "DateTime.Now", "Environment.TickCount"):
        if forbidden in pine_scriptfile_text:
            print("FAIL: native KQ Pine scriptfile model invented side effects",
                  forbidden)
            return 1

    pine_used_command_text = PINE_USED_COMMAND_RUNTIME.read_text(
        encoding="utf-8")
    pine_used_command_tokens = (
        "enum KingdomQuestPineCommandResolution",
        "interface IKingdomQuestPineNativeTickSource",
        "interface IKingdomQuestPineRegenDocumentResolver",
        "interface IKingdomQuestPineUsedCommandSink",
        "class KingdomQuestPineUsedCommandContext",
        "IKingdomQuestPineWaitLoginSource WaitLoginSource",
        "IKingdomQuestPineWaitInterruptSource WaitInterruptSource",
        "KingdomQuestPineInterruptRegistryState InterruptRegistry",
        "IKingdomQuestUnderHallCommandSink UnderHallSink",
        "class KingdomQuestPineUsedCommandRuntime",
        "UsedOneStepCommandCount = 608",
        "SourceUsedUnrecoveredVerbCount = 31",
        "IsSourceUsedUnrecoveredVerb(verb)",
        "? KingdomQuestPineCommandResolution.Invalid",
        'case "timelimit":',
        'case "interruptset":',
        'case "interrupterase":',
        'case "interruptclear":',
        'case "regengroup":',
        'case "scriptfile":',
        'case "questresult":',
        'case "endofkq":',
        "KingdomQuestPineTimingPlan.TryBuildTimeLimit(",
        "KingdomQuestPineInterruptPlan.TryParseUsedSet(",
        "KingdomQuestPineInterruptPlan.TryParseErase(",
        "KingdomQuestPineInterruptPlan.IsInterruptClear(",
        "KingdomQuestPineRegenGroupResolver.TryParseUsedCommand(",
        "KingdomQuestPineScriptFile.TryParseUsed(",
        "context.Sink.TrySetScriptFile(plan)",
        "KingdomQuestPineKqTerminalPlan.TryParseQuestResult(",
        "KingdomQuestPineKqTerminalPlan.TryParseEndOfKq(",
        "pause (202) and waitinterrupt (55) are deliberately excluded",
    )
    for token in pine_used_command_tokens:
        if token not in pine_used_command_text:
            print("FAIL: KQ Pine one-step command dispatcher changed", token)
            return 1
    for forbidden in (
            "DateTime.Now", "Environment.TickCount", "System.Random",
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager"):
        if forbidden in pine_used_command_text:
            print("FAIL: KQ Pine one-step command dispatcher invented a side effect",
                  forbidden)
            return 1

    unrecovered_pine_verbs = {
        "abstatereset", "abstateset", "battlestart", "battlestop",
        "broadcast", "chatwin", "doorbuild", "doorclose", "dooropen",
        "effectobj", "exchange2mob", "invensearch", "invidualreward",
        "itemdrop", "itemerase", "itemowner", "linkto", "mobattr",
        "mobregen", "npcchat", "npcshout", "npcstand", "questmobkill",
        "revival", "reward", "sendquestresult", "suicide",
        "summonmob", "teleport", "vanish",
        "whoclickme",
    }
    if len(unrecovered_pine_verbs) != 31:
        print("FAIL: internal unrecovered Pine verb audit count changed")
        return 1
    missing_unrecovered = [
        verb for verb in sorted(unrecovered_pine_verbs)
        if ('case "' + verb + '":') not in pine_used_command_text
    ]
    if missing_unrecovered:
        print("FAIL: source-used unrecovered Pine verb lost fail-closed guard",
              missing_unrecovered)
        return 1

    for token in (
        "KingdomQuestPineUsedCommandContext commandContext",
        "KingdomQuestUnderHallCommandRuntime.TryStep(",
        "variables,",
        "commandContext.UnderHallSink",
        "TryUsedIdentifierCall(node.Text, out identifier)",
        "StepIdentifierCall(frame, node, identifier)",
        'identifier, "InterruptBlock", StringComparison.Ordinal',
        "variables.TryFind(identifier, out value)",
        "document.Blocks.TryGetValue(value.Text, out block)",
        "Dynamic Pine CALL target not found",
        "Exact UnderHall Pine command dependency failed",
        "KingdomQuestPineUsedCommandRuntime.TryStep(",
        "KingdomQuestPineCommandResolution.Success",
        "KingdomQuestPineCommandResolution.Invalid",
        "Source-proven Pine command dependency failed",
        "host.TryStepCommand(",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine one-step/control bridge changed", token)
            return 1

    underhall_command_source_text = UNDERHALL_COMMAND_SOURCE.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestUnderHallCommandKind : byte",
        "class KingdomQuestUnderHallCommandSourcePlan",
        "class KingdomQuestUnderHallCommandSource",
        "SourceUsedFamilyCount = 9",
        "BroadcastFormCount = 4",
        "SummonDistinctFormCount = 11",
        '"KQReturn5"',
        '"KQReturn10"',
        '"KQReturn20"',
        '"KQReturn30"',
        '"Eld", "Eld", "17214", "13445"',
        '"KQ_BossRobo", "KQ_BossRobo"',
        '"2668", "Daliy_Check", "1"',
        '"KingdomQuest"',
        '"KQUnderHall"',
        '"InterruptBlock", "InterruptArg"',
        '"Wait"',
        '"KQ_SkelWarrior"',
        "count != 3 && count != 5",
        "SummonCounts.TryGetValue(",
    ):
        compact = underhall_command_source_text.replace("\n", " ")
        if token not in underhall_command_source_text and token not in compact:
            print("FAIL: UnderHall KQ Pine exact source-form parser changed",
                  token)
            return 1
    for forbidden in (
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager",
            "new Item(", "Inventory.", "ChangeMap(", "GiveExp("):
        if forbidden in underhall_command_source_text:
            print("FAIL: UnderHall KQ Pine source parser invented side effects",
                  forbidden)
            return 1

    pine_local_command_text = PINE_LOCAL_COMMAND_STATE.read_text(
        encoding="utf-8")
    pine_local_command_tokens = (
        "interface IKingdomQuestPineExternalCommandSink",
        "class KingdomQuestPineLocalCommandState",
        "CreateUnderHallCommandState(",
        "new KingdomQuestUnderHallCommandState(externalSink)",
        "IKingdomQuestPineUsedCommandSink",
        "KingdomQuestPineInterruptRegistryState interrupts",
        "KingdomQuestPineTimeLimitPlan timeLimit",
        "timeLimit = plan",
        "interrupts.TryRegister(plan)",
        "interrupts.Erase(nativeName16)",
        "interrupts.Clear()",
        "KingdomQuestPineRegenRuntimePlanBuilder.TryBuild(",
        "DataProvider.Instance",
        "externalSink.TryRunRegenGroup(runtimePlan)",
        "externalSink.TryApplyQuestResult(plan)",
        "externalSink.TryEndKingdomQuest(plan)",
        "Zero matches still means the command itself ran.",
    )
    for token in pine_local_command_tokens:
        if token not in pine_local_command_text:
            print("FAIL: KQ Pine local command state changed", token)
            return 1
    for forbidden in (
            "DateTime.Now", "Environment.TickCount", "System.Random",
            "MapManager.Instance", "SendPacket(", "Program.DatabaseManager"):
        if forbidden in pine_local_command_text:
            print("FAIL: KQ Pine local command state invented an external side effect",
                  forbidden)
            return 1

    for token in (
        "public bool HasPauseDeadline;",
        "public uint PauseDeadlineTick;",
        'IsCommandVerb(node.Text, "pause")',
        "private void StepPause(",
        "KingdomQuestPineTimingPlan.TryBuildPause(",
        "frame.State = 1",
        "frame.HasPauseDeadline = true",
        "frame.PauseDeadlineTick = plan.DeadlineTick",
        "frame.PauseDeadlineTick < currentTick",
        "Native Pine pause tick dependency missing",
    ):
        if token not in pine_control_text:
            print("FAIL: KQ Pine pause/control runtime changed", token)
            return 1

    scenario_start_text = SCENARIO_START_PLAN.read_text(encoding="utf-8")
    lua_scenario_scope_text = LUA_SCENARIO_SCOPE.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestLuaScenarioSource",
        "class KingdomQuestLuaScenarioScope",
        "UsedLuaScriptLanguageCount = 18",
        "interface IKingdomQuestLuaScenarioHost",
        "this interface does not substitute the community scripting API",
        "public static bool TryGet(",
        "bool TryStart(",
        "KingdomQuestLuaScenarioSource source",
        "string scriptInitValue",
    ):
        if token not in lua_scenario_scope_text:
            print("FAIL: KQ Lua source/runtime scope changed", token)
            return 1

    expected_used_lua_keys = {
        key for key in scripts
        if key in scenario_by_key and
        scenario_by_key[key]["backend"] == "lua"
    }
    if len(expected_used_lua_keys) != 18:
        print("FAIL: KQ used Lua key source set changed",
              sorted(expected_used_lua_keys))
        return 1
    for key in sorted(expected_used_lua_keys):
        row = scenario_by_key[key]
        for token in (
                key,
                row["archive_path"],
                row["sha256"]):
            if token not in lua_scenario_scope_text:
                print("FAIL: KQ used Lua source identity missing", key, token)
                return 1
    for forbidden in (
            "MoonSharp", "NLua", "KeraLua", "lua_register",
            "cEndOfKingdomQuest", "cMobRegen", "cSetAbstate"):
        if forbidden in lua_scenario_scope_text:
            print("FAIL: KQ Lua source scope invented API/runtime semantics",
                  forbidden)
            return 1

    zone_runtime_text = ZONE_RUNTIME.read_text(encoding="utf-8")
    for token in (
        "enum KingdomQuestScenarioStartAction : byte",
        "class KingdomQuestScenarioStartPlan",
        "DropCurrentFilm = 1",
        "CloseAllDoors = 2",
        "PlayFilm = 3",
        "definition.ScriptLanguage",
        "definition.ScriptInitValue",
        "ContainsSourceBackedScenarioBook(",
        'literal top-level block "main"',
        'VariableStack token "InitFlag"',
        "complete 0x100-byte ScriptInitValue token",
    ):
        if token not in scenario_start_text:
            print("FAIL: KQ native scenario start plan changed", token)
            return 1
    for forbidden in (
            "entryBlock", "TryCreate(", "MapManager.Instance",
            "SendPacket(", "Program.DatabaseManager"):
        if forbidden in scenario_start_text:
            print("FAIL: KQ scenario start plan invented execution", forbidden)
            return 1

    pine_scenario_runtime_text = PINE_SCENARIO_RUNTIME.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestPineScenarioRuntime",
        'NativeEntryBlockName = "main"',
        'NativeInitVariableName = "InitFlag"',
        "NativeInitTokenBytes = 0x100",
        "NativeScriptStepAddress = 0x004D8100u",
        "NativeTheaterPlayFilmAddress = 0x005081F0u",
        "NativeProcessStackReadyAddress = 0x004D6B40u",
        "NativeProcessStackPushVariableAddress = 0x004D6BD0u",
        "NativeVariableStackPushAddress = 0x004D6A90u",
        "document.Blocks.TryGetValue(",
        "NativeEntryBlockName, out main",
        "KingdomQuestPineControlRuntime.TryCreate(",
        "runtime.Variables.TryPush(",
        "NativeInitVariableName, out initValue",
        "initValue.TrySetAscii(startPlan.ScriptInitValue)",
        "class KingdomQuestZonePineFilmSession",
        "class KingdomQuestZonePineFilmBridge",
        "TryCreateStartedSession(",
        "KingdomQuestZoneRuntimeRegistry.TryGet(handle, out state)",
        "state.State != KingdomQuestZoneLifecycleState.Started",
        "state.ScenarioStartPlan.GetNativeOrder()",
        "KingdomQuestScenarioStartAction.DropCurrentFilm",
        "KingdomQuestScenarioStartAction.CloseAllDoors",
        "KingdomQuestScenarioStartAction.PlayFilm",
        "commandContext.CurrentKingdomQuestHandle.Value != handle",
        "KingdomQuestPineScriptSource.TryGet(",
        "KingdomQuestPineScenarioRuntime.TryCreate(",
        "return Runtime.Step();",
    ):
        if token not in pine_scenario_runtime_text:
            print("FAIL: native KQ Pine film entry changed", token)
            return 1
    for forbidden in (
            "ScriptInitValue == NativeEntryBlockName",
            "MapManager.Instance", "SendPacket(",
            "Program.DatabaseManager", "System.Random",
            "DateTime.Now", "Environment.TickCount",
            "KingdomQuestLuaScenarioScope", "new MsvcCrtRand("):
        if forbidden in pine_scenario_runtime_text:
            print("FAIL: native KQ Pine film entry invented runtime semantics",
                  forbidden)
            return 1

    for token in (
        "public KingdomQuestScenarioStartPlan ScenarioStartPlan",
        "KingdomQuestScenarioStartPlan.TryBuild(",
        "scenarioStartPlan, roster",
        "current.ScenarioStartPlan, roster",
        "current.Definition, current.ScenarioStartPlan",
    ):
        if token not in zone_runtime_text:
            print("FAIL: KQ Zone scenario-start lifecycle bridge changed", token)
            return 1

    pine_terminal_text = PINE_KQ_TERMINAL.read_text(encoding="utf-8")
    pine_terminal_tokens = (
        "class KingdomQuestPineKqTerminalPlan",
        "class KingdomQuestPineQuestResultPlan",
        "class KingdomQuestPineQuestResultWire",
        "NativeWireSize = 2",
        "TryCreateBytes(",
        "packet.ToNormalArray()",
        "interface IKingdomQuestPineQuestResultTarget",
        "TryApplyKingdomQuestSuccessTitleHook()",
        "TryApplyKingdomQuestFailTitleHook()",
        "TrySendKingdomQuestResult(byte[] nativeWire)",
        "class KingdomQuestPineQuestResultRuntime",
        "TryExecuteSelectedTarget(",
        "NativeStepAddress = 0x004EF450u",
        "SH22Type.KingdomQuestComplete",
        "SH22Type.KingdomQuestFailed",
        "class KingdomQuestPineEndPlan",
        "NativeHeader = 0x16",
        "NativeCompleteType = 0x12",
        "NativeFailType = 0x13",
        "NativeSuccessTitleCategory = 21",
        "NativeFailTitleCategory = 22",
        "NativeAudienceObjectType =",
        "(byte)KingdomQuestPineNativeObjectType.Player",
        "NativePostResultClearObjectTypeMask =",
        "KingdomQuestPineNativeObjectClear.QuestResultMask",
        "VisitCurrentFieldMapPlayers = 1",
        "ClearResultObjects = 2",
        "GetNativeMapOrder()",
        "NativeNoKingdomQuestHandle = 0xFFFFFFFFu",
        "KingdomQuestPineNativeObjectClear.EndOfKqMask",
        "interface IKingdomQuestPineEndWorldSender",
        "interface IKingdomQuestPineEndFieldMapClearOwner",
        "class KingdomQuestPineEndRuntime",
        "NativeStepAddress = 0x004F5FC0u",
        "worldSender.SendKingdomQuestEnd(plan.Handle)",
        "fieldMapClearOwner.ClearObjects(plan.ClearObjectTypeMask)",
        "Both owners must therefore be present before either",
        "index_suc",
        "NC_KQ_FAIL_CMD branch",
        "WorldManagerSession::wms_EndOfKQPacket(handle)",
        "FieldMap::fm_ClearObject(0xB0)",
        "FieldMap::fm_ClearObject(0x1B0)",
        "so_ObjectType == 2",
        "nothing here sends packets, clears map objects or mutates",
        "does not select players, invoke",
    )
    for token in pine_terminal_tokens:
        if token not in pine_terminal_text:
            print("FAIL: KQ Pine terminal command projection changed", token)
            return 1

    result_success_hook = pine_terminal_text.find(
        "target.TryApplyKingdomQuestSuccessTitleHook()")
    result_fail_hook = pine_terminal_text.find(
        "target.TryApplyKingdomQuestFailTitleHook()")
    result_send = pine_terminal_text.find(
        "target.TrySendKingdomQuestResult(nativeWire)")
    if not (0 <= result_success_hook < result_send and
            0 <= result_fail_hook < result_send):
        print("FAIL: KQ questresult native title-hook/send ordering changed")
        return 1

    end_send = pine_terminal_text.find(
        "worldSender.SendKingdomQuestEnd(plan.Handle)")
    end_clear = pine_terminal_text.find(
        "fieldMapClearOwner.ClearObjects(plan.ClearObjectTypeMask)")
    if not (0 <= end_send < end_clear):
        print("FAIL: KQ endofkq native END/clear ordering changed")
        return 1
    for forbidden in (
            "MapManager.Instance", "MapObjectID", "ZoneCharacter.",
            "Map.Objects", "Map.Broadcast(", "GetCharacters",
            "InterHandler.SendKingdomQuestEnd("):
        if forbidden in pine_terminal_text:
            print("FAIL: KQ terminal runtime bypassed explicit native owner",
                  forbidden)
            return 1

    pine_object_clear_text = PINE_NATIVE_OBJECT_CLEAR.read_text(
        encoding="utf-8")
    for token in (
        "enum KingdomQuestPineNativeObjectType : byte",
        "AxialFlag = 0",
        "DropItem = 1",
        "Player = 2",
        "MiniHouse = 3",
        "Npc = 4",
        "Mob = 5",
        "MagicField = 6",
        "Door = 7",
        "Bandit = 8",
        "EffectObject = 9",
        "Servant = 10",
        "Mover = 11",
        "Pet = 12",
        "FieldMapClearObjectAddress = 0x00495A10u",
        "AxialListObjectClearWorkAddress = 0x00495940u",
        "ObjectTypeVtableOffset = 0x4D0",
        "RetrateFromMapVtableOffset = 0x3F4",
        "EndOfKqMask = 0x000000B0u",
        "QuestResultMask = 0x000001B0u",
        "BuildMask(EndOfKqTypes) == EndOfKqMask",
        "BuildMask(QuestResultTypes) == QuestResultMask",
        "does not translate that into an emulator",
    ):
        if token not in pine_object_clear_text:
            print("FAIL: KQ native object-clear projection changed", token)
            return 1
    for forbidden in (
            "Map.RemoveObject", "MapManager.Instance", "MapObjectID",
            "FullRemoveObject", "Objects.Remove"):
        if forbidden in pine_object_clear_text:
            print("FAIL: KQ native object-clear projection guessed live removal",
                  forbidden)
            return 1

    pine_kq_title_text = PINE_KQ_TITLE_HOOK.read_text(encoding="utf-8")
    for token in (
        "class KingdomQuestPineKqTitleHookNativePlan",
        "CharacterTitleZonePlayerOffset = 0x29638",
        "CommonCategoryEvaluateAddress = 0x005CB5F0u",
        "SuccessWrapperAddress = 0x0055AE10u",
        "SuccessHookAddress = 0x005CBFB0u",
        "SuccessCounterLowOffset = 0x5E8",
        "SuccessCounterHighOffset = 0x5EC",
        "SuccessDirtyOffset = 0x5F0",
        "SuccessCategory = 21u",
        "FailWrapperAddress = 0x0055AE30u",
        "FailHookAddress = 0x005CBFD0u",
        "FailCounterLowOffset = 0x5F8",
        "FailCounterHighOffset = 0x5FC",
        "FailDirtyOffset = 0x600",
        "FailCategory = 22u",
        "NativeCounterIncrement = 1UL",
        "NativeDirtyValue = 1u",
        "does not represent these native 64-bit counters/dirty fields",
    ):
        if token not in pine_kq_title_text:
            print("FAIL: KQ native title-hook projection changed", token)
            return 1
    for forbidden in (
            "AdvanceTitleTier", "MobKillTitleTier", "PvPKillTitleTier",
            "FameCountTitleTier", "TotalTitlesEarned", "GiveExp(",
            "Program.DatabaseManager", "ExecuteQuery("):
        if forbidden in pine_kq_title_text:
            print("FAIL: KQ title-hook projection substituted emulator title state",
                  forbidden)
            return 1

    pine_kq_reward_command_text = PINE_KQ_REWARD_COMMAND.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestPineKqRewardCommandPlan",
        "class KingdomQuestPineKqRewardCommandNative",
        "ShineRewardStepAddress = 0x004EF650u",
        "AxialListKqRewardCtorAddress = 0x00428410u",
        "AxialListKqRewardWorkAddress = 0x00428490u",
        "KqContributeGetMobKillAddress = 0x00499D80u",
        "KqElementProtocolInfoOffset = 0x04",
        "KqElementRewardIndexOffset = 0x97",
        "KqElementDemandMobKillOffset = 0x99",
        "GetCharRegistNumberVtableOffset = 0x344",
        "SendErrorCodeVtableOffset = 0x308",
        "KqRewardStructVtableOffset = 0x76C",
        "NativeNoRewardHeader = 0x16",
        "NativeNoRewardType = 0x23",
        "NativeNoRewardError = 0x1104",
        "NativeNoRewardWireSize = 4",
        "SH22Type.KingdomQuestNoReward",
        "packet.WriteUShort(NativeNoRewardError)",
        "definition.DemandMobKill",
        "definition.RewardIndex",
        "mobKillContribution < plan.DemandMobKill",
        "does not enumerate emulator map objects",
        "does not execute the downstream GameDB reward transaction",
    ):
        if token not in pine_kq_reward_command_text:
            print("FAIL: KQ native reward-command projection changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "Map.Objects", "GetCharacters",
            "ZoneCharacter", "Inventory.", "Program.DatabaseManager",
            "ExecuteQuery(", "GiveExp(", "System.Random"):
        if forbidden in pine_kq_reward_command_text:
            print("FAIL: KQ reward-command projection invented live mutation",
                  forbidden)
            return 1

    pine_regen_text = PINE_REGEN_PLAN.read_text(encoding="utf-8")
    pine_regen_tokens = (
        "class KingdomQuestPineRegenGroupPlan",
        "class KingdomQuestPineRegenGroupResolver",
        "UsedPineCommandCount = 243",
        "UsedUniquePairCount = 225",
        "UsedSourceKeyCount = 5",
        "PineScriptMobRegenerator::psmr_find(sourceKey, groupIndex)",
        "MobHatchery::mh_ScriptBreed",
        "document.Groups",
        "document.Mobs",
        "v.RegenIndex",
        "None of the 243 supplied Pine calls uses those optional operands",
        "stops before MobHatchery spawning/timing",
    )
    for token in pine_regen_tokens:
        if token not in pine_regen_text:
            print("FAIL: KQ Pine regengroup source resolver changed", token)
            return 1

    pine_regen_runtime_text = PINE_REGEN_RUNTIME_PLAN.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestPineRegenResolvedMob",
        "class KingdomQuestPineRegenRuntimePlan",
        "class KingdomQuestPineRegenRuntimePlanBuilder",
        "data.MobsByName.TryGetValue(",
        "data.MobData.TryGetValue(",
        "serverInfo.ID > ushort.MaxValue",
        "clientInfo.ID != unchecked((ushort)serverInfo.ID)",
        "new KingdomQuestPineRegenResolvedMob(",
        "MobNum = source.MobNum",
        "KillNum = source.KillNum",
        "RegStandard = source.RegStandard",
        "RegDelta4 = source.RegDelta4",
        "This class still performs no spawn, scheduling, kill tracking or",
    ):
        if token not in pine_regen_runtime_text:
            print("FAIL: KQ Pine regen runtime identity plan changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "FullAddObject(", "new Mob(",
            "SpawnMob(", "Program.Randomizer", "DateTime.Now",
            "Environment.TickCount", "SendPacket(", "Program.DatabaseManager"):
        if forbidden in pine_regen_runtime_text:
            print("FAIL: KQ Pine regen runtime plan invented live spawn semantics",
                  forbidden)
            return 1

    pine_regen_native_layout_text = PINE_REGEN_NATIVE_LAYOUT.read_text(
        encoding="utf-8")
    for token in (
        "class KingdomQuestPineRegenNativeLayout",
        "source.MobNum",
        "source.KillNum",
        "source.RegStandard",
        "source.RegMin",
        "source.RegMax",
        "source.RegDelta0",
        "source.RegSec0",
        "source.RegDelta1",
        "source.RegSec1",
        "source.RegDelta2",
        "source.RegSec2",
        "source.RegDelta3",
        "source.RegSec3",
        "source.RegDelta4",
        "TimeDistribution = (int[])timeDistribution.Clone()",
        "does not interpret timedist",
    ):
        if token not in pine_regen_native_layout_text:
            print("FAIL: KQ Pine native regen row layout changed", token)
            return 1
    for forbidden in (
            "MapManager.Instance", "new Mob(", "FullAddObject(", "SpawnMob(",
            "Program.Randomizer", "System.Random", "DateTime.Now",
            "Environment.TickCount", "SendPacket(", "Program.DatabaseManager"):
        if forbidden in pine_regen_native_layout_text:
            print("FAIL: KQ Pine native regen row layout invented runtime behavior",
                  forbidden)
            return 1

    pine_timing_text = PINE_TIMING_PLAN.read_text(encoding="utf-8")
    pine_timing_tokens = (
        "class KingdomQuestPineTimingPlan",
        "class KingdomQuestPinePausePlan",
        "class KingdomQuestPineTimeLimitPlan",
        "class KingdomQuestPineNativeTime",
        "NativeTicksPerSecond = 10",
        "NativeTicksPerMinute = 600",
        "NativeTicksPerHour = 36000",
        "UsedPauseCommandCount = 202",
        "UsedTimeLimitCommandCount = 14",
        "DeadlineTick >= currentTick",
        "DeadlineTick < currentTick",
        "NativeInitialPreviousLeftValue = 99999999",
        "index_hour",
        "index_minute",
        "index_sec",
        "index_millisec",
        "0x10624DD3",
        "tl_SetTimeLimit",
        "only Min/Sec",
    )
    for token in pine_timing_tokens:
        if token not in pine_timing_text:
            print("FAIL: KQ Pine timing projection changed", token)
            return 1

    pine_interrupt_text = PINE_INTERRUPT_PLAN.read_text(encoding="utf-8")
    pine_interrupt_tokens = (
        "class KingdomQuestPineInterruptPlan",
        "class KingdomQuestPineInterruptSetPlan",
        "class KingdomQuestPineInterruptRegistryState",
        "NativeManagerCapacity = 20",
        "NativeEraseNameBytes = 16",
        "UsedInterruptSetCount = 225",
        "UsedInterruptEraseCount = 14",
        "UsedInterruptClearCount = 65",
        "UsedWaitInterruptCount = 55",
        "PlayerEliminate 50",
        "TimeOut 55",
        "Sec 45",
        "HPLow 21",
        "PlayerDead 15",
        "DeadIndex 12",
        "PickUpItemIndex 10",
        "DeadHandle 9",
        "NPCClickHandle 4",
        "MobEliminate 4",
        "previousDeadlineTick, durationTicks",
        "nextDeadlineTick <= currentTick",
        "(int)(deadlineTick - currentTick) <= 0",
        "removes every matching active interrupt",
        "applies ListEraser to",
        "List<KingdomQuestPineInterruptRuntimeEntry>",
        "bool TryRegister(KingdomQuestPineInterruptSetPlan plan)",
        "entries.Count >=",
        "KingdomQuestPineInterruptPlan.NativeManagerCapacity)",
        "entries.Add(new KingdomQuestPineInterruptRuntimeEntry(plan))",
        "public bool TryBindRegistration(",
        "entries[i].TryBindRegistration(variables)",
        "internal bool RemoveRuntimeEntry(",
        "public int Erase(byte[] nativeName16)",
        "public void Clear()",
        "public bool ContainsReference(",
        "object.ReferenceEquals(",
        "entries[i].SourcePlan, plan)",
    )
    for token in pine_interrupt_tokens:
        if token not in pine_interrupt_text:
            print("FAIL: KQ Pine interrupt projection changed", token)
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
    print("PASS: Pine source parser distinguishes 15 var groups/98 declarations and 48 assignments from gameplay commands")
    print("PASS: native Pine VariableStack is modeled at 127 entries with 0x100-byte tokens, 0x200 stride and newest-first lookup")
    print("PASS: StateVarDeclear/StateAssignment execute push/find -> expression -> pop in native order")
    print("PASS: native Pine literal/copy/+/- expression core runs host-free; system functions/dynamic identifiers remain gated")
    print("PASS: all 7 used @RemoveFirst calls run host-free with native destructive source-list mutation")
    print("PASS: used Pine @Random(0 99) is source-modeled as inclusive MSVC CRT rand modulo without inventing CRT ownership")
    print("PASS: both used Pine @DistanceBetween calls are source-modeled through explicit native-object resolution and exact DirectDistanceTable integer math")
    print("PASS: all 26 used Pine IFs map to native comparison modes: numeric ==/!=/</>/<=/>= and token ===/=!=")
    print("PASS: all 10 used Pine @CharName calls preserve u16 native-handle lookup, empty-on-miss, and TName5 length boundary")
    print("PASS: used Pine @Random/@DistanceBetween/@CharName expressions execute before generic host fallback with explicit native dependencies")
    print("PASS: 589 source-proven one-step Pine commands dispatch before generic host fallback through explicit tick/regen/effect dependencies")
    print("PASS: Pine TimeLimit plus 20-slot interrupt register/erase/clear state has a concrete local owner; regen/result/end remain explicitly delegated")
    print("PASS: all 202 source-used Pine pause commands execute native 10-Hz deadline state before host fallback")
    print("PASS: Pine ScriptInitValue remains a separate cc_PlayFilm token; UnderHall=10 proves it is not a top-level block key")
    print("PASS: Pine questresult/endofkq terminal actions are source-modeled as COMPLETE/FAIL title hooks and Z2W END + fm_ClearObject(0xB0)")
    print("PASS: all 243 used Pine regengroup calls resolve through the source-backed group/MobRegen boundary; 225 unique pairs across 5 sources")
    print("PASS: all 202 used Pine pause and 14 timelimit constants are modeled on the native 10-Hz tick clock with exact deadline semantics")
    print("PASS: used Pine interrupt set/erase/clear/wait forms are source-modeled with the native 20-slot manager, 16-byte erase key and timed-trigger semantics")
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
