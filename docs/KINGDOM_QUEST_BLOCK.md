# Kingdom Quest block

## Status

**IN PROGRESS** on branch `nextgen-step15-kingdom-quests`.

This block starts from the merged Quest runtime baseline. Normal Quest behavior
must remain closed and CI-green while Kingdom Quest work proceeds separately.

## Project-source evidence already available

The repository already contains source-faithful SQL exports for:

- `KQTeam.shn` — 8 rows with team/PvP/division and red/blue regeneration coordinates;
- `KQIsVote.shn` — per-KQ vote enable flags;
- `KQVoteDesc.shn` — four vote reasons;
- `KQVoteMajorityRate.shn` — 70/50 percent thresholds;
- `KingdomQuestDesc.shn` — 39 description rows.

The project documentation also contains real NA2016 packet-capture evidence for:

- World-side KQ list traffic (SH22 type 29);
- instance detail request/response (CH22 type 3 / SH22 type 4);
- registration (CH22 type 5 plus SH22 type 6/50 responses);
- recruitment/countdown broadcasts (SH22 types 11, 30, 31, 37, 38);
- temporary Zone transfer into a real KQ instance;
- a complete failed Lost Mini Dragon session;
- empty `KingdomQuestFailed` packet (SH22 type 19);
- the observed dead-player return-to-town edge case.

No packet field or scheduler rule is to be invented from names alone.

## First implementation slice

World `DataProvider` now loads the source-faithful static KQ metadata:

- `KingdomQuestTeams`;
- `KingdomQuestVoteEnabled`;
- `KingdomQuestVoteReasons`;
- `KingdomQuestVoteMajorityRates`.

This deliberately does **not** replace the current zero-KQ list response yet.
The next step is to import/correlate the authoritative KingdomQuest definition
and schedule data from the project sources, then reproduce the captured SH22
list entry layout before enabling live registration.

## Guardrails

1. KQ definitions/schedules come from project source data, not hard-coded
   recreation from description text.
2. Captured packet fields are reproduced only after byte-level correlation.
3. Temporary Zone allocation and transfer use the existing World->Zone
   architecture; no fixed port is inferred from captures.
4. Vote-kick behavior stays disabled until a real KQ session state exists.
5. Normal Quest code remains untouched unless a proven shared primitive is
   required.


## Captured World wire primitives

The next implementation slice adds definition-neutral packet builders and a
thread-safe live-instance wire-state registry for layouts already proven by the
project captures:

- type 4: `u32 InstanceID + u16 value`;
- type 6: `u32 InstanceID + u16 value`;
- type 19: empty failure packet;
- type 30: `u16 count + u32 InstanceID[count]`;
- type 31: `u16 + u32 InstanceID + u16`;
- type 37: `u32 InstanceID + u16 stateValue`;
- type 38: `u16 count + {u32 InstanceID,u16 stateValue,u16 typeValue}[count]`.

The unresolved u16 fields deliberately keep neutral names. No scheduler meaning,
member-count meaning, map meaning, or status enum is assigned yet.

The legacy login-time zero KQ list is unchanged on the wire but now uses the
named `SH22Type.KingdomQuestList` instead of raw opcode `0x581D`.

CI runs `tools/audit-kingdom-quest-wire.py` to lock these layouts and to keep
the live KQ list empty until authoritative KingdomQuest definition/schedule
source rows are imported.


## Source-backed KQ map catalog

MapInfo already contains a source field named KingdomMap. Exactly 25 rows use value 1, including KDMDragon/KDHDragon, KDKingkong, KDArena, KDGreenHill and the event KQ maps. World DataProvider now exposes only those rows as KingdomQuestMaps; other nonzero KingdomMap classes (3/4/7) are deliberately not mixed into the KQ catalog.

CI locks the exact 25 map IDs/names plus the current team/vote row counts (8/30/4/2). This gives the definition layer a source-backed map set without inferring schedules or session rules.


## Reproducible main KQ SHN export

The build now checks tools/KingdomQuestSourceDump/NextGen.KingdomQuestTool.csproj. It uses the existing NextGen.FiestaLib SHNFile reader and searches source directories for KingdomQuest.shn, KingdomQuestMap.shn, KingdomQuestRew.shn, KQItem.shn and the already-known KQ metadata SHNs. It emits all source columns and rows to SQL without inventing a primary key or gameplay meaning.

Usage: dotnet run --project tools/KingdomQuestSourceDump/NextGen.KingdomQuestTool.csproj -- --sql kq-source.sql <source-directory>

This is the source-faithful path for importing definition/schedule/reward fields before live KQ list or registration semantics are enabled.


## Complete currently-exported static metadata load

World DataProvider now also loads all 39 KingdomQuestDesc rows in source order as KingdomQuestDescriptions. Together with the 25 KingdomMap=1 maps, 8 KQTeam rows, 30 vote flags, 4 vote reasons and 2 vote thresholds, every KQ SHN table currently exported into sql/data is available to the World runtime. The description row order is preserved; no source QuestID is invented for this table because the original export contains only Desc.
