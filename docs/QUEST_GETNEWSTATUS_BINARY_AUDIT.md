# `GetNewQuestStatus` — Step 31 exact PDB type linkage

## New result

The ambiguity from Step 30 is reduced by decoding the **actual S_GPROC32 records** in Quest module symbol stream 330 and resolving their CodeView function-type indices through TPI stream 2.

Two `S_GPROC32` records containing the exact name `CQuest::GetNewQuestStatus` were found:

| PDB symbol offset | record | CodeOffset | CodeSize | FunctionType |
|---:|---|---:|---:|---:|
| `0x2048` | `S_GPROC32` | `0x22F320` | `0x175` | `0x4B32` |
| `0x2198` | `S_GPROC32` | `0x22F4A0` | `0x28` | `0x4B31` |

With Zone.exe image base/text mapping this corresponds to `.text` addresses `0x0062F320` and `0x0062F4A0`.

## Exact function types

TPI index `0x4B31` is `LF_MFUNCTION`:

- return type: `0x1030` = `PLAYER_QUEST_STATUS`
- class type: `0x1B6E` = `CQuest`
- this type: `0x4AD8` = pointer to `CQuest`
- one parameter
- argument list `0x10B7`
- argument list contains type `0x21` = unsigned short

Therefore `CQuest::GetNewQuestStatus(unsigned short)` is directly proven by the PDB type graph.

TPI index `0x4B32` is `LF_MFUNCTION`:

- return type: `0x1030` = `PLAYER_QUEST_STATUS`
- class type: `0x1B6E` = `CQuest`
- this type: `0x4AD8` = pointer to `CQuest`
- one parameter
- argument list `0x4B2E`
- argument list contains type `0x465A`
- `0x465A` is `LF_POINTER` to `0x4659`
- `0x4659` is the `QUEST_DATA` UDT

Therefore `CQuest::GetNewQuestStatus(QUEST_DATA*)` is directly proven by the PDB type graph.

## Machine-code behavior of `0x0062F320`

The full PDB-recorded code span is `0x175` bytes, ending at `0x0062F495`.

The routine operates on the player quest record supplied as its argument and contains several distinct paths. Confirmed state writes include:

- status byte `+0x02 = 6` in the existing-entry path;
- clearing auxiliary fields `+0x03, +0x07, +0x0B, +0x0F, +0x13, +0x17, +0x1B` and byte `+0x1F`;
- on a newly appended entry, the temporary record is initialized with status byte `+0x02 = 6` and then copied into the quest list;
- successful paths return integer `1`; failure/full-list paths return `0`.

The original DB independently proves raw status `6 = PQS_ING` / in-progress. Thus the write of status `6` is safe to identify as the already-proven in-progress state.

The function's complete mapping from its `QUEST_DATA*` input to every return/status transition is not yet normalized into SQL semantics.

## Machine-code behavior around `0x0062F4A0`

The second PDB record has `CodeOffset=0x22F4A0` and `CodeSize=0x28`. The executable contains a short shared/tail region immediately before the independently obvious prologue at `0x0062F4B0`, followed by the quest-ID lookup/state logic documented in Step 30.

The callable code at `0x0062F4B0`:

- receives a quest ID;
- searches the player quest list for that ID;
- resolves quest data;
- if the quest's `QUEST_DATA +0x12` is non-zero, writes player status `4` and clears auxiliary fields;
- otherwise removes the matching quest entry via `0x0062F350`;
- returns `1` on the handled paths and `0` when the entry/quest data cannot be resolved.

Raw status `4 = PQS_REPEAT` remains independently proven by the World DB evidence.

### Remaining address-layout anomaly

The PDB says the second `S_GPROC32` code range starts at `0x0062F4A0`, while the obvious callable prologue for the ID-taking routine is at `0x0062F4B0` and its useful body continues further. This is recorded as a **binary layout anomaly**, not silently corrected. The exact relationship between the PDB range and the callable body still requires further linkage analysis (for example linker tail-sharing/record provenance). No semantic conclusion is based on the address discrepancy.

## Status conclusion

The major Step-30 uncertainty is narrowed substantially:

- both overloads are now tied to exact `S_GPROC32` records;
- their `PLAYER_QUEST_STATUS` return type is proven from TPI;
- the first overload's parameter is proven as `QUEST_DATA*`;
- the second overload's parameter is proven as `unsigned short`;
- raw status writes `6` and `4` are independently mapped to the already-proven quest states.

The **complete `GetNewQuestStatus` decision table remains UNRESOLVED** until the remaining symbol/code-range anomaly and all callers/return-value consumers are reconciled.

No runtime code is changed by this audit.
