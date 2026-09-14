# `GetNewQuestStatus` — Step 32 caller and machine-code audit

## Scope

Step 32 follows direct executable call sites of the two procedures associated by the PDB with `CQuest::GetNewQuestStatus` and compares the actual call arguments and return behavior with the PDB-declared parameter/return types.

## Direct call sites found in `Zone.exe`

| Call site | Target | Observed argument construction |
|---:|---:|---|
| `0x005BDA57` | `0x0062F320` | local 32-byte player-quest record; quest ID copied from an existing player quest; status byte explicitly set to `4` |
| `0x005BE9F4` | `0x0062F320` | local 32-byte player-quest record; status byte explicitly set to `6` |
| `0x005BEAF4` | `0x0062F320` | same local player-quest record shape; status byte explicitly set to `4` |
| `0x005BF34A` | `0x0062F320` | existing player-quest record returned by quest-list lookup; status byte explicitly set to `4` before the call |
| `0x005BF43B` | `0x0062F4B0` | one unsigned-short quest ID pushed as the sole explicit argument |

The `0x62F4B0` call site agrees with the PDB's unsigned-short parameter type.

## New exact return-behavior finding

The machine code provides an additional contradiction to the PDB return type `PLAYER_QUEST_STATUS` for both PDB-associated procedures.

### `0x0062F320`

The routine begins by loading its argument into `EAX`:

- `[ebp+8] -> EAX`
- null argument: `ECX` is zeroed, the null branch returns with `EAX == 0`;
- non-null argument: the routine clears fields in the supplied record and returns without replacing `EAX`.

Therefore the observed return register on the non-null path remains the **original record pointer**, not a quest-status enum. The callers examined here ignore that return value.

### `0x0062F4B0`

The callable body at `0x0062F4B0` returns only raw `1` or `0`:

- handled repeat/remove paths set `EAX = 1`;
- unresolved entry/quest-data paths set `EAX = 0`.

The direct caller at `0x005BF43B` does not consume the return value.

Thus the executable behavior does not support interpreting either observed callable body's return register as `PLAYER_QUEST_STATUS`.

## `0x0062F320` — actual record behavior

The routine directly operates on a compact 32-byte player-quest record:

- `+0x00`: QuestID (`unsigned short` observed at all relevant callers)
- `+0x02`: status byte
- `+0x03..+0x1F`: auxiliary state

It clears:

- `+0x17` byte
- `+0x18` DWORD
- `+0x1C` byte
- low two bits of `+0x1D`
- `+0x1E` WORD

Other callers show the same record's additional fields being populated and copied in 32-byte units.

## `0x0062F4B0` — actual ID-taking routine

The machine code:

1. receives the explicit quest ID from `[ebp+8]`;
2. searches the player's quest list by that ID, with 32-byte entries;
3. resolves quest data through `0x635FF0`;
4. if quest-data byte `+0x12 != 0`, writes player status `4` and clears auxiliary fields;
5. otherwise removes the matching quest-list entry through `0x62F350`;
6. returns `1` for handled paths and `0` for unresolved paths.

Raw status `4 = PQS_REPEAT` remains independently proven.

## PDB range overlap is broader than the original anomaly

The exact PDB procedure records around this cluster are:

| PDB name | CodeOffset | CodeSize | Machine-code observation |
|---|---:|---:|---|
| `SetQuestDone` | `0x22F2B0` | `0x62` | range ends at `0x62F311`, but a distinct callable prologue exists at `0x62F2E0` |
| `GetNewQuestStatus` | `0x22F320` | `0x175` | range spans several distinct callable prologues (`0x62F320`, `0x62F350`, `0x62F3B0`) and ends inside the following procedure cluster |
| `GetNewQuestStatus` | `0x22F4A0` | `0x28` | starts on the tail/epilogue of the preceding code and contains the prologue at `0x62F4B0`, but the callable body continues to `0x62F544` |
| `IsDoingQuest` | `0x22F4D0` | `0x3E` | overlaps the `0x62F4B0` callable body |
| `DoingQuestUpdateStatus` | `0x22F510` | `0x55` | begins inside the `0x62F4B0` callable body |

This establishes that the issue is not isolated to the second `GetNewQuestStatus` record. The PDB procedure ranges in this optimized cluster do not form a one-function/one-contiguous-machine-range mapping.

## What is proven vs. unresolved

### Proven

- The PDB contains two exact `CQuest::GetNewQuestStatus` records.
- TPI resolves their declared types as `QUEST_DATA*` and `unsigned short`, both returning `PLAYER_QUEST_STATUS`.
- The executable callers of `0x0062F320` pass a 32-byte player-quest record.
- `0x0062F320` mutates that record and does not return a `PLAYER_QUEST_STATUS`; on the observed non-null path its original argument pointer remains in `EAX`.
- `0x0062F4B0` receives an unsigned-short quest ID and returns only `1`/`0` in the observed machine code.
- The surrounding PDB procedure ranges overlap or cross independently callable machine-code regions.

### UNRESOLVED

The exact origin of the PDB/source-symbol mismatch remains unresolved. The evidence does not justify choosing among stale debug information, linker/optimizer procedure attribution, or another build-time provenance issue.

No source-level function name is reassigned solely from these observations.

## Consequence for the emulator

No normalized C# signature, quest-status transition table, or runtime behavior is added from the PDB contradiction. The safe reconstruction target remains the observed player-quest state mutation and quest-list behavior.
