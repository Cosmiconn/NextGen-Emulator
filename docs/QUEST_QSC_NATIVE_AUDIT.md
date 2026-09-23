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
