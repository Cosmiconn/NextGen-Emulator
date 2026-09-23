# Quest QSC native command audit — corrected command identities

## Scope

This document records the textual/native quest command identities recovered
directly from the original NA2016 `Zone.exe`. It supersedes the earlier
misidentification of command 29 as `LINK`.

## Native command-name table

The original parser command-name table maps the command indices as follows:

| Value | Command |
|---:|---|
| 0 | ERROR |
| 1 | END |
| 2 | SAY |
| 3 | SCENARIO |
| 4 | CALLPS |
| 5 | CLEAR |
| 6 | ACCEPT |
| 7 | CANCEL |
| 8 | PROGRESS |
| 9 | FAILED |
| 10 | DONE |
| 11 | LINK |
| 12 | ABORT |
| 13 | DELETE_ITEM |
| 14 | CREATE_ITEM |
| 15 | DROP_ITEM |
| 16 | ; |
| 17 | IF |
| 18 | GOTO |
| 19 | : |
| 20 | SET |
| 21 | ADD |
| 22 | SUB |
| 23 | GET_PLAYER_RACE |
| 24 | GET_PLAYER_CLASS |
| 25 | GET_PLAYER_LEVEL |
| 26 | GET_PLAYER_GENDER |
| 27 | GET_PLAYER_EMPTY_INVENTORY |
| 28 | REPEAT_QUEST_GIVE_UP |
| 29 | UNKNOWNED |
| 30 | SET_ABSTATE |
| 31 | RESET_ABSTATE |
| 32 | IS_ABSTATE |
| 33 | GET_ITEM_LOT |
| 34 | MAX |
| 35 | EOF |

The independent `CQuestParserScript::CommandRun` reconstruction agrees with
this table for IF=17, GOTO=18, SET=20, ADD=21 and SUB=22.

## Corrected dispatcher identities

The original Zone quest dispatcher establishes:

- command 11 `LINK` -> `0x005BE0EE`;
- command 30 `SET_ABSTATE` -> `0x005BE325`;
- command 31 `RESET_ABSTATE` -> `0x005BE46B`;
- command 32 `IS_ABSTATE` -> `0x005BE514`;
- command 33 `GET_ITEM_LOT` -> `0x005BE5D5`.

The previous version of this document incorrectly described
`0x005BE46B` as LINK. That code actually belongs to RESET_ABSTATE and its
name-string lookup / player-vtable call must not be used as evidence for LINK.

## DONE

The command-name table maps textual `DONE` to command **10**. In
`CQuestZone::QuestNext`, the command dispatch table at `0x005BF020` routes
command 10 to `0x005BED38`.

That path reads the command's quest ID, resolves the player's quest record
through `0x0062F210`, and calls `0x006300D0`. The latter is a quest-ID
wrapper that resolves the same 32-byte player-quest record and then calls the
proven `CQuestZone::IsRewardAbleQuest` logic at `0x0062FF40`.

Critically, the command-10 branch does **not** test whether the active parser was
entered through QuestStart, QuestDoing, or QuestEnd. `DONE` is therefore a
stage-independent quest command in the native QuestNext dispatcher.

The complete supplied corpus confirms that this matters:

- Start: **351** DONE commands;
- Action/Doing: **1** DONE command (Quest 2230);
- Finish/End: **2251** DONE commands.

All 351 Start-stage DONE occurrences have an `ACCEPT` earlier in the same
Start script. Treating DONE as Finish-only silently leaves those immediate
completion quests in progress and is not native-compatible.

## SET_ABSTATE

At `0x005BE325`, the command consumes the abnormal-state name, strength and
keep-time fields, resolves the state definition and reaches
`ShinePlayer::so_AbnormalState_Set`. The native strength range and duration
rules remain the contract documented by the dedicated AbState audits.

## LINK

The real LINK command is command 11 at `0x005BE0EE`.

It consumes a WORD target quest ID from the QSC payload and resolves the target
quest's **effective** player quest status. The surrounding native dispatcher then
routes:

- effective status 4 / 5 / 20 -> `CQuestZone::QuestStart`;
- effective status 6 / 7 -> `CQuestZone::QuestDoing`;
- effective status 8 -> `CQuestZone::QuestEnd`;
- other statuses do not enter one of those linked stages.

The linked Start/Doing path continues through `QuestNext`; the End path enters
the target quest's finish processing. Full instruction-level LINK evidence and
corpus impact are recorded in `docs/QUEST_LINK_BINARY.md`.

## Consequence

Textual `LINK` and native command 11 are now directly correlated by the
original command-name table and dispatcher. The old rule that kept textual LINK
separate from a supposed command-29 QSC_LINK is superseded.


## Item command failure semantics

Direct disassembly closes the remaining return/packet gap for commands 13 and
14.

### `QSC_DELETE_ITEM = 13`

QuestNext enters the command at `0x005BE253`. The QSC payload is read as a
WORD ItemID at `QSC+0x05` and a DWORD lot at `QSC+0x07`, then passed to
helper `0x00527B60`.

That helper totals all matching inventory stack quantities before mutation. If
the requested lot is positive and greater than the total, it returns false
without issuing the deletion. If the lot is non-positive, the helper replaces it
with the total available quantity, which is the native ALL behavior.

When the helper returns false, QuestNext calls
`CQuestZone::Send_QUEST_ERROR_TO_CLIENT(0x0C0A)` and then
`CQuestZone::QuestClose`.

### `QSC_CREATE_ITEM = 14`

QuestNext enters at `0x005BE2BC`, reads the same WORD ItemID / DWORD lot shape,
and calls helper `0x00528930`. An immediate helper failure produces
`Send_QUEST_ERROR_TO_CLIENT(0x0C0B)` followed by `QuestClose`.

### QSC error wire shape

`CQuestZone::Send_QUEST_ERROR_TO_CLIENT` is `0x005BD870`. It builds a
101-byte `STRUCT_QSC` and sends it through the already-proven
`Send_NC_QUEST_SCRIPT_CMD_REQ` (`0x4401`) path:

```text
Cmd                 DWORD 0
IsPigeonStartType   BYTE  0
Data +0x00          DWORD failed QSC command
Data +0x04          DWORD 0
Data +0x08          WORD  error code
remaining Data      zero
```

This establishes both the item-command failure control flow and the exact
client error packet fields used by the emulator. No symbolic enum name is
assigned to `0x0C0A` or `0x0C0B` beyond their proven DELETE/CREATE failure
branches.


## CANCEL failure semantics

QuestNext command 7 begins at `0x005BEA72`. It resolves both the current
`PLAYER_QUEST_INFO` through `0x0062F210` and the command QuestID's
`QUEST_DATA` through `0x00635FF0`.

If either lookup fails, the branch calls
`CQuestZone::Send_QUEST_ERROR_TO_CLIENT(0x0C04)`, then
`CQuestZone::QuestClose`. The error QSC records failed command 7 through the
same proven `0x4401` QSC_ERROR layout.

When both records exist, `QUEST_DATA.Repeatable` at offset `+0x12` selects
the already-reconstructed mutation:

- repeatable -> write player quest status 4 (`PQS_REPEAT`);
- non-repeatable -> remove the player quest record.

The emulator now requires both SQL records before mutating and mirrors raw
error `0x0C04` plus script close on lookup failure. No symbolic enum name is
assigned to `0x0C04`.
