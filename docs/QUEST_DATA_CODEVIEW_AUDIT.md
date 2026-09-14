# `QUEST_DATA` CodeView field audit — Step 38

## Scope

The supplied original `Zone.pdb` was parsed at the CodeView type-record level and cross-referenced against the supplied `Zone.exe`. This audit extends the earlier start-condition mapping to the complete `QUEST_END_CONDITION` block used by the native quest condition/update routines.

## Proven `QUEST_DATA` layout

- `QUEST_DATA` CodeView type: `0x6aa5`
- `QUEST_DATA` field list: `0x6aa4`
- concrete size: `0x2a8` bytes
- `Start` member: `QUEST_START_CONDITION` at `+0x18`
- `End` member: `QUEST_END_CONDITION` at `+0x58`
- `Reward` member: `QUEST_REWARD` at `+0x204`
- `Action` member: `QUEST_ACTION` at `+0xc4`

The outer CodeView field list directly names the `End` member at `+0x58`.

## `QUEST_END_CONDITION` CodeView structure

The original CodeView record identifies `QUEST_DATA::QUEST_END_CONDITION` as a concrete `0x68`-byte structure.

| Relative offset | Member | Absolute `QUEST_DATA` offset |
|---:|---|---:|
| `0x00` | `bIsWaitListProgress` | `0x58` |
| `0x01` | `bLevel` | `0x59` |
| `0x02` | `Level` | `0x5A` |
| `0x04` | `NPCMobList` | `0x5C` |
| `0x2C` | `ItemList` | `0x84` |
| `0x4A` | `bLocation` | `0xA2` |
| `0x4C` | `Location` | `0xA4` |
| `0x50` | `LocationX` | `0xA8` |
| `0x54` | `LocationY` | `0xAC` |
| `0x58` | `LocationRange` | `0xB0` |
| `0x5C` | `bScenario` | `0xB4` |
| `0x5E` | `ScenarioID` | `0xB6` |
| `0x60` | `bRace` | `0xB8` |
| `0x61` | `Race` | `0xB9` |
| `0x62` | `bClass` | `0xBA` |
| `0x63` | `Class` | `0xBB` |
| `0x64` | `bTimeLimit` | `0xBC` |
| `0x66` | `TimeLimit` | `0xBE` |

The two list members are arrays represented by CodeView array/element types. The NPC/Mob list occupies `0x28` bytes (5 × 8-byte entries). The item list occupies `0x1E` bytes (5 × 6-byte entries).

### `NPCMobList` element

CodeView resolves the concrete element as `QUEST_DATA::QUEST_END_CONDITION::_NPCMobList` with size `0x08`:

| Entry offset | Member |
|---:|---|
| `0x00` | `bNPCMob` |
| `0x02` | `NPCMobID` |
| `0x04` | `NPCMobAction` |
| `0x05` | `NPCMobCount` |
| `0x06` | `TargetGroup` |

The five entries therefore span `QUEST_DATA + 0x5E .. 0x85`.

### `ItemList` element

CodeView resolves the concrete element as `QUEST_DATA::QUEST_END_CONDITION::_ItemList` with size `0x06`:

| Entry offset | Member |
|---:|---|
| `0x00` | `bItem` |
| `0x02` | `ItemID` |
| `0x04` | `ItemLot` |

The five entries therefore span `QUEST_DATA + 0x84 .. 0xA1`.

A native routine often forms its cursor at `QUEST_DATA + 0x86`, i.e. at the first entry's `ItemID`; the gate is then read at cursor `-2`. This is consistent with the CodeView `ItemList` member at `+0x84` and is not a conflicting offset.

## `QUEST_START_CONDITION`

Previously proven directly from CodeView:

| Relative offset | Member |
|---:|---|
| `0x00` | `bIsWaitListView` |
| `0x01` | `bIsWaitListProgress` |
| `0x02` | `bLevel` |
| `0x03` | `LevelMin` |
| `0x04` | `LevelMax` |
| `0x05` | `bNPC` |
| `0x06` | `NPCID` |
| `0x08` | `bItem` |
| `0x0A` | `ItemID` |
| `0x0C` | `ItemLot` |
| `0x0E` | `bLocation` |
| `0x10` | `Location` |
| `0x14` | `LocationX` |
| `0x18` | `LocationY` |
| `0x1C` | `LocationRange` |
| `0x20` | `bQuest` |
| `0x22` | `QuestID` |
| `0x24` | `bRace` |
| `0x25` | `Race` |
| `0x26` | `bClass` |
| `0x27` | `Class` |
| `0x28` | `bGender` |
| `0x29` | `Gender` |
| `0x2A` | `bDate` |
| `0x2B` | `DateMode` |
| `0x30` | `DateStart` |
| `0x38` | `DateEnd` |

`Start` is at `QUEST_DATA + 0x18`, giving the previously verified absolute offsets such as `+0x1A = Start.bLevel`, `+0x1B = Start.LevelMin`, `+0x20 = Start.bItem`, and `+0x38 = Start.bQuest`.

## Native eligibility overloads

The supplied `Zone.exe` contains two relevant `IsRewardAbleQuest` overloads. The distinction matters:

- `CQuest::IsRewardAbleQuest(QUEST_DATA*)` starts at `0x0062FEE0`.
- `CQuest::IsRewardAbleQuest(PLAYER_QUEST_INFO*)` starts at `0x0062FF40`.
- `CQuest::IsSoonableDailyQuest(PLAYER_QUEST_INFO*)` starts at `0x006300D0`.

The earlier audit had assigned the `PLAYER_QUEST_INFO*` overload to `0x0062FEE0`; that address assignment is corrected here. The binary itself proves the distinction because `0x62FEE0` resolves quest data from a quest-ID argument and performs the basic soonable/current-level check, while `0x62FF40` continues into the end-condition checks using the `PLAYER_QUEST_INFO*` runtime state.

## `IsRewardAbleQuest(QUEST_DATA*)` — `0x0062FEE0`

The function:

1. resolves the supplied quest ID to `QUEST_DATA`;
2. calls `IsSoonableQuest` (`0x0062FC80`);
3. if `Start.bLevel` is enabled, checks the player's current level against `Start.LevelMin`/`Start.LevelMax` without the `+5` soonable offset;
4. returns the resulting boolean.

This overload does **not** contain the full reward/end-condition walk.

## `IsRewardAbleQuest(PLAYER_QUEST_INFO*)` — `0x0062FF40`

The supplied binary directly cross-references the following `QUEST_END_CONDITION` fields:

### Level

- `QUEST_DATA + 0x59` = `End.bLevel`
- `QUEST_DATA + 0x5A` = `End.Level`
- player vtable `+0x64` supplies the player's level
- the comparison is `playerLevel < End.Level` -> failure

### NPC/Mob end conditions

The routine walks five 8-byte entries starting from the first `NPCMobList` element. The native cursor corresponds to the CodeView member at `QUEST_DATA + 0x5E`.

For each entry it checks the enable byte and `NPCMobID`, then uses player vtable `+0x7C` to determine whether the target NPC/Mob is present. If present, the reward condition fails.

This directly ties the native reward-condition loop to the CodeView `NPCMobList` element fields. The exact semantic use of `NPCMobAction`, `NPCMobCount`, and `TargetGroup` by this particular routine is **UNRESOLVED**; the reward routine shown here uses the enable/ID path for its immediate rejection test.

### Item end conditions

The routine forms its cursor at `QUEST_DATA + 0x86`, which is `ItemList[0].ItemID`; it reads the gate at cursor `-2`, i.e. `ItemList[0].bItem` at `+0x84`.

For each of five 6-byte entries:

- gate = `ItemList[i].bItem`
- ID = `ItemList[i].ItemID`
- required lot = `ItemList[i].ItemLot`

It calls player vtable `+0x68` for the player's corresponding item quantity/state and rejects when the obtained lot is below `ItemLot`.

### Location

The binary directly reads:

- `+0xA2` = `End.bLocation`
- `+0xA4` = `End.Location`
- `+0xA8` = `End.LocationX`
- `+0xAC` = `End.LocationY`
- `+0xB0` = `End.LocationRange`

It calls player vtable `+0x60` to obtain the current player location/coordinates and then player vtable `+0x28` for the location-range check. A failed check rejects the reward state.

### Scenario

`+0xB4` is directly identified by CodeView as `End.bScenario`, and `+0xB6` as `End.ScenarioID`.

The native scenario completion routine at `0x630BA0` walks active status-6 quests, checks `End.bScenario == 1`, compares `End.ScenarioID` with the supplied scenario ID, tests the quest runtime flag at `PLAYER_QUEST_INFO + 0x1D` bit `0x02`, sets bit `0x02`, and then runs the common status-update path.

This proves that the same `QUEST_END_CONDITION` structure is used by `Occure_ScenarioDone`.

### Race

- `+0xB8` = `End.bRace`
- `+0xB9` = `End.Race`
- player vtable `+0x6C` supplies the player's race

A mismatch rejects the reward state.

### Class

- `+0xBA` = `End.bClass`
- `+0xBB` = `End.Class`
- player vtable `+0x70` supplies the player's class

A mismatch rejects the reward state.

### Time limit

- `+0xBC` = `End.bTimeLimit`
- `+0xBE` = `End.TimeLimit`
- `PLAYER_QUEST_INFO + 0x1E` is compared against `End.TimeLimit`

The native comparison accepts the condition when `End.TimeLimit <= PLAYER_QUEST_INFO + 0x1E`; a value above the stored quest-progress time fails.

## Native `Occure_*` cross-reference

The function ordering in the supplied Zone binary, combined with direct raw-offset access, establishes the following mappings:

| Native routine | Address | Direct `QUEST_END_CONDITION` use |
|---|---:|---|
| `CQuest::Occure_NPCMobKill` | `0x6306D0` | `NPCMobList` at `+0x5E`, five 8-byte entries; checks `bNPCMob`, `NPCMobID`, `NPCMobAction`, `NPCMobCount` and quest progress |
| `CQuest::Occure_TakeItem` | `0x630810` | `ItemList` at `+0x84`, five 6-byte entries; checks `bItem`, `ItemID`, `ItemLot` |
| `CQuest::Occure_DestroyItem` | `0x630920` | `ItemList` at `+0x84`, five 6-byte entries; checks `bItem`, `ItemID`, `ItemLot` |
| `CQuest::Occure_CheckLocation` | `0x630A60` | `bLocation +0xA2`, `Location +0xA4`, `LocationX +0xA8`, `LocationY +0xAC`, `LocationRange +0xB0` |
| `CQuest::Occure_ScenarioDone` | `0x630BA0` | `bScenario +0xB4`, `ScenarioID +0xB6` |
| `CQuest::Occure_RaceChange` | `0x630C90` | `bRace +0xB8`, `Race +0xB9` |
| `CQuest::Occure_ClassChange` | `0x630D70` | `bClass +0xBA`, `Class +0xBB` |
| `CQuest::Occure_TimeProcess` | `0x630E50` | `bTimeLimit +0xBC`, `TimeLimit +0xBE`; updates `PLAYER_QUEST_INFO +0x1E` |

`Occure_LevelChange` is a separate routine in the native sequence and uses the quest start/end level data, but its complete source-level symbol/address tie-in is not needed to establish the end-block field names above.

## Important consequences

The previously unresolved raw fields from the reward-condition investigation are now directly named by original CodeView:

- `+0x59` = `End.bLevel`
- `+0x5A` = `End.Level`
- `+0x5C` = `End.NPCMobList` member start
- `+0x84` = `End.ItemList` member start
- `+0xA2` = `End.bLocation`
- `+0xA4` = `End.Location`
- `+0xA8` = `End.LocationX`
- `+0xAC` = `End.LocationY`
- `+0xB0` = `End.LocationRange`
- `+0xB4` = `End.bScenario`
- `+0xB6` = `End.ScenarioID`
- `+0xB8` = `End.bRace`
- `+0xB9` = `End.Race`
- `+0xBA` = `End.bClass`
- `+0xBB` = `End.Class`
- `+0xBC` = `End.bTimeLimit`
- `+0xBE` = `End.TimeLimit`

The same end-condition structure is therefore demonstrably shared by the native reward check and the event-driven `Occure_*` routines.

## Still unresolved

- Exact normalized SQL column mapping for these original members.
- Exact higher-level semantic meaning of `NPCMobAction` and `TargetGroup` for every event path.
- Exact external enumeration values for race/class where not directly established by CodeView/binary behavior.
- Exact semantics of `bIsWaitListProgress` in the end-condition block outside the paths directly observed.
- Whether the server-side SQL representation should preserve the original packed structure exactly or normalize it into relational child tables. This requires a deliberate schema decision after the remaining quest-data extraction is complete.

## Runtime status

No emulator runtime behavior was changed in this step. The result is an original-source structure/behavior audit intended to prevent speculative SQL or runtime mappings.
