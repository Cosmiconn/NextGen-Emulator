# Quest ACCEPT native validation and corpus audit

## Scope

This audit reconstructs the native preconditions for textual `ACCEPT`
(command 6) in the supplied NA2016 `Zone.exe` and applies them to the
SQL-backed emulator runtime.

## Native command path

`CQuestZone::QuestNext` dispatches command 6 to
`Zone.exe 0x005BE7C2`.

The branch performs these checks before constructing the accepted
`PLAYER_QUEST_INFO` state:

1. resolve the command's WORD QuestID through `CQuestData::GetQuestData`
   at `0x00635FF0`; a missing quest is rejected;
2. call `CQuest::GetNumOfDoingQuest` at `0x0062FB10`;
3. reject when that count is **40 or greater**;
4. call the QuestID overload of `CQuest::IsDoingableQuest` at
   `0x0062FEE0`;
5. reject when the quest is not doingable;
6. only then construct/update status `PQS_ING (6)`.

Direct disassembly of `0x0062FB10` shows that the active-count loop counts
player quest records whose raw status is 6, 7 or 8. It therefore covers
`PQS_ING`, `PQS_FAILED` and `PQS_REWARD`, exactly as the original
machine code compares the status byte after subtracting 6.

`0x0062FEE0` resolves the QuestID to `QUEST_DATA`, calls
`IsSoonableQuest`, and then applies the current-level min/max gate. It is the
QuestID wrapper for the same Doingable logic already reconstructed for NPC
selection.

## Existing-record behavior

The mutation helper `CQuest::SetQuestAccept` begins at `0x0062F3B0`.
For an existing quest it writes status 6 and clears the per-run progress area.
When the previous status is `PQS_READ_ABLE (20)`, it rebuilds the quest record
and refreshes the accept timestamp. New records are likewise created in status
6.

The emulator's normalized equivalent is to set `tQuest.nStatus = 6` and
clear `character_quest_progress` for that quest. Completion history in
`tQuestTimes` is intentionally retained.

## Corpus impact

The verified 2304-record QuestData source contains **2410** ACCEPT commands:

- Start: **2382**
- Action/Doing: **0**
- Finish/End: **28**

There are **22** explicit numeric `ACCEPT <QuestID>` forms. Twenty target the
current quest. Exactly two deliberately target another quest:

- Quest 5 -> `ACCEPT 6`
- Quest 384 -> `ACCEPT 385`

Those two occur after the source quest executes `DONE`, so the native
Doingable check is materially relevant to chained quest acceptance.

Finish-stage ACCEPT is also real source behavior; the runtime must not assume
ACCEPT only occurs in Start scripts.

## Runtime alignment

`QuestRuntime.Accept` now returns success/failure and enforces the two proven
native preconditions before mutating SQL state:

- fewer than 40 persisted quests in statuses 6/7/8;
- the target quest passes the SQL-backed native Doingable reconstruction.

`QuestNpcStartResolver` now loads every QuestData row into its QuestID index,
including quests without a direct start/reward NPC association, so the
Doingable check is not limited to NPC-indexed quests.

On a rejected ACCEPT, Handler17 closes the active quest script, matching the
native rejection control flow. The exact native quest-error packet enum values
(`0x0C02`, `0x0C0F`, `0x0C03` are observed in the branch) remain
**UNRESOLVED** at the protocol-name level and are not fabricated.

CI locks the ACCEPT stage counts, explicit-operand count and the two cross-quest
targets.

## Evidence boundary

This change does not claim byte-for-byte equivalence for the original ItemDB
persistence transaction or error-packet naming. The acceptance eligibility,
40-active-quest cap, status transition and per-run progress reset are all
directly supported by the supplied original binary plus the existing
QuestData/PDB field reconstruction.
