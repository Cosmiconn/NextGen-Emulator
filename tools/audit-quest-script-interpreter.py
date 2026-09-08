#!/usr/bin/env python3
"""Audit normalized QuestScript corpus without inventing script semantics."""
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL = ROOT / 'sql/data/data_quest_script_data.sql'


def parse_rows(text):
    pat = re.compile(r"\((\d+),\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)',\s*'((?:''|[^'])*)'\)", re.S)
    return {m.group(1): [x.replace("''", "'") for x in m.groups()[1:]] for m in pat.finditer(text)}


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
    rows = parse_rows(SQL.read_text(errors='replace'))
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

    print(f'PASS: {len(rows)} quest script rows parsed')
    print(f'PASS: labels={labels_total}')
    print(f'REVIEW: unresolved GOTO/IF targets={len(unresolved)}')
    print(f'  cross-stage label references={len(cross)}')
    print(f'  no-label-anywhere references={len(undefined)}')
    print(f'  cross-stage targets with duplicate label names={len(ambiguous)}')

    known = {
        'ACCEPT', 'CANCEL', 'CREATE_ITEM', 'DELETE_ITEM', 'DONE', 'END',
        'GET_ITEM_LOT', 'GET_PLAYER_EMPTY_INVENTORY', 'GOTO', 'IF', 'LINK',
        'SAY', 'SCENARIO', 'SET_ABSTATE'
    }
    unknown = set(opcodes) - known
    print('PASS: unknown opcodes=0' if not unknown else f'REVIEW: unknown opcodes={sorted(unknown)}')
    print('Opcode counts:', ' '.join(f'{k}={v}' for k, v in sorted(opcodes.items())))

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
    return 1 if unknown else 0


if __name__ == '__main__':
    raise SystemExit(main())
