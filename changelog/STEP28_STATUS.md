# Step 28 Status — `QUEST_DATA` CodeView field mapping

## Result

The supplied original `Zone.pdb` was parsed at the CodeView type-record level. `QUEST_DATA` is type `0x6aa5`, with field list `0x6aa4`, and concrete size `0x2a8` bytes. `Start` is a `QUEST_START_CONDITION` structure at offset `0x18`.

The raw offsets used by `CQuest::GetQuestStatusWithNPC` are now structurally mapped:

- `0x11` -> `QUEST_DATA.Type`
- `0x12` -> `QUEST_DATA.Repeatable`
- `0x1A` -> `QUEST_DATA.Start.bLevel`
- `0x1B` -> `QUEST_DATA.Start.LevelMin`
- `0x20` -> `QUEST_DATA.Start.bItem`
- `0x38` -> `QUEST_DATA.Start.bQuest`

The mapping is taken directly from the original CodeView field lists. No semantic names were inferred from normalized SQL column names.

## Runtime changes

None.

## Tests/build

Not run. No build or runtime success is claimed.

## Next target

Cross-check these proven source fields against `GetNewQuestStatus`, `IsSoonableQuest`, `IsDoingableQuest`, `IsRewardAbleQuest`, `IsSoonableDailyQuest`, `CQuestData::GetQuestData`, and the original DB/SHN representation before changing runtime selection logic or SQL mappings.
