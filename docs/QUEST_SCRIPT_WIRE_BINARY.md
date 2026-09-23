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
continues through `QuestNext`. The handler does not inspect QuestDialog text
markers before continuing; `[MENU]`, `[BUTTON]`, and related tags are client
presentation data, not server-side stop conditions.

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

The verified 2304-record source contains **19,924** SAY commands:

- 13,004 NPC-talker commands;
- 6,920 ME-talker commands;
- exactly 68 NPC-talker commands include the optional third `NPCNo` operand;
- exactly five Quest 15 Start lines spell the dialog operand with a trailing
  comma: `SAY 1502, NPC` through `SAY 1506, NPC/ME`.

There are 19,856 logical two-operand SAY lines and 68 three-operand SAY lines.

The binary tokenizer's default delimiter string at `0x007087FC` is whitespace
only, and `CQuestParserScript::IsDigitStr` at `0x00636FF0` requires every
character of the numeric token to be a digit. Therefore the exact native
preprocessing step that makes those five comma-bearing source lines usable is
**UNRESOLVED**. The emulator handles only this source-proven trailing-comma
variant; it does not generalize commas into a new quest-script grammar.

## Runtime alignment

Handler17 now writes the native 0x4401 body instead of a sequence-number
approximation:

- the first WORD is the actual QuestID;
- the full 101-byte QSC layout is emitted;
- SAY uses a DWORD dialog ID, exact NPC/ME talker enum and the optional NPCNo;
- the five authoritative Quest 15 `<DialogID>,` spellings are normalized only
  at the dialog-ID token boundary;
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


## Explicit END versus parser EOF

The native QuestNext dispatch table maps `QSC_END = 1` to
`0x005BE0D9`. That branch sends the current QSC through
`CQuestZone::Send_NC_QUEST_SCRIPT_CMD_REQ` and then reaches
`CQuestZone::QuestClose`.

This is distinct from parser EOF. `CQuestParserScript::ParserNext` emits
`QSC_MAX = 35` when no next token exists. QuestNext tests command 35 before the
dispatch table and closes the quest without sending a `0x4401` QSC packet.

For textual `END`, the only command-specific semantic field is `Cmd = 1`;
there are no END operands. The emulator therefore sends the proven 101-byte QSC
shape with:

```text
Cmd                 DWORD 1
IsPigeonStartType   BYTE  0
Data[96]            zeroed (unused by QSC_END)
```

and then closes the local quest-script session. Natural end-of-program/EOF
continues to close locally without fabricating an END packet.

The verified corpus contains **9,446** explicit textual `END` commands, so
this is a live NA2016 path rather than forward-compatibility behavior.


## QuestDialog `[MENU]` corpus impact

Cross-correlating every authoritative SAY reference with
`data_questdialog` shows that `[MENU]` is common inside normal quest-script
dialogs:

- **6,088** SAY occurrences reference a dialog containing `[MENU]`;
- those references cover **4,798** distinct dialog IDs;
- they occur across **2,303 of 2,304** quests;
- by stage: Start 2,554, Action/Doing 2,060, Finish/End 1,474.

Therefore treating `[MENU]` as a server-side instruction to terminate the
quest script would cut off essentially the entire supplied quest corpus. The native
0x4402 ACK path contains no such text check: after validating QuestID/current
QSC, QSC_SAY stores the DWORD result and resumes QuestNext.

Handler17 now follows that behavior. Menu/button tags remain opaque client-facing
QuestDialog content; only the actual QSC script controls server continuation.
