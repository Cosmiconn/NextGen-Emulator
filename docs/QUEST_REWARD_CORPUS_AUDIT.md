# Quest reward corpus and completion guard audit

## Source corpus

The generated SQL from the verified 2304-record NA2016 QuestData corpus contains exactly 27,648 `QuestData_Reward` rows (12 slots per quest).

A full row scan gives these raw RewardType counts:

| RewardType | Rows |
|---:|---:|
| 0 | 24,615 |
| 1 | 2,123 |
| 2 | 893 |
| 4 | 17 |

No other RewardType occurs in the supplied reward array.

Considering only active `UseType=1` (fixed) or `UseType=2` (selectable) rows:

- fixed type 0: 2,127
- selectable type 0: 1
- fixed type 1: 2,123
- fixed type 2: 531
- selectable type 2: 362
- fixed type 4: 17

Every active reward row has `Value2 = 0` in this corpus.

ItemID zero is also used by the reward corpus. Quests 7 and 8 each contain an active selectable RewardType-2 row with encoded `ItemID=0, lot=1`. ItemInfo independently maps ID 0 to `LeatherBoots`. The runtime therefore treats zero exactly like any other defined ItemID during reward validation and delivery; only `lot == 0` is skipped as an empty reward payload.

The runtime already handles RewardType 0, 1, 2 and 4. RewardType 8 remains implemented as a supported forward-compatible path but has no active row in this supplied corpus.

## Native completion ordering

The original completion chain documented in `QUEST_COMPLETION_BINARY_STEP37.md` reaches the quest-completion mutation only after a successful ItemDB quest-reward acknowledgement.

That is sufficient to establish an important ordering constraint: known reward-delivery failure must not be followed by a successful quest-state completion in the emulator.

It does **not** prove that the original ItemDB transaction, inventory locking, or rollback strategy matches the emulator's implementation.

## DONE, reward delivery, and later DELETE_ITEM

The original ItemDB ACK path is order-sensitive: a successful quest-reward ACK
reaches the completion mutation and only then calls `QuestNext`. Therefore
Finish-script commands after `DONE` execute after reward delivery/completion,
not before it.

The supplied corpus makes this observable rather than theoretical:

- Quest 230 rewards Item 2618 x10 and, after `DONE`, executes
  `DELETE_ITEM 2618 2`.
- Quest 250 rewards Item 2618 x5 and Item 2619 x3 and, after `DONE`, deletes
  exactly 5 and 3 of those ItemIDs.
- Other quests likewise contain reward/delete ItemID overlap.

Accordingly the emulator must not "clean up" Finish scripts by moving
`DELETE_ITEM` before `DONE`, nor defer rewards until after the remaining
Finish-script commands. `QuestRuntime.Complete` applies the reward set and
completion mutation; Handler17 then resumes the existing script machine after
`DONE`.

## Runtime alignment

`QuestRuntime.Complete` now requires `ApplyRewards` to succeed before writing the completed quest state.

Before any reward is applied, the runtime:

1. resolves the fixed rewards plus exactly the selected `UseType=2` row;
2. rejects unsupported reward types rather than silently completing;
3. validates every item definition;
4. computes the aggregate number of inventory slots needed using the same `MaxLot` stack model as `GiveItemLots`;
5. rejects the completion when the complete item reward set cannot fit.

Item rewards are delivered before EXP/money/fame/kill-point mutations. This prevents the previously possible path where the quest could advance even though an item reward was known to have failed.

## Evidence boundary

The aggregate preflight is a conservative emulator guard derived from the proven native reward-before-completion ordering. It is not claimed to reproduce the original ItemDB transaction byte-for-byte.

Exact `NC_ITEMDB_QUESTREWARD_REQ/ACK` field semantics, server-side transactional rollback, and capture correlation remain separate reverse-engineering targets.


## Native selectable-reward protocol

The remaining selection path is now reconstructed directly from the original
`Zone.exe`.

`CQuestZone::Recv_NC_QUEST_REWARD_SELECT_ITEM_INDEX_CMD` at `0x005BB490`
accepts exactly:

```text
u16 QuestID
u32 selected reward slot
```

It validates the QuestID against the currently driven quest and stores the DWORD
at `CQuestZone + 0x90C`. The handler itself does **not** complete the quest.
The emulator now does the same: `0x4411` records selection state only, so a
selection made while the final SAY dialog is open cannot prematurely execute
DONE.

`CQuestZone::QuestCheckSelectReward` at `0x005BA0E0` loops the twelve
QuestData reward entries. For every `UseType=2` row it compares the selected
DWORD with the **absolute reward-array slot index (0..11)**. It is not the
ordinal among selectable rows. If there are no selectable rows, the check
succeeds without a selection.

On QSC_DONE, an invalid/missing selection calls
`CQuestZone::Send_NC_QUEST_REWARD_NEED_SELECT_ITEM_CMD` at `0x005BB570`:

```text
opcode 0x4412
u16 QuestID
total packet length: 4 bytes
```

The emulator now emits this request and keeps DONE paused instead of silently
waiting with no client packet.

The same DONE branch also proves these raw failure values:

- missing `PLAYER_QUEST_INFO` -> `0x0C07`;
- `IsRewardAbleQuest` false -> `0x0C08`.

Both are sent through the already-reconstructed QSC_ERROR path before
`QuestClose`.

### Remaining boundary

The original `0x4411` receive routine itself only stores the selected slot.
The exact client/server event that re-enters the already-pending DONE after a
server-forced `0x4412` request is still **UNRESOLVED**. The emulator therefore
does not invent an automatic completion side effect on `0x4411`; normal
selection-before-DONE behavior is fully aligned, while that fallback wake-up
remains an explicit wire-state target.
