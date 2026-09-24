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


## Historical capture interpretation — superseded

The early capture pass correctly identified the Header-22 KQ family but several
field names were provisional. The original 2016 command enum and PDB packet
structures now supersede those guesses:

- type 3/4 = `NC_KQ_STATUS_REQ/ACK`, not a generic instance-detail pair;
- type 5/6 = `NC_KQ_JOIN_REQ/ACK`;
- type 30/31 = `NC_KQ_LIST_DELETE_ACK/LIST_UPDATE_ACK`;
- type 36/37/38 = joining-alarm / joining-alarm-end / joining-alarm-list;
- type 50 = `NC_KQ_JOIN_LIST_ACK`, not a localized registration notice;
- type 58 = `NC_KQ_TEAM_TYPE_CMD`, not an unknown zone-entry status byte.

The old observations remain valuable for sequence/timing, but runtime naming and
serialization follow the native structures in
`docs/KINGDOM_QUEST_PROTOCOL_NATIVE.md`.

## Source-backed KQ map catalog

MapInfo already contains a source field named KingdomMap. Exactly 25 rows use value 1, including KDMDragon/KDHDragon, KDKingkong, KDArena, KDGreenHill and the event KQ maps. World DataProvider now exposes only those rows as KingdomQuestMaps; other nonzero KingdomMap classes (3/4/7) are deliberately not mixed into the KQ catalog.

CI locks the exact 25 map IDs/names plus the current team/vote row counts (8/30/4/2). This gives the definition layer a source-backed map set without inferring schedules or session rules.


## Reproducible main KQ SHN export

The build now checks tools/KingdomQuestSourceDump/NextGen.KingdomQuestTool.csproj. It uses the existing NextGen.FiestaLib SHNFile reader and searches source directories for KingdomQuest.shn, KingdomQuestMap.shn, KingdomQuestRew.shn, KQItem.shn and the already-known KQ metadata SHNs. It emits all source columns and rows to SQL without inventing a primary key or gameplay meaning.

Usage: dotnet run --project tools/KingdomQuestSourceDump/NextGen.KingdomQuestTool.csproj -- --sql kq-source.sql <source-directory>

This is the source-faithful path for importing definition/schedule/reward fields before live KQ list or registration semantics are enabled.


## Complete currently-exported static metadata load

World DataProvider now also loads all 39 KingdomQuestDesc rows in source order as KingdomQuestDescriptions. Together with the 25 KingdomMap=1 maps, 8 KQTeam rows, 30 vote flags, 4 vote reasons and 2 vote thresholds, every KQ SHN table currently exported into sql/data is available to the World runtime. The description row order is preserved; no source QuestID is invented for this table because the original export contains only Desc.


## KQ map-transfer prerequisite

A legacy ZoneCharacter.ChangeMap guard rejected every map ID above 120. That is incompatible with the source-backed KQ map catalog, which contains active KingdomMap=1 rows at IDs 126, 129, 131, 137, 138, 146, 148, 149, 155 and 158. The guard now validates the requested ID against DataProvider.MapsByID instead of a historical numeric ceiling. The existing World-to-Zone transfer path remains unchanged.


## Server-side map-instance transport

The existing codebase already modeled multiple map instances through
`Map.InstanceID`, `MapManager.GetMap(info, instance)`, and instance-bearing
`ZoneCharacter.SetMap/ChangeMap` signatures, but the path was incomplete:

- `MapManager.GetMap` could not create instance 1+;
- `SetMap` ignored its instance argument;
- Zone -> World -> target-Zone transfer dropped the internal instance number.

Those plumbing defects are now closed. The internal `short MapInstance`
survives transfer and is used when constructing the character on the target
Zone.

This does **not** assign native World KQ Handles such as 969 to a map
instance. The capture uses a 32-bit KQ registry ID, while `Map.InstanceID` is
a server-internal short namespace. CI explicitly guards against conflating
them until the project definition/session sources prove the mapping.


## Instance-bound mob spawns

`Mobspawn.InstanceID` is part of the project SQL schema and
`MobBreedLocation.CreateLocationFromPlayer()/SaveMobBreeds()` already preserve
it. The load path previously discarded that field and forced every breed to
instance 0.

`Map.LoadMobBreeds()` now queries by both `MapID` and the current
`Map.InstanceID`, then retains the row's `InstanceID`. Consequently a new
dynamic KQ map instance does not leak/update its mob spawns into instance 0.
It also does not clone instance-0 spawns by assumption; KQ-specific regen data
must populate the target instance from authoritative project sources.


## Native STATUS_REQ is live

World handles `NC_KQ_STATUS_REQ (0x5803)` as the original PDB structure
defines it: a single `u32 Handle`. If the Handle exists in
`KingdomQuestInstanceRegistry`, the server returns
`NC_KQ_STATUS_ACK (0x5804)` with Handle, Status, Joiner count and Name5 list.
Unknown Handles are not answered with fabricated state.

`NC_KQ_JOIN_REQ (0x5805)` remains intentionally disabled until the original
admission/error-code semantics are established. Its wire body itself is already
known exactly as `u32 Handle`.

## Explicit World-Handle to Zone-instance routing target

A live KQ now has a dedicated server-internal routing primitive:
`KingdomQuestSessionTargetRegistry`.

It stores an explicit triple:

```text
u32 World KQ Handle
u16 source-backed MapID
i16 internal Map.InstanceID
```

Creation is rejected unless the MapID exists in the source-backed
`KingdomQuestMaps` catalog, and duplicate World Handles or duplicate
`(MapID, MapInstance)` targets are rejected.

The registry does not allocate IDs, choose a map, or derive one namespace from
another. Those values must come from the later source-backed KQ definition/
session creator. This gives registration/transfer code a safe lookup target
without ever casting native Handles such as 969 into a Zone instance number.


## World-driven KQ transfer bridge

The World server can now request a KQ transfer **through the character's current
Zone**, rather than constructing a client ChangeZone packet itself.

`KingdomQuestTransferService.TryRequest` resolves the explicit
`KingdomQuestSessionTarget`, finds the current Zone from the character's
current map, and sends the internal `InterHeader.KingdomQuestTransfer`
control message. The source Zone resolves the active character and calls the
existing:

```text
ZoneCharacter.ChangeMap(targetMapId, x, y, targetMapInstance)
```

That preserves the already-tested transfer-key, target-Zone handoff, character
save/removal and client ChangeZone behavior.

The bridge does **not** select entry coordinates. They remain explicit
parameters so later `KingdomQuestMap.shn`/session evidence can supply them.
No CH22 registration handler calls this bridge yet.


## Native Header-22 protocol correction

The original 2016 NC_KQ_* enum and PDB protocol structures now replace the
capture-era placeholder names. Client type 27 is NC_KQ_LIST_REFRESH_REQ, not
a generic GotIngame/heartbeat. Server types 28-31 are LIST_TIME/LIST_ADD/
LIST_DELETE/LIST_UPDATE, and types 36-38 are JOINING_ALARM/
JOINING_ALARM_END/JOINING_ALARM_LIST.

The inherited 0x581C four-byte constant was a truncated Unix timestamp stub.
The response is now the native 40-byte ServerTime + struct tm body. Type 29
remains an exact empty LIST_ADD response (u16 count = 0) until main KQ
definitions are imported.

STATUS_REQ/ACK is corrected to u32 Handle + u8 Status + u16 joinerCount +
20-byte Name5 entries.

See docs/KINGDOM_QUEST_PROTOCOL_NATIVE.md for the field table. JOIN_REQ remains
disabled because source-backed admission/scheduler/session rules are not yet
loaded, not because its wire structure is unknown.

## Native KQ definition wire model

The client-visible PROTO_KQ_INFO_CLIENT structure is now represented explicitly
as KingdomQuestClientInfo with its native 141-byte layout. The full
PROTO_KQ_INFO server/session structure is represented as KingdomQuestProtocolInfo
with the native 377-byte layout.

The model preserves the original field boundaries: Handle/Status/Joiner count,
ID/title/timers, level/player limits, repeat/revival gates, demand quest/item/
class/gender, server scheduling fields, four 26-byte map links, script language/
init strings and two team regeneration coordinates.

This still does not map KingdomQuest.shn columns by assumption. It creates the
byte-exact destination model for the source exporter. LIST_ADD/LIST_ACK/
SCHEDULE_ACK builders can now serialize real entries once source rows are
available; the live refresh handler continues to send an empty LIST_ADD.

## Source-owned client-definition registry

`KingdomQuestDefinitionRegistry` now holds complete
`PROTO_KQ_INFO_CLIENT` values keyed by their already-assigned native Handle.
The registry deep-copies the 141-byte field model on input/output and sorts
snapshots by Handle.

It deliberately performs none of the unresolved work:

- no handle allocation;
- no start-time calculation;
- no status transition;
- no map selection;
- no session-target creation.

The LIST_REFRESH handler now serializes exactly the snapshot held by this
registry through the native `LIST_ADD_ACK` serializer. With no source-backed
definitions registered, the result remains byte-identical to the previous
`u16 count = 0` response. Once the scheduler supplies real entries, no
additional handler-side mapping or hard-coded list data is required.

This separates source ingestion from protocol serialization without making
schedule guesses.


## Native Handle naming carried into routing

The early capture work called the 32-bit KQ key an "InstanceID". The original
PDB protocol names it `Handle`. The World routing layer now uses that native
name consistently:

```text
u32 KQ Handle        -- client/World protocol identity
u16 MapID            -- source-backed map
i16 Map.InstanceID   -- emulator-internal Zone instance
```

No cast or numeric derivation connects Handle to `Map.InstanceID`; the
explicit target registry remains the only bridge.


## Explicit session-coordination boundary

`KingdomQuestSessionCoordinator.TryCreate` now provides one atomic entry point
for a scheduler that has already resolved a real KQ:

```text
complete PROTO_KQ_INFO_CLIENT definition
exact joiner-name list
source-backed MapID
internal Map.InstanceID
```

Creation succeeds only when the definition's `NumOfJoiner` equals the supplied
name count and the Handle is absent from all three registries. It then creates
the explicit routing target and synchronizes the client-definition and
status/joiner registries. Any exception rolls all three back.

The coordinator does **not** allocate Handles, calculate times, assign status,
choose maps or select joiners. Those remain the source/scheduler layer.


## Native participant roster

KQ membership now has one canonical wire model:
`KingdomQuestParticipantRegistry` stores exact
`KQ_JOIN_CHAR_INFO` entries (Level, Class, Name5, Team) by KQ Handle.

Session creation accepts that full roster, verifies its count against
`PROTO_KQ_INFO_CLIENT.NumOfJoiner`, stores it, and derives the STATUS_ACK
Name5 list from the same entries. This prevents STATUS_ACK and JOIN_LIST_ACK
from drifting into separate membership views.

No player is admitted and no Team value is assigned by this registry; both are
inputs from the later admission/team-selection layer.


## Synchronized session mutation

The session coordinator now also exposes two explicit mutations for the later
scheduler/admission layer:

- `TrySetStatus(handle, status)` updates both the client-list definition and
  STATUS_ACK state with the exact caller-supplied status byte.
- `TrySetParticipants(handle, roster)` updates
  `NumOfJoiner`, the full Level/Class/Name5/Team participant roster and the
  STATUS_ACK Name5 list together.

Neither method decides *which* status is valid or *who* may join. They only
prevent the three native client views from diverging once an authoritative
caller supplies the decision.


## KQ source-dump provenance tightened

`NextGen.KingdomQuestTool` now supports:

```text
--require-main
```

When used, a source dump fails unless all four authoritative main tables are
present: `KingdomQuest.shn`, `KingdomQuestMap.shn`,
`KingdomQuestRew.shn` and `KQItem.shn`.

The tool also rejects ambiguous duplicate basenames discovered under multiple
input directories instead of silently emitting the same SQL table twice. Each
exported table now records the source file SHA-256 plus a raw
`column-name:type-byte:length` manifest before the SQL DDL. This keeps later
field mapping tied to the exact NA2016 source bytes rather than only a filename.


## Native character KQ-map context

Original PDB data resolves `PROTO_NC_CHAR_KQMAP_CMD` at opcode `0x101A`
(Header 16, Type 26):

```text
u32 Handle
char MapName[12]
i32 X
i32 Y
u32 SHINE_DATETIME (packed)
```

The same Handle/MapName/XY/date fields also occur in the original character
data structure, and the PDB-derived proxy correlation confirms that the XY pair
decodes as real coordinates.

`KingdomQuestMapContextRegistry` now stores this context per live KQ Handle.
MapName is derived from the already source-backed `MapInfo.ShortName`, never
accepted as an independent caller string. The packed date remains a raw
`uint`; its 4-bit year field's epoch is still UNRESOLVED.

`KingdomQuestTransferService.TryRequest(character, handle)` no longer accepts
free X/Y arguments. It requires an explicit native KQ map context and reuses
those coordinates in the existing Zone transfer bridge. A byte-exact
`NC_CHAR_KQMAP_CMD` builder is present, but it is not automatically injected
into the transfer sequence because the supplied capture does not prove its
ordering relative to `ChangeZone`.


## Full native server definition retained per session

A live session can no longer be created from only the 141-byte client-list
prefix. `KingdomQuestSessionCoordinator.TryCreate` now requires a complete
`KingdomQuestProtocolInfo` (`PROTO_KQ_INFO`, 377 bytes).

`KingdomQuestProtocolDefinitionRegistry` deep-copies and retains every native
server/session field needed by the original World→Zone lifecycle, including
schedule fields, all four map links, script language/init value, team-PvP flag
and both team regen coordinates. The existing 141-byte
`KingdomQuestDefinitionRegistry` remains the client-visible projection.

Status and participant-count mutations update both registries atomically. No
server-only field is reconstructed from the client prefix, and no scheduler
field is synthesized.


## Explicit World→Zone joiner roster

`NC_KQ_W2Z_START_CMD` does not send player names. Its flexible joiner array
uses native 5-byte `PROTO_NC_KQ_JOINER` entries:

```text
u32 CharacterNumber
u8  TeamType
```

`KingdomQuestZoneJoinerRegistry` now stores exactly those pairs per KQ
Handle. It is intentionally separate from the client-visible
Level/Class/Name5/Team roster. No name lookup, Character.ID assumption, party
lookup or automatic team assignment exists in this registry.

Session removal clears this server-side roster as well.


## Native World→Zone lifecycle carried by emulator transport

The custom inter-server connection can now transport the already-modeled native
KQ lifecycle without defining a second KQ schema:

- World builds the exact original `NC_KQ_W2Z_MAKE_REQ (0x580D)` or
  `NC_KQ_W2Z_START_CMD (0x580F)` bytes;
- the internal message adds only the already-explicit emulator MapID and
  `Map.InstanceID` needed for MAKE;
- Zone reconstructs a normal Fiesta `Packet`, validates its original opcode,
  and parses the 377-byte `PROTO_KQ_INFO` / 5-byte joiner records with the
  shared native parsers;
- END and DESTROY likewise carry their exact original handle-only packet body.

`KingdomQuestZoneRuntimeRegistry` creates the exact requested map instance via
`MapManager.GetMap(mapInfo, mapInstance)` and tracks Made/Started/Ended state.

This transport still does **not** decide when MAKE succeeds, invent
`Z2W_MAKE_ACK.Error`, allocate a Handle/MapInstance, choose teams or admit
players. No scheduler currently invokes these send methods automatically; they
are the validated World→Zone execution boundary for the later authoritative
session owner.


## Machine-readable main-source provenance

The KQ source dumper now emits two metadata tables before the raw SHN tables:

- data_kq_source_manifest: SourceName, SHA-256, RecordCount and ColumnCount.
- data_kq_source_columns: SourceName, zero-based original Ordinal, ColumnName, SHN TypeByte and raw column Length.

These tables are not a gameplay schema and contain no inferred key or semantic mapping. They make the exact project-source bytes and column layout queryable by runtime/audits, so a later KingdomQuest.shn -> PROTO_KQ_INFO mapper can reject a mismatched source snapshot instead of silently accepting it.


## Runtime provenance gate

World reads data_kq_source_manifest and data_kq_source_columns when they exist. Each source is accepted only when SHA-256 is present, ColumnCount matches the manifest rows, and zero-based ordinals are contiguous. HasCompleteKingdomQuestMainSource becomes true only when KingdomQuest, KingdomQuestMap, KingdomQuestRew and KQItem are all structurally valid.

This gate does not create definitions or start sessions. It is the required precondition for the later source-to-PROTO_KQ_INFO mapper.


## Exact-name schema coverage before mapping

`KingdomQuestNativeSchema` now contains the top-level field names of the
original 2016 `PROTO_KQ_INFO` exactly as recovered from PDB/source layout.
When a real `KingdomQuest.shn` manifest is present, World reports three sets:

- source columns whose names exactly equal native fields;
- native field names not present verbatim in the source;
- source column names with no exact native-name match.

This is deliberately a diagnostic boundary, not an alias table. Known
secondary/tutorial spellings such as `ST_Hour`, `NextStartDeleyMin`,
`MinPlayer` or `InitValue` are specifically forbidden from the native
mapping class. They can only be mapped after the supplied NA2016 SHN manifest
and original behavior establish the relationship.


## PDB-bounded KINGDOM_QUEST source projection

The supplied WorldManager PDB exposes the original table-header type
`KINGDOM_QUEST` and the native `PROTO_KQ_INFO` member names. The runtime now
has `KingdomQuestSourceProjection.ApplyProvenStaticFields`, but it is
intentionally a partial projection.

It copies exact/static counterparts such as ID/title/limits/player gates,
repeat/revival fields, DemandQuest/DemandItem/DemandGender, RewardIndex,
DemandMobKill and ScriptLanguage. Two source/protocol naming differences are
now explicitly correlated from the original PDB types:

```text
KINGDOM_QUEST::NextStartDeleyMin  -> PROTO_KQ_INFO::NextStartDelayMin
KINGDOM_QUEST::InitValue          -> PROTO_KQ_INFO::ScriptInitValue
```

The mapper is CI-forbidden from assigning:

- Handle, Status or NumOfJoiner;
- StartTime/tm_StartTime from the source ST_* components;
- DemandClass from UseClass;
- resolved PROTO_KQ_MAP_INFO entries from source MapLink indices;
- ScheduleTime/tm_ScheduleTime or RunCounter;
- IsTeamPVP or TeamRegenXY.

Those fields require the original scheduler/admission/map/team behavior and are
not filled with defaults disguised as semantics.


## Original WorldManager scheduler recovered

The supplied NA2016 `WorldManager.exe` and PDB now close the basic schedule
calculation without field-name inference.

PDB procedure records resolve:

```text
CKQServer::GetNextScheduleTime  .text+0x51160
CKQServer::AddNewScheduleList   .text+0x545D0
CKQServer::DoSchedule           .text+0x54E40
CharClassDataBox::ccdb_UseClassTypeToBit .text+0x64A90
```

The matching executable path proves that `DoSchedule` builds the candidate
`tm` from source `ST_Day/ST_Hour/ST_Minute`, but copies the current local
month/year and zeroes seconds. `ST_Year`, `ST_Month` and `ST_Second` are
not read on this path. It passes source `NextStartDeleyMin` directly to
`GetNextScheduleTime`.

`GetNextScheduleTime` advances the candidate by that many `tm_min` minutes
until it is at-or-after the current minute, then emits exactly two consecutive
schedule entries. `KingdomQuestSourceScheduler.GetNextScheduleTimes` now
models only that proven behavior; it does not publish entries or own handles.

`AddNewScheduleList` further proves that a newly materialized
`PROTO_KQ_INFO` starts with status/joiner count zero and uses the same
`time32/localtime` value for both StartTime/tm_StartTime and
ScheduleTime/tm_ScheduleTime. The scheduler projection accepts an externally
resolved DemandClass and handle, because ownership of those inputs is separate.

The same executable also corrects one earlier partial mapping:

```text
PROTO_KQ_INFO.DemandGender =
    byte(KINGDOM_QUEST.Undefined3 * 2 + KINGDOM_QUEST.DemandGender)
```

and reads only the low 16 bits of source RewardIndex. The projection now follows
those byte/word operations exactly.

Finally, `AddNewScheduleList` calls `GetKQTeamData(KINGDOM_QUEST::ID)` and
copies only IsTeamPVP plus red/blue regen XY into the native definition, or
zeroes those fields when no KQTeam row exists. Team divide semantics remain
UNRESOLVED and are not used here.

DemandClass is still deliberately external to the scheduler projection:
the EXE proves that source UseClass is passed through
`ccdb_UseClassTypeToBit`, and the supplied `UseClassTypeInfo.shn` is the
authoritative conversion table. That dependency must be imported/correlated
before live list publication; no class mask is guessed.
