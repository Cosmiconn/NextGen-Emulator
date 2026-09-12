# Quest/AbState RE — `AbnormalStateContainer::AbstateElementInObject::aeo_Set`

## Provenance

Reverse-engineered from the supplied original `Zone.exe` and matching `Zone.pdb`.
No emulator implementation is treated as evidence for native semantics.

## Native signature

PDB local/argument order identifies `aeo_Set` parameters as:

1. `this`
2. `caster`
3. `stateid`
4. `strength`
5. `sklidx`
6. `starttick`
7. `empowkeeptime`
8. `restcount`
9. `justkeep_millisec`
10. `enchantfrom`
11. `pAbsSetData`

Native function: `0x0040A340`.

## Exact observations

### Strength selection

The requested `strength` is first used to select the SubAbState record. When an existing runtime state is present, the requested strength is compared with the existing runtime strength (`this+0x18`). If the requested value is lower, it is raised to `existing_strength + 1`; the result is then capped by the available maximum strength entry.

This is an update/refresh path, not a generic remove-then-add operation.

### Duration inputs

`justkeep_millisec` is stack argument `[ebp+0x28]`.

- If `justkeep_millisec != 0`, the runtime duration base is taken directly from this value.
- If `justkeep_millisec == 0`, the selected SubAbState `KeepTime` at `SubAbState+0x2B` is added to `empowkeeptime` (`[ebp+0x20]`), with `+999` before timer-unit conversion.

Therefore `empowkeeptime` and `justkeep_millisec` are distinct inputs; the latter is an explicit duration override, while the former participates in the normal KeepTime calculation.

### Adaptive timer conversion

The selected duration is converted according to the native branch at `0x0040A4FC`:

- duration <= `60000` ms: conversion uses 10-ms units
- duration > `60000` ms: conversion uses 100-ms units

The `+999` before conversion makes the operation a ceiling conversion rather than truncation.

The same adaptive conversion is also applied to the explicit `justkeep_millisec` path.

### Start-time handling

After duration conversion, the code derives a time value from the global server-time value at `0x14D41A70` and passes it through the subsequent initialization/validation path. This demonstrates that the native runtime stores derived timing information rather than simply retaining the raw script KeepTime value.

The exact semantic meaning of the PDB parameter `starttick` remains **UNRESOLVED** until the surrounding caller/consumer path is fully mapped.

### Runtime fields observed

The function writes, among others:

- `runtime+0x04` = resolved ABSTATEINDEX
- `runtime+0x08` = caster/object pointer
- `runtime+0x0C` = result of a caster/object virtual lookup
- `runtime+0x18` = selected strength
- `runtime+0x1C` = `sklidx`
- `runtime+0x20` = converted/derived timing value
- `runtime+0x24`, `+0x28` = additional timing values derived from the selected duration and later state checks
- `runtime+0x54` = result of the state-specific object operation
- `runtime+0x70` = result of the global lookup at `0x4A2EF0`

The exact source-level names of all these runtime fields are not assumed merely from their offsets.

## Important consequence for the emulator

The current emulator behavior that removes an existing buff and creates a fresh one is **not proven equivalent** to native `aeo_Set`.

Do not replace this with a guessed refresh implementation. The next required RE target is the consumer path for the runtime timing fields and the remaining `aeo_Set` parameters (`starttick`, `restcount`, `enchantfrom`, `pAbsSetData`).
