# Quest / AbState Runtime Tick — Binary Findings

## Scope

Clean-room reconstruction of the original `Zone.exe` AbState runtime. This document records only behavior directly established from the original binary/PDB analysis. Unresolved semantics are explicitly marked.

## `AbstateElementInObject::aeo_Set`

Original address: `0x0040A340`.

The runtime object is populated with these confirmed fields:

| Offset | Confirmed meaning |
|---:|---|
| `+0x04` | `ABSTATEINDEX` |
| `+0x08` | caster/source pointer |
| `+0x0C` | value returned by a caster virtual call |
| `+0x18` | final/effective AbState strength |
| `+0x1C` | start/reference time |
| `+0x20` | primary absolute expiry time |
| `+0x24` | next periodic/tick time |
| `+0x28` | periodic/tick interval |
| `+0x54` | effect-derived result |
| `+0x58` | first copied `AbStateElementSetData` DWORD |
| `+0x5C` | second copied `AbStateElementSetData` DWORD |
| `+0x70` | result of AbState definition lookup |

The `+0x24` / `+0x28` relationship is directly visible in the native code: the engine computes a tick interval and stores it in `+0x28`, then adds it to a reference value and stores the resulting next-tick time in `+0x24`.

## Refresh / strength rule

When a runtime entry already exists, the native code compares the requested strength with the existing runtime strength at `+0x18`.

If the requested strength is not greater than the existing strength, the requested strength is raised to `existingStrength + 1`, then clamped to the maximum available strength for the selected AbState definition.

This is a proven native rule. It is not an emulator assumption.

The native path updates the existing runtime entry; it does not first free/delete the existing runtime object.

## Primary expiry (`+0x20`)

For the non-explicit-duration path the code first forms:

`selected SubAbState KeepTime + empowkeeptime + 999`

and then performs the original engine's time-unit conversion before storing the resulting value in `+0x20`.

For an explicit `justkeep_millisec` path, the explicit duration is converted instead.

The exact identity/meaning of the helper value returned by `0x00416160(26)` remains **UNRESOLVED**, so this document does not assign a final human-readable unit to the converted internal value.

`AbstateElementInObject::aeo_GetRestTime` at `0x00408990` reads `+0x20`, compares it with global server time at `0x14D41A70`, and returns zero once the current server time has reached/passed `+0x20`.

## `aeo_TickStruct`

PDB symbol:

`AbnormalStateContainer::AbstateElementInObject::aeo_TickStruct`

Binary address: `0x0042AB00`.

The function returns `this + 0x1C`.

Therefore the tick structure begins at runtime offset `+0x1C`.

The exact complete C++ type layout is not yet fully reconstructed; only fields established independently by native accesses should be treated as authoritative.

## Event-triggered expiry

The following native methods are confirmed:

| Function | Address | Tested flag bit | Effect when set |
|---|---:|---:|---|
| `aeo_Run` | `0x004A41C0` | `0x01` | `+0x20 = current server time` |
| `aeo_Walk` | `0x004A4230` | `0x02` | `+0x20 = current server time` |
| `aeo_Attack` | `0x004A42A0` | `0x04` | `+0x20 = current server time` |
| `aeo_Attacked` | `0x004A4310` | `0x08` | `+0x20 = current server time` |

The numeric bit values are proven. The mapping from these bits to the PDB flag names (`Cured`, `Dispeled`, `SystemRemove`, etc.) remains **UNRESOLVED**.

## Important emulator implication

The current emulator's same-AbState `AddBuff` behavior (deactivate/remove and construct a new instance) is not a faithful structural representation of the native update path. The native implementation updates the existing runtime entry and applies the strength-promotion rule above.

A future emulator change should therefore implement an explicit refresh/update operation rather than treating every repeated `SET_ABSTATE` as a fresh independent application.

No code change is made by this documentation commit because the exact native duration conversion and complete tick/update path are still under reconstruction.
