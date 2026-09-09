# `GetNewQuestStatus` — Step 32 caller and machine-code audit

## Scope

Step 32 follows direct executable call sites of the two procedures associated by the PDB with `CQuest::GetNewQuestStatus` and compares the actual call arguments with the PDB-declared parameter types.

## Direct call sites found in `Zone.exe`

A full-text disassembly finds these direct calls:

| Call site | Target | Observed argument construction |
|---:|---:|---|
| `0x005BDA57` | `0x0062F320` | local 32-byte player-quest record at `[ebp-0x30]`; quest ID copied from an existing player quest; status byte explicitly set to `4` |
| `0x005BE9F4` | `0x0062F320` | local 32-byte player-quest record at `[ebp-0x130]`; status byte explicitly set to `6` |
| `0x005BEAF4` | `0x0062F320` | same local player-quest record shape; status byte explicitly set to `4` |
| `0x005BF34A` | `0x0062F320` | existing player-quest record returned by the quest-list lookup; status byte explicitly set to `4` before the call |
| `0x005BF43B` | `0x0062F4B0` | one unsigned-short quest ID pushed as the sole explicit argument |

The `0x62F4B0` call site therefore agrees directly with the PDB type graph for the unsigned-short overload.

## `0x0062F4B0` — ID-taking routine

The machine code beginning at `0x0062F4B0` is a conventional callable routine:

1. receives the explicit quest ID from `[ebp+8]`;
2. searches the player's quest list by that ID;
3. resolves quest data through the zone quest-data lookup;
4. if the quest data has byte `+0x12 != 0`, changes the player quest status to raw value `4` and clears the auxiliary fields;
5. otherwise removes the matching quest-list entry through `0x0062F350`;
6. returns `1` for the handled paths and `0` when the quest entry/data cannot be resolved.

The raw status value `4` is already independently proven as `PQS_REPEAT`.

The direct caller at `0x005BF43B` does **not** consume the return value; control jumps immediately to the common continuation at `0x005BF4CB`.

## `0x0062F320` — parameter-type contradiction

The PDB TPI linkage from Step 31 identifies the first overload as:

`CQuest::GetNewQuestStatus(QUEST_DATA*) -> PLAYER_QUEST_STATUS`

However, the executable callers provide a strong machine-code contradiction to that declared parameter type.

At `0x005BE9F4`, `0x005BEAF4`, and `0x005BDA57`, the caller constructs/copies a compact 32-byte **player-quest record** and passes its address. The record layout used immediately around the call is:

- `+0x00`: quest ID (`unsigned short`)
- `+0x02`: player quest status byte, explicitly initialized to `6` or `4`
- `+0x03..`: auxiliary player-quest fields
- total copied size: `8 * 4 = 32` bytes

The called routine at `0x0062F320` then directly treats its argument as that same compact player-quest record:

- writes status at `+0x02`;
- clears auxiliary fields at `+0x03`, `+0x07`, `+0x0B`, `+0x0F`, `+0x13`, `+0x17`, `+0x1B`;
- clears byte `+0x1F`;
- on the special existing-entry path, restores two generated values into `+0x03` and `+0x07`;
- on the append path, copies exactly 32 bytes into the quest-list entry.

This is incompatible with treating the passed object as a full `QUEST_DATA` instance: the surrounding code demonstrably uses the argument as a 32-byte player-quest state record.

## What is proven vs. unresolved

### Proven

- The PDB contains two exact `CQuest::GetNewQuestStatus` procedure records.
- TPI resolves one as `QUEST_DATA*` and one as `unsigned short`, both returning `PLAYER_QUEST_STATUS`.
- The ID-taking routine's callable machine-code entry is `0x0062F4B0` and its explicit argument is an unsigned-short quest ID.
- The other routine at `0x0062F320` operates on a 32-byte player-quest record in all observed direct call sites.
- Raw status `6` is `PQS_ING` and raw status `4` is `PQS_REPEAT` from independent evidence.

### UNRESOLVED

The exact reason the PDB declares the `0x0062F320` overload with `QUEST_DATA*` while the executable's direct callers and callee machine code use a player-quest record is not yet established. Possible explanations include stale/inaccurate debug type information, symbol/procedure-record provenance issues, or a type-definition mismatch in the original build. None is selected without additional evidence.

Likewise, the second PDB procedure record's reported start at `0x0062F4A0` remains a binary layout/provenance anomaly: the normal callable prologue is at `0x0062F4B0`.

## Consequence for the emulator

No normalized C# signature or quest-status transition table is added from the contradiction. Runtime behavior remains unchanged. The safe reconstruction target is the **observed player-quest state mutation**, while the original source-level type identity remains UNRESOLVED.
