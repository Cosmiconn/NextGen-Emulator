# Quest LINK — native binary and corpus audit

## Native identity

The original Zone command-name table maps textual `LINK` to quest command
value **11**. The QuestNext dispatcher routes command 11 to
`Zone.exe 0x005BE0EE`.

The LINK payload supplies a WORD target quest ID. The native path resolves the
target quest and computes its effective player quest status before selecting a
target script stage.

## Proven status routing

The command-11 branch calls the target quest-status resolver and dispatches the
result after subtracting 4. The observed branches are:

| Effective status | Native linked stage |
|---:|---|
| 4 = PQS_REPEAT | QuestStart |
| 5 = PQS_ABLE | QuestStart |
| 20 = PQS_READ_ABLE | QuestStart |
| 6 = PQS_ING | QuestDoing |
| 7 = PQS_FAILED | QuestDoing |
| 8 = PQS_REWARD | QuestEnd |

The QuestStart and QuestDoing branches continue through `QuestNext`. The
QuestEnd branch enters the target quest's finish processing.

Original diagnostic strings independently identify the called stage helpers as
`CQuestZone::QuestStart`, `CQuestZone::QuestDoing`,
`CQuestZone::QuestEnd`, and the surrounding `QuestNext` path.

## Supplied corpus

The verified 2304-quest corpus contains **350** textual LINK commands:

- **348** have numeric operands;
- **2** have blank operands (quests 6 and 385);
- **253** numeric links target `current QuestID + 1`;
- **24** are self-links;
- one source operand, `LINK 300010` in quest 30015, does not name an
  existing quest in the supplied corpus.

The malformed/missing target is preserved as source data. The emulator does not
silently rewrite it to a guessed quest ID.

## Runtime implementation

`Handler17` now executes numeric LINK targets through the exact target
`QuestScriptInfo`, not by reverse-looking-up the first SAY dialog. This is
important because many first SAY IDs are shared by multiple quests/stages.

The target stage comes from
`QuestNpcStartResolver.TryResolveLinkedStage`, which reuses the native
effective-status reconstruction:

- 4/5/20 -> Start;
- 6/7 -> Action/Doing;
- 8 -> Finish/End.

A fresh `QuestScriptMachine` is created for the exact linked quest and stage.
The current dialog session then continues through that machine, so subsequent
SAY/IF/GOTO/ACCEPT/DONE operations remain bound to the target quest rather than
to an ambiguous dialog ID.

Blank, zero, out-of-WORD-range and missing targets are not invented. They leave
the current parser flow without a fabricated link destination.

## Evidence boundary

This closes LINK semantics for the supplied numeric corpus. It does not repair
source-data typos or assign extra behavior to malformed/blank LINK operands.
