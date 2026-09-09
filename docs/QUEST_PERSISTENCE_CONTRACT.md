# Quest Persistence Contract – Step 23 Evidence

## Scope

This document records only quest-persistence behavior recoverable from the supplied server database backup and PDBs. It does not infer quest eligibility, NPC priority, packet layouts, or the opaque `sData` format.

## Original `tQuest` record

The recovered character database procedures operate on the complete logical record:

- `nCharNo`
- `nQuestNo`
- `nStatus`
- `sData` (`varbinary(100)`)

This confirms that quest state is not a binary active/completed flag.

### `p_Quest_Get`

Recovered behavior selects `nStatus` and `sData` for one `(nCharNo, nQuestNo)` pair and returns the matching-row count.

### `p_Quest_GetAll`

Recovered behavior:

```sql
SELECT nQuestNo, nStatus, sData
FROM tQuest
WHERE nCharNo = @nCharNo
```

Thus all quest records for a character retain both the status and opaque per-quest payload.

### `p_Quest_GetAllDone`

Recovered behavior filters by a caller-supplied status value. The procedure does not itself assign a numeric value to “done”.

### `p_Quest_GetAllDoing`

Recovered behavior also filters by a caller-supplied status and returns `nQuestNo`, `nStatus`, and `sData`. The parameter name in the recovered procedure is `@nStatusDone`; this is not treated as proof of a particular numeric status.

### `p_Quest_Add` / `p_Quest_Del`

`p_Quest_Add` inserts the complete quest record. `p_Quest_Del` removes one character/quest pair.

### `p_Quest_Set`

The recovered procedure reads the previous status, updates both `nStatus` and `sData`, and inserts when no existing row was updated. Therefore status transitions persist the opaque quest payload together with the status.

The backup contains historical variants of this procedure. One variant also contains a FiestaHeroes NA2016 quest-item guard tied to status `6`; this is evidence from the supplied database artifact and is not generalized beyond that guarded case.

## Status values directly proven by the database source

The recovered procedure comments explicitly state:

- `4 = PQS_REPEAT` — repeat quest done, now re-acceptable.
- `6 = PQS_IN_PROGRESS`.

The supplied Zone PDB uses the symbolic spelling `PQS_ING` for the latter. No numeric values are assigned here to the other `PLAYER_QUEST_STATUS` symbols.

## `tQuestTimes`

The database backup contains a separate `tQuestTimes` structure with:

- `nCharNo`
- `nQuestNo`
- `nTimes`
- `dLastComplete`

A historical commented section of `p_Quest_Set` shows logic that would increment `nTimes` and update `dLastComplete` when a quest moved from status `6` to status `4`. Because that block is commented in the recovered procedure text, it is treated as historical evidence rather than active behavior of the supplied build.

The backup also exposes `p_getQuestTimes`, confirming that repeat/completion counts are a separate persistence concept from `tQuest.sData`.

## Consequences for NextGen

The quest-state representation should remain source-like:

- `QuestID` ↔ `nQuestNo`
- `Status` ↔ `nStatus`
- `Data` ↔ `sData`

The opaque `Data` bytes must not be interpreted until their layout is independently recovered.

NPC quest selection, status priority, eligibility predicates, and the `NC_QUEST_SELECT_START_REQ/ACK` wire format remain separate investigations.
