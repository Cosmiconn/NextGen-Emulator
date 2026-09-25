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

World answers STATUS_REQ from typed live state. LIST_REFRESH_REQ now follows
the original per-session refresh model: LIST_TIME_ACK is sent once, then the
visible (Status 0..4) snapshot is compared with the session's prior snapshot
and only LIST_DELETE/LIST_UPDATE/LIST_ADD deltas are emitted. Repeated KQ
refreshes still do not replay unrelated one-time World-login callbacks.

JOIN_REQ remains disabled at the network-handler boundary. Its admission
Error/status/class/gender/team rules are now recovered, but the emulator still
does not carry the original per-character `prisonmin` state or the complete
cross-KQ disjoin/registration-number mutation needed to reproduce the handler
without silently treating missing state as zero.

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


## JOIN_LIST_REQ Error/cooldown behavior recovered

`NC_KQ_JOIN_LIST_REQ (0x5831)` is one `u32 Handle`. The original
`CKQServer::Recv_NC_KQ_JOIN_LIST_REQ` now closes the ACK semantics:

```text
0x3118  success
0x3119  requested/effective KQ BF does not exist
0x311A  list-request cooldown has not elapsed
```

Before the BF lookup, World checks the session-owned current KQ Handle with
`InKQStatusRunning`. When that current KQ is Status 4, its Handle replaces
the request Handle; otherwise the request Handle is used unchanged.

The cooldown is source-backed as well. The original `SingleData.shn` entry
`KQPlayerList_ResetListCoolTime` is exactly `5`. The server compares the
last successful JOIN_LIST request time plus five seconds against current
`_time32`; only a successful response updates that timestamp. Invalid-handle
and cooldown replies carry a zero entry count.

The live handler now follows those branches directly. It no longer depends on
the former external `KingdomQuestJoinListReplyRegistry` Error placeholder.


## LIST_REQ / SCHEDULE_REQ selection recovered from WorldManager.exe

The PDB bodies remain:

```text
NC_KQ_LIST_REQ      (0x5801): u32 StartHandle + u32 EndHandle
NC_KQ_SCHEDULE_REQ  (0x5809): u32 StartHandle + u32 EndHandle
```

The original NA2016 executable closes the previously unresolved selection
behavior. `CParserClient::fc_NC_KQ_LIST_REQ` at `.text+0x12980` validates
the request but never reads either handle for selection. It walks the complete
377-byte scheduler array and copies the 141-byte client prefix only when the
unsigned Status byte is <= 4. `NewStartHandle` and `NewEndHandle` are the
first and last emitted Handles.

`CParserClient::fc_NC_KQ_SCHEDULE_REQ` at `.text+0x12E00` also ignores the
request bounds, but copies the 141-byte prefix of **every** scheduler entry.
Its ACK bounds are the first and last scheduler Handles.

The previous exact-pair `KingdomQuestRangeReplyRegistry` workaround is
therefore no longer used by the live handlers. This is not an inferred paging
rule; the original handlers simply do not apply the request values.

For an empty result the original code explicitly writes
`NewStartHandle = 0xFFFFFFFF` and count zero, but leaves the four
`NewEndHandle` bytes uninitialized on its stack. The emulator writes zero for
those bytes instead of reproducing an original memory-disclosure bug. No
gameplay meaning is assigned to that sanitized empty `NewEndHandle`.

## LIST_REFRESH delta behavior recovered

`CKQServer::Ack_NC_KQ_LIST_REFRESH` at `.text+0x549E0` keeps a per-client
snapshot of the same Status<=4 visible list. On the first refresh it sends the
40-byte LIST_TIME_ACK once. It then performs, in order:

```text
old handle absent now                  -> LIST_DELETE_ACK
same handle, Status/NumOfJoiner changed -> LIST_UPDATE_ACK
new handle absent in old snapshot       -> LIST_ADD_ACK
```

The session snapshot is then replaced with the current visible list. LIST_ADD
is flushed at 53 entries: the executable checks the accumulated
`2 + 141*N` body against `0x1D26` after adding an entry. The project's
existing captures independently show 53 / 53 / 8 entry batches, so the runtime
now uses the same 53-entry batch boundary.


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


## Simple runtime lifecycle broadcasts

Several later KQ packets are now byte-exact builders because their PDB bodies
are fully self-contained:

```text
NC_KQ_RESTDEADNUM_CMD       0x5818: u8 Number
NC_KQ_ENTRYRESPONCE_ACK     0x581A: u8 Reply + u32 EncHandle
NC_KQ_MOBKILLNUMBER_CMD     0x5822: u16 CurrentMobKill + u16 DemandMobKill
NC_KQ_SCORE_INFO_CMD        0x5836: u32 Score[2]
NC_KQ_SCORE_BOARD_INFO_CMD  0x583C:
    u8 UseRound + u8 Round
    + TEAM_SCORE_INFO Red
    + TEAM_SCORE_INFO Blue
NC_KQ_WINTER_EVENT_2014_SCORE_CMD 0x583D:
    TEAM_SCORE_INFO Red + TEAM_SCORE_INFO Blue

TEAM_SCORE_INFO = u8 WinFlag + u8 Score.
```

These are serialization primitives only. They do not invent when a KQ decrements
its dead-player allowance, how an entry Reply/EncHandle is produced, how mob
kills are counted, or how scores are awarded. The variable `SCORE_CMD` /
`SCORE_SIMPLE_CMD` and reward packets remain outside the runtime until their
nested/variable structures and gameplay triggers are fully proven.

## Native reward data and GameDB boundary

Original Zone PDB fixes `KINGDOM_QUEST_REW` at 128 bytes with two
`u16[15]` arrays: Reward handles and RewardRate. The source SHN exposes those
same bit patterns as signed 16-bit columns; runtime projection is therefore
explicit and bit-preserving rather than changing the raw imported source type.

`ShinePlayer::sp_KQReward` consumes all 15 slots in order. Each slot takes
one `well512_GetRandom(1000)` sample and passes when
`sample < RewardRate[i]`; only then does it call
`RewardData::rd_FindHandle(Reward[i])`. The emulator now models this pure
dice boundary from caller-supplied 0..999 samples and does not manufacture a
second RNG stream.

Native reward-table selection has two distinct overloads.
`KQRewardDataBox::operator[](ushort)` linearly compares against
`KINGDOM_QUEST_REW.ID`; `operator[](char*)` linearly compares the fixed
32-byte `IndexString`. `so_ply_KQRewardStruct` passes the
`PROTO_KQ_INFO.RewardIndex` word to the ID overload, whereas
`so_ply_KQRewardIndex` uses the string overload. Neither path interprets a
numeric value as a physical SHN row number.

In this exact source snapshot, 22 distinct KQ RewardIndex values are used and
the native ID lookup has no row for exactly `45,51,57,63,71,79,83`.
Those are preserved as real lookup misses. Source scenarios corroborate
separate named reward paths for Warrior's Code (`HERO_*`), KDSpring
(`REW_KQ_SPRING_*`) and KDArena (`REW_KQ_ARENA_*`), so the emulator
must not synthesize numeric aliases for the missing IDs.


The handle target is the original 435-row `ShineReward.shn`
(SHA-256 `09acc18d24877fc5dfa9ab431d8dd45561e36ffd48b518cbdf05ddd1810a325f`). It is now an independently validated optional runtime
source: manifest/hash, 435-row table count and exact source-row projection
must all succeed before World exposes it to the KQ reward resolver.
All 300 distinct nonzero handles referenced by the supplied
`KingdomQuestRew` corpus resolve; those used rows are 247 ITEM, 39 EXP and
14 MONEY. The checked-in compact SQL snapshot
retains all 435 original rows and all 16 fields: zero-valued trailing fields
use schema defaults and exactly 18 nonzero-tail rows are patched explicitly.
CI locks that LF-normalized generated representation by SHA-256 and source ordinal.
PDB fixes `ShineReward` at 66 bytes and the reward-type enum at
NONE/ITEM/EXP/MONEY/HONOR/HP_SOUL_STONE/SP_SOUL_STONE/GURAD_SOUL_STONE/
ATTACK_SOUL_STONE/CLASS_CHANGE/PET = 0..10, with MAX=11.
The recovered KQ reward function actively handles NONE through HONOR (0..4);
later types are not promoted to KQ behavior without another native path.

Zone builds `NC_KQ_REWARD_REQ` with opcode `0x5815`. Its fixed payload
base is 35 bytes (`u32 fame + u64 cen + 23-byte item-create request base`)
before the variable item list. Native `REWARDSUC_ACK=0x5816` is exactly
8 payload bytes and `REWARDFAIL_ACK=0x5817` exactly 10:
`u16 clienthandle + u32 charregistnumber + u16 lockindex`, with FAIL adding
`u16 error`.

Both original GameDB ACK handlers resolve and validate the player identity
before applying the LockIndex to the player's item-store object. Success and
Fail call different item-store virtual methods. Their symbolic method names
remain unresolved, so no commit/rollback label is invented in code. The
FAIL handler does not read the packet's Error field at all; the emulator still
preserves it in the native structure.

A mutation-free selection plan now composes the already-proven dice and
RewardHandle lookup layers. It preserves native slot order and classifies only
the proven positive KQ switch branches (ITEM/EXP/MONEY/HONOR); NONE is ignored,
lookup misses are retained as misses, and types 5..10 are retained without an
invented effect.

The ITEM argument is also now guarded against a tempting but incorrect direct
ItemInfo assumption. Across the 247 KQ-used ITEM handles, 161 handles / 95
distinct arguments exactly match `ItemInfo.inxname`; 86 handles / 36
arguments do not. Native `sp_KQReward` nevertheless sends every ITEM branch
through `TreasureChestMaker`. The shared classifier therefore records only an
ordinal exact ItemInfo match or an opaque TreasureChest argument and never
synthesizes an alias. The opaque corpus includes `Weapon3`,
`NamedWeapon4`, `HighDust`, `NorProduct`, `P_KQHBAT1` and
`Upsource15`.

No live grant/GameDB path is enabled yet because item generation, exact numeric
accumulation/overflow behavior, transaction persistence, and the scenario
completion trigger must be source-correlated before mutation.


## Original World ↔ Zone KQ lifecycle wire

The PDB also resolves the server-side lifecycle packets. They are now modeled
in `KingdomQuestServerProtocol`, deliberately separate from client
`Handler22` traffic:

```text
NC_KQ_W2Z_MAKE_REQ     0x580D:
    PROTO_KQ_INFO[377]

NC_KQ_Z2W_MAKE_ACK     0x580E:
    u32 Handle
    u16 Error

NC_KQ_W2Z_START_CMD    0x580F:
    PROTO_KQ_INFO[377]
    u16 NumOfJoiner
    PROTO_NC_KQ_JOINER[NumOfJoiner]

PROTO_NC_KQ_JOINER[5]:
    u32 CharacterNumber
    u8  TeamType

NC_KQ_Z2W_END_CMD      0x5810:
    u32 Handle

NC_KQ_W2Z_DESTROY_CMD  0x5811:
    u32 Handle
```

These native packets are carried over the emulator's custom World/Zone bridge.
The bridge adds only emulator routing metadata (MapID/Map.InstanceID) around
the original bytes. The original MAKE success Error is now proven and the Zone
returns it only after a successful local instance creation; no failure Error is
invented.


## Registry-backed native MAKE/START builders

The server-only protocol layer can now build `W2Z_MAKE_REQ` and
`W2Z_START_CMD` directly from the explicit per-Handle runtime state:

- `KingdomQuestProtocolDefinitionRegistry` supplies the complete 377-byte
  `PROTO_KQ_INFO`;
- `KingdomQuestZoneJoinerRegistry` supplies the exact 5-byte
  CharacterNumber/TeamType roster.

If either source is absent, the builder returns no packet. It does not derive
CharacterNumber from a player name, select a team, allocate a map, or invent
missing definition fields. The packets are still not sent over the emulator's
custom InterServer transport until the native MAKE/ACK decision path is
resolved.


## Native KQ structures are now bidirectional

The shared KQ protocol model now has byte-exact `TryRead` counterparts for
the existing writers:

- `PROTO_KQ_INFO_CLIENT` (141 bytes);
- `PROTO_KQ_INFO` (377 bytes);
- `PROTO_KQ_MAP_INFO` (26 bytes);
- `KQ_JOIN_CHAR_INFO` (23 bytes);
- `PROTO_NC_KQ_JOINER` (5 bytes);
- `SHINE_XY_TYPE` and the native 36-byte `tm`.

This does not add gameplay semantics. It closes the serialization boundary so
the Zone side can consume the exact World→Zone MAKE/START payloads instead of
maintaining a second field mapping.


## Raw MAKE_ACK return path

The emulator inter-server bridge now also carries the original
`NC_KQ_Z2W_MAKE_ACK (0x580E)` body back from Zone to World:

```text
u32 Handle
u16 Error
```

World stores that ushort unchanged in `KingdomQuestMakeAckRegistry`.
The original `CParserZone::fc_NC_KQ_Z2W_MAKE_ACK` compares it with
`0x0981`: that value calls `CKQServer::SetJoining`; every other value calls
`SetNoMapBF`.

The runtime now reproduces the proven transition for represented sessions:
non-`0x0981` moves the definition to Status 8. Success is accepted only from
Status 1 or 2, clears the participant view as `SetJoining` does and leaves
Status 2. The original invalid-status branch additionally destroys/frees map
state; that destructive edge remains guarded instead of being approximated.


## Vote wire family closed without policy inference

The PDB-derived vote packet bodies are now explicit builders:

```text
0x5828 VOTE_START_ACK:        u16 Error
0x5829 VOTE_VOTING_CMD:      Name5 Starter + Name5 Target + u8 VoteType
                              + tm EndTime + u8 Len + bytes[Len]
0x582B VOTE_VOTING_ACK:      u16 Error
0x582C VOTE_RESULT_SUC_CMD:  Name5 Target + u8 VoteRate + u8 Yes + u8 No + u8 Cancel
0x582D VOTE_RESULT_FAIL_CMD: Name5 Target + u8 Yes + u8 No + u8 Cancel
0x582E VOTE_CANCEL_CMD:      Name5 Target
0x582F VOTE_BAN_MSG_CMD:     u8 VoteRate + u8 Yes + u8 No + u8 Cancel
0x5830 VOTE_BAN_MSG_LOGOFF:  empty
0x5833 LINK_TO_FORCE_BY_BAN: u32 CharacterNumber + Name3[4]
0x5835 VOTE_START_CHECK_ACK: u16 Error
```

These serializers do not decide who may start a vote, which `VoteType` values
are valid, how the two source majority thresholds are selected, when a vote
passes, or what any raw Error means. Request handlers remain disabled until
those rules are tied to original behavior.


## JOIN admission Error values recovered

The original executable resolves the capture's formerly-ambiguous
`JOIN_ACK.Error = 0x0991`. `CParserClient::fc_NC_KQ_JOIN_REQ`
(`.text+0x12CC0`) adds `0x0991` to the return from
`CKQServer::PlayerJoin`; the successful return is zero. Therefore
`0x0991` is the native successful JOIN_ACK value.

| Error | Original branch |
| --- | --- |
| `0x0991` | PlayerJoin success |
| `0x0992` | KQ Handle/BF lookup failed |
| `0x0993` | joiner count reached 100 or MaxPlayers |
| `0x0994` | KQ Status is not 2 |
| `0x0995` | Level outside MinLevel..MaxLevel |
| `0x0996` | class bit absent from DemandClass |
| `0x0997` | gender bit absent from DemandGender |
| `0x0998` | PlayerJoin fallback for an unexpected IsJoinable result |
| `0x0999` | `PROTO_NC_CHAR_BASE_CMD::prisonmin != 0` |
| `0x099A` | the same character is already joined to the requested Handle |

`CKQ::IsJoinable` forms the class test as a 64-bit `1 << CharClass`
against DemandClass and the gender test as byte `1 << Gender` against
DemandGender. The project character-shape serializer independently uses those
same native source values: `Job << 2` and `Male << 7`.

The original JOIN request first rejects prison/already-in-target, then calls
`PlayerDisjoin(session)` **before** attempting `PlayerJoin` for the new
Handle. The emulator now carries the original DB-backed prison minute value
fail-closed as nullable state and has a source-correlated CharacterNumber plus
an atomic combined membership owner.

The live JOIN handler therefore executes the native sequence for characters
whose original PrisonMin is known: prison/same-handle precheck, session-owned
PlayerDisjoin, then PlayerJoin and JOIN_ACK. A nullable/unknown PrisonMin
produces no guessed native Error and no mutation.

## Team divide modes and initial assignment recovered

The original PDB names the enum values used by the executable:
`KQTD_RANDOM=1` and `KQTD_USERSELECT=2`.

`CKQServer::PlayerJoin` calls `GetKQTeamData(KQ ID)`. If no row exists, or
if the divide type is not USERSELECT (2), it writes neutral TeamType `2`.
For USERSELECT (2), the initial join chooses the currently smaller native team
counter; a tie selects numeric team `1`. That is an initialization for the
user-select mode, not the random-start divider.

Every supplied NA2016 `KQTeam.shn` row is RANDOM (1). Those joins therefore
remain neutral TeamType 2 until the start sequence invokes
`KQTeam_DivideRandom`.

At the Status-3 countdown expiry, `DoSetStart` writes Status 4, executes
`KQTeam_DivideRandom`, then `KQTeam_LeaveParty`, then sends
`NC_KQ_W2Z_START_CMD`. RANDOM division walks native joiners in order and
uses one `RandomBox::rb_1000()` sample each: values below 500 prefer team 1,
otherwise team 0, with floor(N/2) caps forcing balance while only one side has
reached that cap.

The executable also closes `RandomBox::rb_1000`: 16 MSVCRT-rand-seeded
WELL512 words feed the WELL output; it is normalized by 2^-32, multiplied by
1e11, truncated and reduced modulo 1000. The emulator source model preserves
that algorithm rather than substituting a framework RNG.


## Original prison state and JOIN_CANCEL result recovered

The supplied original `World00_Character.bak` closes the provenance of the
JOIN pre-check's `prisonmin` field. `tCharacter` contains
`nPrisonMin`; `p_Char_GetAllData` selects it, and
`p_Char_SetAllData` receives it as `smallint`. The prison procedures also
write that same column directly:

- `p_Prison_Add(... @nMinute smallint ...)` stores `nPrisonMin=@nMinute`;
- `p_Prison_UpdateCharPrisonMin` stores the minute value and routes zero back
  to `Rou` while nonzero remains `EldPri`;
- `p_Prison_End` explicitly writes `nPrisonMin=0`.

The original WorldManager PDB/EXE matches that database path:
`CParserCharDB::fc_NC_CHAR_BASE_CMD` stores the received character-base
packet and `CWMClientSession::GetPrisonMin` returns its 16-bit
`prisonmin` field. The KQ JOIN handler tests only whether that value is
nonzero before producing `0x0999`.

The original table has a default constraint for `nPrisonMin`, and
`p_Char_Create` omits the column, but the constraint expression itself has
not yet been decoded independently from the backup catalog. The emulator
therefore stores `characters.PrisonMin` as nullable: `NULL` is an
emulator-only provenance sentinel meaning "the original value is unknown".
It is deliberately not treated as zero.

The same executable now closes `NC_KQ_JOIN_CANCEL_REQ`. The request copies
its `u32 Handle` into the ACK, calls `CKQServer::PlayerDisjoin(session)`,
then returns exactly:

```text
0x09A1  PlayerDisjoin returned nonzero
0x09A2  PlayerDisjoin returned zero / no current KQ membership
```

`PlayerDisjoin` operates on the session's current KQ membership rather than
using the request Handle as the removal selector. On a real removal it deletes
the matching native joiner, decrements `PROTO_KQ_INFO.NumOfJoiner`, adjusts a
team counter when TeamType is 0/1, sends
`NC_KQ_PLAYER_DISJOIN_CMD(Handle, CharacterNumber)`, rebroadcasts the join
list, clears the session KQ handle to `0xFFFFFFFF`, and returns 1.

JOIN_CANCEL is now live on the same session-owned membership. Its ACK still
echoes the request Handle, while removal uses the current session KQ Handle.
On removal World preserves the original order: broadcast
`NC_KQ_PLAYER_DISJOIN_CMD(currentHandle, CharacterNumber)` to every Zone,
broadcast the refreshed `0x3118` JOIN_LIST to remaining KQ sessions, then
clear the session KQ Handle.

Original Zone behavior is also correlated: its
`wms_NC_KQ_PLAYER_DISJOIN_CMD` searches the KQ by Handle and calls
`KQPlayerInfoList::kqpil_DeletePlayerInfo(CharacterNumber)`. The emulator's
inter-server transport now carries those exact native 0x583B bytes and applies
that deletion to represented Zone KQ state.


## Combined native membership owner

`KingdomQuestMembershipEntry` binds native `CharacterNumber`,
Level/Class/Name and TeamType in one row. From that one row the runtime can
project both client `KQ_JOIN_CHAR_INFO` and World-to-Zone
`PROTO_NC_KQ_JOINER`, avoiding a name/ID reconciliation guess between two
independent registries.

Original `CWMClientSession::GetCharRegNo` returns the first DWORD of the
stored `PROTO_NC_CHAR_BASE_CMD`, i.e. `chrregnum`. That identity is now
cross-correlated through three independent original/runtime paths:

- World sends `PROTO_NC_CHAR_CHARDATA_REQ::chrregnum`; the original
  Character server logs the same request value as `nCharNo` and uses it for
  `p_Char_GetAllData(@nCharNo)`;
- original `p_Char_Create` returns `nCharNo = @@IDENTITY`;
- the emulator already serializes `Character.ID` in the first
  `PROTO_AVATARINFORMATION::chrregnum` position in CharacterList/Create
  responses.

`KingdomQuestCharacterIdentity` therefore exposes `Character.ID` as the
emulator's existing native CharacterNumber/chrregnum representation; this is
not a KQ-specific ID guess.

The source-proven Status-3 expiry is now live. Original World distinguishes
`RaidLeave` from normal `LeaveParty`; this emulator has no Raid model or Raid
creation path, so every grouped World session it can represent is a normal
Party. START therefore preflights every CharacterNumber back to the correlated
World session and validates its KQ Handle/Party state before mutation, then
runs the recovered order Status 4 -> KQTD_RANDOM division -> represented
normal LeaveParty -> W2Z START. Any session state the emulator cannot
represent authoritatively is fail-closed before Status 4.


## Zone MAKE_ACK error family and KQRegen lookup

Original Zone executable branches now identify the MAKE result values directly:

```text
NC_KQ_Z2W_MAKE_ACK.Error
0x0981  success
0x0982  duplicate KQ Handle
0x0983  KQ container/buffer full
0x098C  ScriptLanguage runtime lookup failed
```

The duplicate branch is live because the emulator can represent that exact
condition atomically. The container boundary is also now source-correlated:
RTTI identifies the global Zone owner as
`KingdomQuest::KingdomQuestContainer : List<KQElement>`, and its fixed
construction/destruction loop has exactly `0x12C = 300` KQElement slots.
The native "Buffer full" MAKE branch is therefore the exact 300-slot
`0x0983` condition.

The emulator models that capacity but does not send `0x0983` yet. Original
MAKE performs ScriptLanguage runtime lookup before testing the KQ container
for a free slot, so a live 0x0983 branch would be order-incorrect until the
source-equivalent script container is represented. `0x098C` is likewise not
used as a catch-all.

The MAKE script lookup is owned by `ScenarioBookShelf`, not by
`KQScriptManager`. `sbs_LoadScripts` reads the `PineScript/ScriptName`
catalog from `World/PineScript.txt`; `sbs_Read` tries
`ScenarioBookShelf/<key>.ps` first, then `LuaScript/<key>.lua`, and only
successfully loaded books enter the lookup tree. MAKE calls
`sbs_GetScenarioBook(ScriptLanguage,...)`; a null result is `0x098C`.

All 27 KQ ScriptLanguage keys are source-present: 18 as exact Lua entrypoints
and 9 as exact PineScript sources. Absence of a `.lua` file is therefore not
evidence for `0x098C`. The separate `KQScriptManager::kqsm_Load` path
reads `DialogFile` and has its own 64-entry manager; that capacity is not
the ScenarioBookShelf/MAKE limit.

The same Zone binary proves the static KQ regen lookup order:
`MobRegen/KingdomQuest/<name>.txt` first, then
`MobRegen/Instant/<name>.txt`. Global loading enumerates the same two
directories in that order. In the supplied Server.zip, none of
`KDArena`, `KDMine`, or `KDSpring` exists in the Instant directory,
so their missing KingdomQuest regen file cannot be repaired by the native
static fallback. Lua files are not consulted by `KQRegenTable`.

The native START path is now also separated from static regen loading.
`KQElement::kqe_QuestStart` walks the four KQ map slots, drops a previous
film, closes all doors, and invokes `CinemaComplex::cc_PlayFilm` with the
`ScriptLanguage` and `ScriptInitValue` tokens prepared during MAKE. It does
not blindly spawn the map's static regen table.

A PineScript `regengroup` action later resolves
`PineScriptMobRegenerator::psmr_find(sourceKey, groupIndex)`. Cache misses call
`psmr_Load(sourceKey)`, which looks up the 12-byte key in the already loaded
`KQRegenTable`, reads `MobRegenGroup`/`MobRegen` through its
`OptionReader`, and only then reaches `MobHatchery::mh_ScriptBreed`.
`KQRegenTable::kqrt_Load` itself permits exactly **50** source elements.

For the fifteen present static files used by this source snapshot, all rows use
the modern 7-field group / 16-field mob schema, totaling **732 group rows** and
**766 mob rows**. `KingdomQuestRegenSourceLoader` preserves those fields and
the native KingdomQuest->Instant lookup order as a source layer. It does not
activate groups or convert them to the emulator's simpler persistent spawn
representation; doing so before ScenarioBook/Lua execution is correlated would
change native semantics.


## SetDoneSkip and old-schedule deletion

Original WorldManager closes the skip-cleanup sequence without assigning new
meanings to the intermediate status values. `SetDoneSkip` writes Status 6,
broadcasts native `NC_KQ_W2Z_DESTROY_CMD`, frees the native map-slot
reservation, sends the reason notification(s) to the current joiners, clears
their session-owned KQ Handle, and broadcasts
`NC_KQ_JOINING_ALARM_END_CMD(Handle, ID)`.

The reason-message source is the exact supplied `MsgWorldManager.shn`
(SHA-256
`36b573c6f604a693cf0d0c7533fc90615233ae4a8be62f3ced9bd4119f25884e`,
3 rows). Reason 2 formats row 2 with Title/NumOfJoiner/MinPlayers. Reason 3
requests source indices 3 and 4; because they are out of range,
`WorldManagerServer::GetMsg` returns the executable's empty fallback, so
two zero-length KQ NOTIFY messages are source-correct for this snapshot.

`DelOldShceduleList` considers raw Status values 5 through 10 inclusive.
An entry becomes Status 11 only if a later-`ScheduleTime` entry with the same
KQ ID is also in that 5..10 range. The subsequent pass performs
`FreeMapLink`, `FreeJoiner`, then `Del(Handle)`. No semantic names are
invented for raw Status 7, 9 or 10.


## Z2W END, SetDone and logout preservation

`NC_KQ_Z2W_END_CMD (0x5810)` is Zone-to-World, not World-to-Zone. The
original World parser reads its `u32 Handle` and invokes
`CKQServer::SetDone`. SetDone writes Status 5, broadcasts W2Z DESTROY,
frees the native map-link allocation, then FreeJoiner clears live sessions'
KQ Handle. The scheduler entry and KQ joiner buffer are retained for the
later `DelOldShceduleList` lifecycle.

The emulator transport now follows the same direction and order; there is no
World-side END sender.

Original `CWMClientSession::Logout` also defines disconnect behavior:
`InKQStatusRunning(nKQHandle)` is true only for an existing Status-4 KQ.
Logout calls `PlayerDisjoin` only when that check is false. Thus running
KQ membership survives disconnect by design, while non-running membership is
removed through the normal PlayerDisjoin path. Reconnect restoration is now bounded by the original pre-rebind predicate.
The Character DB supplies `nKQHandle/sKQMap/nKQX/nKQY/dKQDate`; World
calls `CKQServer::IsExisted` before `JoinerInfoUpdateByLogin`.
`IsExisted` requires a non-`0xFFFFFFFF` Handle, an exact match against one
of that KQ's four native MapName slots plus live map-data resolution, then
decodes `SHINE_DATETIME` as year=(low4+2000), month/day/hour/minute/second
from 4/5/5/6/6 bits. It adds exactly ten minutes and accepts only
`now < saved+10min`.

Character.exe's `p_Char_GetKQMap` path proves the inverse SQL timestamp
packing: `year&0xF`, month at bit 4, day at bit 8, hour at bit 13, minute at
bit 18 and second at bit 24, using the corresponding field masks. The original
four-bit year therefore wraps after 2015; no wider emulator epoch is
substituted.

The emulator now models that packing and predicate without adding a lower-bound
check.
The DB-backed reconnect is now wired through the same boundary.
After the packed-date/map predicate succeeds, the emulator requires an
already-retained membership whose Name5 is byte-equal across all 20 bytes,
rebinds only the new World session Handle, and routes the saved KQ X/Y plus
the existing internal Map.InstanceID to Zone. It never creates a membership
from persisted Handle data.

The following native `CheckCharBannedInLogin` tests the joiner DWORD at
offset +0x20 for value 1; PlayerJoin initializes it to zero. The emulator now
preserves that DWORD verbatim as `LoginBanStateRaw` in the combined
membership row and carries it through clones/team transitions. New joins write
zero exactly, and a raw per-character setter exists for the future vote engine.
No semantic meaning is assigned to values other than the proven login check
for 1.

The value-1 login side effect itself remains disabled until the native
vote-success mutation plus disjoin/notification ordering is fully correlated;
the raw state model is not used as a substitute for that missing policy.


## Character save-location KQ persistence wire

Original Zone `PROTO_NC_CHARSAVE_LOCATION_CMD` is exactly 48 bytes. PDB
field names and `ShinePlayer::so_SaveLocation` offsets correlate it as
`u32 chrregnum`, a 20-byte normal coordinate (MapName[12]+X+Y),
`u32 kqhandle`, `map_kq[12]`, and `coord_kq(X,Y)`.

That KQ suffix feeds the original Character DB save-location procedure, which
persists `nKQHandle/sKQMap/nKQX/nKQY` and timestamps `dKQDate` with
`GetDate()`. The shared protocol model preserves those exact 48 bytes.

Zone disassembly further proves that the KQ suffix is independent of the
normal return-coordinate selection: it always stores
`FieldMap::fm_GetKQhandle()`, current FieldMap name and current X/Y.
The FieldMap constructor initializes the handle to `0xFFFFFFFF`. The
emulator now persists this suffix live, preserving a KQ instance's native
dynamic MapName and using a DB-side current timestamp. It still does not
replace the first/normal coordinate's existing policy: the native function has
separate rollback-event, guild-tournament, regen-city-link and other branches
that must be modeled before that return-location decision can be changed.
