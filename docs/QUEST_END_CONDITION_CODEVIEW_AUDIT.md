# `QUEST_DATA.QUEST_END_CONDITION` CodeView / native audit

## Scope

This document records structure facts recovered directly from the supplied original `Zone.pdb` and the native `CQuestZone::IsRewardAbleQuest` implementation in the matching `Zone.exe`.

No emulator behavior is changed by this document.

## Proven CodeView type facts

- `QUEST_DATA` CodeView type: `0x6aa5`
- `QUEST_DATA` size: `0x2A8`
- `QUEST_DATA::QUEST_END_CONDITION` CodeView type: `0x6aba`
- `QUEST_DATA::QUEST_END_CONDITION` size: `0x68`
- nested `_ItemList` concrete type: `0x6abc`
- nested `_NPCMobList` concrete type: `0x6abe`
- `_ItemList` element size: `0x06`
- `_NPCMobList` element size: `0x08`
- `NPCMobList` is an array of 5 `_NPCMobList` elements (`0x28` bytes)
- `ItemList` is an array of 5 `_ItemList` elements (`0x1E` bytes)

## `QUEST_END_CONDITION` layout

The `QUEST_END_CONDITION` member is at `QUEST_DATA + 0x58` in the native layout. Therefore the following absolute `QUEST_DATA` offsets are obtained by adding `0x58` to the CodeView-relative offsets:

| End-condition relative | `QUEST_DATA` absolute | Member |
|---:|---:|---|
| `0x00` | `0x58` | `bIsWaitListProgress` |
| `0x01` | `0x59` | `bLevel` |
| `0x02` | `0x5A` | `Level` |
| `0x04` | `0x5C` | `NPCMobList[0]` |
| `0x2C` | `0x84` | `ItemList[0]` |
| `0x4A` | `0xA2` | `bLocation` |
| `0x4C` | `0xA4` | `Location` |
| `0x50` | `0xA8` | `LocationX` |
| `0x54` | `0xAC` | `LocationY` |
| `0x58` | `0xB0` | `LocationRange` |
| `0x5C` | `0xB4` | `bScenario` |
| `0x5E` | `0xB6` | `ScenarioID` |
| `0x60` | `0xB8` | `bRace` |
| `0x61` | `0xB9` | `Race` |
| `0x62` | `0xBA` | `bClass` |
| `0x63` | `0xBB` | `Class` |
| `0x64` | `0xBC` | `bTimeLimit` |
| `0x66` | `0xBE` | `TimeLimit` |

The two nested list element layouts are:

### `_ItemList` (6 bytes)

| Relative | Member |
|---:|---|
| `0x00` | `bItem` |
| `0x02` | `ItemID` |
| `0x04` | `ItemLot` |

### `_NPCMobList` (8 bytes)

| Relative | Member |
|---:|---|
| `0x00` | `bNPCMob` |
| `0x02` | `NPCMobID` |
| `0x04` | `NPCMobAction` |
| `0x05` | `NPCMobCount` |
| `0x06` | `TargetGroup` |

## Native `IsRewardAbleQuest` confirmation

Native function: `CQuestZone::IsRewardAbleQuest` at `0x0062FF40`.

The native function uses the exact offsets above:

- `QUEST_DATA + 0x59` / `+0x5A` are read as the `bLevel` / `Level` end-condition fields.
- The five `_NPCMobList` entries begin at `QUEST_DATA + 0x5C`; each entry advances by `0x08` bytes.
- Within each entry native code tests the gate at entry `+0x00`, reads the ID at `+0x02`, reads the action at `+0x04`, and reads the count at `+0x05`.
- The location gate is `QUEST_DATA + 0xA2`; its payload starts at `+0xA4` and continues through `+0xB0`.
- The scenario gate is `QUEST_DATA + 0xB4` and the scenario ID is `+0xB6`.
- Race/class gates and values are `+0xB8/+0xB9` and `+0xBA/+0xBB`.
- The time-limit gate is `+0xBC` and the limit is the WORD at `+0xBE`.
- The native comparison for the time-limit condition is `TimeLimit <= PLAYER_QUEST_INFO + 0x1E` for failure; therefore the reward check passes that condition only when `TimeLimit > current +0x1E` or the gate is disabled.

## Important distinction

The earlier `QUEST_DATA + 0x59`, `+0x5A`, and `+0x5C` offsets are **not unnamed fields**. They are members of `QUEST_END_CONDITION` at `QUEST_DATA + 0x58`.

This resolves the structural identity of the native reward-condition block. It does **not** by itself prove the higher-level business meaning of every `NPCMobAction`, `TargetGroup`, location callback, or the emulator's SQL normalization.

## Evidence status

- Structure offsets: **PROVEN — original PDB**
- Native use of offsets: **PROVEN — original Zone.exe**
- Emulator/SQL field mapping: **UNRESOLVED** until separately tied to the project schema by evidence.
