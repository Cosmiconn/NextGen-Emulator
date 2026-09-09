# Quest `GetNewQuestStatus` binary audit — Step 30

## Scope

Step 30 follows the original `CQuest::GetNewQuestStatus` symbols from the supplied `Zone.pdb` into the corresponding `Quest.obj` module stream and disassembles the referenced code in the byte-identical `Zone.exe` copies.

No runtime implementation is changed. The goal is to establish what can be stated directly from the binary and to isolate the remaining PDB/address ambiguity rather than guessing a status algorithm.

## Source/PDB evidence

The Quest object module is present in the DBI module list as `Release US\\Quest.obj` and uses symbol stream **330**.

The CodeView symbol material contains two overload names:

- `CQuest::GetNewQuestStatus(unsigned short)`
- `CQuest::GetNewQuestStatus(QUEST_DATA*)`

The corresponding public-symbol records expose section 1 offsets `0x22f4a0` and `0x22f320` respectively.

The same PDB also contains C13/source records for two `CQuest::GetNewQuestStatus` entries. The source-level record set is sufficient to prove the overloads exist, but the raw symbol/source-record address material is not internally consistent with the independently observed function boundaries used elsewhere in this audit. Therefore the overload names are **not** used as proof of a semantic implementation at a particular VA.

## Directly disassembled code in the surrounding Quest state area

### VA `0x0062F320`

The code at this VA is a standalone function with a one-argument cdecl/thiscall-compatible shape and `ret 4`.

For a non-null argument it writes:

- byte `+0x17` = `0`
- dword `+0x18` = `0`
- byte `+0x1C` = `0`
- clears bits `0x02` and `0x01` in byte `+0x1D`
- word `+0x1E` = `0`

A null argument returns immediately. The routine does **not** directly write the status byte at `+0x02`.

This exact behavior is also visible at call sites that prepare a `PLAYER_QUEST_INFO`-shaped record before invoking the routine. The structure interpretation is therefore an observed binary shape, not a guessed status name.

### VA `0x0062F350`

This routine scans the quest-info array (`count` at `+0x08`, storage pointer at `+0x10`) for a quest ID supplied by the caller. When found, it removes the matching 0x20-byte entry by shifting the following entries left and decrements the count. It returns success/failure as `1/0`.

### VA `0x0062F4B0`

This routine receives a quest ID, finds the corresponding player quest entry, resolves quest data, and then branches on the quest data byte at `+0x12`.

Observed behavior:

1. If the player quest entry or quest data cannot be resolved, it returns `0`.
2. If quest-data `+0x12` is non-zero, it writes player-quest status byte `+0x02 = 4` and clears the auxiliary fields in the same record (`+0x17`, `+0x18`, `+0x1C`, `+0x1D` low bits, `+0x1E`). It then returns `1`.
3. If quest-data `+0x12` is zero, it calls `0x0062F350` on the quest ID, which removes the matching player quest entry, and then returns `1`.

The supplied World DB procedure independently proves raw status value **4** as `PQS_REPEAT`. This allows the write of `+0x02 = 4` to be named as the already-proven repeat/re-acceptable state, without assigning any new status semantics.

## Cross-reference from the quest packet/state update path

A caller around `0x005BF43B` invokes `0x0062F4B0` with the quest ID when processing one of the incoming quest-state cases. The adjacent cases call the neighboring routines at `0x0062F3B0`, `0x0062F550`, `0x0062F5B0`, `0x0062F680`, and `0x006302B0`.

The same path also directly writes status byte `+0x02 = 4` before calling `0x0062F320` in one branch. This establishes that the routines around `0x0062F320..0x0062F680` form a quest-state mutation cluster.

## What is proven

- `GetNewQuestStatus` exists in the original PDB in two overloads.
- The Quest module is stream 330 and its CodeView material can be correlated with the original executable.
- A nearby quest-state routine explicitly writes raw status `4` and clears auxiliary player-quest fields.
- Another nearby routine removes a quest entry from the player quest list.
- The original DB independently identifies raw status `4` as `PQS_REPEAT`.
- These state mutations are exercised from the quest-state packet handling path.

## What remains UNRESOLVED

- The exact machine-code VA corresponding to each overloaded `GetNewQuestStatus` PDB name cannot yet be treated as proven solely from the raw symbol/source records because the recovered address records and independently verified function boundaries do not line up consistently across the same module.
- The exact return-value semantics of the two overloaded PDB declarations are therefore **UNRESOLVED** at this stage.
- No complete `GetNewQuestStatus` decision table is implemented or claimed here.
- The byte at `QUEST_DATA +0x12` is structurally proven as `QUEST_DATA.Repeatable` by CodeView, but its complete role in the higher-level quest transition remains separate from the raw state mutation observed above.

## Consequence for NextGen

No runtime quest-status logic is changed in Step 30. In particular, no guessed `GetNewQuestStatus` implementation is added.

The next evidence target remains the exact linkage between the PDB overload records, the corresponding function boundaries, and the callers that consume their return values. Only after that linkage is independently established should a status-transition implementation be considered.
