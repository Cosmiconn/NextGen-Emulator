# Quest NPC selection: equal-priority tie-break audit

## Scope

Step 27 isolates the control flow of the original `CQuest::GetQuestStatusWithNPC` implementation in the supplied `Zone.exe`.

The goal is to preserve the exact observed comparison sequence without assigning meanings to raw `QUEST_DATA` byte offsets that have not yet been independently proven.

## Original function

- Binary: `Zone.exe`
- Function: `CQuest::GetQuestStatusWithNPC`
- VA: `0x00630f60`
- Constructor priority tables: `0x006301d0`

The function iterates the NPC-associated quest candidates and stores the currently selected candidate plus its status-priority index.

## Proven comparison sequence

For each candidate returned by the quest-data lookup:

1. A candidate must be non-null and pass the preceding lookup/eligibility call at `0x00630fba..0x00630fc5`.
2. If no candidate has yet been retained, the candidate becomes the retained candidate.
3. The candidate's status value is used as an index into `m_QuestStatusPriority` at `this + 0x24`.
4. The retained candidate's status value is likewise indexed into `this + 0x24`.
5. If the candidate status priority is **greater than or equal to** the retained status priority, the candidate does not win this comparison (`jae` at `0x00630fe3`). Therefore lower status-priority values win.
6. When status priorities are equal, the routine applies additional predicates before reaching quest-type priority.
7. The first additional predicate reads candidate byte offset `0x1A`.
8. When that predicate is active, the routine compares candidate byte offset `0x1B` with the retained candidate's saved byte value. A larger value replaces the retained candidate; a smaller value does not.
9. The routine then applies a sequence involving candidate/retained byte offsets `0x11`, `0x38`, `0x20`, and `0x12`.
10. At offset `0x11`, the candidate is compared against literal value `3`. The routine gives this value special treatment before continuing.
11. For offsets `0x38`, `0x20`, and `0x12`, the routine prefers the candidate when its byte is zero while the retained candidate's corresponding byte is non-zero; if both are equal, selection continues.
12. Only after those predicates does the routine compare quest-type priorities using `m_QuestTypePriority` at `this + 0x78`.
13. The candidate quest-type priority is rejected when it is greater than or equal to the retained candidate's type priority (`jae` at `0x006310de`). Therefore lower quest-type-priority values win at this stage.

## Raw-offset control-flow table

| Candidate field | Observed operation | Proven meaning |
|---:|---|---|
| status index | `this + 0x24 + index*4` | `PLAYER_QUEST_STATUS` index used by status-priority table |
| `+0x1A` | zero/non-zero branch | **UNRESOLVED** |
| `+0x1B` | numeric comparison when `+0x1A` is active | **UNRESOLVED** |
| `+0x11` | compared with literal `3`; later indexes type-priority table | enum value used for `m_QuestTypePriority`; semantic source field mapping remains to be reconstructed |
| `+0x38` | zero/non-zero comparison | **UNRESOLVED** |
| `+0x20` | zero/non-zero comparison | **UNRESOLVED** |
| `+0x12` | zero/non-zero comparison | **UNRESOLVED** |

The table intentionally does not label these offsets as `DailyQuest`, `EnableQuest`, `InstAcc`, `QuestGrade`, etc. Such mappings require additional type/source evidence.

## Important control-flow detail

The status and type priorities are **not combined into one numeric score** in the observed routine. They are separate comparisons in a defined sequence, with several equal-status predicates between them.

This means an emulator implementation that simply sorts by `(statusPriority, typePriority)` is still incomplete, because the original function can decide between equal-status-priority candidates using the intermediate raw fields first.

## Current reconstruction status

### Proven

- Exact 21-entry status-priority table.
- Exact 11-entry quest-type-priority table.
- Lower numeric priority wins for both tables.
- Status priority is evaluated before quest-type priority.
- Additional equal-status predicates occur between the two priority comparisons.
- Raw candidate offsets involved in those predicates: `0x1A`, `0x1B`, `0x11`, `0x38`, `0x20`, `0x12`.

### UNRESOLVED

- Semantic names for offsets `0x1A`, `0x1B`, `0x38`, `0x20`, `0x12`.
- Exact semantic mapping of offset `0x11` to the normalized QuestData columns, despite its proven use as the quest-type-priority index.
- Complete meaning of the `+0x1A/+0x1B` replacement path.
- Whether the intermediate zero/non-zero predicates correspond to availability, instance, repeat, event, or another QuestData/player-state property.

## Next evidence target

The highest-value next step is reconstruction of the original `QUEST_DATA` CodeView field list and/or the routines that populate/read these exact offsets. Candidate methods include:

- `CQuest::GetNewQuestStatus`
- `CQuest::IsSoonableQuest`
- `CQuest::IsDoingableQuest`
- `CQuest::IsRewardAbleQuest`
- `CQuest::IsSoonableDailyQuest`
- `CQuestData::GetQuestData`

Only mappings supported by those structures or by independent capture/DB evidence should be transferred into the emulator.
