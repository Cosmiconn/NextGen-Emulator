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
- **24** numeric links are explicit self-links;
- one source operand is `LINK 300010` in quest 30015.

ParserNext directly resolves the two anomalies rather than leaving their
semantics open:

- command 11 shares the ACCEPT-style operand parser at `0x00638A9C`;
- a digit-only token is converted with the native integer parser and stored by
  `mov WORD [parser+0x7D5], AX`, so `300010` becomes WORD **37866**;
- a missing/non-digit operand falls back to WORD `[parser+0x83C]`;
- the parser setup routine at `0x00639650` stores its first WORD argument at
  `+0x83C`, and QuestStart/QuestDoing/QuestEnd call it with the current
  QuestID.

Therefore the blank LINKs in quests 6 and 385 are native **self-links**. Both
occur immediately after ACCEPT, so their effective status is PQS_ING and they
route into the same quest's Action/Doing stage. The truncated quest-30015 target
37866 does not exist in the supplied 2304-row corpus.

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

Handler17 mirrors the parser operand rules: blank/non-numeric source uses the
current quest ID, numeric source is reduced to the native low WORD, and a target
whose effective status cannot enter Start/Doing/End closes the active quest
script just as the unmatched native LINK branch calls QuestClose.

This makes the two blank corpus LINKs functional without inventing a target:
quests 6 and 385 self-link into Action after ACCEPT. Quest 30015's source value
300010 remains untouched in SQL/source data; only the runtime parser applies the
proven WORD truncation to 37866, which is absent and therefore closes.

## Evidence boundary

This closes LINK operand parsing and status routing for every LINK occurrence in
the supplied corpus. The source typo/anomaly itself is preserved verbatim; the
runtime reproduces the original parser rather than repairing the data.
