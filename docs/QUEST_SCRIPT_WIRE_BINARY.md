# Quest script wire contract — 0x4401 / 0x4402

## Scope

This audit resolves the normal quest-script client request/acknowledgement pair
from the supplied NA2016 `Zone.exe` and `Zone.pdb`. It also corrects an older
PCAP-only interpretation that treated the first WORD of the dialog packet as a
sequence counter.

## Native send path

`CQuestZone::Send_NC_QUEST_SCRIPT_CMD_REQ(WORD, STRUCT_QSC*)` begins at
`Zone.exe 0x005BA960`.

The function writes:

```text
+0x00  WORD  0x4401
+0x02  WORD  nQuestID
+0x04  STRUCT_QSC (101 bytes)
total         105 bytes
```

The original PDB proves `STRUCT_QSC` has size **101**:

```text
+0x00 DWORD Cmd
+0x04 BYTE  IsPigeonStartType
+0x05 BYTE  Data[96] union
```

The parser constructor at `0x00636F60` initializes
`IsPigeonStartType = 0`.

For `QSC_SAY = 2`, the PDB resolves the union member as:

```text
+0x00 DWORD nID
+0x04 QUEST_SCRIPT_TALKER TalkerType
+0x08 WORD  NPCNo
```

`QUEST_SCRIPT_TALKER` is directly enumerated in the PDB:

- `QS_TALKER_NPC = 0`
- `QS_TALKER_ME = 1`
- `QST_MAX = 2`

The native parser branch at `0x00638875..` additionally proves that
`NPCNo` defaults to `0xffff`; when the talker is NPC an optional third
numeric token replaces it.

## Client ACK

The PDB gives `PROTO_NC_QUEST_SCRIPT_CMD_ACK` a concrete size of **7 bytes**:

```text
+0x00 WORD  nQuestID
+0x02 BYTE  nQSC
+0x03 DWORD nResult
```

With the 2-byte network opcode, the complete client packet is 9 bytes and uses
`0x4402`.

`CQuestZone::Recv_NC_QUEST_SCRIPT_CMD_ACK` begins at
`0x005BF0D0`. It verifies both:

- incoming `nQuestID` equals the currently driven quest ID;
- incoming `nQSC` equals the current parser command.

For QSC_SAY it stores the complete DWORD `nResult` as the parser result and
continues through `QuestNext`.

## Important 0x4403 distinction

`0x005BAA80` is
`CQuestZone::Send_NC_QUEST_DB_SET_INFO_REQ(PLAYER_QUEST_INFO*, STRUCT_QSC*)`,
not a client quest-update packet.

The corresponding PDB structure is 39 bytes:

```text
PROTO_NC_QUEST_DB_SET_INFO_REQ
+0x00 ZoneHeader  (6 bytes)
+0x06 BYTE nQSC
+0x07 PLAYER_QUEST_INFO (32 bytes)
```

Together with its 2-byte command this produces the observed 41-byte
`0x4403` packet sent to the GameDB path. The emulator intentionally replaces
that persistence hop with direct SQL writes; it must not mirror `0x4403` to
the game client.

## Supplied QuestData corpus

All **19,924** SAY commands in the verified 2304-record source fit the native
text grammar used by the reconstructed parser:

- `SAY <DialogID> NPC`: part of 13,004 NPC-talker commands;
- `SAY <DialogID> ME`: 6,920 commands;
- exactly 68 NPC-talker commands include the optional third `NPCNo` operand.

There are 19,856 two-operand SAY lines and 68 three-operand SAY lines. No other
SAY operand shape occurs in this corpus.

## Runtime alignment

Handler17 now writes the native 0x4401 body instead of a sequence-number
approximation:

- the first WORD is the actual QuestID;
- the full 101-byte QSC layout is emitted;
- SAY uses a DWORD dialog ID, exact NPC/ME talker enum and the optional NPCNo;
- unused SAY union bytes remain zero.

The 0x4402 handler now consumes the full DWORD result and validates the echoed
QuestID and QSC command before advancing the script.

NPC selection also carries the already-resolved Start/Action/Finish stage into
Handler17, avoiding dialog-ID reverse lookup when the same dialog ID occurs in
multiple quests or stages.

## Evidence boundary

This audit covers the normal QSC_SAY request/ACK pair. Other QSC commands are
sent to the client only where their native dispatch proves that behavior; no
additional client packets are inferred from enum names alone.
