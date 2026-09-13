# Quest runtime: `CQuest::QuestPlayer_TimeProcess` binary cross-reference

## Source

Original `Zone.exe` / `Zone.pdb` from the supplied original-server archive.

PDB public symbol:

`?QuestPlayer_TimeProcess@CQuest@@UAEHXZ`

PDB address record resolves to `.text` offset `0x22FE50`; with the PE `.text` VMA `0x00401000`, the function VA is:

`0x00630E50`

## Proven function behavior

`CQuest::QuestPlayer_TimeProcess()` maintains time-based quest progress for active player quests.

The function first calls the original Unix-time helper at `0x0065910B`. The returned current Unix time is compared with the cached quest-manager time at `CQuest + 0x14`.

If unchanged, the function returns `0` immediately.

If changed:

1. `currentTime - oldTime` is computed as a **WORD-sized delta**.
2. `CQuest + 0x14` is updated to the current Unix time.
3. The function iterates the `PLAYER_QUEST_INFO` array (`CQuest + 0x10`) using `CQuest + 0x08` as the entry count and a stride of `0x20` bytes.
4. Only entries whose status byte at `+0x02` equals `6` (`PQS_ING`) are processed.
5. The quest definition is resolved from the entry QuestID at `+0x00` via `0x00635FF0`.
6. The embedded `QUEST_END_CONDITION` must have `bTimeLimit == 1`.
7. The runtime WORD at `PLAYER_QUEST_INFO + 0x1E` is compared with `QUEST_END_CONDITION::TimeLimit`.
8. If the runtime value is below the definition value, the elapsed-time delta is added to `+0x1E`.
9. The resulting value is passed, together with QuestID and the definition limit, through the quest object's virtual callback at vtable slot `+0x4C`.
10. The quest status is then recalculated through the internal status routine at `0x00630510`. If the resulting status is `8`, vtable slot `+0x50` is called; if it is `7`, vtable slot `+0x54` is called. The exact public symbol names of those virtual callbacks are not asserted here.

## Exact quest-definition cross-reference

The PDB `QUEST_DATA::QUEST_END_CONDITION` field list identifies:

- `bTimeLimit` at `QUEST_END_CONDITION + 0x64`
- `TimeLimit` at `QUEST_END_CONDITION + 0x66`
- `QUEST_END_CONDITION` is embedded in `QUEST_DATA` at `+0x58` for this binary layout.

Therefore the native accesses in `QuestPlayer_TimeProcess` map exactly to:

- `QUEST_DATA + 0xBC` = `QUEST_END_CONDITION::bTimeLimit`
- `QUEST_DATA + 0xBE` = `QUEST_END_CONDITION::TimeLimit`

This resolves the previously unknown `+0xBC/+0xBE` definition fields.

## `PLAYER_QUEST_INFO` field confirmed by this function

| Offset | Width | Proven meaning |
|---|---:|---|
| `+0x00` | WORD | QuestID |
| `+0x02` | BYTE | QuestStatus; `6` is `PQS_ING` |
| `+0x1E` | WORD | **time-based running-time counter**, incremented by elapsed Unix-time seconds and bounded by `QUEST_END_CONDITION::TimeLimit` |

The earlier identification of `+0x1E` as an unknown field is therefore obsolete.

## Important distinction

`+0x1E` is **not** the general quest progress byte at `+0x17`.

The separately proven `CQuest::SetQuestProgress(WORD nID, BYTE ProgressStep)` writes the script/event progress byte at `PLAYER_QUEST_INFO + 0x17`.

`QuestPlayer_TimeProcess()` instead updates the WORD at `+0x1E` from elapsed Unix time. The binary evidence therefore establishes two distinct runtime concepts:

- `+0x17`: quest/event progress step (`BYTE`)
- `+0x1E`: time-based running-time counter (`WORD`) for quests with `QUEST_END_CONDITION::bTimeLimit == 1`

## Boundary behavior

The time-processing path only adds elapsed time while the runtime counter is **strictly below** `TimeLimit`.

The function then invokes the quest callback with the updated counter and the configured limit. Status handling after that callback is performed by the existing quest-status machinery; this document does not rename the two virtual callbacks without direct symbol proof.

## Current status

**PROVEN:** `QuestPlayer_TimeProcess` address, Unix-time source, cached-time update, `PLAYER_QUEST_INFO` iteration, active-status gate, QuestID resolution, `QUEST_END_CONDITION::bTimeLimit`, `QUEST_END_CONDITION::TimeLimit`, `+0x1E` time-counter semantics, limit boundary, and status-transition callback path.

**UNRESOLVED:** exact public symbol names represented by the two virtual callbacks at vtable `+0x50/+0x54`.
