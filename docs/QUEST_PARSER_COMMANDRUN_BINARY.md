# Native Quest Parser `CommandRun` Binary Cross-Reference

## Original binary

Target: original `Zone.exe` from the supplied executable/PDB set.

Recovered function:

`CQuestParserScript::CommandRun` → `0x00639390`

The function receives the parser command record through its first stack argument and dispatches on the DWORD at record offset `+0x00`.

## Dispatch

The native dispatcher accepts command values `0..0x21` and uses:

- jump table: `0x00639554`
- command/type classification bytes: `0x00639574`

Relevant recovered entries:

| Command value | Native target | Proven behavior |
|---:|---:|---|
| `16` | `0x006393BE` | invokes parser-owned routine at `0x0063B150`; exact command name unresolved |
| `17` | `0x006393D6` | evaluates two operands through `0x006370A0`, then performs comparison |
| `18` | `0x00639482` | passes `record+0x05` to `0x00637030`; this is the native GOTO path |
| `20` | `0x0063949D` | evaluates destination/value and writes result to parser variable array |
| `21` | `0x006394D7` | evaluates destination/value and adds to parser variable array |
| `22` | `0x00639515` | evaluates destination/value and subtracts from parser variable array |
| `29` | `0x00639546` | falls through to the common successful return; exact semantic name unresolved |

## Variable storage

The native variable array is addressed as:

`parser + 0x18 + index * 4`

For command 20 the final write is:

`[parser + index*4 + 0x18] = evaluatedValue`

For command 21:

`[parser + index*4 + 0x18] += evaluatedValue`

For command 22:

`[parser + index*4 + 0x18] -= evaluatedValue`

Therefore native SET/ADD/SUB semantics are now directly proven rather than inferred from the emulator.

## Native comparison evaluator

Command 17 obtains two operands using `0x006370A0` and then dispatches on a comparison selector stored at the command record's `+0x0D` field.

The six native cases are:

- equality (`==`)
- inequality (`!=`)
- less-than (`<`)
- greater-than (`>`)
- less-than-or-equal (`<=`)
- greater-than-or-equal (`>=`)

The boolean result is then tested; a false comparison enters the common successful parser return path, while a true comparison continues into `0x006387F0` (the parser state/next-command routine recovered separately).

## Operand record layout evidence

The CommandRun implementation reads command-record fields at:

- `+0x00` command value
- `+0x05` first operand / argument field
- `+0x09` second operand / argument field
- `+0x0D` comparison selector for command 17
- `+0x11` additional operand/value field
- `+0x15` additional operand/value field

The exact high-level type of every field is intentionally not asserted here because the binary uses overlapping DWORD accesses and the complete PDB structure correlation is still in progress.

## GOTO

Command value `18` calls `0x00637030` with the DWORD at record `+0x05`.

This confirms that the native GOTO command consumes its target through the command record rather than through the generic comparison evaluator.

## Important correction to emulator work

The native parser's SET/ADD/SUB implementation is independent of the earlier QSC dispatcher observation in `CQuestZone::QuestEnd`.

Both layers are now evidenced:

`CQuestZone::QuestEnd` → native QSC dispatch

and

`CQuestParserScript::CommandRun` → native parser command execution.

They must not be conflated merely because they share numeric command values in parts of the recovered command space.

## Remaining work

1. Resolve command values 16 and 17 against PDB/type information and parser source data.
2. Correlate the command-record fields with `STRUCT_QSC`/parser structures.
3. Resolve `0x006370A0` operand decoding, including literal vs variable references.
4. Correlate `0x006387F0` state transitions with `ParserNext`.
5. Only then extend the emulator's textual command set where syntax is directly supported by original script data.
