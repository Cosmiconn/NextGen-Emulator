# QSC Operand Selector Binary Findings

## Scope

Original `Zone.exe` / `CQuestParserScript` binary analysis. This document records only semantics directly supported by disassembly.

## Resolver

`CQuestParserScript` operand resolver: `0x006370A0`.

Signature shape proven by calling convention:

- `ECX` = parser object (`this`)
- `[EBP+08]` = output DWORD pointer
- `[EBP+0C]` = additional argument/context
- `[EBP+10]` = operand selector/index
- `[EBP+14]` = operand value/additional data
- `ret 0x10`

The resolver accepts selectors `0..7`; values above `7` fail.

## Selectors 0..5

The jump table at `0x006370FC` maps selectors `0,1,2,3,4,5` to the same code at `0x006370B6`:

```asm
mov eax,[ecx+eax*4+0x18]
mov [output],eax
mov eax,1
ret 0x10
```

Therefore selectors `0..5` all resolve a DWORD from the parser variable/result array at `this+0x18`, indexed by the selector value itself.

This is stronger than a generic "variable selector" statement: the selector directly selects one of six DWORD storage positions.

## Selector 6

Selector `6` enters `0x006370C8`:

```asm
movzx edx,word [ecx+0x83c]
mov    ecx,[ecx+0x838]
push  edx
call  0x0062F210
test  eax,eax
je    failure
movzx eax,byte [eax+0x17]
mov [output],eax
```

The resolver therefore consults a parser-held context at `+0x838/+0x83C` and a helper at `0x0062F210`, then reads byte `+0x17` from the returned record.

Immediately afterwards the shared code at `0x006370E5` writes `[EBP+0x14]` to the output and returns success. Consequently, the final returned value for this path is the fourth argument `[EBP+0x14]`, while the `0x0062F210` lookup is still a required validation/context operation.

The semantic name of selector 6 is **UNRESOLVED**. It must not be guessed from the apparent item/database context alone.

## Selector 7

Selector `7` jumps directly to `0x006370E5`:

```asm
mov ecx,[ebp+0x14]
mov [output],ecx
mov eax,1
ret 0x10
```

Therefore selector `7` is definitively a **literal/immediate-value operand**: the value supplied in `[EBP+0x14]` is copied unchanged to the output.

## CommandRun correlation

`CQuestParserScript::CommandRun` at `0x00639390` calls `0x006370A0` for the operands of native comparison, `SET`, `ADD`, and `SUB` commands.

The command record fields used by these calls are:

- `[pCmd+0x05]` = destination variable index for SET/ADD/SUB
- `[pCmd+0x0D]` = operand selector/index
- `[pCmd+0x11]` = operand value/additional data
- comparison uses the corresponding second operand at `[pCmd+0x11]` and `[pCmd+0x15]` as observed in `CommandRun`.

Native comparisons dispatch six operators:

`==`, `!=`, `<`, `>`, `<=`, `>=`.

## Implementation consequence

The current emulator's `EvaluateIf()` is not yet a 1:1 native implementation because it only parses an integer literal as the right-hand operand. Native `CommandRun` resolves typed operands through `0x006370A0`.

No emulator code is changed by this document. Selector 6 remains UNRESOLVED until its parser syntax and surrounding data structures are proven.
