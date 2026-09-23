# Original `AbnormalStateContainer::AbstateListInObject::asl_AbstateReset` binary findings

## Evidence

Source: original `Zone.exe` and `Zone.pdb` supplied for the project.

PDB symbol:

`AbnormalStateContainer::AbstateListInObject::asl_AbstateReset`

PDB signature:

`asl_AbstateReset(this, ShineObject*, ABSTATEINDEX)`

return type: `bool`.

The implementation matching this signature is at VA `0x0040A930` and ends with `ret 0x8`.

## Exact lookup path

The function receives:

- `this` in ECX
- `+0x08` = `ShineObject*`
- `+0x0C` = `ABSTATEINDEX`

It validates the ABSTATEINDEX against `0x318` and looks up the runtime AbState entry through global table `0x87B060[index]`.

The runtime entry is then used to recover its stored AbState definition/index information. The function also validates the corresponding definition and object-side state structures before performing the reset operation.

Invalid/missing index, missing runtime entry, or invalid definition paths return `false`.

## Proven reset effect

For a valid runtime state, the function reaches the object/definition-specific reset dispatch at approximately `0x0040AB38` and then stores the current server time:

`[runtime_entry + 0x20] = [0x14D41A70]`

Immediately afterward it invokes the target object's virtual method at vtable slot `+0x528`.

The function returns `true` after this path.

This is important: the native reset implementation does **not** simply perform the emulator equivalent of `RemoveBuff()` by deleting the runtime entry in the code examined here. It marks the runtime state's timer field with the current server time and triggers the object's `+0x528` update/notification path. Any physical list removal is therefore either deferred/implicit elsewhere or unnecessary in the native container representation.

## Relationship to `asl_AbstateSet`

`asl_AbstateSet` uses `0x00409B10` to find an existing runtime entry. `asl_AbstateReset` instead uses the supplied `ABSTATEINDEX` to reach the runtime entry through the global `0x87B060` table and then marks its timer field as now.

The reset path therefore supports the interpretation that the native runtime entry remains represented while being made immediately expired/invalidated, rather than being destroyed synchronously by `asl_AbstateReset` itself.

## QSC RESET_ABSTATE connection

The original QSC RESET_ABSTATE handler resolves the script's NUL-terminated AbState name through the same AbState dictionary and obtains the corresponding runtime ABSTATEINDEX before invoking the player-side reset path.

The exact player-side virtual dispatch used by the QSC handler is still being mapped separately; the binary identity of `asl_AbstateReset` itself is established by the PDB signature and the implementation above.

## Emulator consequence

The current emulator `QuestRuntime.ResetAbstate()` calls `character.RemoveBuff(abState.ID)`. This is functionally reasonable as an expiration/removal operation, but it is **not a binary-faithful representation of the native reset container mechanics** proven above.

A future faithful implementation should model immediate expiration plus the object's state-refresh/notification behavior rather than relying only on deletion, once the emulator's `Buff` lifecycle and object update path have been mapped.

## UNRESOLVED

1. Exact identity and source-level semantics of the object vtable `+0x528` call.
2. Exact identity of the definition/runtime virtual call at `0x0040AB3E` (`vtable +0x20`) and its side effects.
3. Whether native container/list cleanup occurs later from the timer/update loop after `[entry+0x20]` is set to current time.
4. Exact QSC player-side virtual slot used to reach `asl_AbstateReset`.
