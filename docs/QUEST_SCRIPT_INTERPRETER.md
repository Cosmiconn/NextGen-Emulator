# Quest Script Interpreter – Step 14

The normalized `data_quest_script` table is the SQL representation of the original QuestData script corpus. The importer is one-time migration tooling; the runtime does not read `QuestData.shn`.

## Verified corpus

The supplied `QuestData.shn` contains exactly **2304** records. The one-time importer parses the custom fixed `QUEST_DATA` record (`0x2A8` bytes) plus the three length-delimited scripts in on-disk order **Start, Doing, End**, and emits SQL rows for all 2304 quests.

The supplied client and server `QuestData.shn` copies are byte-identical. The verified source SHA-256 is:

`8c4ba17267967883169142c736e6d31d1a016c843d61411da7bca8dd2448b`

## Script opcode corpus

The complete supplied corpus contains exactly these textual opcodes:

- `ACCEPT` 2410
- `CANCEL` 12
- `CREATE_ITEM` 206
- `DELETE_ITEM` 1474
- `DONE` 2603
- `END` 9446
- `GET_ITEM_LOT` 104
- `GET_PLAYER_EMPTY_INVENTORY` 676
- `GOTO` 4
- `IF` 3036
- `LINK` 350
- `SAY` 19924
- `SCENARIO` 52
- `SET_ABSTATE` 51

No other textual opcode occurs in the supplied 2304-record corpus.

The CI audit is now mandatory and fails when its required corpus is absent or when the exact corpus counts change; it is no longer silently skipped.

## Control flow

Labels and `GOTO` targets are audited without assigning unproven semantics. Cross-stage and undefined targets are reported for review rather than rewritten automatically.

All **3036** textual `IF` lines in the supplied corpus fit the runtime evaluator's
existing `IF <left> <comparison> <integer> GOTO <label>` grammar:

- 2256 × `IF RESULT == <integer> GOTO <label>`
- 676 × `IF VAR1 < <integer> GOTO <label>`
- 104 × `IF RESULT < <integer> GOTO <label>`

There are no other IF operand/operator shapes in this QuestData snapshot. CI
checks these exact counts. This establishes complete IF syntax coverage for the
supplied NA2016 corpus; it does **not** claim binary-equivalence for native
operand-selector forms that are absent from this corpus.

## LINK

The original command-name table now directly maps textual `LINK` to native
quest command **11**, whose QuestNext dispatch is at `0x005BE0EE`.
The linked quest is selected by its effective status:

- 4 / 5 / 20 -> Start;
- 6 / 7 -> Action/Doing;
- 8 -> Finish/End.

Handler17 implements this using the exact target QuestID and a fresh
`QuestScriptMachine` for the resolved target stage, avoiding ambiguous
dialog-ID reverse lookup. The verified corpus contains 348 numeric LINKs and
two blank LINKs. One source value (`Quest 30015 -> LINK 300010`) is invalid
for the supplied corpus/WORD target and is intentionally not repaired by guess.
See `docs/QUEST_LINK_BINARY.md`.

## Implemented runtime commands

The current runtime adapter has explicit implementations for the commands for which native/corpus evidence is sufficient, including `ACCEPT`, `CANCEL`, `DONE`, `LINK`, item operations, the player-value queries used by this corpus, `SET_ABSTATE`, `SCENARIO`, and progress handling. Any command whose exact native side effect is not established remains isolated rather than being guessed.

Quest 1 / Baby Steps remains a corpus regression check and contains the known `SAY 202 NPC`, `SAY 203 NPC`, `IF RESULT == 1 GOTO MARK1`, `:MARK1`, `ACCEPT`, `END` structure.
