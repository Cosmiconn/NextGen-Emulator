# Step 31 Status — exact `GetNewQuestStatus` PDB type linkage

## Ergebnis

Step 31 decoded the actual `S_GPROC32` records in Quest module symbol stream 330 and resolved their function-type indices through TPI stream 2.

Proven:

- `CQuest::GetNewQuestStatus(QUEST_DATA*)` uses function type `0x4B32` and returns `PLAYER_QUEST_STATUS`.
- `CQuest::GetNewQuestStatus(unsigned short)` uses function type `0x4B31` and returns `PLAYER_QUEST_STATUS`.
- The `QUEST_DATA*` parameter is proven through `LF_POINTER -> QUEST_DATA`.
- The unsigned-short parameter is proven through the corresponding argument list.
- The `QUEST_DATA*` routine writes raw player-quest status `6` on its in-progress paths.
- The ID-taking routine writes raw status `4` on its repeat path and otherwise removes the quest entry.
- `6 = PQS_ING` and `4 = PQS_REPEAT` are independently established by the existing binary/DB evidence.

## Important unresolved point

The second PDB procedure record reports code offset `0x0062F4A0` and code size `0x28`, while the obvious callable prologue is at `0x0062F4B0` and its useful body continues further. This is recorded as a **binary layout anomaly** and is not silently normalized.

## Runtime

- No runtime changes.
- No SQL changes.
- No quest-status implementation added.

## Validation

- Original `Zone.pdb` parsed: DBI module stream 330.
- TPI stream 2 decoded for the relevant type indices.
- Original `Zone.exe` disassembled around both procedures.
- No build/runtime tests performed.
