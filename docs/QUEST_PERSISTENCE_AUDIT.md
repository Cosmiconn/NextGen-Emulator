# Quest persistence audit

## Original database evidence

The supplied `World00_Character.bak` contains the original SQL Server procedure `dbo.p_Quest_Set` with the contract:

`@nCharNo int, @nQuestNo int, @nStatus tinyint, @sData varbinary(100), @nRet int OUTPUT`

The original procedure updates `tQuest.nStatus` and `tQuest.sData` for `(nCharNo,nQuestNo)` and inserts the row when it does not already exist. The procedure comments explicitly identify status `4` as `PQS_REPEAT` and status `6` as `PQS_IN_PROGRESS`.

The same backup contains `tQuestTimes`, with `nCharNo`, `nQuestNo`, `nTimes`, and `dLastComplete`. The increment/update block visible in `p_Quest_Set` is commented out; therefore `tQuestTimes` must not be attributed to `p_Quest_Set` itself without the native `CQuest::SetQuestDone` evidence.

## Emulator mapping

The current SQL schema preserves the original persistence names and core types:

- `tQuest(nCharNo,nQuestNo,nStatus,sData)`
- `tQuestTimes(nCharNo,nQuestNo,nTimes,dLastComplete)`

The normalized `character_quest_progress` table is emulator-side runtime progress for objective counters/flags and is not claimed to be an original table.

`Accept` writes `tQuest` with status `6` and clears normalized runtime progress. `Cancel` removes non-repeat entries and preserves status `4` for repeat entries while clearing normalized runtime progress. `Complete` changes status to `2` for non-repeatable quests and `4` for repeatable quests and increments `tQuestTimes` for repeatable completion.

## Parity boundary

The persistence table names and the `p_Quest_Set` field contract are proven. Exact byte-level meaning of original `sData`, exact timestamp/increment ordering inside native `CQuest::SetQuestDone`, and all special historical `QuestItemFix` behavior remain separate evidence items and must not be silently represented as equivalent by the emulator.
