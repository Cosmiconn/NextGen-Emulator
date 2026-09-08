#!/usr/bin/env python3
import re
from pathlib import Path
SQL = Path(__file__).resolve().parents[1] / 'sql/data/data_quest_script_data.sql'
text = SQL.read_text(encoding='utf-8')
rows = re.findall(r"\((\d+), '(.*?)', '(.*?)', '(.*?)'\)(?:,|;)", text, re.S)
if len(rows) != 2304: raise SystemExit(f'FAIL: expected 2304 quest scripts, found {len(rows)}')
known = {'SAY','IF','END','ACCEPT','DONE','DELETE_ITEM','GET_PLAYER_EMPTY_INVENTORY','LINK','CREATE_ITEM','GET_ITEM_LOT','SCENARIO','SET_ABSTATE','CANCEL','GOTO'}
unknown, invalid_goto = [], []
labels_count, opcode_counts = 0, {}
for qid, *scripts in rows:
    for script in scripts:
        labels=set(); lines=script.replace('\r\n','\n').replace('\r','\n').split('\n')
        for raw in lines:
            line=raw.strip()
            if not line or line.startswith(';'): continue
            if line.startswith(':'): labels.add(line[1:].strip()); labels_count+=1; continue
            op=line.split()[0].upper(); opcode_counts[op]=opcode_counts.get(op,0)+1
            if op not in known: unknown.append((qid,line))
        for lineno,raw in enumerate(lines,1):
            line=raw.strip()
            if not line or line.startswith(';'): continue
            parts=line.split(); op=parts[0].upper(); upper=[x.upper() for x in parts]
            if op=='GOTO' and len(parts)>=2 and parts[1] not in labels: invalid_goto.append((qid,lineno,line))
            elif op=='IF' and 'GOTO' in upper:
                i=upper.index('GOTO')
                if i+1>=len(parts) or parts[i+1] not in labels: invalid_goto.append((qid,lineno,line))
q1=next(r for r in rows if r[0]=='1')
assert 'SAY 202 NPC' in q1[1] and 'SAY 203 NPC' in q1[1]
assert 'IF RESULT == 1 GOTO MARK1' in q1[1] and ':MARK1' in q1[1]
print(f'PASS: {len(rows)} quest script rows parsed')
print(f'PASS: labels={labels_count}')
print(f'REVIEW: unresolved GOTO/IF targets={len(invalid_goto)} (source-data anomalies, not silently repaired)')
print(f'PASS: unknown opcodes={len(unknown)}')
print('Opcode counts:', ' '.join(f'{k}={v}' for k,v in sorted(opcode_counts.items())))
if unknown: raise SystemExit(1)
print('PASS: Quest 1 Baby-Steps control-flow smoke test')
