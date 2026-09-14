# Quest priority initialization audit

## Scope

Step 26 re-ran the previous priority audit with the newly supplied original Zone server binaries. This closes the Step 25 evidence gap: the initialized contents of `CQuest::m_QuestStatusPriority` and `CQuest::m_QuestTypePriority` are present in the `CQuest::CQuest` constructor.

The work remains clean-room and evidence-driven. No priority was inferred from enum order or quest names.

## Binary identity

The supplied Zone variants are byte-identical:

- `Zone.exe`
- `Zone 2.exe`
- `Zone 3.exe`
- `Zone 4.exe`
- `Zone 5.exe`

SHA-256 of each: `db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5`.

The supplied `Zone.pdb` copies are also byte-identical. SHA-256: `569a9d3ee6c4478b8e96d176f15ce64a9aab3f42e52d29d17c3c4d5af8f9ec6c`.

The PDB symbol information identifies the `CQuest` constructor and the field layout used below.

## Exact initialized status priorities

`PLAYER_QUEST_STATUS` has 21 indexed values (`0..20`), with `PQS_MAX_PLAYER_QUEST_STATUS = 21`.

`m_QuestStatusPriority` is an 84-byte array at object offset `0x24`, i.e. 21 x 32-bit values.

The `CQuest::CQuest` constructor writes these exact values:

| Status index | Priority |
|---:|---:|
| 0 | 22 |
| 1 | 21 |
| 2 | 21 |
| 3 | 21 |
| 4 | 21 |
| 5 | 2 |
| 6 | 1 |
| 7 | 2 |
| 8 | 0 |
| 9 | 21 |
| 10 | 21 |
| 11 | 21 |
| 12 | 21 |
| 13 | 21 |
| 14 | 21 |
| 15 | 21 |
| 16 | 21 |
| 17 | 21 |
| 18 | 21 |
| 19 | 21 |
| 20 | 2 |

The constructor instruction sequence is at `Zone.exe` VA `0x006301d0` (the `CQuest::CQuest` symbol is in the Quest module). Relevant writes are to `this+0x24` through `this+0x74` in 4-byte steps.

## Exact initialized quest-type priorities

The quest-type enum has 11 usable values (`0..10`) followed by `QT_MAX_QUEST_TYPE = 11`.

`m_QuestTypePriority` is a 44-byte array at object offset `0x78`, i.e. 11 x 32-bit values.

The constructor writes:

| Quest type index | Priority |
|---:|---:|
| 0 | 5 |
| 1 | 11 |
| 2 | 1 |
| 3 | 0 |
| 4 | 11 |
| 5 | 3 |
| 6 | 2 |
| 7 | 2 |
| 8 | 2 |
| 9 | 4 |
| 10 | 11 |

These are direct binary writes, not inferred values.

## Proven comparison direction and dimensions

The `CQuest::GetQuestStatusWithNPC` implementation contains the actual priority comparisons.

For the status-priority comparison it performs an indexed load from `this + 0x24 + status * 4`, compares the candidate value against the currently retained value, and rejects the candidate when the candidate priority is greater than or equal to the current priority. Therefore **smaller numeric priority wins**.

When the status priorities are equal, the function evaluates several additional candidate fields before reaching the quest-type comparison. Those additional predicates are not assigned semantic names here because the supplied evidence does not prove their meaning.

The final quest-type comparison performs indexed loads from `this + 0x78 + type * 4` and likewise rejects the candidate when its type-priority value is greater than or equal to the retained candidate's value. Therefore **smaller numeric quest-type priority wins** at that comparison stage.

This proves that the original selection logic has two explicit priority dimensions:

1. status priority first;
2. quest-type priority later, after the other equal-status tie-break predicates.

It does **not** prove that status and type priorities are numerically added together or form a single scalar score. They are compared at separate points in the original control flow.

## Relevant disassembly evidence

`CQuest::CQuest` (`Zone.exe`, VA `0x006301d0`) contains the direct initialization:

- `movl $0x16, 0x24(%esi)` -> status index 0 = 22
- `movl $0x15, ...` at offsets `0x28..0x34` -> indices 1..4 = 21
- `movl $0x2, 0x38(%esi)` -> index 5 = 2
- `movl $0x1, 0x3c(%esi)` -> index 6 = 1
- `movl $0x2, 0x40(%esi)` -> index 7 = 2
- `movl $0x0, 0x44(%esi)` -> index 8 = 0
- `movl $0x15, ...` at offsets `0x48..0x70` -> indices 9..19 = 21
- `movl $0x2, 0x74(%esi)` -> index 20 = 2
- `movl $0x5, 0x78(%esi)` -> quest type 0 = 5
- `movl $0xb, 0x7c(%esi)` -> type 1 = 11
- `movl $0x1, 0x80(%esi)` -> type 2 = 1
- `movl $0x0, 0x84(%esi)` -> type 3 = 0
- `movl $0xb, 0x88(%esi)` -> type 4 = 11
- `movl $0x3, 0x8c(%esi)` -> type 5 = 3
- `movl $0x2, 0x90(%esi)` -> type 6 = 2
- `movl $0x2, 0x94(%esi)` -> type 7 = 2
- `movl $0x2, 0x98(%esi)` -> type 8 = 2
- `movl $0x4, 0x9c(%esi)` -> type 9 = 4
- `movl $0xb, 0xa0(%esi)` -> type 10 = 11

The relevant `CQuest::GetQuestStatusWithNPC` code is at `Zone.exe` VA `0x00630f60`. The status-priority indexed comparisons are at approximately `0x00630fd9`, and the quest-type indexed comparison is at approximately `0x006310d3`.

## Consequence for implementation

The Step 25 prohibition on invented priority values is now removed. The exact priority tables and comparison direction are proven from the original binary.

However, `QuestNpcStartResolver` must **not** simply sort SQL candidates by these two values. The original routine has additional equal-status predicates (visible in the same `GetQuestStatusWithNPC` control flow) and operates on character/quest state. Those predicates still need to be mapped to the emulator's character quest state before the full NPC selection behavior can be reproduced.

Therefore:

- the exact priority constants are now safe to encode as source-level constants;
- lower priority numbers win;
- status priority precedes quest-type priority;
- the remaining equal-priority predicates stay `UNRESOLVED` until their corresponding fields are reconstructed.

No change is made here to `Handler8` or `QuestNpcStartResolver` selection behavior solely from these constants.

## Next target

The next useful reconstruction is the input/output contract around `CQuest::GetQuestStatusWithNPC` and the equal-status predicates. In particular, map its raw candidate fields to the proven `QUEST_DATA` / player quest state structures and then reproduce the selection path without collapsing distinct conditions into guessed semantics.
