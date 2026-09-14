# Native AbState RESET cross-reference

Status: reverse-engineering evidence, no emulator semantic change in this step.

## Original Zone.exe

The original `Zone.exe` contains the literal log string:

`AbnormalStateContainer::AbstateListInObject::asl_AbstateReset`

The string is referenced by the function at VA `0x0040A930`. This identifies `0x0040A930` as the native `AbnormalStateContainer::AbstateListInObject::asl_AbstateReset` implementation.

Relevant observed structure of `0x0040A930`:

- It is a `__thiscall`-style function (`ECX` is the object pointer).
- It accepts two stack arguments and returns `bool` (`ret 0x8`).
- The second stack argument is used as a runtime AbState object: the function reads `[arg2+0x04]`, `[arg2+0x18]`, etc.
- It validates the AbState index against the original AbState definition table (`0x87B060`), and returns false for an invalid/missing definition.
- It invokes object callbacks after the reset path; in particular the function calls the target object's virtual slot `+0x528` after the runtime state has been updated.
- It writes the current server time (`0x14D41A70`) into the runtime entry at offset `+0x20` before the final object callback.

This is materially different from a proven simple dictionary-ID delete operation. The exact mapping of the two source-level parameters (`me`, `stateindex`, `key` in the PDB locals) is still unresolved.

## QSC RESET_ABSTATE

The QSC dispatcher maps command value `31` to the RESET handler at approximately `0x005BE46B`.

That handler:

1. Reads `STRUCT_QSC + 0x05` as a NUL-terminated AbState name.
2. Resolves the name through `0x00418F80` against the original AbState dictionary.
3. On success, reads the resolved definition's `+0x22` value (the AbState index).
4. Passes that index to a virtual function at offset `+0x650` on the object stored at `[quest/player + 0xB4]`.

The exact source-level identity of that `+0x650` virtual slot is not asserted here; it must not be conflated with the separate player virtual slots observed elsewhere.

## Current emulator implication

`QuestRuntime.ResetAbstate` currently resolves the name and removes the matching emulator buff by ID. This remains a provisional approximation. The native `asl_AbstateReset` path proves that reset involves runtime-state handling and object callbacks; it does **not** yet prove that the emulator's current removal behavior is equivalent.

No code change is made solely from this finding.
