#!/usr/bin/env python3
"""Audit the complete lossless QuestScript corpus without inventing semantics."""
import base64
import os
import re
import zlib
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL = Path(os.environ.get("QUEST_SCRIPT_SQL", ROOT / "sql/data/data_quest_script_data.sql"))
MANIFEST = ROOT / "tests/fixtures/quest_script_corpus_manifest.sql"
CORPUS = ROOT / "tests/fixtures/quest_script_opcode_corpus.zlib.b64"
ITEM_SQL = ROOT / "sql/data/data_iteminfo.sql"
MOB_SQL = ROOT / "sql/data/data_mobinfo.sql"
EXPECTED = {'ACCEPT':2410,'CANCEL':12,'CREATE_ITEM':206,'DELETE_ITEM':1474,'DONE':2603,'END':9446,'GET_ITEM_LOT':104,'GET_PLAYER_EMPTY_INVENTORY':676,'GOTO':4,'IF':3036,'LINK':350,'SAY':19924,'SCENARIO':52,'SET_ABSTATE':51}
EXPECTED_IF_SHAPES = {('RESULT', '=='): 2256, ('VAR1', '<'): 676, ('RESULT', '<'): 104}
EXPECTED_DONE_BY_STAGE = {'Start': 351, 'Action': 1, 'Finish': 2251}
EXPECTED_ACCEPT_BY_STAGE = {'Start': 2382, 'Finish': 28}
SOURCE_SHA = '8c4ba17267967883169142c736e6d31d1a016c843d61411da7bca8dd244cc8b8'

def decode_script(raw):
    s = raw
    for _ in range(4):
        old = s
        s = re.sub(r'\\+r\\+n', '\n', s)
        s = re.sub(r'\\+n', '\n', s)
        s = re.sub(r'\\+r', '\r', s)
        s = re.sub(r'\\+t', '\t', s)
        if s == old:
            break
    return s

def parse_rows(text):
    pat = re.compile(rb"\((\d+),\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)'\)")
    rows = {}
    for m in pat.finditer(text):
        rows[m.group(1).decode()] = [decode_script(x.replace(b"''", b"'").decode('utf-8', 'replace')) for x in m.groups()[1:]]
    return rows

def parse_manifest(text):
    return {op:int(n) for op,n in re.findall(r"\('([A-Z_]+)',(\d+)\)", text)}

def parse_manifest_source_sha(text):
    m = re.search(r'^-- Source SHA-256:\s*([0-9a-fA-F]{64})\s*$', text, re.M)
    return m.group(1).lower() if m else None

def load_verified_corpus():
    if not CORPUS.is_file():
        return None
    try:
        encoded = re.sub(r"\s+", "", CORPUS.read_text(encoding="ascii"))
        decoded = zlib.decompress(base64.b64decode(encoded, validate=True))
    except (OSError, ValueError, zlib.error) as exc:
        raise RuntimeError(f"cannot decode verified Quest corpus fixture: {exc}") from exc
    rows = parse_rows(decoded)
    if len(rows) != 2304:
        raise RuntimeError(f"verified Quest corpus decoded, but expected 2304 rows, got {len(rows)}")
    return rows

def labels(script):
    return {x.lower() for x in re.findall(r'^\s*:([^\s]+)', script, re.M)}

def goto_refs(script):
    refs = []
    for ln, line in enumerate(script.splitlines(), 1):
        m = re.search(r'\bGOTO\s+([^\s]+)', line, re.I)
        if m:
            refs.append((ln, m.group(1), line.strip()))
    return refs


def report_low_item_ids():
    if not ITEM_SQL.is_file():
        print('REVIEW: data_iteminfo.sql unavailable; low ItemID cross-check skipped')
        return
    found = {}
    with ITEM_SQL.open('r', encoding='utf-8', errors='replace') as handle:
        for line in handle:
            m = re.search(r'\bVALUES\s*\(\s*(\d+)\s*,', line, re.I)
            if not m:
                m = re.match(r'^\s*\(\s*(\d+)\s*,', line)
            if not m:
                continue
            item_id = int(m.group(1))
            if item_id in (0, 1, 2, 3) and item_id not in found:
                found[item_id] = line.strip()[:240]
                if len(found) == 4:
                    break
    print('REVIEW: low ItemID rows in data_iteminfo.sql:')
    for item_id in range(4):
        print(f'  {item_id}: ' + found.get(item_id, '<not found>'))
    missing = [item_id for item_id in range(4) if item_id not in found]
    if missing:
        print('FAIL: quest corpus depends on missing low ItemIDs:', missing)
        return False
    if "'LeatherBoots'" not in found[0]:
        print('FAIL: ItemID 0 no longer resolves to the verified LeatherBoots row')
        return False
    return True

def report_low_mob_ids():
    if not MOB_SQL.is_file():
        print('REVIEW: data_mobinfo.sql unavailable; low MobID cross-check skipped')
        return
    wanted = {0, 1, 355}
    found = {}
    with MOB_SQL.open('r', encoding='utf-8', errors='replace') as handle:
        for line in handle:
            m = re.search(r'\bVALUES\s*\(\s*(\d+)\s*,', line, re.I)
            if not m:
                m = re.match(r'^\s*\(\s*(\d+)\s*,', line)
            if not m:
                continue
            mob_id = int(m.group(1))
            if mob_id in wanted and mob_id not in found:
                found[mob_id] = line.strip()[:300]
                if len(found) == len(wanted):
                    break
    print('REVIEW: selected MobID rows in data_mobinfo.sql:')
    for mob_id in sorted(wanted):
        print(f'  {mob_id}: ' + found.get(mob_id, '<not found>'))
    expected_names = {0: "'Slime'", 1: "'MushRoom'", 355: "'Mandragora'"}
    for mob_id, name in expected_names.items():
        if mob_id not in found or name not in found[mob_id]:
            print('FAIL: quest-progress MobID mapping changed:', mob_id, name)
            return False
    return True

def audit_full_sql(rows):
    stage_names = ('Start', 'Action', 'Finish')
    unresolved = []
    cross = []
    undefined = []
    ambiguous = []
    labels_total = 0
    opcodes = Counter()
    links = []
    blank_link_quests = set()
    delete_all = 0
    delete_numeric = 0
    invalid_deletes = []
    zero_item_deletes = []
    zero_item_creates = []
    if_shapes = Counter()
    invalid_ifs = []
    done_by_stage = Counter()
    start_done_without_accept = []
    action_done_quests = []
    accept_by_stage = Counter()
    explicit_accepts = []
    invalid_accepts = []
    for q, scripts in rows.items():
        stage_labels = {st:labels(s) for st,s in zip(stage_names, scripts)}
        global_labels = {}
        for st, ls in stage_labels.items():
            for label in ls:
                global_labels.setdefault(label, []).append(st)
        labels_total += sum(map(len, stage_labels.values()))
        for st, script in zip(stage_names, scripts):
            seen_accept = False
            for line in script.splitlines():
                z = line.strip()
                if not z or z.startswith(';') or z.startswith(':'):
                    continue
                parts = z.split(None, 1)
                opcode = parts[0].upper()
                opcodes[opcode] += 1
                if opcode == 'ACCEPT':
                    seen_accept = True
                    accept_by_stage[st] += 1
                    command = z.split(';', 1)[0].strip()
                    m = re.fullmatch(r'ACCEPT(?:\s+(\d+))?', command, re.I)
                    if not m:
                        invalid_accepts.append((q, st, z))
                    elif m.group(1) is not None:
                        target = int(m.group(1))
                        if target > 0xffff:
                            invalid_accepts.append((q, st, z))
                        else:
                            explicit_accepts.append((q, target))
                elif opcode == 'DONE':
                    done_by_stage[st] += 1
                    if st == 'Start' and not seen_accept:
                        start_done_without_accept.append((q, z))
                    if st == 'Action':
                        action_done_quests.append(q)
                if opcode == 'IF':
                    command = z.split(';', 1)[0].strip()
                    m = re.fullmatch(
                        r'IF\s+([A-Za-z0-9_]+)\s+(==|!=|<=|>=|<|>)\s+(-?\d+)\s+GOTO\s+([^\s]+)',
                        command, re.I)
                    if not m:
                        invalid_ifs.append((q, st, z))
                    else:
                        if_shapes[(m.group(1).upper(), m.group(2))] += 1
                elif opcode == 'LINK':
                    operand = parts[1].strip() if len(parts) > 1 else ''
                    if not operand:
                        blank_link_quests.add(q)
                    elif re.fullmatch(r'\d+', operand):
                        links.append((q, int(operand)))
                elif opcode == 'CREATE_ITEM':
                    operand = parts[1].strip() if len(parts) > 1 else ''
                    operand = operand.split(';', 1)[0].strip()
                    m = re.fullmatch(r'(\d+)\s+(\d+)', operand)
                    if m and int(m.group(1)) == 0:
                        zero_item_creates.append((q, st, z))
                elif opcode == 'DELETE_ITEM':
                    operand = parts[1].strip() if len(parts) > 1 else ''
                    # Quest source permits trailing '; ...' comments on command
                    # lines. Validate only the command operands, not the comment.
                    operand = operand.split(';', 1)[0].strip()
                    m = re.fullmatch(r'(\d+)\s+(ALL|\d+)', operand, re.I)
                    if not m:
                        invalid_deletes.append((q, st, z))
                    else:
                        item_id = int(m.group(1))
                        lot = m.group(2).upper()
                        if item_id == 0:
                            zero_item_deletes.append((q, st, z))
                            # The verified source contains exactly one zero-ID
                            # delete: Quest 244 Finish, DELETE_ITEM 0 1. Its
                            # end ItemList likewise contains enabled ItemID 0.
                            # Preserve that source anomaly rather than silently
                            # rewriting it to another item.
                            if not (q == '244' and st == 'Finish' and lot == '1'):
                                invalid_deletes.append((q, st, z))
                            else:
                                delete_numeric += 1
                        elif item_id > 0xffff:
                            invalid_deletes.append((q, st, z))
                        elif lot == 'ALL':
                            delete_all += 1
                        elif int(lot) <= 0:
                            invalid_deletes.append((q, st, z))
                        else:
                            delete_numeric += 1
            for ln, target, line in goto_refs(script):
                if target.lower() in stage_labels[st]:
                    continue
                other = [x for x in stage_names if target.lower() in stage_labels[x]]
                item = (q, st, ln, target, other, line)
                unresolved.append(item)
                (cross if other else undefined).append(item)
                if other and len(global_labels.get(target.lower(), [])) > 1:
                    ambiguous.append(item)
    if dict(opcodes) != EXPECTED:
        print('FAIL: Quest opcode corpus changed')
        print('expected:', ' '.join(f'{k}={v}' for k,v in sorted(EXPECTED.items())))
        print('actual:  ', ' '.join(f'{k}={v}' for k,v in sorted(opcodes.items())))
        q1 = rows.get('1')
        if q1:
            for i, script in enumerate(q1):
                print(f'DEBUG Quest1 stage{i+1}: {script!r}')
        return 1
    print(f'PASS: {len(rows)} quest script rows parsed')
    print(f'PASS: labels={labels_total}')
    print(f'REVIEW: unresolved GOTO/IF targets={len(unresolved)}')
    print(f'  cross-stage label references={len(cross)}')
    print(f'  no-label-anywhere references={len(undefined)}')
    print(f'  cross-stage targets with duplicate label names={len(ambiguous)}')
    print('PASS: unknown opcodes=0')
    print('PASS: exact opcode corpus counts match the supplied 2304-record source')
    if invalid_ifs:
        print('FAIL: unsupported IF syntax in supplied corpus:', invalid_ifs[:20])
        return 1
    if if_shapes != Counter(EXPECTED_IF_SHAPES):
        print('FAIL: Quest IF syntax corpus changed')
        print('expected:', EXPECTED_IF_SHAPES)
        print('actual:  ', dict(if_shapes))
        return 1
    print('PASS: IF corpus fully covered:',
          'RESULT==', if_shapes[('RESULT', '==')],
          'VAR1<', if_shapes[('VAR1', '<')],
          'RESULT<', if_shapes[('RESULT', '<')])
    if done_by_stage != Counter(EXPECTED_DONE_BY_STAGE):
        print('FAIL: DONE stage corpus changed')
        print('expected:', EXPECTED_DONE_BY_STAGE)
        print('actual:  ', dict(done_by_stage))
        return 1
    if start_done_without_accept:
        print('FAIL: Start-stage DONE without prior ACCEPT:', start_done_without_accept[:20])
        return 1
    if action_done_quests != ['2230']:
        print('FAIL: Action-stage DONE corpus changed:', action_done_quests)
        return 1
    print('PASS: DONE stage corpus = Start 351, Action 1, Finish 2251')
    print('PASS: all 351 Start-stage DONE paths have a prior ACCEPT; Action DONE is Quest 2230')
    if invalid_accepts:
        print('FAIL: malformed ACCEPT operands:', invalid_accepts[:20])
        return 1
    if accept_by_stage != Counter(EXPECTED_ACCEPT_BY_STAGE):
        print('FAIL: ACCEPT stage corpus changed')
        print('expected:', EXPECTED_ACCEPT_BY_STAGE)
        print('actual:  ', dict(accept_by_stage))
        return 1
    if len(explicit_accepts) != 22:
        print('FAIL: explicit ACCEPT operand count changed:', len(explicit_accepts))
        return 1
    cross_accepts = sorted((q, target) for q, target in explicit_accepts if int(q) != target)
    if cross_accepts != [('384', 385), ('5', 6)]:
        print('FAIL: cross-quest ACCEPT targets changed:', cross_accepts)
        return 1
    print('PASS: ACCEPT corpus = Start 2382, Finish 28, explicit targets 22')
    print('PASS: cross-quest ACCEPT targets are exactly Quest 5 -> 6 and Quest 384 -> 385')
    if report_low_item_ids() is False:
        return 1
    if report_low_mob_ids() is False:
        return 1

    if invalid_deletes:
        print('FAIL: malformed DELETE_ITEM operands:', invalid_deletes[:20])
        return 1
    if zero_item_creates != [('103', 'Start', 'CREATE_ITEM 0000 1')]:
        print('FAIL: zero-ItemID CREATE_ITEM source usage changed:', zero_item_creates)
        return 1
    print('PASS: Quest 103 preserves CREATE_ITEM 0000 1 against real ItemID 0')
    if zero_item_deletes != [('244', 'Finish', 'DELETE_ITEM 0 1')]:
        print('FAIL: zero-ItemID DELETE_ITEM source usage changed:', zero_item_deletes)
        return 1
    print('PASS: Quest 244 preserves DELETE_ITEM 0 1 against real ItemID 0')
    if delete_all + delete_numeric != EXPECTED['DELETE_ITEM']:
        print('FAIL: DELETE_ITEM operand coverage changed:',
              'ALL=', delete_all, 'numeric=', delete_numeric,
              'expected total=', EXPECTED['DELETE_ITEM'])
        return 1
    print('PASS: DELETE_ITEM operand forms valid:',
          'ALL=', delete_all, 'numeric=', delete_numeric)

    undefined_pairs = sorted({(q, target.upper()) for q, _st, _ln, target, _other, _line in undefined})
    expected_undefined = sorted({
        ('85', 'MARK100'), ('108', 'MARK100'), ('229', 'MARK100'),
        ('416', 'MARK100'), ('2313', 'MARK100'),
        ('60102', 'MARK2'), ('60108', 'MARK2'), ('60024', 'MARK2')
    })
    if undefined_pairs != expected_undefined:
        print('FAIL: undefined GOTO/IF target corpus changed')
        print('expected:', expected_undefined)
        print('actual:  ', undefined_pairs)
        return 1
    print('PASS: exactly 8 known no-label-anywhere references remain preserved')

    if len(links) != 348 or blank_link_quests != {'6', '385'}:
        print('FAIL: LINK operand corpus changed')
        print('numeric:', len(links), 'blank quests:', sorted(blank_link_quests, key=int))
        return 1
    self_links = sum(1 for q,t in links if int(q) == t)
    next_links = sum(1 for q,t in links if int(q) + 1 == t)
    missing_links = sorted((q,t) for q,t in links if str(t) not in rows)
    out_of_word = sorted((q,t) for q,t in links if t > 0xffff)
    if self_links != 24 or next_links != 253:
        print('FAIL: LINK topology changed:', 'self=', self_links, 'next=', next_links)
        return 1
    if missing_links != [('30015', 300010)] or out_of_word != [('30015', 300010)]:
        print('FAIL: LINK missing/out-of-WORD targets changed')
        print('missing:', missing_links, 'out-of-word:', out_of_word)
        return 1
    print('PASS: LINK corpus = 348 numeric + 2 blank, 253 next, 24 self')
    print('REVIEW: preserved source anomaly Quest 30015 -> LINK 300010')

    q1 = rows.get('1')
    if not (q1 and 'SAY 202 NPC' in q1[0] and 'SAY 203 NPC' in q1[0] and ':MARK1' in q1[0] and 'ACCEPT' in q1[0]):
        print('FAIL: Quest 1 Baby-Steps source structure')
        return 1
    print('PASS: Quest 1 Baby-Steps source structure')
    return 0

def main():
    if not MANIFEST.is_file():
        print('FAIL: verified Quest script corpus manifest is missing')
        return 2
    manifest_text = MANIFEST.read_text(encoding='utf-8')
    manifest = parse_manifest(manifest_text)
    manifest_source_sha = parse_manifest_source_sha(manifest_text)
    if not re.fullmatch(r'[0-9a-f]{64}', SOURCE_SHA):
        print('FAIL: configured QuestData source SHA-256 is malformed:', SOURCE_SHA)
        return 1
    if manifest_source_sha != SOURCE_SHA:
        print('FAIL: QuestData source SHA-256 provenance mismatch')
        print('expected:', SOURCE_SHA)
        print('manifest:', manifest_source_sha)
        return 1
    if manifest != EXPECTED:
        print('FAIL: Quest opcode manifest does not match the verified corpus')
        return 1
    print(f'PASS: source provenance SHA-256 = {SOURCE_SHA}')
    print('PASS: manifest source SHA-256 matches the configured provenance')
    print('PASS: manifest opcode counts match the verified source')
    try:
        rows = load_verified_corpus()
    except RuntimeError as exc:
        print(f'FAIL: {exc}')
        return 2
    if rows is not None:
        print('PASS: lossless Quest script corpus fixture decoded from zlib/base64')
        print('PASS: full 2304-record corpus is authoritative for this audit')
        return audit_full_sql(rows)
    if SQL.is_file():
        rows = parse_rows(SQL.read_bytes())
        if len(rows) != 2304:
            print(f'FAIL: expected 2304 Quest script rows, got {len(rows)}')
            return 1
        print('REVIEW: verified corpus fixture is unavailable; auditing generated SQL fallback')
        return audit_full_sql(rows)
    print('FAIL: Quest script SQL and verified lossless corpus fixture are both missing')
    return 2

if __name__ == '__main__':
    raise SystemExit(main())
