# Kingdom Quest native World protocol

## Evidence boundary

The original 2016 KQ command enum and PDB-derived protocol structures resolve
the Header-22 family that project captures previously named only by observed
behavior. Numeric opcodes stay unchanged; names and field boundaries are now
source-level rather than guessed from timing.

Key correlations:

| Opcode | Original name | Proven body |
| --- | --- | --- |
| 0x5803 | NC_KQ_STATUS_REQ | u32 Handle |
| 0x5804 | NC_KQ_STATUS_ACK | u32 Handle, u8 Status, u16 NumOfJoiner, Name5[] |
| 0x5805 | NC_KQ_JOIN_REQ | u32 Handle |
| 0x5806 | NC_KQ_JOIN_ACK | u32 Handle, u16 Error |
| 0x580B | NC_KQ_NOTIFY_CMD | u8 length + message bytes |
| 0x5813 | NC_KQ_FAIL_CMD | empty |
| 0x581B | NC_KQ_LIST_REFRESH_REQ | empty |
| 0x581C | NC_KQ_LIST_TIME_ACK | i32 ServerTime + C tm (9 x i32) |
| 0x581D | NC_KQ_LIST_ADD_ACK | u16 count + PROTO_KQ_INFO_CLIENT[] |
| 0x581E | NC_KQ_LIST_DELETE_ACK | u16 count + u32 Handle[] |
| 0x581F | NC_KQ_LIST_UPDATE_ACK | u16 count + {u32 Handle,u8 Status,u16 NumOfJoiner}[] |
| 0x5824 | NC_KQ_JOINING_ALARM_CMD | KQ_JOINING_ALARM_INFO + u8 len + message |
| 0x5825 | NC_KQ_JOINING_ALARM_END_CMD | u32 Handle + u16 ID |
| 0x5826 | NC_KQ_JOINING_ALARM_LIST | u16 count + KQ_JOINING_ALARM_INFO[] |
| 0x5832 | NC_KQ_JOIN_LIST_ACK | u16 Error + u8 count + KQ_JOIN_CHAR_INFO[] |

Name5 is 20 bytes. KQ_JOINING_ALARM_INFO is u32 Handle, u16 ID, u8 MinLevel,
u8 MaxLevel. KQ_JOIN_CHAR_INFO is u8 level, u8 class, Name5, u8 team.

## Important corrections

The inherited CH22 type-27 name GotIngame was wrong. It is
NC_KQ_LIST_REFRESH_REQ. The client also sends it during initial World entry,
which explains why legacy code used it as a bootstrap trigger.

The old 0x581C response wrote only constant 0x4d0bc167. That value is a Unix
timestamp, but the original body is 40 bytes: signed 32-bit ServerTime followed
by a complete C tm.

Type 29 is NC_KQ_LIST_ADD_ACK. An empty list is exactly u16 zero, so the
definition-safe empty response remains valid after renaming.

Capture-era interpretations of types 30/31/37/38 are superseded by the
original structures: list delete, list update, joining-alarm end and
joining-alarm list.

## Runtime consequence

World now answers STATUS_REQ from typed live state and LIST_REFRESH_REQ with a
complete LIST_TIME_ACK plus an empty LIST_ADD_ACK. Repeated KQ refreshes no
longer replay unrelated one-time World-login callbacks.

JOIN_REQ remains disabled even though its packet structures are known:
admission still requires source-backed KQ definitions, schedule/status and
membership rules.

## JOIN_LIST_ACK structure closed

The PDB-derived `NC_KQ_JOIN_LIST_ACK` (0x5832) structure is now represented
explicitly instead of the old capture-era "26-byte type-50 notice" guess:

```text
u16 Error
u8  Count
Count * KQ_JOIN_CHAR_INFO {
    u8  Level
    u8  Class
    char Name5[20]
    u8  Team
}
```

`KingdomQuestJoinCharacterInfo` is therefore exactly 23 bytes and
`KingdomQuestProtocol.CreateJoinListAck` serializes the native body. The
client request handler is not enabled yet because the request-side selector
fields have not been extracted into the committed project evidence; no handle
or membership query is guessed.


## Additional source-level request/command layouts

The remaining membership/team packet boundaries are now explicit:

```text
NC_KQ_JOIN_CANCEL_REQ  0x5807: u32 Handle
NC_KQ_JOIN_CANCEL_ACK  0x5808: u32 Handle + u16 Error
NC_KQ_JOIN_LIST_REQ    0x5831: u32 Handle
NC_KQ_TEAM_SELECT_REQ  0x5837: u8 TeamType
NC_KQ_TEAM_SELECT_ACK  0x5838: u16 Error + u8 TeamType
NC_KQ_TEAM_SELECT_CMD  0x5839: Name5[20] + u8 TeamType
NC_KQ_TEAM_TYPE_CMD    0x583A: u8 TeamType
NC_KQ_PLAYER_DISJOIN   0x583B: u32 Handle + u32 CharacterNumber
```

The old project capture's one-byte type-58 packet is therefore no longer
unknown: it is `NC_KQ_TEAM_TYPE_CMD`. Runtime builders are present for these
server packets, but request handlers are not enabled until their admission/team
decision rules and raw `Error` values are proven.


## JOIN_LIST_REQ live without an invented Error code

`NC_KQ_JOIN_LIST_REQ (0x5831)` is now handled as its native body defines it:
one `u32 Handle`.

The response path deliberately does not assume that `0` or the observed
`0x0991` from `JOIN_ACK` is also the `JOIN_LIST_ACK.nError` value.
`KingdomQuestJoinListReplyRegistry` must receive the exact per-Handle
`ushort Error` from a later authoritative admission/session owner. Only when
that value **and** the native Level/Class/Name5/Team participant roster exist
does World emit `NC_KQ_JOIN_LIST_ACK`.

This makes the request path operational without collapsing two distinct Error
fields into one guessed constant.


## LIST_REQ and SCHEDULE_REQ live through explicit response windows

The PDB proves both request bodies but not the WorldManager's range-selection
algorithm:

```text
NC_KQ_LIST_REQ      (0x5801): u32 StartHandle + u32 EndHandle
NC_KQ_SCHEDULE_REQ  (0x5809): u32 StartHandle + u32 EndHandle
```

World now handles both without assuming inclusive/exclusive bounds or paging
rules. `KingdomQuestRangeReplyRegistry` keys an **exact requested pair** to
an explicitly supplied `NewStartHandle`, `NewEndHandle` and ordered list
of KQ Handles. The handler resolves those Handles through the source-owned
definition registry and emits the already-native LIST_ACK/SCHEDULE_ACK
serializers.

No comparison, sorting or automatic range filter is performed in the reply
registry. Until the real scheduler supplies a window, the request is logged and
left unanswered rather than receiving fabricated list contents.


## Capture correlation for LIST_ADD_ACK

The old project capture sizes independently validate the native 141-byte client
entry layout. Total packet size is:

```text
2-byte opcode + 2-byte count + 141 * N
```

The observed 7477 / 7477 / 1132 / 850 / 145 byte packets therefore contain
exactly 53 / 53 / 8 / 6 / 1 entries. This also resolves the previously isolated
`0x03ec` and `0x03ef` words immediately before the Lost Mini Dragon titles
as the native `PROTO_KQ_INFO_CLIENT.ID` field at entry offset +7; `Title`
starts at +9 and is a fixed 64-byte array.

The capture remains evidence for batching/order, but it no longer supports the
old variable-title or guessed sub-list framing.
