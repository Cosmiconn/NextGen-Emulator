# AbState `aeo_Run` investigation

## Scope

This note records the binary investigation performed against the supplied original `Zone.exe`/`Zone.pdb`. The goal is to avoid assigning the wrong native routine to `AbnormalStateContainer::AbstateElementInObject::aeo_Run` merely because a routine touches the same runtime fields.

## PDB evidence

The original PDB contains these symbols:

- `AbnormalStateContainer::AbstateElementInObject::aeo_Run`
- `AbnormalStateContainer::AbstateElementInObject::aeo_Walk`
- `AbnormalStateContainer::AbstateElementInObject::aeo_Attack`
- `AbnormalStateContainer::AbstateElementInObject::aeo_Attacked`
- `AbnormalStateContainer::AbstateElementInObject::aeo_ItemChange`

The mangled `aeo_Run` signature is:

`?aeo_Run@AbstateElementInObject@AbnormalStateContainer@@QAEEPAV?$List@VAbstateElementInObject@AbnormalStateContainer@@@@G@Z`

This establishes a `thiscall` method returning a byte/bool with two stack arguments: a `List<AbstateElementInObject>*` and a `WORD`/`unsigned short`.

## Important negative result

The previously suspected long routine beginning at approximately `0x0040E5E0` is **not proven to be `aeo_Run`** and must not be labeled as such.

Reasons:

1. Its observed stack cleanup is `ret 0x18`, incompatible with the PDB `aeo_Run` signature's two stack arguments (`ret 0x8` expected for thiscall on this binary/compiler).
2. It consumes several additional stack arguments and performs unrelated player/mobile processing.
3. Although it reads runtime `+0x24`, compares it with `[0x14D41A70]`, and updates `+0x24` from `+0x28`, that field access alone does not establish symbol identity.

## Runtime-field evidence

A binary-wide search found several routines containing the exact sequence that reads runtime `+0x24` and compares it with the global server-time value `[0x14D41A70]`. The occurrences include approximately:

- `0x00406AF3`
- `0x0040E68A`
- `0x0040EC6A`
- `0x0040F01A`

These routines have differing stack-cleanup/signature characteristics. Therefore the earlier statement that the `+0x24/+0x28` periodic processing belongs specifically to `aeo_Run` remains **UNRESOLVED**.

The observed semantics of the relevant runtime fields remain supported at the structural level:

- `runtime + 0x20`: primary expiry/remaining-time basis; `aeo_GetRestTime` reads it and compares it with `[0x14D41A70]`.
- `runtime + 0x24`: a secondary time point that at least some native processing compares to current server time and advances by `runtime + 0x28`.
- `runtime + 0x28`: increment used by that secondary processing.

The exact PDB member names and the exact owner routine of the secondary processing are still **UNRESOLVED**.

## Consequence for emulator implementation

Do not modify `Buffs.AddBuff` replacement/refresh semantics on the basis of the `0x40E5E0` or other periodic routines. The original `asl_AbstateSet`/`aeo_Set` path already proves that an existing runtime AbState is updated through the native set path; however, the exact periodic `aeo_Run` behavior must first be tied to the correct symbol.

Current QuestRuntime `SET_ABSTATE`/`RESET_ABSTATE` changes therefore remain unchanged by this investigation.

## Next RE target

Identify the actual native address of `aeo_Run` by correlating its PDB symbol/signature with the class vtable or equivalent compiler-generated dispatch structure, then reconstruct only the behavior visible from that exact function and its direct callees.
