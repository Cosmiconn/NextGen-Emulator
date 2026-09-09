# Changelog — `QUEST_DATA_CODEVIEW_AUDIT.md`

Step 28 adds a direct CodeView/PDB reconstruction of the original `QUEST_DATA` and `QUEST_START_CONDITION` layouts.

Proven mappings for raw offsets used by `CQuest::GetQuestStatusWithNPC`:

- `+0x11` -> `QUEST_DATA.Type`
- `+0x12` -> `QUEST_DATA.Repeatable`
- `+0x1A` -> `QUEST_DATA.Start.bLevel`
- `+0x1B` -> `QUEST_DATA.Start.LevelMin`
- `+0x20` -> `QUEST_DATA.Start.bItem`
- `+0x38` -> `QUEST_DATA.Start.bQuest`

No runtime behavior was changed.
