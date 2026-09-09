# Quest selection reconstruction

## Source evidence

The original server PDB exposes the quest-selection path:

- `CQuest::GetQuestStatusWithNPC`
- `CQuest::IsDoingableQuest`
- `CQuest::IsSoonableQuest`
- `CQuest::IsSoonableDailyQuest`
- `CQuest::IsRewardAbleQuest`
- `CQuest::GetNewQuestStatus`
- `CQuest::DoingQuestUpdateStatus`
- `CQuest::GetQuestInfo`
- `CQuest::SetQuestInfo`
- `CQuest::AddQuestInfo`
- `CQuestZone::Recv_NC_QUEST_SELECT_START_REQ`
- `CQuestZone::Send_NC_QUEST_SELECT_START_ACK`

The PDB also exposes `m_QuestStatusPriority` and `m_QuestListWithNPC`.
This is direct symbol evidence that the original server builds a status-aware
quest list for an NPC rather than choosing the first quest arbitrarily.

## Quest selection packet

The original protocol symbols identify:

- `NC_QUEST_SELECT_START_REQ` = Header 17 opcode `0x440F`
- `NC_QUEST_SELECT_START_ACK` = Header 17 opcode `0x4410`

The request is known to contain:

- `nCharNo`
- `nNPCID`
- `nQuestID`

The ACK has three `unsigned short` parameters in the recovered server
signature, but their wire order/meaning is not yet sufficiently evidenced.
Do not implement guessed fields.

## Status model

The PDB exposes these symbolic status names:

- `PQS_NONE`
- `PQS_ABLE`
- `PQS_SOON`
- `PQS_LOWABLE`
- `PQS_READ_ABLE`
- `PQS_ING`
- `PQS_REWARD`
- `PQS_DONE`
- `PQS_REPEAT`
- `PQS_FAILED`
- `PQS_ABORT`
- `PQS_MAX_PLAYER_QUEST_STATUS`

Only the following numeric mappings are currently source-proven:

- `4 = PQS_REPEAT`
- `6 = PQS_ING` / in-progress

The remaining numeric values are deliberately not assigned until the original
binary/database evidence establishes them.

## Character quest storage

The original World database contains a `tQuest` character quest table with
fields including:

- `nCharNo`
- `nQuestNo`
- `nStatus`
- `sData VARBINARY(100)`

The recovered server procedure `p_Quest_Set` confirms status handling for at
least `PQS_REPEAT` and `PQS_IN_PROGRESS`.

This means a simple two-state Active/Completed replacement is insufficient for
faithful emulation. The emulator must retain the raw status and quest payload
until all status semantics are reconstructed.

## Implementation rule

Until `GetQuestStatusWithNPC`, `m_QuestStatusPriority`, and the `0x440F/0x4410`
wire format are fully reconstructed:

1. do not select the first quest merely because it is first in SQL order;
2. do not invent numeric values for unknown `PQS_*` states;
3. do not invent ACK fields or their ordering;
4. keep objective progress/daily-reset/abort semantics separate from the
   recovered raw quest status;
5. use `data_quest_start_dialog` only as the data source for the quest's known
   starting dialog, not as a substitute for the original status-selection
   algorithm.

## Next reconstruction target

The next implementation target is the status-priority and eligibility path:

`NPC -> m_QuestListWithNPC -> GetQuestStatusWithNPC -> status priority ->
0x440F selection -> quest start`

Only after that path is evidenced should `QuestNpcStartResolver` be changed to
choose among multiple candidates.
