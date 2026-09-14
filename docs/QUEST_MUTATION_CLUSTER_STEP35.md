# Quest Mutation Cluster — Step 35 Binary Reconciliation

## Scope

Direct x86 inspection of the original `Zone.exe` around the PDB-named quest mutation cluster `0x0062F2B0..0x0062F510`, with caller cross-references.

## Proven machine-code behavior

### `0x0062F2E0`

The executable contains a callable routine with a normal prologue at `0x0062F2E0`. It receives a pointer to a 32-byte player-quest record, searches the quest list by quest ID, and inserts/copies the record when no existing entry is found and capacity permits. It returns `1` on insertion/success and `0` on failure. This is a quest-list insertion/update helper; it does not return a `PLAYER_QUEST_STATUS`.

### `0x0062F320`

The executable contains a normal callable routine which accepts a 32-byte player-quest record pointer and clears auxiliary fields at offsets `+0x17`, `+0x18`, `+0x1C`, low bits of `+0x1D`, and `+0x1E`. It does not return a `PLAYER_QUEST_STATUS`; on the non-null path the argument pointer remains in `EAX`.

### `0x0062F350`

This is a normal quest-list removal routine. It searches by unsigned-short quest ID, shifts later 32-byte records down using `0x6572B0`, decrements the list count, and returns through the observed calling convention.

### `0x0062F3B0`

This callable routine accepts an unsigned-short quest ID, resolves the quest data, finds the player quest entry, and then either:

- if `QUEST_DATA+0x12 != 0`, writes raw player status `4` and clears auxiliary fields; or
- otherwise removes the player quest entry through `0x0062F350`.

It returns `1` when the quest was handled and `0` when the quest/player state could not be resolved.

Raw status `4` is independently proven as `PQS_REPEAT`.

### `0x0062F550`

This routine accepts an unsigned-short quest ID and an additional byte. It finds the corresponding player quest record and writes that byte to record offset `+0x17`, returning `1`/`0` for success/failure. It is an auxiliary quest-record update operation, not a status transition by itself.

### `0x0062F610`

This routine accepts a player-quest record pointer. It resolves the associated quest data and checks `QUEST_DATA+0x12` (repeatable). It writes:

- raw status `2` when the quest is not repeatable;
- raw status `3` when the quest is repeatable.

It increments the completion/count field at record offset `+0x13`, obtains a timestamp through `0x659159`, stores the returned time values at `+0x0B/+0x0F`, then invokes a C++ virtual callback with the quest data and player-quest record. It returns `1` on the observed success path and `0` if the player-quest pointer or quest-data lookup is absent.

This gives direct binary evidence for a completion-state transition involving raw `2` versus `3`, but the source-level symbol assignment remains unresolved because the PDB function ranges overlap unrelated callable prologues.

### `0x0062F680`

This routine accepts an unsigned-short quest ID and, after resolving the player quest entry, writes raw status `1` to the player quest record. It returns `1`/`0`. Raw status `1` is independently proven as `PQS_ABORT`.

### `0x0062F6E0` and following

The next routines perform quest-list aggregation/copy operations and are not used to assign missing status semantics in this step.

## Caller evidence

Known direct callers include:

- `0x005BBFFF -> 0x0062F610`: passes a copied player-quest record pointer during a larger quest-processing path.
- `0x005BF451 -> 0x0062F550`: passes quest ID plus one byte.
- `0x005BF4C6 -> 0x0062F680`: passes quest ID.
- `0x005BF42A -> 0x0062F3B0`: passes quest ID.
- `0x006302E0`, `0x006302F4`, `0x00630306 -> 0x0062F610`: pass either a resolved player-quest record pointer or null.
- `0x0062F52D -> 0x0062F350`: removes a quest entry by ID from the repeatability branch of `0x0062F3B0`.

## PDB reconciliation result

The PDB contains exact `CQuest` names for:

- `CQuest::SetQuestDone`
- `CQuest::GetNewQuestStatus` (two overload records)
- `CQuest::IsDoingQuest`
- `CQuest::DoingQuestUpdateStatus`

However, the corresponding CodeView ranges overlap independently callable x86 prologues in the executable. Examples:

- the PDB range for `GetNewQuestStatus` beginning at `0x0062F320` covers the independent routines at `0x0062F320`, `0x0062F350`, and `0x0062F3B0`;
- the PDB range beginning at `0x0062F4A0` contains the prologue at `0x0062F4B0`, whose body continues through the PDB ranges labelled `IsDoingQuest` and `DoingQuestUpdateStatus`.

Therefore the symbol names cannot safely be reassigned to the observed callable prologues merely by address. This is treated as a **PDB/optimized-code range anomaly**, not corrected by guesswork.

## Status conclusions

Directly supported by machine code and independent enum evidence in this cluster:

| Raw status | Proven enum | Direct mutation observed |
|---:|---|---|
| 1 | `PQS_ABORT` | `0x0062F680` |
| 2 | `PQS_DONE` | `0x0062F610` for non-repeatable quest |
| 3 | `PQS_SOON` | `0x0062F610` for repeatable quest |
| 4 | `PQS_REPEAT` | `0x0062F3B0` repeatable branch |
| 6 | `PQS_ING` | existing-entry/new-entry initialization elsewhere in the same cluster |
| 7 | `PQS_FAILED` | separate routine at `0x0062F5B0` |

The fact that `0x0062F610` writes raw `3` for repeatable quests is now directly proven. The higher-level source symbol/name attached to that machine-code routine remains **UNRESOLVED**.

## Runtime impact

None. No runtime code or SQL was changed.

## Next step

Use the mutation routines plus the quest captures to correlate actual status transitions and packet updates. Do not rename runtime methods or implement a state machine from the PDB names until the overlapping CodeView ranges are reconciled.
