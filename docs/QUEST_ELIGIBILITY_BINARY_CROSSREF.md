# Quest eligibility binary cross-reference — Step 29

## Scope

Cross-reference of the original `CQuest` eligibility routines in the supplied `Zone.exe` against the `QUEST_DATA` / `QUEST_START_CONDITION` CodeView layout recovered in Step 28.

No runtime code is changed by this audit.

## Proven function locations

The original PDB/module symbol records identify these quest routines in the Zone text image; the surrounding function boundaries were confirmed from the x86 prologues/epilogues:

- `CQuest::IsSoonableQuest(QUEST_DATA*)` — `0x0062FC80`
- `CQuest::IsDoingableQuest(QUEST_DATA*)` — `0x0062FEA0`
- `CQuest::IsRewardAbleQuest(PLAYER_QUEST_INFO*)` — `0x0062FEE0`
- `CQuest::IsSoonableDailyQuest(PLAYER_QUEST_INFO*)` — `0x0062FF40`

## `IsSoonableQuest`

The routine first evaluates the `QUEST_DATA.Start` conditions in sequence.

### Level

If `Start.bLevel != 0`, the routine calls the player's level accessor, adds **5**, and requires:

- `playerLevel + 5 >= Start.LevelMin`
- `playerLevel + 5 <= Start.LevelMax`

Therefore the raw selection comparison at `+0x1A/+0x1B` is directly tied to the start-condition level enable/minimum fields.

### Item

If `Start.bItem != 0`, the routine obtains the player's relevant item quantity/state for `Start.ItemID` and requires the resulting quantity to be at least `Start.ItemLot`.

### Location

If `Start.bLocation != 0`, the routine compares the player's map/location and coordinates against `Start.Location`, `LocationX`, `LocationY`, and `LocationRange` through the player's location-check virtual call.

### Quest predecessor

If `Start.bQuest != 0`, the routine retrieves the referenced quest using `Start.QuestID` and checks the returned quest/player state. The observed accepted states are raw status values `2` and `4`; the routine additionally invokes a quest-state check for the state-2 path. The higher-level semantic interpretation of that complete predecessor rule remains intentionally conservative here.

### Race/class/gender

The routine conditionally checks:

- `Start.bRace` -> player's race equals `Start.Race`
- `Start.bClass` -> player's class equals `Start.Class`
- `Start.bGender` -> player's gender equals `Start.Gender`

### Date

If `Start.bDate != 0`, the routine reads `DateMode`, `DateStart`, and `DateEnd` and performs mode-specific date/time comparisons. The exact external date-mode enumeration mapping is not assigned here beyond the observed raw branch values.

## `IsDoingableQuest`

`IsDoingableQuest(QUEST_DATA*)` first calls `IsSoonableQuest` and rejects immediately if that fails.

If `Start.bLevel != 0`, it then repeats the level-window test using the player's **current level without the `+5` offset**:

- `playerLevel >= Start.LevelMin`
- `playerLevel <= Start.LevelMax`

If no level condition is enabled, the additional level test is skipped. This proves that `IsDoingableQuest` is stricter than the soonable level test while reusing all earlier start-condition checks.

## `IsRewardAbleQuest`

The observed overload accepts `PLAYER_QUEST_INFO*`, resolves the associated quest data, then calls `IsSoonableQuest` and applies the same current-level (`no +5`) window check when `Start.bLevel` is enabled.

The complete reward-state transition is not inferred from this routine alone; no new status mapping is introduced here.

## `IsSoonableDailyQuest`

The routine accepts `PLAYER_QUEST_INFO*` and first resolves the corresponding quest/player information. It checks additional daily-specific condition arrays and values in the `PLAYER_QUEST_INFO`/quest-data representation, including:

- a level-related byte/threshold pair,
- repeated condition slots containing item IDs and required quantities,
- location/date-related condition data,
- further daily-specific flags/values.

The routine contains direct player-state lookups for item/state checks and returns failure immediately on violated conditions.

Because these fields are not the same `QUEST_START_CONDITION` offsets used by the Step-27 selection tie-break, they are not collapsed into the normalized `data_quest` schema without independent source mapping.

## Direct consequences for the selection audit

The Step-27/28 raw fields now have a stronger binary cross-reference:

| Raw `QUEST_DATA` offset | CodeView member | Confirmed use in eligibility/selection |
|---:|---|---|
| `0x11` | `Type` | indexes `m_QuestTypePriority`; raw enum value `3` receives special handling in selection |
| `0x12` | `Repeatable` | read by selection tie-break; full semantic role still requires the surrounding state logic |
| `0x1A` | `Start.bLevel` | gates level checks in `IsSoonableQuest` / `IsDoingableQuest` |
| `0x1B` | `Start.LevelMin` | lower bound for the level window |
| `0x20` | `Start.bItem` | gates item requirement in `IsSoonableQuest` |
| `0x38` | `Start.bQuest` | gates predecessor-quest requirement in `IsSoonableQuest` |

This is stronger than a name-based mapping because the same fields are now observed in multiple original binary routines.

## Important unresolved items

- Exact semantic meaning of `QUEST_DATA.Repeatable` within the selection tie-break remains **UNRESOLVED**.
- Exact higher-level meaning of the raw predecessor accepted states `2` and `4` in the `Start.bQuest` path remains **UNRESOLVED** here.
- Exact normalized SQL mappings for `Repeatable`, `Start.bLevel`, `Start.LevelMin`, `Start.bItem`, and `Start.bQuest` remain separate from the original CodeView mapping.
- `IsSoonableDailyQuest` contains additional source fields whose normalized SQL mapping is not proven by this audit.

## Runtime status

No runtime behavior changed. No quest selection or eligibility implementation was altered.
