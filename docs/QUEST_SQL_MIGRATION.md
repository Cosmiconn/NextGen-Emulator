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

- `QuestData` — one row per quest, including `Type`, `Repeatable`, normalized `DailyQuestType` (`QUEST_DATA +0x13`), and the complete 0x2A8 raw block.
- `QuestData_ConditionStart` — `QUEST_START_CONDITION` fields, including normalized map/X/Y/range and 64-bit DateStart/DateEnd values.
- `QuestData_ConditionEnd` — `QUEST_END_CONDITION` fields, including normalized map/X/Y/range.
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
quest status/times. Native `SetQuestDone` independently proves an in-memory
completion counter and 64-bit completion timestamp; the emulator durably maps
those values to `tQuestTimes.nTimes/dLastComplete`. This also drives the proven
Type-10 daily prerequisite reset check. `character_quest_progress` stores
normalized per-objective progress needed by the emulator runtime.

## Native end-condition coverage

The runtime now checks the proven end-condition model for:

- minimum level;
- NPC/Mob action 1 kill progress;
- the supplied corpus has **961** active action-1 entries and **zero** active action-2/action-3 entries; therefore missing action-2/3 event sources are not a blocker for this NA2016 dataset;
- required item lots;
- map/coordinate/range;
- scenario completion flag.

The supplied 2304-record corpus contains no active end-condition race, class, or
time-limit gates. Their raw fields remain preserved in SQL rather than assigning
unsupported emulator semantics.

Start/end location data is now normalized at import time. `LocationRaw` contains
only the 18-byte location subrange (`bLocation` through `LocationRange`) rather
than spilling into following condition fields. `DateRaw` is the exact 16-byte
`DateStart` + `DateEnd` range at start-condition offsets `0x30..0x3f`; the older
importer incorrectly started that slice four bytes early. Runtime code prefers
the normalized SQL columns and retains a legacy fallback for databases imported
with the older layout.

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
- race/date runtime semantics for source data that actually enables those gates; the supplied corpus has no active start-race/start-date conditions;
- NPC/Mob action 2/3 event sources for future QuestData variants that actually use those action values (the supplied corpus uses none);
- reward application semantics for unused/unsupported reward types 3, 5, 6, 7.

The corrected CodeView LevelChange symbol is:

`?Occure_LevelChange@CQuest@@UAEXGEE@Z`

## Import regression audit

CI runs `python3 tools/audit-quest-importer.py`. The audit builds a synthetic
QuestData record with a daily subtype, signed coordinates, map/range, scenario
data and two 64-bit date values, runs the real importer, and verifies the emitted
normalized SQL plus the corrected raw byte slices.


## SQL-only runtime regression audit

CI also runs `python3 tools/audit-quest-runtime-sql.py`. This guard makes the
migration boundary executable: the live Zone quest paths must keep their SQL
sources (`QuestData*`, `data_questdialog`, and `data_quest_script`) and must
not introduce SHN reader APIs for quest runtime data.

The only `QuestData.shn` mention allowed in `NextGen.Zone` is the migration
diagnostic that tells operators to re-import an older SQL layout. The file
itself remains valid in importer/tests/docs as the one-time authoritative import
source, but it is not opened by the server at runtime.

This audit deliberately proves the dependency boundary only. It does not assign
semantics to unused QuestData fields or commands that are absent from the
supplied NA2016 corpus.
