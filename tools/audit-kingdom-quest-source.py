#!/usr/bin/env python3
"""Lock source-backed Kingdom Quest map/team/vote metadata."""
from pathlib import Path
import hashlib
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
WORLD_MEMBERSHIP = ROOT / "NextGen.World/Data/KingdomQuestMembershipRegistry.cs"
WORLD_RANDOM = ROOT / "NextGen.World/Data/KingdomQuestNativeRandom.cs"
WORLD_START_SESSIONS = ROOT / "NextGen.World/Data/KingdomQuestStartSessionResolver.cs"
WORLD_DONE_SKIP_MESSAGES = ROOT / "NextGen.World/Data/KingdomQuestDoneSkipMessages.cs"
WORLD_RECONNECT = ROOT / "NextGen.World/Data/KingdomQuestReconnectRules.cs"
WORLD_MAP_CONTEXT = ROOT / "NextGen.World/Data/KingdomQuestMapContext.cs"
WORLD_SESSION = ROOT / "NextGen.World/Data/KingdomQuestSessionCoordinator.cs"
WORLD_REWARD_RESOLVER = ROOT / "NextGen.World/Data/KingdomQuestRewardSourceResolver.cs"
NATIVE_INFO = ROOT / "NextGen.FiestaLib/Data/KingdomQuestProtocolInfo.cs"
NATIVE_REWARD = ROOT / "NextGen.FiestaLib/Data/KingdomQuestRewardInfo.cs"
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
SHINE_REWARD_SQL = ROOT / "sql/data/data_kq_source_60_shinereward.sql"
SHINE_REWARD_SQL_SHA256 = "9fa4fc1ce2db998cc61f01dc0ef6ba46575a68162efa71c467b9904032c65a88"

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
        WORLD_MAP_ROUTE, WORLD_START_GATE, WORLD_MEMBERSHIP, WORLD_RANDOM,
        WORLD_START_SESSIONS, WORLD_DONE_SKIP_MESSAGES, WORLD_RECONNECT,
        WORLD_MAP_CONTEXT, WORLD_SESSION, WORLD_REWARD_RESOLVER,
        NATIVE_INFO, NATIVE_REWARD,
        ZONE_CHARACTER, SOURCE_MANIFEST_SQL, SHINE_REWARD_SQL,
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
    for token in (
        "'ShineReward', '09acc18d24877fc5dfa9ab431d8dd45561e36ffd48b518cbdf05ddd1810a325f', 435, 16",
        "('ShineReward', 0, 'RewardHandle', 2, 2)",
        "('ShineReward', 1, 'RewardType', 12, 1)",
        "('ShineReward', 2, 'Argument', 9, 33)",
        "('ShineReward', 3, 'Quantity', 3, 4)",
        "('ShineReward', 4, 'Upgrade', 21, 2)",
        "('ShineReward', 14, 'OptionDegree', 2, 2)",
        "('ShineReward', 15, 'TitleDegree', 3, 4)",
    ):
        if token not in manifest_sql:
            print('FAIL: checked-in ShineReward source manifest changed', token)
            return 1
    shine_reward_sql = SHINE_REWARD_SQL.read_text(encoding='utf-8')
    actual_shine_sql_sha = hashlib.sha256(
        SHINE_REWARD_SQL.read_bytes()).hexdigest()
    if actual_shine_sql_sha != SHINE_REWARD_SQL_SHA256:
        print('FAIL: checked-in compact ShineReward SQL snapshot changed',
              actual_shine_sql_sha)
        return 1
    if ('sha256=09acc18d24877fc5dfa9ab431d8dd45561e36ffd48b518cbdf05ddd1810a325f; records=435; columns=16'
            not in shine_reward_sql):
        print('FAIL: ShineReward compact source header changed')
        return 1
    for token in (
        '`RewardHandle` SMALLINT UNSIGNED NOT NULL',
        '`RewardType` TINYINT UNSIGNED NOT NULL',
        '`Argument` VARCHAR(33) NOT NULL',
        '`Quantity` INT UNSIGNED NOT NULL',
        '`Upgrade` SMALLINT NOT NULL DEFAULT 0',
        '`Undefined 8` SMALLINT NOT NULL DEFAULT 0',
        '`OptionDegree` SMALLINT UNSIGNED NOT NULL DEFAULT 0',
        '`TitleDegree` INT UNSIGNED NOT NULL DEFAULT 0',
    ):
        if token not in shine_reward_sql:
            print('FAIL: compact ShineReward schema changed', token)
            return 1

    shine_reward_rows = [split_row_fields(row)
                         for row in data_rows(SHINE_REWARD_SQL)]
    if len(shine_reward_rows) != 435:
        print('FAIL: compact ShineReward row count changed',
              len(shine_reward_rows))
        return 1
    if any(len(row) != 5 for row in shine_reward_rows):
        print('FAIL: compact ShineReward base rows no longer have five explicit source fields')
        return 1
    if [int(row[0]) for row in shine_reward_rows] != list(range(435)):
        print('FAIL: compact ShineReward source ordinals changed')
        return 1
    if shine_reward_sql.count('UPDATE `data_shinereward` SET') != 18:
        print('FAIL: compact ShineReward nonzero-tail patch count changed')
        return 1

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

    kingdom_rows = [split_row_fields(row)
                    for row in data_rows(RAW_SOURCES["KingdomQuest"][0])]
    reward_rows = [split_row_fields(row)
                   for row in data_rows(RAW_SOURCES["KingdomQuestRew"][0])]
    reward_ids = {int(row[1]) for row in reward_rows}
    used_reward_indices = {int(row[26]) for row in kingdom_rows}
    unresolved_default_reward_indices = (
        used_reward_indices - reward_ids)
    if len(used_reward_indices) != 22:
        print('FAIL: supplied KQ distinct RewardIndex corpus changed',
              sorted(used_reward_indices))
        return 1
    if unresolved_default_reward_indices != {45, 51, 57, 63, 71, 79, 83}:
        print('FAIL: native KQ default reward-ID miss set changed',
              sorted(unresolved_default_reward_indices))
        return 1

    used_shine_reward_handles = {
        int(value)
        for row in reward_rows
        for value in row[4:19]
        if int(value) != 0
    }
    shine_reward_type_by_handle = {}
    for row in shine_reward_rows:
        handle = int(row[1])
        if handle not in shine_reward_type_by_handle:
            shine_reward_type_by_handle[handle] = int(row[2])
    missing_shine_handles = (
        used_shine_reward_handles - set(shine_reward_type_by_handle))
    if len(used_shine_reward_handles) != 300 or missing_shine_handles:
        print('FAIL: KQ -> ShineReward handle coverage changed',
              len(used_shine_reward_handles), sorted(missing_shine_handles))
        return 1
    used_reward_types = {}
    for handle in used_shine_reward_handles:
        reward_type = shine_reward_type_by_handle[handle]
        used_reward_types[reward_type] = used_reward_types.get(reward_type, 0) + 1
    if used_reward_types != {1: 247, 2: 39, 3: 14}:
        print('FAIL: KQ-used ShineReward type split changed',
              used_reward_types)
        return 1

    reward_index_strings = [unquote_sql(row[2]) for row in reward_rows]
    if max(len(value) for value in reward_index_strings) != 20:
        print('FAIL: KingdomQuestRew IndexString width corpus changed')
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
    world_membership = WORLD_MEMBERSHIP.read_text(encoding='utf-8')
    world_random = WORLD_RANDOM.read_text(encoding='utf-8')
    world_start_sessions = WORLD_START_SESSIONS.read_text(encoding='utf-8')
    world_done_skip_messages = WORLD_DONE_SKIP_MESSAGES.read_text(encoding='utf-8')
    world_reconnect = WORLD_RECONNECT.read_text(encoding='utf-8')
    world_map_context = WORLD_MAP_CONTEXT.read_text(encoding='utf-8')
    world_session = WORLD_SESSION.read_text(encoding='utf-8')
    world_reward_resolver = WORLD_REWARD_RESOLVER.read_text(encoding='utf-8')
    native_info = NATIVE_INFO.read_text(encoding='utf-8')
    native_reward = NATIVE_REWARD.read_text(encoding='utf-8')
    for token in ('KingdomQuestMaps = Maps.Values', '.Where(map => map.Kingdom == 1)', 'KingdomQuestDescriptions', 'data_kingdomquestdesc', 'KingdomQuestTeams', 'KingdomQuestVoteEnabled'):
        if token not in provider:
            print('FAIL: DataProvider KQ source catalog missing', token)
            return 1

    tool = TOOL.read_text(encoding='utf-8')
    for source in ('\"KingdomQuest.shn\"', '\"KingdomQuestMap.shn\"', '\"KingdomQuestRew.shn\"', '\"KQItem.shn\"', '\"UseClassTypeInfo.shn\"', '\"ShineReward.shn\"'):
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
        '"ShineReward"',
        'HasKingdomQuestUseClassSource',
        'HasKingdomQuestShineRewardSource',
        'KingdomQuestShineRewards',
        'LoadKingdomQuestShineRewardSourceRows()',
        'ValidateKingdomQuestSourceTables(new[] { "ShineReward" })',
        'data_shinereward',
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

    for token in (
        '"ShineReward"',
        '"data_shinereward"',
        '"09acc18d24877fc5dfa9ab431d8dd45561e36ffd48b518cbdf05ddd1810a325f"',
        '435, 16',
    ):
        if token not in world_snapshot:
            print('FAIL: original ShineReward source snapshot missing', token)
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
        'class KingdomQuestRewardSourceResolver',
        'NativeIndexStringBytes = 32',
        'current.ID == nativeId',
        'string.Equals(',
        'StringComparison.Ordinal',
        'value.Length >= NativeIndexStringBytes',
        "value[i] == '\\0' || value[i] > 0x7f",
        'TryFindById(rows, definition.RewardIndex, out reward)',
        'TryFindShineRewardByHandle(',
        'current.RewardHandle == rewardHandle',
        'duplicate handle 0 rows',
        'Neither overload treats a request as a source-row index',
    ):
        if token not in world_reward_resolver:
            print('FAIL: KQRewardDataBox lookup projection missing', token)
            return 1

    for forbidden in ('SourceRow == rewardIndex', 'rows[rewardIndex]',
                      'rows[(int)rewardIndex]'):
        if forbidden in world_reward_resolver:
            print('FAIL: KQ reward lookup regressed to source-row indexing',
                  forbidden)
            return 1

    for token in (
        'public bool TryProjectNative(out KingdomQuestNativeRewardInfo value)',
        'KingdomQuestNativeRewardInfo.TryCreate(',
        'KQBoxItemIDX, RewardColumns, RewardRateColumns',
    ):
        if token not in world_source_rows:
            print('FAIL: KQ reward raw-source/native projection missing', token)
            return 1

    for token in (
        'enum ShineRewardType : byte',
        'None = 0',
        'Item = 1',
        'Experience = 2',
        'Money = 3',
        'Honor = 4',
        'HpSoulStone = 5',
        'SpSoulStone = 6',
        'GuardSoulStone = 7',
        'AttackSoulStone = 8',
        'ClassChange = 9',
        'Pet = 10',
        'Max = 11',
        'EntryCount = 15',
        'NativeStructSize = 128',
        'ShineRewardNativeStructSize = 66',
        'RewardRequestOpCode = 0x5815',
        'RewardSuccessAckOpCode = 0x5816',
        'RewardFailAckOpCode = 0x5817',
        'RewardRequestPayloadBaseSize = 35',
        'ItemCreateRequestBaseSize = 23',
        'RewardSuccessAckSize = 8',
        'RewardFailAckSize = 10',
        'class KingdomQuestRewardSuccessAckInfo',
        'class KingdomQuestRewardFailAckInfo',
        'packet.WriteUShort(ClientHandle)',
        'packet.WriteUInt(CharacterNumber)',
        'packet.WriteUShort(LockIndex)',
        'packet.WriteUShort(Error)',
        'packet.TryReadUInt(out characterNumber)',
        'the recovered Zone',
        'handler does not read it',
        'unchecked((ushort)rewardColumns[i])',
        'unchecked((ushort)rewardRateColumns[i])',
        'randomSamples.Count != EntryCount',
        'randomSamples[i] >= 1000',
        'randomSamples[i] < RewardRates[i]',
        'does not resolve ShineReward handles or grant rewards',
    ):
        if token not in native_reward:
            print('FAIL: native KQ reward boundary missing', token)
            return 1

    for forbidden in ('Inventory', 'ExecuteQuery', 'GiveExp', 'Money +=', 'Fame +='):
        if forbidden in native_reward:
            print('FAIL: native KQ reward boundary activated gameplay/persistence', forbidden)
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
        'class ShineRewardSourceRow',
        'RewardHandle = GetDataTypes.GetUshort(row["RewardHandle"])',
        'RewardType = (ShineRewardType)rewardType',
        'UnknownShorts = Array.AsReadOnly(unknown)',
        'ToNativeInfo()',
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
        'UserSelectTeamDivideType',
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
        'StatusRunning = 4',
        'StatusDoneSkip = 6',
        'RandomTeamDivideType = 1',
        'UserSelectTeamDivideType = 2',
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

    for token in (
        'class KingdomQuestMembershipEntry',
        'public uint CharacterNumber',
        'public byte Level',
        'public byte Class',
        'public string Name',
        'public byte TeamType',
        'ToClientInfo()',
        'ToZoneInfo()',
        'never infers one identity from the other',
    ):
        if token not in world_membership:
            print('FAIL: combined native KQ membership owner missing', token)
            return 1

    for token in (
        'class KingdomQuestRandomTeamDivider',
        'team.TeamDivideType != KingdomQuestNativeConstants.RandomTeamDivideType',
        'int half = members.Count / 2',
        'ushort sample = random.Next1000()',
        'if (sample < 500)',
        'team1 >= half && team0 < half',
        'team0 >= half && team1 < half',
        'members[i].TeamType = selected',
    ):
        if token not in world_start_gate:
            print('FAIL: native KQTD_RANDOM split missing', token)
            return 1

    for token in (
        'crtState * 0x343fdu + 0x269ec3u',
        '(crtState >> 16) & 0x7fffu',
        'new uint[16]',
        '2.3283064365386963e-10',
        '100000000000.0',
        'scaled % 1000UL',
        '0xfed22169u',
    ):
        if token not in world_random:
            print('FAIL: source-correlated RandomBox/WELL512 path missing', token)
            return 1

    for token in (
        'KingdomQuestMembershipRegistry.Set(',
        'new KingdomQuestMembershipEntry[0]',
        'KingdomQuestMembershipRegistry.TryGet(',
    ):
        if token not in world_source_scheduler:
            print('FAIL: scheduler is not using authoritative combined KQ membership', token)
            return 1

    for token in (
        'private readonly KingdomQuestNativeRandom nativeRandom',
        'new KingdomQuestNativeRandom(',
        'RunStartCountdownExpiry(local)',
        'KingdomQuestStartSessionResolver.TryResolve(',
        'KingdomQuestSessionCoordinator.TryEnterRunning(',
        'KingdomQuestStartSessionResolver.LeaveRepresentedParties(',
        'zone.SendKingdomQuestStart(definition.Handle)',
    ):
        if token not in world_source_scheduler:
            print('FAIL: live Status-3 expiry/START bridge missing', token)
            return 1

    for token in (
        'public static bool TryEnterRunning',
        'KingdomQuestStartCountdownRegistry.TryGet(',
        'currentTime32 < countdownEndsAt',
        'KingdomQuestNativeConstants.StatusRunning',
        'KingdomQuestRandomTeamDivider.Apply(',
        'TrySetMembership(handle, updated)',
    ):
        if token not in world_session:
            print('FAIL: synchronized Status-3 -> Status-4 transition missing', token)
            return 1

    for token in (
        'class KingdomQuestStartSessionResolver',
        'ClientManager.Instance.GetClientByCharID(',
        'client.KingdomQuestHandle.Value != handle',
        'client.Character.Group.Members.Count < 2',
        'NextGen.World.GroupManager.Instance.LeaveParty(client)',
        're-read Group on every iteration',
    ):
        if token not in world_start_sessions:
            print('FAIL: represented KQTeam_LeaveParty bridge missing', token)
            return 1

    for forbidden in (
        'RaidLeave(',
        'Random(',
    ):
        if forbidden in world_start_sessions:
            print('FAIL: KQ START bridge invented unsupported runtime state/policy', forbidden)
            return 1

    for token in (
        '36b573c6f604a693cf0d0c7533fc90615233ae4a8be62f3ced9bd4119f25884e',
        'records=3, columns=1',
        "Recruitment for Kingdom Quest - '%s' has begun.",
        'Kingdom Quest - %s will begin in  %d seconds.',
        'Kingdom Quest - %s has been canceled due to lack of participants(%d/%d).',
        'GetSourceMessage(3)',
        'GetSourceMessage(4)',
        ': string.Empty',
    ):
        if token not in world_done_skip_messages:
            print('FAIL: exact MsgWorldManager/SetDoneSkip source mapping missing', token)
            return 1

    for token in (
        'ApplyDoneSkip(definition, decision.DoneSkipReason)',
        'zone.SendKingdomQuestDestroy(definition.Handle)',
        'KingdomQuestMapAllocationRegistry.Free(definition.Handle)',
        'KingdomQuestDoneSkipMessages.Create(reason, definition)',
        'client.KingdomQuestHandle = null',
        'KingdomQuestProtocol.CreateJoiningAlarmEnd(',
        'RunDeleteOldSchedules()',
        'status >= 5 && status <= 10',
        'other.ID == current.ID',
        'current.ScheduleTime < other.ScheduleTime',
        'KingdomQuestNativeConstants.StatusDelete',
        'KingdomQuestSessionCoordinator.Remove(handle)',
    ):
        if token not in world_source_scheduler:
            print('FAIL: recovered SetDoneSkip/DelOldShceduleList lifecycle missing', token)
            return 1

    done_skip_pos = world_source_scheduler.find(
        'KingdomQuestSessionCoordinator.TrySetDoneSkip(')
    destroy_pos = world_source_scheduler.find(
        'zone.SendKingdomQuestDestroy(definition.Handle)', done_skip_pos)
    free_map_pos = world_source_scheduler.find(
        'KingdomQuestMapAllocationRegistry.Free(definition.Handle)', destroy_pos)
    notify_pos = world_source_scheduler.find(
        'KingdomQuestDoneSkipMessages.Create(reason, definition)', free_map_pos)
    free_joiner_pos = world_source_scheduler.find(
        'client.KingdomQuestHandle = null', notify_pos)
    alarm_end_pos = world_source_scheduler.find(
        'KingdomQuestProtocol.CreateJoiningAlarmEnd(', free_joiner_pos)
    if not (0 <= done_skip_pos < destroy_pos < free_map_pos < notify_pos <
            free_joiner_pos < alarm_end_pos):
        print('FAIL: SetDoneSkip side-effect order diverged from WorldManager.exe')
        return 1

    for token in (
        'class KingdomQuestReconnectRules',
        'ReconnectWindowMinutes = 10',
        '2000 + (int)(packed & 0x0Fu)',
        '(packed >> 4) & 0x0Fu',
        '(packed >> 8) & 0x1Fu',
        '(packed >> 13) & 0x1Fu',
        '(packed >> 18) & 0x3Fu',
        '(packed >> 24) & 0x3Fu',
        'handle == uint.MaxValue',
        'definition.MapLink.Length != 4',
        'link.MapName, savedMapName',
        'target.NativeMapName, savedMapName',
        'saved.AddMinutes(ReconnectWindowMinutes)',
        'return localNow <',
        'Do not add a lower-bound/future-date policy',
        'public static uint EncodeNativeDate(DateTime databaseDateTime)',
        '((uint)databaseDateTime.Year & 0x0Fu)',
        '((uint)databaseDateTime.Month << 4) & 0xF0u',
        '((uint)databaseDateTime.Day << 8) & 0x1F00u',
        '((uint)databaseDateTime.Hour << 13) & 0x3E000u',
        '((uint)databaseDateTime.Minute << 18) & 0xFC0000u',
        '((uint)databaseDateTime.Second << 24) & 0x3F000000u',
        'public static bool TryIsExistingFromDatabase(',
        'EncodeNativeDate(savedDatabaseDate)',
        'original year field is only four',
    ):
        if token not in world_reconnect:
            print('FAIL: native KQ IsExisted/reconnect/date-packing rule missing', token)
            return 1

    if 'year epoch/base is not yet source-proven' in world_map_context:
        print('FAIL: stale unresolved SHINE_DATETIME year note remains')
        return 1
    for token in (
        'low 4-bit',
        'year + 2000',
        'ten-minute reconnect expiry',
    ):
        if token not in world_map_context:
            print('FAIL: KQ map context lost source-proven native date semantics', token)
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
    print('PASS: PDB enum names lock KQTD_RANDOM=1 and KQTD_USERSELECT=2; supplied team rows are RANDOM')
    print('PASS: native KQTD_RANDOM assignment and RandomBox/WELL512 path are source-correlated')
    print('PASS: combined membership owns CharacterNumber and client identity together without inference')
    print('PASS: CKQServer::IsExisted packed SHINE_DATETIME decode, exact MapName match and native ten-minute reconnect expiry are source-modeled')
    print('PASS: Character.exe dKQDate -> SHINE_DATETIME packing is source-modeled exactly, including the native four-bit year wrap')
    print('PASS: Status-3 expiry now runs live as Status 4 -> KQTD_RANDOM divide -> represented normal-party leave -> W2Z START')
    print('PASS: START preflights every native CharacterNumber/session and target Zone before mutating status/team/party state')
    print('PASS: SetDoneSkip preserves Status6 -> DESTROY -> FreeMapLink -> source-backed notify -> FreeJoiner -> JOINING_ALARM_END order')
    print('PASS: DelOldShceduleList removes an old Status5..10 entry only after a later same-ID Status5..10 schedule exists')
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
