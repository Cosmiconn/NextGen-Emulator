#!/usr/bin/env python3
"""Audit the normalized QuestScript corpus without inventing semantics."""
import os
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL = Path(os.environ.get("QUEST_SCRIPT_SQL", ROOT / "sql/data/data_quest_script_data.sql"))


def parse_rows(text):
    # Keep CR bytes inside script literals intact; universal-newline conversion
    # would otherwise create false corpus mismatches.
    pat = re.compile(
        rb"\((\d+),\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)'\)"
    )
    rows = {}
    for m in pat.finditer(text):
        vals = [x.replace(b"''", b"'") for x in m.groups()[1:]]
        rows[m.group(1).decode()] = [x.decode("utf-8", "replace") for x in vals]
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


def main():
    if not SQL.is_file():
        print(f"FAIL: required Quest script corpus is missing: {SQL}")
        return 2
    rows = parse_rows(SQL.read_bytes())
    if len(rows) != 2304:
        print(f"FAIL: expected 2304 Quest script rows, got {len(rows)}")
        return 1

    stage_names = ('Start', 'Action', 'Finish')
    unresolved = []
    cross = []
    undefined = []
    ambiguous = []
    labels_total = 0
    opcodes = Counter()

    for q, scripts in rows.items():
        stage_labels = {st: labels(s) for st, s in zip(stage_names, scripts)}
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
                op = z.split(None, 1)[0].upper()
                opcodes[op] += 1

            for ln, target, line in goto_refs(script):
                if target.lower() in stage_labels[st]:
                    continue
                other = [x for x in stage_names if target.lower() in stage_labels[x]]
                item = (q, st, ln, target, other, line)
                unresolved.append(item)
                (cross if other else undefined).append(item)
                if other and len(global_labels.get(target.lower(), [])) > 1:
                    ambiguous.append(item)

    expected = {
        'ACCEPT': 2410,
        'CANCEL': 12,
        'CREATE_ITEM': 206,
        'DELETE_ITEM': 1474,
        'DONE': 2603,
        'END': 9446,
        'GET_ITEM_LOT': 104,
        'GET_PLAYER_EMPTY_INVENTORY': 676,
        'GOTO': 4,
        'IF': 3036,
        'LINK': 350,
        'SAY': 19924,
        'SCENARIO': 52,
        'SET_ABSTATE': 51,
    }
    if dict(opcodes) != expected:
        print('FAIL: Quest opcode corpus changed')
        print('expected:', ' '.join(f'{k}={v}' for k, v in sorted(expected.items())))
        print('actual:  ', ' '.join(f'{k}={v}' for k, v in sorted(opcodes.items())))
        return 1

    print(f'PASS: {len(rows)} quest script rows parsed')
    print(f'PASS: labels={labels_total}')
    print(f'REVIEW: unresolved GOTO/IF targets={len(unresolved)}')
    print(f'  cross-stage label references={len(cross)}')
    print(f'  no-label-anywhere references={len(undefined)}')
    print(f'  cross-stage targets with duplicate label names={len(ambiguous)}')
    print('PASS: unknown opcodes=0')
    print('PASS: exact opcode corpus counts match the supplied 2304-record source')

    if cross:
        print('Cross-stage references:')
        for item in cross:
            print(' ', item)
    if undefined:
        print('Undefined-anywhere references:')
        for item in undefined:
            print(' ', item)

    q1 = rows.get('1')
    if q1 and 'SAY 202 NPC' in q1[0] and 'SAY 203 NPC' in q1[0] and ':MARK1' in q1[0] and 'ACCEPT' in q1[0]:
        print('PASS: Quest 1 Baby-Steps source structure')
    else:
        print('FAIL: Quest 1 Baby-Steps source structure')
        return 1
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
