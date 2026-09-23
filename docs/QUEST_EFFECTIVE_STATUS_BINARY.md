# Effective quest status — native binary reconstruction

## Source

Original NA2016 `Zone.exe`, effective quest-status routine at
`0x00630320`. The routine is consumed by NPC selection and by the native LINK
path.

## Persisted-status decision table

The original jump table distinguishes persisted raw quest status from the
effective status exposed to NPC/script routing.

| Persisted/raw state | Effective-state rule |
|---|---|
| no player quest row | Soonable false -> NONE; Soonable true + Doingable level -> ABLE; otherwise SOON |
| 0 NONE | Soonable -> SOON, otherwise NONE |
| 1 ABORT | Soonable -> SOON, otherwise NONE |
| 2 DONE | only the native daily-reset path can reopen it; when reset elapsed and Doingable -> ABLE, otherwise DONE |
| 3 SOON | Doingable -> ABLE; else Soonable -> SOON; else NONE |
| 4 REPEAT | Doingable -> REPEAT; otherwise NONE |
| 5 ABLE | Doingable -> ABLE; otherwise NONE |
| 6 ING | rewardable -> REWARD; otherwise ING |
| 7 FAILED | Soonable -> SOON; otherwise NONE |
| 8 REWARD | rewardable -> REWARD; otherwise ING |
| 20 READ_ABLE | Doingable -> READ_ABLE; otherwise NONE |
| 9..19 not otherwise handled | returned through the native default path |

This distinction is required for repeatable quests: native completion writes
PQS_SOON (3), but the next status calculation promotes it to PQS_ABLE (5) once
the quest is doingable again.

## Daily DONE reopening

The virtual daily check reached from the raw-status-2 path maps to the native
routine at `0x00630130`. It rejects non-Type-10 quests, reads the quest's
DailyQuestType and completion timestamp, and returns true once the relevant
daily/weekly/monthly/yearly reset boundary has elapsed.

NextGen normalizes the proven completion timestamp in
`tQuestTimes.dLastComplete` and uses it for this status-2 reopening rule.

## Runtime implementation

`QuestNpcStartResolver.TryGetEffectiveStatus` reproduces this state-dependent
flow. Start eligibility is no longer globally imposed on every persisted
status: active/reward quests use end-condition rewardability, while
SOON/REPEAT/ABLE paths use Soonable/Doingable as the original routine does.

This also provides the effective status consumed by LINK stage dispatch.
