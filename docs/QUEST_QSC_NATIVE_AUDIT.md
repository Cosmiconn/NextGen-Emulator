# Quest QSC native audit — QSC 28 / QSC 29

## Scope

This document records only behavior established from the original `Zone.pdb` / `Zone.exe`. Script-language `LINK` and native `QSC_LINK` are kept separate until a direct equivalence is proven.

## Dispatch identity

The original quest-script dispatch table contains separate entries for:

- `28 = QSC_SET_ABSTATE`
- `29 = QSC_LINK`
- `30 = QSC_IS_ABSTATE`

Therefore `29` is **not** an unknown command.

## QSC 28 — SET_ABSTATE

Native handler: `0x005BE325`.

Proven data layout consumed by the handler:

| QSC offset | Meaning | Width |
|---:|---|---:|
| `+0x05` | abnormal-state name string | NUL-terminated |
| `+0x09` | strength | BYTE |
| `+0x0A` | keep time | DWORD |

Native flow:

1. Resolve the name through the same dictionary at `0x00418F80` used by the other state commands.
2. Reject strength `<= 0` or `> 40` with result `1`.
3. If `keepTime` is non-zero, use the supplied DWORD; otherwise use the resolved substate's `KeepTime`.
4. Call the player vtable entry at `+0x638`, identified in the PDB as `ShinePlayer::so_AbnormalState_Set`.
5. `EnchantFrom` is supplied as `0`.

This is the native contract. The emulator must not silently substitute a different duration rule.

## QSC 29 — LINK

Native handler: `0x005BE46B`.

Proven data flow:

1. The command payload begins at `QSC + 0x05`.
2. The handler copies the NUL-terminated payload into a local buffer/stack representation.
3. The copied string is resolved through dictionary `0x00418F80`.
4. If resolution fails / the definition is invalid, the handler returns the parser-side result without performing the player operation.
5. For a resolved definition, the handler reads the definition field at `definition + 0x22`.
6. It then calls the player virtual method at vtable offset `+0x650`, passing that resolved definition value.

The exact semantic name/signature of the player vtable `+0x650` call is **UNRESOLVED** from the currently recovered evidence. Consequently this audit does **not** claim that it is a quest-state transition, a dialog transition, or the script-language `LINK N` operation.

## Important distinction: script LINK vs QSC_LINK

The normalized script corpus contains a textual `LINK` opcode with both numeric targets and blank targets. The native QSC dispatcher separately identifies QSC type `29` as `QSC_LINK`.

At present, there is no direct proof that textual `LINK` is encoded into QSC type `29` with identical semantics. Treating the two as equivalent would be an unsupported inference.

## Completion criterion

QSC 28 is fully documented at the recovered native level. QSC 29 is fully classified as `QSC_LINK` and its native data/control flow is documented; only the semantic identity of the player vtable `+0x650` target remains `UNRESOLVED`. No guessed runtime behavior is permitted on that basis.
