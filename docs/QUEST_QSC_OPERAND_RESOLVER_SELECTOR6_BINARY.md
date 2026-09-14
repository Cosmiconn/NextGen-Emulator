# Quest QSC operand resolver — selector 6 binary evidence

## Scope

Original `Zone.exe` was inspected directly. This note records only semantics supported by the machine code.

## `0x006370A0`

`0x006370A0` is the native QSC operand resolver used by `CQuestParserScript::CommandRun`.

Observed argument layout:

- `ECX` = parser object (`this`)
- `[EBP+0x08]` = output `DWORD*`
- `[EBP+0x0C]` = parser context, supplied by callers as `[this+0x83C]`
- `[EBP+0x10]` = selector
- `[EBP+0x14]` = operand/value
- `ret 0x10`

Selectors `0..5` read `[this + 0x18 + selector*4]` into the output and return success.

Selector `7` jumps directly to the common literal path at `0x006370E5`, which copies `[EBP+0x14]` to the output and returns success. Therefore selector 7 is definitively an immediate/literal operand selector.

## Selector 6

Selector 6 enters `0x006370C8`:

```asm
movzx edx,word [ecx+0x83c]
mov    ecx,[ecx+0x838]
push   edx
call   0x62f210
test   eax,eax
je     failure
movzx  eax,byte [eax+0x17]
mov    [output],eax
```

`0x0062F210` was inspected directly. Its input is a `WORD`. The object at `ECX` contains:

- `+0x0C` = non-null guard
- `+0x08` = count
- `+0x10` = pointer to entries

It scans entries of **0x20 bytes** and compares the requested WORD against each entry's first WORD. On a match it returns `base + index*0x20`; otherwise it returns null. Thus `0x62F210` is proven to be a bounded WORD-keyed table lookup.

If the lookup succeeds, selector 6 reads the matched entry's byte at `+0x17` and then immediately reaches the common path `0x006370E5`, which **overwrites the output with `[EBP+0x14]`**. Therefore the final selector-6 operand value is still the fourth argument `[EBP+0x14]`; the lookup is a required validation/side-effect path, but its higher-level semantic name is unresolved.

## Important consequence

Do not implement selector 6 merely as an alias for selector 7 without preserving the native validation condition. Native behavior can fail when the table/context lookup at `0x62F210` fails.

The exact semantic identity of the table at `this+0x838` and the meaning of entry byte `+0x17` remain **UNRESOLVED**.

## Cross-reference with `CommandRun`

For `SET`, `ADD`, and `SUB`, `CommandRun` passes:

- selector from QSC command `+0x0D`
- operand value from QSC command `+0x11`

For `IF`, it performs two resolver calls:

- first selector `+0x05`, value `+0x09`
- second selector `+0x11`, value `+0x15`

The resulting values are then compared using the six native comparison operators.

## Status

- Selector 0..5: **PROVEN** variable-array lookup.
- Selector 7: **PROVEN** literal/immediate.
- Selector 6: **PROVEN** WORD-table validation followed by literal output; higher semantic identity **UNRESOLVED**.
