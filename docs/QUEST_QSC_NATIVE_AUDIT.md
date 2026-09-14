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

### Exact recovered instruction-level flow

The relevant original instructions are:

```text
005BE46B  push 0FFFFFFFFh
005BE470  push 006FCB38h
005BE475  push 0
005BE477  lea  ecx,[ebp-144h]
005BE47D  call 00402880h
005BE482  mov  ecx,[ebx+08FCh]
005BE488  mov  ecx,[ecx+05h]
005BE48B  mov  al,[ecx]
005BE492  mov  esi,1
005BE497  mov  [edx],al
005BE499  add  ecx,esi
005BE49B  add  edx,esi
005BE49D  test al,al
005BE49F  jne 005BE492
005BE4A1  lea  edx,[ebp-90h]
005BE4A4  push edx
005BE4A5  mov  ecx,0087AEA8h
005BE4AA  call 00418F80h
005BE4AF  test eax,eax
005BE4B1  jne 005BE4C7
005BE4B3  mov  eax,[ebp-144h]
005BE4B9  sub  [007549A8],esi
005BE4BF  mov  byte ptr [ebp-4],2
005BE4C3  push eax
005BE4C4  jmp 005BE197
005BE4C9  mov  eax,[eax]
005BE4CB  test eax,eax
005BE4CD  jne 005BE4E5
005BE4CF  mov  ecx,[ebp-144h]
005BE4D5  sub  [007549A8],esi
005BE4DB  mov  byte ptr [ebp-4],2
005BE4DF  push ecx
005BE4E0  jmp 005BE197
005BE4E5  mov  ecx,[ebx+0B4h]
005BE4EB  mov  edx,[ecx]
005BE4ED  mov  eax,[eax+22h]
005BE4F0  mov  edx,[edx+650h]
005BE4F6  push eax
005BE4F7  call edx
005BE4F9  mov  eax,[ebp-144h]
005BE4FF  sub  [007549A8],esi
005BE505  mov  byte ptr [ebp-4],2
005BE509  push eax
005BE50A  jmp 005BE197
```

The instructions establish the following facts without requiring a semantic guess:

1. A local buffer/object is initialized before the command payload is copied.
2. The source string begins at `QSC + 0x05` and is copied byte-by-byte through the terminating NUL.
3. The resulting string is passed to dictionary function `0x00418F80` with dictionary/object base `0x0087AEA8`.
4. A zero return from the dictionary lookup causes the handler to leave without the player virtual call.
5. A non-zero lookup result is dereferenced once; a null definition pointer also causes the handler to leave without the player virtual call.
6. For a valid definition, the DWORD at `definition + 0x22` is loaded.
7. The player object's vtable is read from `[player]`, then slot `+0x650` is loaded and called with that DWORD as its argument.
8. The handler then returns through the common parser epilogue.

### What remains unresolved

The exact PDB semantic identity/signature of the player virtual slot `+0x650` has not yet been tied unambiguously to a named `ShinePlayer` member. Therefore the native evidence proves the data/control flow above, but does **not** prove whether the operation is a quest-state transition, dialog transition, map/link operation, or another player-side action.

## Important distinction: script LINK vs QSC_LINK

The normalized script corpus contains a textual `LINK` opcode with both numeric targets and blank targets. The native QSC dispatcher separately identifies QSC type `29` as `QSC_LINK`.

At present, there is no direct proof that textual `LINK` is encoded into QSC type `29` with identical semantics. Treating the two as equivalent would be an unsupported inference.

## Completion criterion

QSC 28 is fully documented at the recovered native level. QSC 29 is fully classified as `QSC_LINK` and its native instruction/data/control flow is documented. The only remaining `UNRESOLVED` item is the semantic identity of the player vtable `+0x650` target; no guessed runtime behavior is permitted on that basis.
