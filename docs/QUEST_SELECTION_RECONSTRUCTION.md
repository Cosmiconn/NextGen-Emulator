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

## PLAYER_QUEST_STATUS — numeric values now proven by the PDB

A CodeView type record embedded in the supplied `Zone.pdb` gives the enum
values directly. The complete mapping recovered from that record is:

| Value | Symbol |
|---:|---|
| 0 | `PQS_NONE` |
| 1 | `PQS_ABORT` |
| 2 | `PQS_DONE` |
| 3 | `PQS_SOON` |
| 4 | `PQS_REPEAT` |
| 5 | `PQS_ABLE` |
| 6 | `PQS_ING` |
| 7 | `PQS_FAILED` |
| 8 | `PQS_REWARD` |
| 9 | `PQS_LOWABLE` |
| 20 | `PQS_READ_ABLE` |
| 21 | `PQS_MAX_PLAYER_QUEST_STATUS` |

This is stronger evidence than the earlier database-only mappings. The
World database independently confirms `4 = PQS_REPEAT` and `6 = in-progress`.
The values 10–19 are not missing states: the PDB enum explicitly jumps from 9
to 20.

## NPC_QUEST_STATUS

The PDB contains a concrete `CQuest::NPC_QUEST_STATUS` type. Its debug/type
information shows members associated with the NPC quest-result path and the
class also exposes:

- `m_MaxOfQuestListWithNPC`
- `m_NumOfQuestListWithNPC`
- `m_pQuestListWithNPC`
- `m_QuestStatusPriority`
- `m_QuestTypePriority`
- `GetQuestListWithNPC`
- `GetQuestStatusWithNPC`
- `IsDoingQuestStatus`

The recovered `GetQuestStatusWithNPC` debug information shows locals named
`kQuestStatus`, `lpQuestData`, `uiMainCharLv`, `bQmark`, `bLowRepeat`, and
`eQuestState`, plus multiple branch labels. This establishes that the method
computes a status/result for an NPC quest rather than merely returning a raw
persisted status.

The exact assignments made by those branches are still not recoverable from
PDB symbols alone. In particular, the PDB does not expose the initialized
contents of `m_QuestStatusPriority` as source-level values.

## Quest selection packet

The original protocol symbols identify:

- `NC_QUEST_SELECT_START_REQ` = Header 17 opcode `0x440F`
- `NC_QUEST_SELECT_START_ACK` = Header 17 opcode `0x4410`

The request is known to contain:

- `nCharNo`
- `nNPCID`
- `nQuestID`

The request/ACK wire contract is now directly reconstructed and implemented:

- request `0x440F`: `NPCID u16 + QuestID u16`;
- ACK `0x4410`: `NPCID u16 + QuestID u16 + ErrorType u16`.

The runtime validates the requested quest against the same character/NPC
selection result before returning the proven success/failure ACK.

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

Until `GetQuestStatusWithNPC`, `m_QuestStatusPriority`, and the
`0x440F/0x4410` wire format are fully reconstructed:

1. do not select the first quest merely because it is first in SQL order;
2. do not invent eligibility predicates from QuestData field names;
3. do not invent ACK fields or their ordering;
4. keep objective progress/daily-reset/abort semantics separate from the
   recovered raw quest status;
5. use `data_quest_start_dialog` only as the data source for the quest's known
   starting dialog, not as a substitute for the original status-selection
   algorithm.

## Current reconstruction state

The original priority tables and most equal-priority predicates are now
reconstructed and implemented. The current SQL-backed resolver has proven
handling for:

- exact status and quest-type priority tables;
- `Start.bLevel/LevelMin` tie-break behavior;
- `Start.bQuest`, `Start.bItem`, and `Repeatable` zero/non-zero tie-breaks;
- Soonable/Doingable level windows;
- start item, location, class and gender gates used by the supplied corpus;
- the full native effective-status state machine at `0x00630320`, including
  PQS_SOON -> PQS_ABLE reopening and Type-10 DONE reset behavior;
- effective ING/REWARD rewardability recalculation;
- native Start-NPC / end-NPC action-0/3 association and the NPC-role status
  filter at `0x00630570`;
- Start/Action/Finish script entry selection by effective status;
- the proven `0x440F/0x4410` select-start request/ACK contract.

For the supplied NA2016 QuestData corpus, the NPC-selection eligibility path is
now covered for every active start-condition family present in the data,
including Type-10 daily predecessor reset checks.

The remaining selection-adjacent boundaries are deliberately narrow:

1. future start Race/Date gates, which have zero active rows in the supplied
   corpus and therefore are not needed for current content;
2. legacy characters created before normalized completion timestamps were
   persisted may safely fall back for a Type-10/status-2 prerequisite until
   they have a real `tQuestTimes.dLastComplete` value;
3. NPC-visible statuses whose exact dialog presentation is unused/unproven for
   the supplied content remain on the legacy interaction path rather than being
   assigned invented UI behavior.

The Type-3 equal-priority branch is fully reconstructed and implemented.
The status-2 predecessor path is fully resolved, including the native daily
reset rule. In the supplied corpus, 1390 quests have a predecessor gate and
only two edges point to a Type-10 predecessor: `20037 -> 20036` and
`20048 -> 20047`.


## Later binary closures

The effective status routine and NPC-role filter are documented in
`QUEST_EFFECTIVE_STATUS_BINARY.md` and `QUEST_NPC_ASSOCIATION_BINARY.md`.
The same effective-status reconstruction is reused by native command-11 LINK
stage selection, documented in `QUEST_LINK_BINARY.md`.
