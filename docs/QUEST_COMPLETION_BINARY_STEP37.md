# Quest Completion Binary Correlation — Step 37

Date: 2026-09-10
Branch: `nextgen-step14-quest-scripts`

## New binary finding

The PDB procedure record for `CQuest::SetQuestDone` has `CodeOffset=0x22F2B0`, mapping to executable address `0x006302B0` using the Zone `.text` base `0x00401000`.

The machine code at `0x006302B0` searches the player's 32-byte quest records by quest ID and, when a matching record is found, calls `0x0062F610` with the record pointer. The `0x0062F610` routine performs the previously proven completion mutation:

- non-repeatable quest: raw status `2` (`PQS_DONE`)
- repeatable quest: raw status `3` (`PQS_SOON`)
- increment completion/count at record `+0x13`
- update completion timestamp fields
- invoke the quest callback

Thus `0x006302B0` is the PDB-associated `CQuest::SetQuestDone` entry, while `0x0062F610` is the lower-level mutation helper reached by it. The optimized-code range overlap remains documented; no function is reassigned solely from range size.

## Critical completion caller

The PDB procedure record for `CQuestZone::Recv_NC_ITEMDB_QUESTREWARD_ACK` maps to `0x005BF7F0`.

Its x86 body proves:

1. receives `PROTO_NC_ITEMDB_QUESTREWARD_ACK*`;
2. checks packet offset `+0x0A` against `0x0BC1`;
3. consumes a character identifier at packet offset `+0x06`;
4. consumes a quest identifier at packet offset `+0x08`;
5. resolves the player's quest record through `0x0062F210`;
6. copies the 32-byte `PLAYER_QUEST_INFO` record locally;
7. calls `0x0062F610` with that record;
8. sets `CQuestZone + 0x900` to `1`;
9. calls `0x005BAA80`, the PDB-mapped `CQuestZone::Send_NC_QUEST_DB_SET_INFO_REQ`;
10. calls `0x005BDEF0`, the PDB-mapped `CQuestZone::QuestNext`.

This establishes a direct binary reward-ACK → quest-completion path.

## Proven chain

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

The PDB name `CQuest::SetQuestDone` belongs to `0x006302B0`, which itself also reaches `0x0062F610`. The ACK handler directly invokes the mutation helper instead of the `0x006302B0` wrapper in the observed x86 body.

## ACK structure evidence

PDB type data exposes these members for `PROTO_NC_ITEMDB_QUESTREWARD_ACK`:

- `header`
- `lockindex`
- `nQuestID`
- `err`

The exact complete C/C++ packet offsets and all semantics are not yet normalized into the emulator. The observed handler additionally consumes packet offsets `+0x06` and `+0x08`; their complete semantic mapping remains partially unresolved.

## Relation to Attempt 7

Attempt 7 proves five QuestID-956 `0x440D` progress packets and then the transition to QuestID 10. The binary evidence now shows that completion is performed after an ItemDB quest-reward acknowledgement, but the corresponding ItemDB ACK was not yet isolated in the capture interval.

Thus:

```text
5th QuestProgressUpdate
        ↓
reward/turn-in trigger       UNRESOLVED
        ↓
NC_ITEMDB_QUESTREWARD_ACK    PROVEN binary handler
        ↓
completion mutation          PROVEN
        ↓
QuestNext                    PROVEN binary caller path
```

## Still unresolved

- exact event that sends `NC_ITEMDB_QUESTREWARD_REQ`
- exact reward-request packet fields
- exact reward-ACK field semantics
- whether `PQS_REWARD` is used before the ItemDB ACK
- NPC turn-in packet in Attempt 7
- capture-to-ACK correlation
- reward item selection/application details
- QuestID-956 Zach/Slime versus Ruby/Mandragora source/capture inconsistency

## Runtime impact

None. No runtime code, SQL schema, packet definition, or `.shn` runtime loading was changed.
