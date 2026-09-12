# Original `AbnormalStateContainer::AbstateElementInObject::aeo_Set` binary findings

## Evidence

Source: original `Zone.exe` and `Zone.pdb` supplied for the project.

The PDB contains the symbol:

`AbnormalStateContainer::AbstateElementInObject::aeo_Set`

with locals including:

- `caster`
- `stateid`
- `strength`
- `starttick`
- `sklidx`
- `empowkeeptime`
- `restcount`
- `justkeep_millisec`
- `enchantfrom`
- `pAbsSetData`
- `start`
- `element`
- `maxdup`
- `alchat`
- `Engarg`

The implementation is at VA `0x0040A340` and returns `bool`; its epilogue is `ret 0x2c`.

## Proven update semantics

`asl_AbstateSet` at `0x0040D020` finds an existing runtime AbState entry with `0x00409B10`. When found, it does **not** remove/free that runtime entry before calling `aeo_Set` at `0x0040A340`.

The existing-entry path first calls the current source/definition object's virtual method at vtable slot `+0x34`, passing the existing runtime entry and the current AbState definition pointer, then calls `aeo_Set`.

Therefore SET on an already-active AbState is an **in-place update/refresh path**, not proven equivalent to remove+add.

## Strength handling

At `0x0040A3B9` the requested strength is used to select the SubAbState record:

`definition + strength * 9 * 4 + 0x0C`.

The selected record must exist; otherwise `aeo_Set` returns false.

If the target object has the relevant mode flag at `object + 0x120`, the requested strength is raised to at least `existing_runtime_strength + 1` and then clamped to the highest available SubAbState strength. The selected SubAbState is recalculated after this clamp.

This establishes that the native SET path has explicit existing-strength/update rules. A generic remove-then-add implementation is therefore not behaviorally proven equivalent.

## `justkeep_millisec` / KeepTime handling

The DWORD at `aeo_Set` stack argument `+0x28` is used as the explicit keep-duration input in the implementation; this corresponds to the PDB local `justkeep_millisec` in the relevant path.

When an existing definition/record is available, the code computes:

`adjusted = requested_justkeep_millisec - old_selected_SubAbState_KeepTime`

and, if the explicit value was non-zero, adds the **new selected SubAbState KeepTime** back:

`requested_justkeep_millisec = adjusted + new_selected_SubAbState_KeepTime`.

This matters when the selected strength changes: the explicit duration is adjusted by the difference between old and new SubAbState KeepTime values.

If the explicit just-keep value is zero, the duration base comes from the selected SubAbState KeepTime plus the separate `empowkeeptime` input.

## Runtime timer conversion

The resulting duration is converted to the native runtime timer representation at `0x0040A4E7` onward.

For duration `D` milliseconds, the code uses an adaptive resolution:

- `D <= 60,000 ms`: approximately `ceil(D / 10)` native units.
- `D > 60,000 ms`: approximately `ceil(D / 100)` native units.

The conversion uses reciprocal constant `0x10624DD3` and the `+0x3E7` rounding term.

After conversion, the result is scaled by the value returned from `0x00416160`, then divided by 1000, and the `starttick` input is incorporated into the stored runtime value. The exact semantic name of that `0x00416160` return value remains UNRESOLVED here.

## Other runtime state initialized by `aeo_Set`

On success the function stores, among other fields:

- runtime AbState index at `[entry+0x04]`
- source/caster object pointer at `[entry+0x08]`
- selected strength at `[entry+0x18]`
- another input/runtime value at `[entry+0x1C]`
- timer/expiry representation at `[entry+0x20]`
- additional effect-derived values at `[entry+0x24]`, `[entry+0x28]`, `[entry+0x54]`, `[entry+0x58]`, `[entry+0x5C]`, `[entry+0x70]`

The function also invokes the object's `+0x528` virtual method after a successful `aeo_Set` call from `asl_AbstateSet`.

## Emulator consequence

The current emulator implementation in `NextGen.Zone/Game/Buffs/Buffs.cs` removes an existing same-ID buff and constructs a fresh `Buff`:

`existing.Deactivate(this); CurrentBuffs.Remove(existing);`

That behavior is **not yet justified by the original binary**. It should remain unchanged until the exact mapping of the native runtime entry fields and the reset path is completed, but it must not be described as a faithful implementation of native `asl_AbstateSet`.

## Still UNRESOLVED

1. Exact source-level type/name mapping of every `aeo_Set` stack argument from the PDB signature versus compiler-generated call sites.
2. Exact meaning of `empowkeeptime` versus `restcount` in all branches.
3. Exact semantics of `start`, `element`, `maxdup`, `alchat`, and `Engarg` in the effect-processing loop.
4. Exact meaning of native `0x00416160` return value used for timer scaling.
5. Exact reset/removal behavior of `asl_AbstateReset`.
