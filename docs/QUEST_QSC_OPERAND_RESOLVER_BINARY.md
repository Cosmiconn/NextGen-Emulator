# Quest QSC Operand Resolver — Binary Cross-Reference

## Scope

This document records only findings directly observed in the original `Zone.exe` build from the supplied binary/PDB set.

## Native functions

- `CQuestParserScript::CommandRun` = `0x00639390`
- `CQuestParserScript::ParserNext` = `0x006387F0`
- operand resolver = `0x006370A0`

## Resolver calling convention

`0x006370A0` is called as a member-style function with four stack arguments and returns `bool` (`ret 0x10`). The caller supplies:

1. `+0x08` — pointer to DWORD output
2. `+0x0C` — WORD value loaded from parser `this + 0x83C`
3. `+0x10` — operand selector stored in the QSC record
4. `+0x14` — operand value stored in the QSC record

The selector is therefore the field at the QSC command's operand-specific offset (`+0x05` for the first IF operand, `+0x0D` for SET/ADD/SUB's value operand). It is not itself the operand value.

## Proven selector cases

### Selector 0

`0x006370A0` dispatches selector `0` to code that loads:

```text
[parser + 0x18 + operandValue * 4]
```

and writes that DWORD to the output pointer.

Therefore selector `0` is definitively a **quest-script variable reference**, with the operand value acting as the variable index.

### Selectors 1–5

The jump table entries for selectors `1`, `2`, `3`, `4`, and `5` all point to the same code as selector `0` in this binary. Consequently, for this exact build they currently execute the same variable-array lookup path. Their historical/source-level names are not inferred here.

### Selector 6

Selector `6` uses parser fields `+0x838` and `+0x83C`, calls `0x0062F210`, reads byte `+0x17` from the returned record, and writes a DWORD result. The exact semantic name of this selector is **UNRESOLVED**.

### Selector 7

Selector `7` has a distinct dispatch target at `0x006370E5`. The observed implementation ultimately writes the fourth stack argument (`+0x14`) directly to the output and returns success. This establishes the runtime behavior for selector `7`, but its source-level semantic name is **UNRESOLVED**.

## IF instruction

`CommandRun` resolves both IF operands through `0x006370A0` before dispatching the comparison operator. The operator byte supports six comparisons:

- `0` → `==`
- `1` → `!=`
- `2` → `<`
- `3` → `>`
- `4` → `<=`
- `5` → `>=`

The native comparison result controls whether `ParserNext` (`0x006387F0`) is called to advance execution when the condition is false.

## SET / ADD / SUB

`CommandRun` uses the same resolver for the right-hand operand of:

- `QSC_SET = 20`
- `QSC_ADD = 21`
- `QSC_SUB = 22`

After resolution, the target index is read from QSC `+0x05`, and the operation is applied to `parser + 0x18 + targetIndex*4`.

## Emulator consequence

The current emulator's generic `EvaluateIf()` implementation does not yet model the native selector/value operand representation. It should **not** be expanded by guessing selector names. The next evidence target is the parser token construction in `ParserNext` and the script corpus, to establish which textual forms generate selectors `0..7`.

## Status

- Resolver existence: **PROVEN**
- Selector `0` = quest variable: **PROVEN**
- Selectors `1–5` execute same native path in this build: **PROVEN**
- Selector `6` exact semantic name: **UNRESOLVED**
- Selector `7` exact semantic name: **UNRESOLVED**
- IF six comparison operators: **PROVEN**
- SET/ADD/SUB use resolver: **PROVEN**
