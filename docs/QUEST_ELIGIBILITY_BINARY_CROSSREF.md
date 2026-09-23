# Quest eligibility binary cross-reference — Step 29

## Scope

Cross-reference of the original `CQuest` eligibility routines in the supplied `Zone.exe` against the `QUEST_DATA` / `QUEST_START_CONDITION` CodeView layout recovered in Step 28.

No runtime code is changed by this audit.

## Proven function locations

The original PDB/module symbol records identify these quest routines in the Zone text image; the surrounding function boundaries were confirmed from the x86 prologues/epilogues:

- `CQuest::IsSoonableQuest(QUEST_DATA*)` — `0x0062FC80`
- `CQuest::IsDoingableQuest(QUEST_DATA*)` — `0x0062FEA0`
- routine at `0x0062FEE0` — source-level identity **UNRESOLVED** after later CodeView reconciliation
- `CQuestZone::IsRewardAbleQuest(PLAYER_QUEST_INFO*)` — `0x0062FF40` (**corrected by the later end-condition CodeView audit**)
- `CQuest::IsSoonableDailyQuest(PLAYER_QUEST_INFO*)` — `0x00630130` (resolved from the CQuest/CQuestZone vtable slot `+0x80` plus the PDB virtual signature)

> **Correction:** Step 29 originally labelled `0x0062FEE0` as `IsRewardAbleQuest` and `0x0062FF40` as `IsSoonableDailyQuest`. That name/address pair is superseded. `docs/QUEST_END_CONDITION_CODEVIEW_AUDIT.md` ties `0x0062FF40` directly to `CQuestZone::IsRewardAbleQuest` and to `QUEST_DATA.End`. No replacement address is assigned here to `IsSoonableDailyQuest`.

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

If `Start.bQuest != 0`, the routine retrieves the referenced player quest using
`Start.QuestID`.

The exact branch is now resolved:

- status `4` passes directly;
- status other than `2` or `4` fails;
- status `2` calls virtual slot `+0x80`;
- that slot is `CQuest::IsSoonableDailyQuest` at `0x00630130`;
- if `IsSoonableDailyQuest` returns `1`, the predecessor check fails;
- if it returns `0`, status `2` passes.

`IsSoonableDailyQuest` returns `0` immediately for predecessor quest definitions
whose `QUEST_DATA.Type != 10`. Therefore status-2 predecessors are fully proven
eligible for all non-Type-10 predecessor quests. Type 10 enters the native daily
reset-time comparison and is handled separately in
`docs/QUEST_DAILY_PREREQUISITE_BINARY.md`.

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

## Superseded Step-29 routine identities

The old Step-29 descriptions for the routines at `0x0062FEE0` and `0x0062FF40` are intentionally not used as runtime evidence anymore. The later CodeView/native end-condition audit proves that `0x0062FF40` evaluates `QUEST_DATA.End` reward eligibility and is reached when deriving effective status from `PQS_ING`.

The earlier start-condition-oriented behavior attributed here to those two source names is retained only as historical investigation context in git history. It must not be used to infer reward or daily semantics until `0x0062FEE0` is independently reconciled.

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
- The general status-2/status-4 predecessor branch is resolved. Only the Type-10 daily reset-time subcase still needs normalized emulator persistence/reset-time state.
- Exact normalized SQL mappings for `Repeatable`, `Start.bLevel`, `Start.LevelMin`, `Start.bItem`, and `Start.bQuest` remain separate from the original CodeView mapping.
- The exact source-level identity of the routine at `0x0062FEE0` remains **UNRESOLVED**. `IsSoonableDailyQuest` is now resolved at `0x00630130`.

## Runtime status

No runtime behavior changed. No quest selection or eligibility implementation was altered.


## NA2016 start-attribute corpus cross-check

The supplied server/client `QuestData.shn` corpus contains 2304 records. Direct byte-level enumeration of `QUEST_START_CONDITION` gives:

- `bClass != 0`: 127 quests.
- Active `Start.Class` values are direct class IDs: 1,2,3,4,6,7,8,9,11,12,13,14,16,17,18,19,21,22,23,26.
- `ClassName.shn` independently maps those IDs to the same class numbering used by the emulator's `Character.Job` / `NextGen.FiestaLib.Job` model (for example Fighter=1, Cleric=6, Archer=11, Mage=16; the source data additionally contains Sentinel=26 and Savior=27).
- `bGender != 0`: exactly two quests. Quest 64101 has `Gender=1`; its Valentine dialog addresses the NPC as a woman from the player's side. Quest 64201 has `Gender=0`; its paired dialog addresses the player as beautiful/feminine. This is consistent with Fiesta's character gender bit used by the emulator: 1=male, 0=female.
- `bRace != 0`: 0 quests.
- `bDate != 0`: 0 quests.

Therefore class and gender checks have concrete runtime coverage in the supplied content. Race/date remain implemented conservatively: future content enabling either flag must not be silently accepted until the player-race mapping / date-mode semantics are normalized.
