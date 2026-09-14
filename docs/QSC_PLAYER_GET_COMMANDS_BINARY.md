# Native QSC player getter mappings

## Scope

This note records only behavior established from the supplied original `Zone.exe` and matching `Zone.pdb` evidence. It does not infer unsupported field representations in the emulator.

## Dispatch

`CQuestZone::QuestEnd` dispatches the raw `STRUCT_QSC.Cmd` value through the jump table at `0x005BF020`.

The following raw command values are proven by the PDB enum and the native jump targets:

| Raw Cmd | PDB name | Native target | Player vtable slot | Result handling |
|---:|---|---:|---:|---|
| `0x17` (23) | `QSC_GET_PLAYER_RACE` | `0x005BE5F1` | `+0x6C` | `AL` -> zero-extended -> quest variable at `player + 0xD0 + Data*4` |
| `0x18` (24) | `QSC_GET_PLAYER_CLASS` | `0x005BE612` | `+0x70` | `AL` -> zero-extended -> quest variable at `player + 0xD0 + Data*4` |
| `0x19` (25) | `QSC_GET_PLAYER_LEVEL` | `0x005BE619` | `+0x64` | `AL` -> zero-extended -> quest variable at `player + 0xD0 + Data*4` |
| `0x1A` (26) | `QSC_GET_PLAYER_GENDER` | `0x005BE620` | `+0x74` | `AL` -> zero-extended -> quest variable at `player + 0xD0 + Data*4` |
| `0x1B` (27) | `QSC_GET_PLAYER_EMPTY_INVENTORY` | `0x005BE627` | `+0x78` | `AL` -> zero-extended -> quest variable at `player + 0xD0 + Data*4` |

The common native sequence is:

```asm
call player_vtable_method
movzx ecx,al
mov edx,[player+0x8FC]
mov eax,[edx+0x05]       ; STRUCT_QSC.Data
mov [player+eax*4+0xD0],ecx
```

Therefore `STRUCT_QSC.Data` is proven to be the destination quest-variable index for these GET commands. It is not the getter argument itself.

## Emulator consequence

The current quest-script corpus uses the textual form:

```text
GET_PLAYER_EMPTY_INVENTORY VAR1
```

The same destination-variable convention is therefore appropriate for newly supported player getters, but only getter semantics that can be represented from the current emulator character model should be implemented.

`GET_PLAYER_CLASS` can be represented by the emulator's existing `ZoneCharacter.Job` value, and `GET_PLAYER_LEVEL` by `ZoneCharacter.Level`.

`GET_PLAYER_RACE` remains **UNRESOLVED** in the emulator because the current character storage model has no proven player-race field. Do not synthesize a race value from Job or another field.

`GET_PLAYER_GENDER` is also **UNRESOLVED** at the numeric-value level in the emulator. The current model stores a boolean `LookInfo.Male`, but the native getter's exact returned encoding has not yet been proven from the original data/code path. Do not guess the 0/1 mapping.

`GET_PLAYER_EMPTY_INVENTORY` is already represented in `QuestRuntime`, but its exact capacity semantics remain a separate audit item.

## QSC 28/29 note

Raw `QSC_REPEAT_QUEST_GIVE_UP` (`0x1C`) and `QSC_UNKNOWNED` (`0x1D`) both dispatch to `0x005BE1A1` in this build. That target immediately advances/reloads the quest command state through `0x006387F0`; it does not expose a direct give-up or other uniquely named operation at that entry point. Their exact script-command semantics remain **UNRESOLVED** and must not be equated with the separate wire-level quest give-up routines without further evidence.
