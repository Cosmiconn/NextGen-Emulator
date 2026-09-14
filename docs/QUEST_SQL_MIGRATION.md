# Quest SQL migration — final QuestData path

## Result

`QuestData.shn` is now treated as a **one-time import source**, not a runtime dependency.
The emulator-side quest runtime reads the imported SQL tables only.

The supplied NA2016 `QuestData.shn` was parsed deterministically as:

```text
4-byte file header
repeat:
    QUEST_DATA fixed block: 0x2A8 bytes
    Start script: SizeOfScriptStart
    Doing/Action script: SizeOfScriptDoing
    End/Finish script: SizeOfScriptEnd
```

The size fields are stored at `QUEST_DATA +0x294/+0x296/+0x298` in the order
`Start, End, Doing`, while the serialized script order is `Start, Doing, End`.
This was validated across the complete file: **2304 records, zero trailing bytes**.

## Generated SQL tables

The importer creates the source-faithful SQL tables:

- `QuestData` — one row per quest, including the complete 0x2A8 raw block.
- `QuestData_ConditionStart` — `QUEST_START_CONDITION` fields.
- `QuestData_ConditionEnd` — `QUEST_END_CONDITION` fields.
- `QuestData_NPCMob` — five 8-byte NPC/Mob condition entries.
- `QuestData_Item` — five 6-byte item condition entries.
- `QuestData_Reward` — twelve 12-byte reward slots.
- `data_quest_script` — Start/Action/Finish script text used by the existing interpreter.
- `data_quest_start_dialog` — first `SAY` dialog ID of each start script when present.

Compatibility views are also generated for the current resolver:

- `data_quest`
- `data_quest_objective`

## Reward layout

Direct binary inspection establishes the reward slot layout as:

```text
+0x00 UseType
+0x01 RewardType
+0x02..0x03 reserved
+0x04 Value1
+0x08 Value2
```

Observed reward types in the supplied corpus are 0 (EXP), 1 (MONEY), 2 (ITEM),
and 4 (FAME). Selectable slots use `UseType=2`; the client selection index is the
zero-based ordinal among selectable slots.

## Runtime persistence

The original supplied SQL Server backup independently proves the character quest
persistence names `tQuest` and `tQuestTimes` with `nCharNo`, `nQuestNo`, `nStatus`,
`sData`, `nTimes`, and `dLastComplete`. The emulator now uses those names for
quest status/times. `character_quest_progress` stores normalized per-objective
progress needed by the emulator runtime.

## Native end-condition coverage

The runtime now checks the proven end-condition model for:

- minimum level;
- NPC/Mob action 1 kill progress;
- action 2/3 progress slots when populated by future event handlers;
- required item lots;
- map/coordinate/range;
- scenario completion flag.

The supplied 2304-record corpus contains no active end-condition race, class, or
time-limit gates. Their raw fields remain preserved in SQL rather than assigning
unsupported emulator semantics.

## Event wiring

Native `MOB_KILL` progress is now recorded from both normal and AoE mob deaths.
Scenario completion is represented in the same SQL progress store when the Header
17 scenario-done path is used.

## Reproducibility

Run:

```bash
python3 tools/import-questdata.py /path/to/QuestData.shn -o QuestData.sql
```

The importer validates the fixed `0x2A8` record size, all three script lengths,
and exact EOF alignment before writing SQL. After import, the `.shn` file is not
needed by the emulator runtime.

## Remaining reverse-engineering boundary

The quest corpus itself is complete and importable. The following are deliberately
not invented:

- optimized Zone.exe address mapping for `Occure_LevelChange`;
- native numeric mapping for race/class where the current emulator has no proven
  equivalent;
- native implementations of NPC/Mob action 2/3 event sources;
- reward application semantics for unused/unsupported reward types 3, 5, 6, 7.

The corrected CodeView LevelChange symbol is:

`?Occure_LevelChange@CQuest@@UAEXGEE@Z`
