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
ZONE_CHARACTER = ROOT / "NextGen.Zone/Game/ZoneCharacter.cs"

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

def main():
    for path in (MAP, TEAM, VOTE, REASONS, RATES, DESC, DP, TOOL, ZONE_CHARACTER):
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

    provider = DP.read_text(encoding='utf-8')
    for token in ('KingdomQuestMaps = Maps.Values', '.Where(map => map.Kingdom == 1)', 'KingdomQuestDescriptions', 'data_kingdomquestdesc', 'KingdomQuestTeams', 'KingdomQuestVoteEnabled'):
        if token not in provider:
            print('FAIL: DataProvider KQ source catalog missing', token)
            return 1

    tool = TOOL.read_text(encoding='utf-8')
    for source in ('\"KingdomQuest.shn\"', '\"KingdomQuestMap.shn\"', '\"KingdomQuestRew.shn\"', '\"KQItem.shn\"'):
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
    ):
        if token not in tool:
            print('FAIL: KQ source dumper provenance/ambiguity guard missing', token)
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
    print('PASS: source dumper targets main KQ definition/map/reward/item SHNs')
    print('PASS: source dumper can require all main SHNs, rejects duplicate basenames and records SHA-256/column manifests')
    print('PASS: ChangeMap accepts source-backed KQ map IDs above the legacy 120 cutoff')
    return 0

if __name__ == '__main__':
    sys.exit(main())
