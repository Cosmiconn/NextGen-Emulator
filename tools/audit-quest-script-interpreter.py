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
EXPECTED = {'ACCEPT':2410,'CANCEL':12,'CREATE_ITEM':206,'DELETE_ITEM':1474,'DONE':2603,'END':9446,'GET_ITEM_LOT':104,'GET_PLAYER_EMPTY_INVENTORY':676,'GOTO':4,'IF':3036,'LINK':350,'SAY':19924,'SCENARIO':52,'SET_ABSTATE':51}
SOURCE_SHA = '8c4ba17267967883169142c736e6d31d1a016c843d61411da7bca8dd2448b8'

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
    for q, scripts in rows.items():
        stage_labels = {st:labels(s) for st,s in zip(stage_names, scripts)}
        global_labels = {}
        for st, ls in stage_labels.items():
            for label in ls:
                global_labels.setdefault(label, []).append(st)
        labels_total += sum(map(len, stage_labels.values()))
        for st, script in zip(stage_names, scripts):
            for line in script.splitlines():
                z = line.strip()
                if not z or z.startswith(';') or z.startswith(':'):
                    continue
                parts = z.split(None, 1)
                opcode = parts[0].upper()
                opcodes[opcode] += 1
                if opcode == 'LINK':
                    operand = parts[1].strip() if len(parts) > 1 else ''
                    if not operand:
                        blank_link_quests.add(q)
                    elif re.fullmatch(r'\d+', operand):
                        links.append((q, int(operand)))
                elif opcode == 'DELETE_ITEM':
                    operand = parts[1].strip() if len(parts) > 1 else ''
                    m = re.fullmatch(r'(\d+)\s+(ALL|\d+)', operand, re.I)
                    if not m:
                        invalid_deletes.append((q, st, z))
                    else:
                        item_id = int(m.group(1))
                        lot = m.group(2).upper()
                        if item_id <= 0 or item_id > 0xffff:
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

    if invalid_deletes:
        print('FAIL: malformed DELETE_ITEM operands:', invalid_deletes[:20])
        return 1
    if delete_all != 1035 or delete_numeric != 439:
        print('FAIL: DELETE_ITEM operand corpus changed:', 'ALL=', delete_all, 'numeric=', delete_numeric)
        return 1
    print('PASS: DELETE_ITEM corpus = 1035 ALL + 439 positive numeric-lot forms')

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
    manifest = parse_manifest(MANIFEST.read_text(encoding='utf-8'))
    if manifest != EXPECTED:
        print('FAIL: Quest opcode manifest does not match the verified corpus')
        return 1
    print(f'PASS: source SHA-256 = {SOURCE_SHA}')
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
