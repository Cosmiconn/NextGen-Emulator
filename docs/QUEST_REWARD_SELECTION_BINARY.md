# Quest reward selection — native binary audit

## Wire contract

The original NA2016 Zone binary establishes both halves of selectable quest
rewards.

### Client -> Zone: 0x4411

`CQuestZone::Recv_NC_QUEST_REWARD_SELECT_ITEM_INDEX_CMD` begins at
`0x005BB490`. It compares the first WORD of the request with the currently
driven QuestID at `CQuestZone+0x906`. On a match it copies the DWORD at request
offset `+0x02` to `CQuestZone+0x90C`.

Body:

```text
u16 QuestID
u32 selectedSlot
```

The receive routine does no quest completion and no QuestNext call.

### Zone -> client: 0x4412

`CQuestZone::Send_NC_QUEST_REWARD_NEED_SELECT_ITEM_CMD` begins at
`0x005BB570`. It writes opcode `0x4412`, writes its WORD argument at packet
offset `+0x02`, and sends exactly four bytes.

Body:

```text
u16 QuestID
```

## Selection semantics

`CQuestZone::QuestCheckSelectReward` at `0x005BA0E0` resolves QuestData and
loops exactly twelve reward entries beginning at `QUEST_DATA+0x204`.

For each entry whose `UseType == 2`, it compares the selected DWORD directly
with the loop index `0..11`. Therefore the 0x4411 value is the absolute
QuestData reward slot, not an ordinal among selectable rewards.

If no `UseType=2` entry exists, the routine returns true without requiring a
selection. If selectable entries exist, it returns true only when the selected
slot names one of them.

QuestStart/QuestDoing/QuestEnd reset the stored value to `0xffffffff`; the
selection therefore belongs to the current quest-script stage, not to an
individual SAY page.

## DONE integration

QSC_DONE at `0x005BED38` performs, in order:

1. resolve PLAYER_QUEST_INFO; missing -> QSC error `0x0C07` + QuestClose;
2. call rewardability; false -> QSC error `0x0C08` + QuestClose;
3. call QuestCheckSelectReward with the stored selected slot;
4. if false, send `0x4412 QuestID` and return without advancing DONE;
5. if true, continue into reward processing.

Handler17 now retains the raw reward slot across SAY pages of the same
QuestScriptMachine, resets it when LINK enters a new quest stage, and lets DONE
consume it. 0x4411 itself only stores the slot, matching the original receive
handler.

## Adjacent QuestStart protocol is not yet the wake-up

The quest protocol enum places `NC_QUEST_START_REQ/ACK` at
`0x4414/0x4415`. Secondary PDB-derived structure exports agree on these bodies:

```text
0x4414 request: u16 QuestID
0x4415 ack:     u16 err
```

That structural agreement is useful for targeting the original handler, but it
does **not** establish a control-flow relationship with `0x4412` or the stored
reward slot. No runtime handler is added from adjacency alone.

## UNRESOLVED

After a server-forced 0x4412, the native 0x4411 receiver still only stores the
slot. The exact later event that causes the pending DONE to be evaluated again
has not yet been tied to a client/server callsite. No synthetic completion is
assigned to the 0x4411 receive operation until that wake-up is proven.

The next primary-evidence target is the original Zone handler/callsite for
`NC_QUEST_START_REQ (0x4414)` plus all callers that can re-enter the current
QuestNext/DONE state after `CQuestZone+0x90C` changes.
