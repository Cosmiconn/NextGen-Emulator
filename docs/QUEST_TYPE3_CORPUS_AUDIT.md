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

## Runtime consequence

`QuestNpcStartResolver.TryCompareOriginal` deliberately returns unresolved
when equal status priority reaches a comparison between different quest types
and either side is Type 3. The caller then uses the legacy interaction path.

This fallback is retained until the exact Type-3 branch at
`CQuest::GetQuestStatusWithNPC` is reconstructed. Replacing it with ordinary
`m_QuestTypePriority` ordering would be a guess and could select a different
quest from the original server on all eight NPC families above.

## Evidence boundary

Proven:

- `QUEST_DATA +0x11 = Type`.
- the original selection routine compares that byte with literal 3 before the
  remaining equal-status predicates and final type-priority lookup.
- the exact type-priority table gives Type 3 priority 0.
- the corpus counts and NPC/type mixtures above.

Unresolved:

- the source-level semantic name of Type 3.
- the exact replacement/rejection rule in the special branch.
