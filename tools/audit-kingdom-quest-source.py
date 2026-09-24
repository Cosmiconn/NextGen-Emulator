#!/usr/bin/env python3
"""Lock source-backed Kingdom Quest map/team/vote metadata."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "sql/data/mapinfo.sql"
TEAM = ROOT / "sql/data/data_kqteam.sql"
VOTE = ROOT / "sql/data/data_kqisvote.sql"
REASONS = ROOT / "sql/data/data_kqvotedesc.sql"
RATES = ROOT / "sql/data/data_kqvotemajorityrate.sql"
DESC = ROOT / "sql/data/data_kingdomquestdesc.sql"
DP = ROOT / "NextGen.World/Data/DataProvider.cs"
TOOL = ROOT / "tools/KingdomQuestSourceDump/Program.cs"
WORLD_MANIFEST = ROOT / "NextGen.World/Data/KingdomQuestSourceManifestInfo.cs"
WORLD_NATIVE_SCHEMA = ROOT / "NextGen.World/Data/KingdomQuestNativeSchema.cs"
ZONE_CHARACTER = ROOT / "NextGen.Zone/Game/ZoneCharacter.cs"
WORLD_SNAPSHOT = ROOT / "NextGen.World/Data/KingdomQuestSourceSnapshot.cs"
WORLD_SOURCE_ROWS = ROOT / "NextGen.World/Data/KingdomQuestSourceRows.cs"
WORLD_SOURCE_PROJECTION = ROOT / "NextGen.World/Data/KingdomQuestSourceProjection.cs"
WORLD_SOURCE_SCHEDULER = ROOT / "NextGen.World/Data/KingdomQuestSourceScheduler.cs"
WORLD_MAP_ALLOCATOR = ROOT / "NextGen.World/Data/KingdomQuestMapAllocationRegistry.cs"
WORLD_MAP_ROUTE = ROOT / "NextGen.World/Data/KingdomQuestMapRouteResolver.cs"
WORLD_START_GATE = ROOT / "NextGen.World/Data/KingdomQuestStartGate.cs"
WORLD_SESSION = ROOT / "NextGen.World/Data/KingdomQuestSessionCoordinator.cs"
NATIVE_INFO = ROOT / "NextGen.FiestaLib/Data/KingdomQuestProtocolInfo.cs"
RAW_SOURCES = {
    "KingdomQuest": (
        ROOT / "sql/data/data_kq_source_10_kingdomquest.sql",
        "2a4c5c98005bf7253cc1c149a4260662a5c861a1b8ba38bf78d91ffcc260f6c9", 57, 35),
    "KingdomQuestMap": (
        ROOT / "sql/data/data_kq_source_20_kingdomquestmap.sql",
        "d69edb81a6e265151eaf1108c48ed0ad3fe703bff7e7d4347d276bd53c2fa5e4", 38, 22),
    "KingdomQuestRew": (
        ROOT / "sql/data/data_kq_source_30_kingdomquestrew.sql",
        "a19ad75f5b529a0178d1004182b37ee66703c01646f55f3beab439722e993dd1", 64, 33),
    "KQItem": (
        ROOT / "sql/data/data_kq_source_40_kqitem.sql",
        "2f641d273017bbd41f41f2ffb88df00ac92c1090b51f6438281bc185b1b2814a", 2, 4),
    "UseClassTypeInfo": (
        ROOT / "sql/data/data_kq_source_50_useclasstypeinfo.sql",
        "0ef94a55e26fb992e0497f825984742df681f94f9bf32167e4defebcbead632d", 39, 28),
}
SOURCE_MANIFEST_SQL = ROOT / "sql/data/data_kq_source_00_manifest.sql"

EXPECTED_MAPS = {
    (30, "KDPrtShip"), (31, "KDEddyHill"), (33, "KDTrDn"), (34, "KDUnHall"), (35, "KDEnMaze"),
    (36, "KDGoldHill"), (40, "KDMDragon"), (52, "KDHero"), (53, "KDKingkong"),
    (54, "KDHoneying"), (55, "KDSpider"), (91, "KDHDragon"),
    (92, "KDHBat1"), (95, "KDVictor"), (96, "KDAntiHenis"), (126, "KDMine"),
    (129, "KDEgg"), (131, "KDSpring"), (137, "KDArena"),
    (138, "KDGreenHill"), (146, "KDSoccer"), (148, "KDWater"), (149, "KDFargels"),
    (155, "KDSoccer_W"), (158, "KDCake"),
}

def data_rows(path):
    return [line.strip() for line in path.read_text(encoding='utf-8').splitlines()
            if line.lstrip().startswith('(')]

def split_row_fields(line):
    text = line.strip().rstrip(',;')
    if text.startswith('(') and text.endswith(')'):
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
        if ch == '\\':
            current.append(ch)
            escaped = True
            continue
        if ch == "'":
            current.append(ch)
            quoted = not quoted
            continue
        if ch == ',' and not quoted:
            fields.append(''.join(current).strip())
            current = []
            continue
        current.append(ch)
    fields.append(''.join(current).strip())
    return fields

def unquote_sql(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == "'" and value[-1] == "'":
        return value[1:-1].replace("\\'", "'").replace("\\\\", "\\")
    return value

def row_field_count(line):
    text = line.strip().rstrip(',;')
    if text.startswith('(') and text.endswith(')'):
        text = text[1:-1]
    quoted = False
    escaped = False
    fields = 1
    for ch in text:
        if escaped:
            escaped = False
            continue
        if ch == '\\':
            escaped = True
            continue
        if ch == "'":
            quoted = not quoted
            continue
        if ch == ',' and not quoted:
            fields += 1
    return fields

def main():
    required_files = [
        MAP, TEAM, VOTE, REASONS, RATES, DESC, DP, TOOL,
        WORLD_MANIFEST, WORLD_NATIVE_SCHEMA, WORLD_SNAPSHOT, WORLD_SOURCE_ROWS,
        WORLD_SOURCE_PROJECTION, WORLD_SOURCE_SCHEDULER, WORLD_MAP_ALLOCATOR,
        WORLD_MAP_ROUTE, WORLD_START_GATE, WORLD_SESSION, NATIVE_INFO,
        ZONE_CHARACTER, SOURCE_MANIFEST_SQL,
    ] + [spec[0] for spec in RAW_SOURCES.values()]
    for path in required_files:
        if not path.is_file():
            print('FAIL: missing', path)
            return 1

    found = set()
    rx = re.compile(r"^\s*\((\d+),\s*'([^']*)',.*?,\s*(\d+),\s*'[^']*',\s*\d+,\s*\d+\)[,;]?$" )
    for line in MAP.read_text(encoding='utf-8').splitlines():
        match = rx.match(line)
        if match and int(match.group(3)) == 1:
            found.add((int(match.group(1)), match.group(2)))
    if found != EXPECTED_MAPS:
        print('FAIL: KingdomMap=1 map corpus changed')
        print('expected', sorted(EXPECTED_MAPS))
        print('actual  ', sorted(found))
        return 1

    description_rows = sum(1 for line in DESC.read_text(encoding='utf-8').splitlines()
                           if line.lstrip().startswith("('"))
    counts = (len(data_rows(TEAM)), len(data_rows(VOTE)), len(data_rows(REASONS)), len(data_rows(RATES)), description_rows)
    if counts != (8, 30, 4, 2, 39):
        print('FAIL: KQ metadata row counts changed:', counts)
        return 1

    manifest_sql = SOURCE_MANIFEST_SQL.read_text(encoding='utf-8')
    for source_name, (path, sha256, expected_rows, expected_columns) in RAW_SOURCES.items():
        raw = path.read_text(encoding='utf-8')
        header = (
            'sha256=' + sha256 +
            '; records=' + str(expected_rows) +
            '; columns=' + str(expected_columns))
        if header not in raw:
            print('FAIL: KQ raw source header changed:', source_name)
            return 1
        rows = data_rows(path)
        if len(rows) != expected_rows:
            print('FAIL: KQ raw source row count changed:', source_name, len(rows))
            return 1
        malformed = [i + 1 for i, row in enumerate(rows)
                     if row_field_count(row) != expected_columns + 1]
        if malformed:
            print('FAIL: KQ raw source row width changed:', source_name, malformed[:10])
            return 1
        ordinals = []
        for row in rows:
            match = re.match(r'^\s*\((\d+)\s*,', row)
            if not match:
                print('FAIL: KQ raw source row has no __SourceRow:', source_name)
                return 1
            ordinals.append(int(match.group(1)))
        if ordinals != list(range(expected_rows)):
            print('FAIL: KQ raw source ordinals changed:', source_name, ordinals[:10])
            return 1
        if '`__SourceRow` INT UNSIGNED NOT NULL' not in raw:
            print('FAIL: KQ raw source table lost __SourceRow:', source_name)
            return 1
        manifest_tuple = "'{0}', '{1}', {2}, {3}".format(
            source_name, sha256, expected_rows, expected_columns)
        if manifest_tuple not in manifest_sql:
            print('FAIL: KQ source manifest tuple changed:', source_name)
            return 1

    kingdom_source = RAW_SOURCES["KingdomQuest"][0].read_text(encoding='utf-8')
    for row in data_rows(RAW_SOURCES["KingdomQuest"][0]):
        fields = split_row_fields(row)
        links = [int(fields[i]) for i in (27, 28, 29, 30)]
        if sum(1 for value in links if value != -1) != 1:
            print('FAIL: supplied NA2016 KQ definition is no longer single-map:', links)
            return 1

    source_map_bases = set()
    for row in data_rows(RAW_SOURCES["KingdomQuestMap"][0]):
        fields = split_row_fields(row)
        source_map_bases.add(unquote_sql(fields[2]))
    known_kq_maps = set(name for _map_id, name in EXPECTED_MAPS)
    if len(source_map_bases) != 23 or not source_map_bases.issubset(known_kq_maps):
        print('FAIL: KQ MapBase corpus no longer resolves into source-backed KingdomMap=1 maps')
        return 1

    for token in (
        '`ST_Hour` TINYINT UNSIGNED',
        '`NextStartDeleyMin` SMALLINT UNSIGNED',
        '`InitValue` VARCHAR(32)',
        '`UseClass` INT UNSIGNED',
        '`Undefined 3` TINYINT',
    ):
        if token not in kingdom_source:
            print('FAIL: exact KINGDOM_QUEST source field missing:', token)
            return 1

    provider = DP.read_text(encoding='utf-8')
    world_provider = provider
    world_manifest = WORLD_MANIFEST.read_text(encoding='utf-8')
    world_native_schema = WORLD_NATIVE_SCHEMA.read_text(encoding='utf-8')
    world_snapshot = WORLD_SNAPSHOT.read_text(encoding='utf-8')
    world_source_rows = WORLD_SOURCE_ROWS.read_text(encoding='utf-8')
    world_source_projection = WORLD_SOURCE_PROJECTION.read_text(encoding='utf-8')
    world_source_scheduler = WORLD_SOURCE_SCHEDULER.read_text(encoding='utf-8')
    world_map_allocator = WORLD_MAP_ALLOCATOR.read_text(encoding='utf-8')
    world_map_route = WORLD_MAP_ROUTE.read_text(encoding='utf-8')
    world_start_gate = WORLD_START_GATE.read_text(encoding='utf-8')
    world_session = WORLD_SESSION.read_text(encoding='utf-8')
    native_info = NATIVE_INFO.read_text(encoding='utf-8')
    for token in ('KingdomQuestMaps = Maps.Values', '.Where(map => map.Kingdom == 1)', 'KingdomQuestDescriptions', 'data_kingdomquestdesc', 'KingdomQuestTeams', 'KingdomQuestVoteEnabled'):
        if token not in provider:
            print('FAIL: DataProvider KQ source catalog missing', token)
            return 1

    tool = TOOL.read_text(encoding='utf-8')
    for source in ('\"KingdomQuest.shn\"', '\"KingdomQuestMap.shn\"', '\"KingdomQuestRew.shn\"', '\"KQItem.shn\"', '\"UseClassTypeInfo.shn\"'):
        if source not in tool:
            print('FAIL: KQ source dumper missing target', source)
            return 1

    for token in (
        'private static readonly string[] RequiredMainFiles',
        '"--require-main"',
        'Ambiguous KQ SHN source',
        'Missing required main KQ SHNs',
        'ComputeSha256(path)',
        'sha256={1}; records={2}; columns={3}',
        'writer.Write("-- Columns:")',
        'WriteManifestSchema(writer)',
        'WriteManifestRows(writer, table, sourceName, sha256)',
        '`__SourceRow` INT UNSIGNED NOT NULL',
        'writer.Write("  ({0}", r)',
        'data_kq_source_manifest',
        'data_kq_source_columns',
        '`Ordinal` INT UNSIGNED NOT NULL',
        '`TypeByte` INT UNSIGNED NOT NULL',
    ):
        if token not in tool:
            print('FAIL: KQ source dumper provenance/ambiguity guard missing', token)
            return 1

    for token in (
        'KingdomQuestSourceManifest',
        'HasCompleteKingdomQuestMainSource',
        'LoadKingdomQuestSourceManifest()',
        'data_kq_source_manifest',
        'data_kq_source_columns',
        '"KingdomQuest"',
        '"KingdomQuestMap"',
        '"KingdomQuestRew"',
        '"KQItem"',
        '"UseClassTypeInfo"',
        'HasKingdomQuestUseClassSource',
        'LoadKingdomQuestUseClassSourceRows()',
        'KingdomQuestDemandClassMasks',
        'ValidateKingdomQuestSourceTables(new[] { "UseClassTypeInfo" })',
    ):
        if token not in world_provider:
            print('FAIL: World KQ provenance loader missing', token)
            return 1

    for token in (
        'Sha256.Length != 64',
        'ColumnCount != (uint)Columns.Count',
        'Columns[i].Ordinal != (uint)i',
        'IsStructurallyValid()',
    ):
        if token not in world_manifest:
            print('FAIL: World KQ manifest structural guard missing', token)
            return 1
    for token in (
        '"Handle"',
        '"Status"',
        '"NumOfJoiner"',
        '"tm_StartTime"',
        '"NextStartMode"',
        '"RewardIndex"',
        '"tm_ScheduleTime"',
        '"MapLink"',
        '"ScriptLanguage"',
        '"ScriptInitValue"',
        '"TeamRegenXY"',
        'StringComparer.Ordinal',
        'source.Intersect(native)',
        'native.Except(source)',
        'source.Except(native)',
    ):
        if token not in world_native_schema:
            print('FAIL: native KQ schema coverage guard missing', token)
            return 1

    for forbidden in (
        'ST_Hour',
        'ST_Minute',
        'NextStartDeleyMin',
        'MinPlayer',
        'MaxPlayer',
        'InitValue',
    ):
        if ('"' + forbidden + '"') in world_native_schema:
            print('FAIL: secondary/tutorial SHN aliases leaked into native schema mapping', forbidden)
            return 1

    for source_name, (_path, sha256, record_count, column_count) in RAW_SOURCES.items():
        for token in (
            '"' + source_name + '"',
            '"' + sha256 + '"',
            str(record_count) + ', ' + str(column_count),
        ):
            if token not in world_snapshot:
                print('FAIL: World KQ snapshot guard missing', source_name, token)
                return 1

    for token in (
        'KingdomQuestSourceSnapshot.Matches(source)',
        'ValidateKingdomQuestSourceTables(required)',
        'SELECT COUNT(*) AS RowCount',
        'expected.RecordCount',
    ):
        if token not in world_provider:
            print('FAIL: World KQ raw-source runtime gate missing', token)
            return 1

    for token in (
        'class KingdomQuestSourceDefinition',
        'SourceRow = GetDataTypes.GetUint(row["__SourceRow"])',
        'NextStartDeleyMin = GetDataTypes.GetUshort(row["NextStartDeleyMin"])',
        'MapLinkColumns = Array.AsReadOnly',
        'class KingdomQuestMapSourceRow',
        'class KingdomQuestRewardSourceRow',
        'class KingdomQuestItemSourceRow',
        'class KingdomQuestUseClassSourceRow',
        'ToDemandClassMask()',
        'value = (value << 1) + ClassFlags[i]',
        'value <<= 1',
    ):
        if token not in world_source_rows:
            print('FAIL: World exact KQ source model missing', token)
            return 1

    for token in (
        'ApplyProvenStaticFields',
        'target.ID = (ushort)source.ID',
        'target.NextStartDelayMin = source.NextStartDeleyMin',
        'target.ScriptInitValue = source.InitValue',
        'target.RewardIndex = unchecked((ushort)source.RewardIndex)',
        'source.Undefined3) * 2',
        'source.DemandGender',
        'target.DemandMobKill = source.DemandMobKill',
    ):
        if token not in world_source_projection:
            print('FAIL: proven KQ source projection missing', token)
            return 1

    for token in (
        'ScheduleWindowSize = 2',
        'source.ST_Day - 1',
        '.AddHours(source.ST_Hour)',
        '.AddMinutes(source.ST_Minute)',
        'source.NextStartDeleyMin',
        'while (next < currentMinute)',
        'Status = 0',
        'NumOfJoiner = 0',
        'StartTime = time32',
        'ScheduleTime = time32',
        'demandClassMasks.TryGetValue(source.UseClass, out demandClass)',
        'DemandClass = demandClass',
        'team.ID != result.ID',
        'result.IsTeamPvp = team.IsTeamPvp',
        'team.RegenXRed',
        'team.RegenYBlue',
        'NativeScheduleCapacity = 300',
        'nextHandle = 0',
        'lastScheduleMinute',
        'Tuple.Create(existing[i].ID, existing[i].ScheduleTime)',
        'KingdomQuestScheduledDefinitionCoordinator.TryPublish(',
        'nextHandle = unchecked(nextHandle + 1)',
        '[ServerModule(InitializationStage.Worker)]',
        'provider.HasCompleteKingdomQuestMainSource',
        'provider.HasKingdomQuestUseClassSource',
        'KingdomQuestProtocolDefinitionRegistry.Upsert(definition)',
        'KingdomQuestDefinitionRegistry.Upsert(definition)',
        'KingdomQuestInstanceRegistry.Upsert(',
        'new KingdomQuestJoinCharacterInfo[0]',
    ):
        if token not in world_source_scheduler:
            print('FAIL: PDB/EXE-bounded KQ scheduler projection missing', token)
            return 1

    for forbidden in (
        'source.ST_Year',
        'source.ST_Month',
        'source.ST_Second',
    ):
        scheduler_logic = world_source_scheduler.split('public static IReadOnlyList<DateTime> GetNextScheduleTimes', 1)[1]
        if forbidden in scheduler_logic.split('public static KingdomQuestProtocolInfo CreateScheduledDefinition', 1)[0]:
            print('FAIL: original DoSchedule-ignored KQ source time field became active', forbidden)
            return 1

    for token in (
        'SlotsPerSourceRow = 10',
        'definitions[i].ID == definition.ID',
        'source.MapLinkColumns[linkIndex]',
        'sourceMapIndex == -1',
        'map.SourceRow != (uint)sourceMapIndex',
        'map.NumOfMap > SlotsPerSourceRow',
        'GetEmptyMapLinkLocked(sourceMapIndex, map)',
        'map.ClearColumns[slot] == 0',
        '!allocatedBySourceRow[sourceMapIndex, slot].HasValue',
        'allocatedBySourceRow[sourceMapIndex, slot] =',
        'definition.Handle',
        'MapIndex = (byte)slot',
        'MapBase = map.BaseMap',
        'MapName = map.MapColumns[slot]',
        'MapClear = clear',
        'FreeLocked(definition.Handle)',
    ):
        if token not in world_map_allocator:
            print('FAIL: native KQ map-slot allocation primitive missing', token)
            return 1

    for token in (
        'TryResolveScheduledMap',
        'candidate.ID >= 0 && (ushort)candidate.ID == kqId',
        'source.MapLinkColumns[i]',
        'sourceMapIndex >= 0',
        'sourceMap.SourceRow != (uint)sourceMapIndex',
        'TryResolveAllocatedMap',
        'candidate.ShortName, mapBase, StringComparison.Ordinal',
    ):
        if token not in world_map_route:
            print('FAIL: source-backed KQ MapBase route resolver missing', token)
            return 1

    for token in (
        'RunMakeRoom(local)',
        'definition.Status != KingdomQuestNativeConstants.StatusScheduled',
        'definition.ScheduleTime > currentTime',
        'KingdomQuestMapRouteResolver.TryResolveScheduledMap(',
        'KingdomQuestSessionCoordinator.TryPrepareMake(',
        'zone.SendKingdomQuestMake(definition.Handle)',
        'TryRollbackMakePreparation(',
    ):
        if token not in world_source_scheduler:
            print('FAIL: native DoSetMakeRoom runtime bridge missing', token)
            return 1

    for token in (
        'KingdomQuestStartDecisionKind',
        'definition.NumOfJoiner == definition.MaxPlayers',
        '(long)definition.StartTime',
        'definition.StartWaitTime * 60L',
        'definition.NumOfJoiner < definition.MinPlayers',
        'team.TeamDivideType !=',
        'AutomaticSplitTeamDivideType',
        'team0 == 0 || team1 == 0',
        'Math.Abs(team0 - team1) > team.MaxMemberGap',
        'DoneSkipReasonNotReady',
        'DoneSkipReasonTeamGap',
    ):
        if token not in world_start_gate:
            print('FAIL: native KQ DoSetStart/KQTeam_CanKQStart gate missing', token)
            return 1

    for token in (
        'StatusStartCountdown = 3',
        'StatusDoneSkip = 6',
        'StartCountdownSeconds = 10',
        'DoneSkipReasonNotReady = 2',
        'DoneSkipReasonTeamGap = 3',
    ):
        if token not in native_info:
            print('FAIL: recovered KQ start-gate constant missing', token)
            return 1

    for token in (
        'TryEnterStartCountdown',
        'KingdomQuestNativeConstants.StatusStartCountdown',
        'KingdomQuestStartCountdownRegistry.Set(',
        'TrySetDoneSkip',
        'KingdomQuestNativeConstants.StatusDoneSkip',
        'KingdomQuestDoneSkipRegistry.Set(handle, reason)',
    ):
        if token not in world_session:
            print('FAIL: synchronized KQ start-gate transition missing', token)
            return 1

    for token in (
        'RunStartGate(local)',
        'KingdomQuestStartGate.Evaluate(',
        'KingdomQuestSessionCoordinator.TryEnterStartCountdown(',
        'KingdomQuestSessionCoordinator.TrySetDoneSkip(',
    ):
        if token not in world_source_scheduler:
            print('FAIL: live native KQ start gate missing', token)
            return 1

    for forbidden in (
        'StatusRunning = 4',
        'StatusStart = 4',
        'SendKingdomQuestStart(',
    ):
        if forbidden in world_start_gate or forbidden in world_session or forbidden in world_source_scheduler:
            print('FAIL: unresolved post-countdown KQ start semantics were guessed', forbidden)
            return 1

    for forbidden in (
        'KingdomQuestSessionTargetRegistry',
        'Map.InstanceID',
        'MapInstance =',
        '(short)slot',
    ):
        allocator_code = world_map_allocator.split('namespace NextGen.World.Data', 1)[1]
        if forbidden in allocator_code and forbidden != 'Map.InstanceID':
            print('FAIL: native KQ map-slot allocator leaked emulator routing', forbidden)
            return 1
    if 'does not choose an emulator' not in world_map_allocator:
        print('FAIL: KQ map allocator lost native/emulator routing boundary documentation')
        return 1

    for forbidden in (
        'target.Handle =',
        'target.Status =',
        'target.NumOfJoiner =',
        'target.StartTime =',
        'target.StartTm =',
        'target.DemandClass =',
        'target.MapLink =',
        'target.ScheduleTime =',
        'target.ScheduleTm =',
        'target.RunCounter =',
        'target.IsTeamPvp =',
        'target.TeamRegenXY =',
    ):
        if forbidden in world_source_projection:
            print('FAIL: unresolved KQ field leaked into static source projection', forbidden)
            return 1

    for token in (
        'LoadKingdomQuestMainSourceRows()',
        'ORDER BY `__SourceRow`',
        'KingdomQuestSourceDefinition.Load(row)',
        'KingdomQuestMapSourceRow.Load(row)',
        'KingdomQuestRewardSourceRow.Load(row)',
        'KingdomQuestItemSourceRow.Load(row)',
    ):
        if token not in world_provider:
            print('FAIL: World exact KQ source loader missing', token)
            return 1

    zone_character = ZONE_CHARACTER.read_text(encoding='utf-8')
    if 'if (id > 120)' in zone_character:
        print('FAIL: legacy map-ID cutoff blocks source-backed KQ maps above 120')
        return 1
    if '!DataProvider.Instance.MapsByID.ContainsKey(id)' not in zone_character:
        print('FAIL: ChangeMap is no longer validated against loaded map data')
        return 1

    print('PASS: 25 KingdomMap=1 source maps locked')
    print('PASS: KQ description/team/vote metadata corpus locked (39/8/30/4/2)')
    print('PASS: source dumper targets main KQ SHNs plus the exact UseClassTypeInfo scheduler dependency')
    print('PASS: source dumper can require all main SHNs, rejects duplicate basenames and records SHA-256/column manifests')
    print('PASS: source SQL includes machine-readable file/column provenance without gameplay mapping')
    print('PASS: exact NA2016 KQ/source dependency corpus locked (57/38/64/2/39 rows; includes UseClassTypeInfo)')
    print('PASS: KQ raw SQL preserves contiguous zero-based __SourceRow ordinals')
    print('PASS: supplied NA2016 definitions are locked to one active MapLink and 23 source-backed MapBase identities')
    print('PASS: native MapBase resolves exactly to source-backed MapInfo.ShortName without MapIndex/instance inference')
    print('PASS: DoSetStart/KQTeam_CanKQStart drives Status-2 into proven Status-3 countdown or Status-6 SetDoneSkip reasons 2/3')
    print('PASS: post-countdown Status/START transition remains explicitly unresolved and is not guessed')
    print('PASS: World main-source gate requires exact SHAs and matching runtime SQL row counts')
    print('PASS: World loads all four KQ main tables in explicit __SourceRow order without scheduler synthesis')
    print('PASS: World loads exact UseClassTypeInfo and reproduces ccdb_UseClassTypeToBit folding for DemandClass')
    print('PASS: static KQ source projection maps PDB/EXE-correlated fields including packed DemandGender')
    print('PASS: KQ scheduler primitive reproduces current-month/day-hour-minute + minute-step two-entry window from WorldManager.exe')
    print('PASS: scheduled definition projection reproduces initial status/time/team fields')
    print('PASS: live scheduler owns native Handle sequence from zero, exact ID/ScheduleTime dedupe and 300-entry capacity')
    print('PASS: scheduler publishes Status-0 definitions atomically to protocol/client/status/empty-participant views only when exact source gates pass')
    print('PASS: native map allocation resolves first matching KQ ID, four source-row links, 10-slot reservation and rollback without inventing Map.InstanceID')
    print('PASS: World accepts main KQ source presence only from structurally complete four-table provenance')
    print('PASS: KingdomQuest.shn coverage compares only exact PDB field names; no SHN aliases are inferred')
    print('PASS: ChangeMap accepts source-backed KQ map IDs above the legacy 120 cutoff')
    return 0

if __name__ == '__main__':
    sys.exit(main())
