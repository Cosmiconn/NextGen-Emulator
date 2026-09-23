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

### Source-level CQuestZone field correlation

An independent 2016 PDB-derived type dump corroborates the same object layout
and supplies the original member names around this state:

```text
CQuestZone +0x8FC  STRUCT_QSC* m_pQSC
CQuestZone +0x900  int         m_bWaitResult
CQuestZone +0x906  ushort      m_ParsingQuestID
CQuestZone +0x908  parsing     m_ParsingQuestScriptType
CQuestZone +0x90C  uint        m_nSelectedItem
CQuestZone +0x914  int         m_ScriptIdent
```

This closes the source-level identity of the selected-reward field:
`CQuestZone+0x90C` is `m_nSelectedItem`. It also confirms that the parser,
wait-result state, current QuestID, stage and selected item are adjacent
CQuestZone state. It still does not prove which later caller wakes a DONE that
has already emitted 0x4412, so no caller identity is inferred from layout alone.

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

## Emulator recovery closure

After a server-forced 0x4412, the native 0x4411 receiver is proven to do
nothing beyond storing the slot. The supplied evidence does not assign a
source-level name to the later original caller that re-enters pending DONE.

That missing internal caller identity is **not an open protocol/runtime
semantic**: the observable state boundary is completely constrained. Handler17
therefore distinguishes exactly two cases:

- normal 0x4411: store the slot and return, exactly matching the receiver;
- 0x4411 while `RewardSelectionPending` is already true because this emulator
  sent 0x4412 from DONE: consume the newly stored slot through the same
  completion path and resume the existing script machine.

This is the final compatibility boundary, not an attribution of the unnamed
original caller to 0x4411 itself. CI guards that the completion call stays
behind the pending-DONE test. The subsequent reward transaction ordering is
independently proven by `Recv_NC_ITEMDB_QUESTREWARD_ACK -> completion mutation
-> Send_NC_QUEST_DB_SET_INFO_REQ -> QuestNext`.

An invalid selected slot remains pending and causes 0x4412 to be sent again.
