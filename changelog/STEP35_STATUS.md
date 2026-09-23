# Step 35 Status — Quest Mutation Cluster Reconciliation

## Result

Direct x86 analysis of the original `Zone.exe` substantially narrowed the quest-status mutation behavior.

### Newly proven

- `0x0062F3B0` handles a quest by ID: repeatable quest data causes raw status `4` (`PQS_REPEAT`) and auxiliary reset; non-repeatable quest data removes the entry.
- `0x0062F610` updates a player-quest record after resolving quest data. It writes raw status `2` when `QUEST_DATA+0x12 == 0` and raw status `3` when `QUEST_DATA+0x12 != 0`, increments the completion/count field at `+0x13`, stores a timestamp, and invokes a callback.
- `0x0062F680` writes raw status `1` (`PQS_ABORT`) for a resolved quest entry.
- `0x0062F550` writes an auxiliary byte at player-quest record offset `+0x17`.
- `0x0062F320` is an auxiliary record reset routine, not a `PLAYER_QUEST_STATUS` return routine.

### PDB anomaly remains

The PDB names/ranges for `SetQuestDone`, the two `GetNewQuestStatus` overloads, `IsDoingQuest`, and `DoingQuestUpdateStatus` overlap independently callable executable prologues. No source-level function-name reassignment was made.

### Runtime

No runtime changes.
No SQL changes.

## Completion assessment

This removes a major part of the quest-state mutation uncertainty. Remaining work is protocol/capture correlation and final reconciliation of the optimized-code/PDB ranges before any runtime status-transition implementation is promoted.
