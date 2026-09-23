# Quest End Condition – native binary mapping

## Source

Original `Zone.exe` / `Zone.pdb` from the supplied 2016 build.

- `CQuestZone::IsRewardAbleQuest` = `0x0062FF40`
- `QUEST_DATA` size = `0x2A8`
- `QUEST_DATA::End` begins at `+0x58`
- `QUEST_END_CONDITION` occupies `0x68` bytes (`End + 0x00..0x67`)

The field names below are taken from CodeView/PDB type information; semantics are additionally tied to native reads in `IsRewardAbleQuest` where applicable.

## QUEST_END_CONDITION layout

| End-relative | QUEST_DATA absolute | Field | Type / evidence |
|---:|---:|---|---|
| `+0x00` | `+0x58` | `bIsWaitListProgress` | BYTE, PDB |
| `+0x01` | `+0x59` | `bLevel` | BYTE; read by `IsRewardAbleQuest` |
| `+0x02` | `+0x5A` | `Level` | BYTE; read by `IsRewardAbleQuest` |
| `+0x04` | `+0x5C` | `_NPCMobList` | embedded helper/array type, PDB |
| `+0x04` | `+0x5C` | `NPCMobList` | list storage begins here, PDB |
| `+0x2C` | `+0x84` | `_ItemList` | embedded helper/array type, PDB |
| `+0x2C` | `+0x84` | `ItemList` | list storage begins here, PDB |
| `+0x4A` | `+0xA2` | `bLocation` | BYTE; read by `IsRewardAbleQuest` |
| `+0x4C` | `+0xA4` | `Location` | WORD; read by `IsRewardAbleQuest` |
| `+0x50` | `+0xA8` | `LocationX` | DWORD; passed to native location callback |
| `+0x54` | `+0xAC` | `LocationY` | DWORD; passed to native location callback |
| `+0x58` | `+0xB0` | `LocationRange` | DWORD; passed to native location callback |
| `+0x5C` | `+0xB4` | `bScenario` | BYTE; native checks player quest-info bit 1 |
| `+0x5E` | `+0xB6` | `ScenarioID` | WORD; PDB |
| `+0x60` | `+0xB8` | `bRace` | BYTE; read by `IsRewardAbleQuest` |
| `+0x61` | `+0xB9` | `Race` | BYTE; compared with player vtable `+0x6C` |
| `+0x62` | `+0xBA` | `bClass` | BYTE; read by `IsRewardAbleQuest` |
| `+0x63` | `+0xBB` | `Class` | BYTE; compared with player vtable `+0x70` |
| `+0x64` | `+0xBC` | `bTimeLimit` | BYTE; read by `IsRewardAbleQuest` |
| `+0x66` | `+0xBE` | `TimeLimit` | WORD; compared with `PLAYER_QUEST_INFO +0x1E` |

The two list helper members are backed by the PDB-defined `_NPCMobList` and `_ItemList` types. Their concrete element layouts are:

### `_ItemList` element

PDB proves:

- `bItem` at `+0x00`
- `ItemID` at `+0x02`
- `ItemLot` at `+0x04`
- element size `0x06`

`IsRewardAbleQuest` iterates 5 item elements beginning at `QUEST_DATA + 0x84` (`End + 0x2C`); for enabled entries it requires the relevant player item state/lot to satisfy the entry. The `+0x84` start is the later CodeView-corrected array offset.

### `_NPCMobList` element

PDB proves:

- `bNPCMob` at `+0x00`
- `NPCMobID` at `+0x02`
- `NPCMobAction` at `+0x04`
- `NPCMobCount` at `+0x05`
- `TargetGroup` at `+0x06`
- element size `0x08`

`IsRewardAbleQuest` reads the five NPC/Mob entries beginning at `QUEST_DATA + 0x5C` (`End +0x04`). The exact higher-level meaning of each `NPCMobAction`, callback and `TargetGroup` remains constrained to the separately documented native observations; it is not inferred from the item-list offset.

## Native Reward eligibility, confirmed

`CQuestZone::IsRewardAbleQuest` (`0x0062FF40`) returns true only if all enabled checks pass:

1. `End.bLevel` / `End.Level` level gate.
2. Five NPC/Mob-list entries.
3. `End.bLocation`, `Location`, `LocationX`, `LocationY`, `LocationRange` location gate/callback.
4. `End.bScenario`: tests bit `0x02` in `PLAYER_QUEST_INFO +0x1D`.
5. `End.bRace` / `End.Race` against player vtable `+0x6C`.
6. `End.bClass` / `End.Class` against player vtable `+0x70`.
7. `End.bTimeLimit` / `End.TimeLimit`: the later CodeView/native audit corrects the branch direction: the time-limit check fails when `TimeLimit <= PLAYER_QUEST_INFO +0x1E`; it passes only while `TimeLimit > current` (or the gate is disabled).

This function is called by `GetNewQuestStatus()` for status `PQS_ING` (`6`). If `IsRewardAbleQuest` is true, native status becomes `PQS_REWARD` (`8`); otherwise it remains `PQS_ING` (`6`).

## Emulator alignment

The SQL-backed runtime now applies the proven `End.bClass / End.Class` gate during reward eligibility. The comparison uses the same direct class-ID convention already proven for start eligibility and `Character.Job`.

`End.bRace` remains intentionally unimplemented until the player's race representation is normalized. `End.bTimeLimit / End.TimeLimit` also remains unresolved at runtime because the native comparison operand at `PLAYER_QUEST_INFO +0x1E` is structurally proven but its emulator-side unit/state source is not yet established.

## Important non-inferences

- `bScenario` is the exact PDB field name, but native `IsRewardAbleQuest` does **not** compare `ScenarioID`; it checks bit `0x02` at `PLAYER_QUEST_INFO +0x1D`.
- `TimeLimit` is proven as a WORD; the corrected comparison direction above supersedes the older Step-29 wording. The broader unit/meaning of `PLAYER_QUEST_INFO +0x1E` remains unresolved.
- No SQL column mapping is asserted here.
