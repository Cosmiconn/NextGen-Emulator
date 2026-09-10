# Step 37 Status — Quest Turn-in / Completion Capture Reconstruction

Date: 2026-09-10
Branch: `nextgen-step14-quest-scripts`
Base: `e434ed38d91ff20502e94f47ce47c3f06284080b`

## Scope

Capture-first investigation of the quest completion interval after the fifth QuestProgressUpdate, with QuestID 956 as the primary correlation target. No runtime implementation was performed.

## Completed

- Inventoried attempts 4–7 from the newly supplied `mitschnitt.zip`.
- SHA256 recorded for all four PCAPNGs.
- Reassembled TCP streams for Zone port 9022.
- Confirmed the per-connection XOR key-position handshake (`0x0807`) and used the existing `NetCrypto` implementation to decrypt client→server traffic.
- Enumerated the complete QuestProgressUpdate sequence for both clients.
- Confirmed five `0x440D` packets for QuestID 956 per client.
- Confirmed body `01 63 01 BC 03`, giving the observed values 355 / 956 / 0x01.
- Rechecked the current client `QuestData.shn`: quest 956 contains objective target 355 with kill flag 1 and amount 5.
- Rechecked `MobInfo`: mob 355 is `Mandragora`.
- Rechecked quest 956 script: finish sequence is SAY 104829–104834 followed by `DONE` and `LINK 957`.
- Reconciled the PDB procedure record for `CQuest::SetQuestDone`: its CodeOffset is `0x22F2B0`, corresponding to `0x006302B0`; this routine searches the player quest list and calls the lower-level completion mutation at `0x0062F610`.
- Rechecked `CQuestZone::Recv_NC_ITEMDB_QUESTREWARD_ACK` at `0x005BF7F0`: it resolves the quest record from the ACK's quest identifier, copies the 32-byte record, directly calls `0x0062F610`, then sends `NC_QUEST_DB_SET_INFO_REQ` and calls `QuestNext`.

## Critical new completion finding

The binary now provides a direct reward-ACK → completion path:

```text
NC_ITEMDB_QUESTREWARD_ACK
        ↓
CQuestZone::Recv_NC_ITEMDB_QUESTREWARD_ACK
        ↓
resolve PLAYER_QUEST_INFO by QuestID
        ↓
0x0062F610 completion mutation
        ↓
PQS_DONE (2) / PQS_SOON (3)
        ↓
Send_NC_QUEST_DB_SET_INFO_REQ
        ↓
QuestNext
```

The PDB-associated `CQuest::SetQuestDone` entry is `0x006302B0`; that wrapper also reaches `0x0062F610`. The ItemDB reward ACK handler directly invokes the mutation helper in the observed machine code.

## Critical source/capture consistency check

The human description of attempt 7 labels the earlier five-Slime task as Zach's quest, while the current QuestData/resource evidence maps QuestID 956 to the Mandragora objective and NPC ID 31 / Skill Master Ruby. The capture description and resource mapping therefore cannot both be accepted as the same QuestID-956 event without additional evidence.

This remains unresolved. No mapping or runtime correction is made by assumption.

## Turn-in result

The interval between the fifth QuestID-956 progress packet and the first QuestID-10 progress packet was checked for decoded client→server packets. No Header-17 quest dialog/selection request was isolated there.

The new binary evidence changes the completion picture: the quest is completed after an `NC_ITEMDB_QUESTREWARD_ACK`, but the corresponding ACK and the preceding reward request are not yet isolated in Attempt 7. Therefore the capture-to-completion trigger remains unresolved.

## Status

### Proven

- QuestProgressUpdate count and payload for QuestID 956.
- QuestData objective correlation 355 / amount 5.
- `CQuest::SetQuestDone` PDB procedure entry at `0x006302B0`.
- `0x006302B0` quest-record lookup and call to `0x0062F610`.
- `0x0062F610` raw completion mutation: `2=PQS_DONE`, `3=PQS_SOON`.
- `CQuestZone::Recv_NC_ITEMDB_QUESTREWARD_ACK` at `0x005BF7F0` directly invoking `0x0062F610` and then `Send_NC_QUEST_DB_SET_INFO_REQ` / `QuestNext`.

### UNRESOLVED

- exact event that sends `NC_ITEMDB_QUESTREWARD_REQ`;
- exact reward request/ACK packet framing and fields;
- NPC turn-in request packet in Attempt 7;
- capture-to-ItemDB-ACK correlation;
- whether `PQS_REWARD` is used before reward completion;
- reward item selection/application details;
- quest-list/status refresh packet details;
- resolution of the Zach/Slime versus QuestID-956/Ruby/Mandragora inconsistency.

## Runtime / SQL

No runtime code changed.
No SQL schema changed.
No `.shn` runtime loading introduced.
No new quest status semantics were implemented.

## Tests

No build or runtime test was claimed. This step is a static/capture/binary audit only.
