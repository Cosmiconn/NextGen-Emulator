# Quest AbState KeepTime — Native Binary Cross-Reference

## Scope

This document records the timing conversion observed in the original `Zone.exe` AbState runtime path. It is evidence-only; emulator behavior should not be changed beyond what the binary establishes.

## Native path

The QSC `SET_ABSTATE` handler supplies the KeepTime value to the native AbState setter. The lower-level runtime setup routine at `0x0040A340` processes the selected `SubAbState` record and stores a converted duration in the runtime entry at `+0x20`.

Relevant original instructions:

```asm
0040A4E3  test edx,edx
0040A4E5  jne  0040A540
0040A4E7  mov  eax,[ebp-0x14]
0040A4EA  mov  ecx,[eax]
0040A4EC  mov  edx,[ecx+0x2B]       ; selected SubAbState KeepTime
0040A4EF  mov  eax,[ebp+0x20]      ; supplied time argument
0040A4F2  lea  ecx,[edx+eax+0x3E7]  ; +999
0040A4F9  mov  [esi+0x20],ecx
0040A4FC  mov  eax,0x10624DD3
0040A501  cmp  ecx,0xEA60           ; 60000
0040A507  jbe  0040A518
0040A509  mul  ecx
0040A50B  shr  edx,6
0040A50E  lea  ecx,[edx+edx*4]
0040A511  add  ecx,ecx              ; /100
0040A513  mov  [esi+0x20],ecx
0040A518  lea  edx,[ecx+ecx*4]
0040A51B  add  edx,edx              ; *10
0040A1D  mul  edx
0040A51F  shr  edx,6              ; /100
0040A522  mov  [esi+0x20],edx
```

(The final `lea/add` sequence is the multiplication by 10 before the same reciprocal-division sequence.)

## Established conversion

For the branch where the resulting duration is `<= 60000`, the stored value is effectively:

`floor((duration + 999) * 10 / 100)` = `ceil(duration / 10)`

For the branch where the resulting duration is `> 60000`, it is effectively:

`floor((duration + 999) / 100)` = `ceil(duration / 100)`

Therefore the native runtime uses an **adaptive timer resolution**:

- `<= 60,000 ms`: duration is represented in **10 ms units**, rounded upward.
- `> 60,000 ms`: duration is represented in **100 ms units**, rounded upward.

The `+999` occurs before the threshold test, so the exact boundary is applied to the already-adjusted duration.

## Important correction

It is **not** correct to describe this as a universal millisecond-to-100-ms conversion. The short-duration path has 10-ms resolution; the longer-duration path has 100-ms resolution.

The original QSC `KeepTime` field itself remains a DWORD and is supplied directly to the native setter. The binary evidence does not by itself prove that the source-language name `KeepTime` means milliseconds; the conversion is consistent with millisecond input, but the semantic unit of the upstream field should remain marked as **strongly indicated / not independently proven** until a source/data declaration or another native caller confirms it.

## Refresh interaction

Before this conversion, the setter can alter the selected strength when an AbState already exists. It enforces a minimum of `(existingStrength + 1)` and a maximum equal to the available strength count. When the explicit time argument is nonzero, it adjusts the time argument using the selected SubAbState KeepTime before the runtime-duration conversion.

This means the current emulator strategy of simply removing an existing buff and creating a replacement should remain provisional until the complete existing-entry path is mapped.
