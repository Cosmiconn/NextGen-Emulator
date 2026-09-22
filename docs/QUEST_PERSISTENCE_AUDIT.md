# Quest persistence audit

## Original database evidence

The supplied `World00_Character.bak` contains `dbo.p_Quest_Set` with contract `@nCharNo int, @nQuestNo int, @nStatus tinyint, @sData varbinary(100), @nRet int OUTPUT`. It updates/inserts `tQuest.nStatus` and `tQuest.sData`. Comments identify status `4` as `PQS_REPEAT` and status `6` as `PQS_IN_PROGRESS`.

The backup also contains `tQuestTimes(nCharNo,nQuestNo,nTimes,dLastComplete)`. The `tQuestTimes` update block found inside `p_Quest_Set` is commented out and is a historical special case for quest `20016`; it is not evidence for a generic repeatable-quest counter.

## Emulator mapping

The schema preserves the original persistence names and core types:

- `tQuest(nCharNo,nQuestNo,nStatus,sData)`
- `tQuestTimes(nCharNo,nQuestNo,nTimes,dLastComplete)`

`character_quest_progress` is emulator-side runtime progress and is not claimed to be an original table.

`Accept` writes status `6` and clears normalized progress. `Cancel` removes non-repeat entries and preserves status `4` for repeat entries while clearing normalized progress. `Complete` writes status `2` for non-repeatable quests and status `3` (`PQS_SOON`) for repeatable quests, matching the native `CQuest::SetQuestDone` -> `0x0062F610` completion mutation. Status `4` (`PQS_REPEAT`) remains a distinct later/re-acceptable state and is not used as the immediate completion state. It deliberately does **not** write `tQuestTimes`, because generic repeatable-quest persistence there is not proven.

## Native completion boundary

Native `CQuest::SetQuestDone` selects status `4` for repeatable quests and `2` otherwise, updates the in-memory `PLAYER_QUEST_INFO`, and then calls the player/quest virtual callback at `+0x5C`. That callback is the unresolved native side-effect/persistence boundary. No generic `tQuestTimes` increment is claimed.

## Parity boundary

The persistence table names and `p_Quest_Set` contract are proven. Exact byte-level meaning of original `sData`, exact timestamp/increment ordering at the native callback boundary, and historical `QuestItemFix` behavior remain separate evidence items and are not silently represented as equivalent by the emulator.
