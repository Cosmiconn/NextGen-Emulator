# Quest select-start wire contract — native binary audit

## Scope

This audit reconstructs the original Zone-server request/ACK pair used when a
client selects a concrete quest for the currently selected NPC.

Evidence source: the matching original NA2016 `Zone.exe` / `Zone.pdb`.

## Protocol identities

The original protocol symbols identify:

- `NC_QUEST_SELECT_START_REQ` = Header 17 / Type 15 / opcode `0x440F`
- `NC_QUEST_SELECT_START_ACK` = Header 17 / Type 16 / opcode `0x4410`

The PDB type information names the request fields:

- `nNPCID`
- `nQuestID`

and the ACK fields:

- `nNPCID`
- `nQuestID`
- `ErrorType`

## Request layout

`CQuestZone::Recv_NC_QUEST_SELECT_START_REQ` is the routine beginning at
`Zone.exe` VA `0x005C0550`.

Its request reads are unambiguous:

```text
[pReq + 0x00] WORD -> nNPCID
[pReq + 0x02] WORD -> nQuestID
```

Therefore the client request body is exactly four bytes:

```text
u16 nNPCID
u16 nQuestID
```

The earlier working note that included an `nCharNo` member in this wire
request is superseded. Character identity is already represented by the
connection/player object.

## Native validation flow

The original handler:

1. resolves `QUEST_DATA` from the requested `nQuestID`;
2. derives the quest/NPC status into the local `kQuestStatus`;
3. rejects the request unless the requested quest and NPC match the
   quest/NPC pair currently retained by the player's quest/NPC selection
   state;
4. on a matching pair, calls the quest script dispatcher at
   `0x005C0080` with the effective quest status and quest ID;
5. the dispatcher selects the appropriate quest-script path from the status;
6. the request is acknowledged as success only when that script dispatch
   succeeds.

This is important: `0x440F` is not a blind "accept QuestID" operation. It is
a validated selection of the quest already chosen for the current NPC.

## ACK layout

`CQuestZone::Send_NC_QUEST_SELECT_START_ACK` begins at
`Zone.exe` VA `0x005BB340`.

The native packet construction is:

```text
packet + 0x00 = WORD 0x4410
packet + 0x02 = first  unsigned-short argument
packet + 0x04 = second unsigned-short argument
packet + 0x06 = third  unsigned-short argument
send length   = 8 bytes total
```

The PDB field names and the sole native caller establish the body as:

```text
u16 nNPCID
u16 nQuestID
u16 ErrorType
```

The caller at `0x005C0688` passes the original request's NPCID and QuestID in
the first two positions.

## Observed native result values

Within `Recv_NC_QUEST_SELECT_START_REQ`:

- successful validated script dispatch -> `ErrorType = 0x0B41`;
- the examined rejection paths -> `ErrorType = 0x0B47`.

The higher-level enum names of these two numeric result values are not assigned
without a direct enum-name/value linkage. Their success/failure branch meaning
is proven by control flow.

## Emulator implementation

The emulator now exposes:

- `CH17Type.QuestSelectStart = 15`;
- `SH17Type.QuestSelectStartAck = 16`.

`Handler17.QuestSelectStartHandler` reads exactly the two request WORDs,
requires the requested NPC to be the character's current NPC target, reruns the
SQL-backed original quest-selection logic for that NPC, and accepts only when
the resolver's selected QuestID equals the request QuestID.

On success it enters the status-appropriate Start/Action/Finish dialog/script
path and sends `0x4410` with `0x0B41`. Invalid NPC/quest selections receive
the same ACK shape with `0x0B47`.

For compatibility with the already-observed direct NPC-dialog flow, an
identical dialog page already active in the current Handler17 session is not
sent twice; the `0x4410` ACK is still returned.
