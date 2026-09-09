# `QUEST_DATA` CodeView field audit — Step 28

## Scope

The supplied original `Zone.pdb` was parsed at the CodeView type-record level to resolve the raw `QUEST_DATA` offsets used by `CQuest::GetQuestStatusWithNPC`.

## Proven structure records

- `QUEST_DATA`: CodeView type `0x6aa5`
- `QUEST_DATA` field list: `0x6aa4`
- `QUEST_DATA` concrete size: `0x2a8` bytes
- `QUEST_DATA.Start`: `QUEST_START_CONDITION` at offset `0x18`
- `QUEST_START_CONDITION`: concrete size `0x40` bytes

## Proven raw-offset mappings

| `QUEST_DATA` absolute offset | Source member |
|---:|---|
| `0x11` | `Type` |
| `0x12` | `Repeatable` |
| `0x1A` | `Start.bLevel` |
| `0x1B` | `Start.LevelMin` |
| `0x20` | `Start.bItem` |
| `0x38` | `Start.bQuest` |

The `Start` member is at `QUEST_DATA + 0x18`; therefore `0x1A` is `Start + 0x02`, `0x1B` is `Start + 0x03`, `0x20` is `Start + 0x08`, and `0x38` is `Start + 0x20`.

## Relevant `QUEST_START_CONDITION` layout

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

## Consequence for Step 27

The six raw fields previously marked unresolved by the tie-break audit are now structurally identified directly from the original PDB:

- `+0x11` = `QUEST_DATA.Type`
- `+0x12` = `QUEST_DATA.Repeatable`
- `+0x1A` = `QUEST_DATA.Start.bLevel`
- `+0x1B` = `QUEST_DATA.Start.LevelMin`
- `+0x20` = `QUEST_DATA.Start.bItem`
- `+0x38` = `QUEST_DATA.Start.bQuest`

This is a structure mapping only. It does **not** by itself establish how these source fields map to the emulator's normalized `data_quest` SQL columns, nor does it establish the complete higher-level meaning of the branch sequence in `GetQuestStatusWithNPC`.

No runtime behavior was changed.
