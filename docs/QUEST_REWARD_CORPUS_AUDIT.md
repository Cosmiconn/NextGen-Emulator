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

The runtime already handles RewardType 0, 1, 2 and 4. RewardType 8 remains implemented as a supported forward-compatible path but has no active row in this supplied corpus.

## Native completion ordering

The original completion chain documented in `QUEST_COMPLETION_BINARY_STEP37.md` reaches the quest-completion mutation only after a successful ItemDB quest-reward acknowledgement.

That is sufficient to establish an important ordering constraint: known reward-delivery failure must not be followed by a successful quest-state completion in the emulator.

It does **not** prove that the original ItemDB transaction, inventory locking, or rollback strategy matches the emulator's implementation.

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
