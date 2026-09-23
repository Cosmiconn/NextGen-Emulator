# Quest Type-3 corpus audit

## Scope

This note measures how the proven `QUEST_DATA.Type == 3` special branch in
`CQuest::GetQuestStatusWithNPC` affects the supplied NA2016 QuestData corpus.
It does not assign a semantic name to quest type 3.

## Corpus result

The supplied 2304-record QuestData corpus contains exactly **116** quests with
`Type = 3`. Of those, **15** are marked repeatable.

NPC-bound Type-3 quests occur on only eight start-NPC IDs:

- 27
- 86
- 87
- 98
- 112
- 146
- 148
- 149

Every one of those eight NPC IDs also hosts quests whose `Type` is not 3.
The observed type sets are:

| NPC ID | Quest types present |
|---:|---|
| 27 | 0, 1, 2, 3, 10 |
| 86 | 0, 1, 2, 3 |
| 87 | 0, 1, 2, 3 |
| 98 | 0, 1, 2, 3, 7, 10 |
| 112 | 0, 2, 3, 10 |
| 146 | 0, 1, 2, 3 |
| 148 | 0, 1, 2, 3, 5, 10 |
| 149 | 0, 1, 2, 3 |

Therefore the native Type-3 special comparison is observable in real
multi-type NPC candidate sets; it is not a theoretical edge case.

## Exact native branch

Direct disassembly of the matching original `Zone.exe` resolves the branch at
`CQuest::GetQuestStatusWithNPC` `0x0063103A..0x00631060`:

- candidate `Type == 3`, retained type != 3 -> candidate replaces retained;
- candidate `Type == 3`, retained `Type == 3` -> candidate is rejected;
- candidate type != 3, retained `Type == 3` -> candidate is rejected;
- only when neither side is Type 3 does selection continue to the
  `Start.bQuest`, `Start.bItem`, `Repeatable` and final type-priority predicates.

So Type 3 has an explicit early dominance rule; it is not merely winning because
`m_QuestTypePriority[3] == 0`.

## Runtime consequence

`QuestNpcStartResolver.TryCompareOriginal` now implements this branch exactly.
The former Type-3 legacy fallback has been removed.

## Evidence boundary

Proven:

- `QUEST_DATA +0x11 = Type`.
- the original selection routine compares that byte with literal 3 before the
  remaining equal-status predicates and final type-priority lookup.
- the exact type-priority table gives Type 3 priority 0.
- the corpus counts and NPC/type mixtures above.

Unresolved:

- the source-level semantic name of Type 3. The comparison behavior itself is
  now resolved and implemented.
