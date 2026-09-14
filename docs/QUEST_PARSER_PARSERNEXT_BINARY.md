# Quest Parser `ParserNext` Binary Cross-Reference

## Scope

This document records findings from the original `Zone.exe` for
`CQuestParserScript::ParserNext` and `CQuestParserScript::CommandRun`.
No semantics are assigned where the binary does not prove them.

## Proven addresses

- `CQuestParserScript::ParserNext` = `0x006387F0`
- `CQuestParserScript::CommandRun` = `0x00639390`
- operand resolver used by CommandRun = `0x006370A0`
- parser token reader = `0x0063B2B0`
- string token reader = `0x0063B010`

The PDB address values in this build are `.text` offsets; the `.text` VMA is
`0x00401000`, therefore the PDB offset for ParserNext (`0x012377F0`) maps to
VA `0x006387F0`.

## Parser state / output area

`ParserNext` stores the parsed command at:

- `this + 0x7D0` = command/QSC value
- `this + 0x7D5` = parsed command-specific first field
- `this + 0x7D7` = parsed command-specific byte/field in several branches
- `this + 0x7D9` = parsed command-specific value
- `this + 0x7DD`, `+0x7E1`, `+0x7E5` = additional operands for multi-operand commands
- `this + 0x83C` = WORD context/index used by the operand resolver
- `this + 0x18 + index*4` = parser variable/result DWORD array

These offsets are binary observations; field names are intentionally not
invented.

## QSC command values proven by the PDB enum

The original QSC enum contains:

- `QSC_IF = 17`
- `QSC_GOTO = 18`
- `QSC_SET = 20`
- `QSC_ADD = 21`
- `QSC_SUB = 22`
- `QSC_GET_PLAYER_RACE = 23`
- `QSC_GET_PLAYER_CLASS = 24`
- `QSC_GET_PLAYER_LEVEL = 25`
- `QSC_GET_PLAYER_GENDER = 26`
- `QSC_GET_PLAYER_EMPTY_INVENTORY = 27`
- `QSC_REPEAT_QUEST_GIVE_UP = 28`
- `QSC_UNKNOWNED = 29`
- `QSC_SET_ABSTATE = 30`
- `QSC_RESET_ABSTATE = 31`
- `QSC_IS_ABSTATE = 32`
- `QSC_GET_ITEM_LOT = 33`

## `IF` layout

`CommandRun` for QSC 17 reads:

- `pCmd + 0x05` = left operand selector
- `pCmd + 0x09` = left operand value/additional operand data
- `pCmd + 0x0D` = comparison operator (`0..5` dispatch)
- `pCmd + 0x11` = right operand selector
- `pCmd + 0x15` = right operand value/additional operand data

Both operands are resolved through `0x006370A0`.

The comparison dispatch implements six operators:

1. `==`
2. `!=`
3. `<`
4. `>`
5. `<=`
6. `>=`

If the comparison is false, `CommandRun` calls `ParserNext` again. The exact
textual token-to-selector encoding is therefore separate from the comparison
semantics and must not be guessed from the emulator syntax alone.

## `GOTO` layout

QSC 18 (`GOTO`) passes the DWORD at `pCmd + 0x05` to `0x00637030`.
If that lookup succeeds, `CommandRun` returns success; otherwise it returns
failure. `0x00637030` searches parser-owned entries by string and returns the
associated value. The exact internal entry type/name is unresolved.

## `SET`, `ADD`, `SUB` layouts

For all three commands `CommandRun` resolves the value operand with
`0x006370A0` using:

- output = local DWORD
- context = `this + 0x83C` (WORD)
- selector = `pCmd + 0x0D`
- operand data/value = `pCmd + 0x11`

Then the destination index is read from `pCmd + 0x05` and the parser variable
array is updated:

- QSC 20 `SET`: `vars[index] = value`
- QSC 21 `ADD`: `vars[index] += value`
- QSC 22 `SUB`: `vars[index] -= value`

The corresponding native arithmetic semantics are therefore proven.

## Operand resolver `0x006370A0`

Call shape observed in CommandRun:

```text
ECX       = parser this
[EBP+08]  = output DWORD*
[EBP+0C]  = context
[EBP+10]  = selector
[EBP+14]  = operand data/value
```

The resolver returns success/failure and has `ret 0x10`.

Proven selector behavior:

- selectors `0..5`: output = `this + 0x18 + selector*4`
- selector `7`: output = `[EBP+0x14]` unchanged (literal/immediate)
- selector `6`: performs an additional lookup/validation through `0x0062F210`,
  but the final output is still `[EBP+0x14]`; the semantic reason for the
  validation is unresolved.

Therefore selector 7 is definitively an immediate/literal operand selector.
Selector 6 must not be given a semantic name until the `0x0062F210` path is
identified.

## Parser-side evidence for GOTO

In `ParserNext` the branch that emits QSC 18 (`GOTO`) is at approximately
`0x00638CA9`. It resolves a token with the parser variable/index context and,
when the token is not an indexed parser variable, falls back to a string/label
lookup path. This is consistent with the `CommandRun` lookup through
`0x00637030`, but the exact label table structure remains unresolved.

## Current emulator gap

The emulator `QuestScript.cs` currently evaluates `IF` using the textual form
`LEFT OP INTEGER GOTO LABEL` and its own dictionary of named variables. The
native binary instead represents both operands using selector/value pairs and
supports six native comparison operators. The emulator implementation should
therefore not be considered binary-equivalent for all operand forms yet.

This document does **not** change runtime behavior. It records the evidence
needed for the next implementation pass.
