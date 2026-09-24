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
accepted as an independent caller string. The packed date remains stored as a raw `uint`, but its World reconnect
decode is now source-proven by `CKQServer::IsExisted`. The low four bits
become `tm_year = value + 100` (effective year 2000+nibble), followed by
1-based month (4 bits), day (5), hour (5), minute (6) and second (6).

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


## Source-backed DemandClass conversion

The shared original `UseClassTypeInfo.shn` dependency is now imported with
SHA-256 `0ef94a55e26fb992e0497f825984742df681f94f9bf32167e4defebcbead632d`
(39 rows, 28 columns). It is included in the reproducible SHN dumper's
preferred corpus and is provenance-checked independently of the four main KQ
tables.

`CharClassDataBox::ccdb_UseClassTypeToBit` at WorldManager
`.text+0x64A90` looks up the row by `UseClass`, reads the 27 class bytes
from Sav back through Fig, folds them by shift/add, then shifts once more.
The runtime reproduces that exact byte folding in
`KingdomQuestUseClassSourceRow.ToDemandClassMask()`; no class/job ordering is
invented from names.

`KingdomQuestSourceScheduler.CreateScheduledDefinition` now requires the
source-backed UseClass mask dictionary and refuses to materialize a definition
if the referenced UseClass row is absent. This closes the previously external
DemandClass input while keeping handle allocation and live state transitions
separate.


## Native client list selection and refresh deltas recovered

Original `WorldManager.exe` closes the earlier LIST/SCHEDULE range blocker.
`CParserClient::fc_NC_KQ_LIST_REQ` (`.text+0x12980`) and
`fc_NC_KQ_SCHEDULE_REQ` (`.text+0x12E00`) validate the two request Handle
fields but do not use them for filtering. LIST returns every scheduler entry
whose unsigned Status is 0..4; SCHEDULE returns every scheduler entry. ACK
start/end Handles come from the first/last returned entry.

`CKQServer::Ack_NC_KQ_LIST_REFRESH` (`.text+0x549E0`) is also recovered:
LIST_TIME is one-shot per client session, followed by delete/update/add deltas
against that session's prior Status<=4 snapshot. Update compares only Status
and NumOfJoiner. LIST_ADD is flushed in 53-entry groups, matching both the
executable's 0x1D26 threshold and the existing 53/53/8 packet capture.

The live handlers now reproduce those source-backed rules and no longer depend
on the old exact-request-pair range registry. Empty ACKs use
NewStartHandle=0xFFFFFFFF; the original leaves empty NewEndHandle
uninitialized, so the emulator deliberately zeroes it rather than reproduce
stack-memory disclosure.

This closes list-selection/refresh semantics, but it does **not** activate
JOIN admission or synthesize scheduler entries. Live scheduler ownership,
status transitions and map lifecycle remain separate work.


## Native JOIN admission and MAKE result closed

Original WorldManager code now closes the previously blocking JOIN_ACK value.
`CParserClient::fc_NC_KQ_JOIN_REQ` at `.text+0x12CC0` emits
`0x0991 + CKQServer::PlayerJoin(...)`; PlayerJoin returns zero on success.
The resulting exact ACK range is 0x0991 success, then 0x0992 handle/BF,
0x0993 capacity, 0x0994 status, 0x0995 level, 0x0996 class, 0x0997 gender,
0x0998 unexpected fallback, 0x0999 nonzero `prisonmin`, and 0x099A
already joined to that Handle.

The emulator already serializes Character.Job and LookInfo.Male into the same
original shape bits used by PlayerJoin. It does **not** currently persist/model
`PROTO_NC_CHAR_BASE_CMD::prisonmin`, and the original request also performs
PlayerDisjoin before joining a different Handle. JOIN_REQ therefore remains
network-disabled until those state mutations can be represented without
defaulting an absent original field to zero.

The MAKE handshake is independently closed:
`CParserZone::fc_NC_KQ_Z2W_MAKE_ACK` treats exactly `0x0981` as success.
Success enters `CKQServer::SetJoining`; any other Error executes
`SetNoMapBF` and writes Status 8. SetJoining accepts Status 1/2, resets
joiner/team counters and writes Status 2. Zone now emits 0x0981 only after a
successful represented MAKE; World applies the corresponding represented
status/roster transition. No failure Error is guessed.

`PlayerJoin` also proves the supplied KQTeam divide behavior: only
`KQTeamDivideType == 2` uses the two-team balancing branch. Otherwise
TeamType is 2. Since all supplied KQTeam rows use divide type 1, the native
automatic join result for those rows is TeamType 2.

## Native scheduler Handle ownership and start gate

`CKQServer::AddNewScheduleList` proves the World scheduler's Handle owner:
the CKQServer constructor initializes its next-Handle counter to zero; a newly
added unique (KQ ID, ScheduleTime) entry receives the current counter and then
increments it. Existing ID/time pairs are not allocated a new Handle. The
scheduler array has a hard 300-entry capacity.

`CKQServer::DoSetStart` resolves the Status-2 start gate. A KQ enters the
10-second Status-3 countdown immediately when NumOfJoiner reaches MaxPlayers.
Otherwise it waits until StartTime + StartWaitTime minutes; after that
deadline, fewer than MinPlayers calls SetDoneSkip(index, 2). With enough
players it additionally calls KQTeam_CanKQStart; only a successful result
enters the same Status-3/10-second countdown.

For KQTeamDivideType 2, KQTeam_CanKQStart requires both native team counters
to be nonzero and rejects a team-size difference greater than MaxMemberGap,
using SetDoneSkip reasons 2/3 respectively. Other divide types bypass those
team-count gates. The later sections below document the now-live MAKE, DoSetStart and
post-countdown START boundaries.


## Live source scheduler publication

The recovered `CKQServer::AddNewScheduleList` ownership is now live without
crossing into unresolved map allocation. `KingdomQuestScheduleRuntime`
starts after `DataProvider` in the World `Worker` initialization stage and
enables itself only when the exact four-table KQ corpus **and**
`UseClassTypeInfo` provenance gates are complete.

It reproduces the original scheduler-array rules already established from
WorldManager.exe:

- the process-local next Handle starts at `0`;
- scheduling is executed at most once per local minute;
- every source row contributes the two-entry
  `GetNextScheduleTime` window;
- an existing exact `(KQ ID, ScheduleTime)` pair is not added again;
- the hard scheduler-array capacity is `300`;
- the Handle counter increments only after a new entry is successfully
  published.

A new Status-0 entry is atomically registered in the full 377-byte definition,
the client 141-byte projection, STATUS state, and an empty participant roster.
No `KingdomQuestSessionTarget`, MapID, `Map.InstanceID`, MAKE request or
status advancement is created here. That boundary matches the original split:
`AddNewScheduleList` owns schedules/Handles; `DoSetMakeRoom/AllocMapLink`
owns map allocation later.

As a result, LIST/SCHEDULE/STATUS now have a real source-backed scheduler owner
instead of waiting for an external registry feeder, while the next map-routing
step remains gated on the still-to-be-correlated `AllocMapLink` behavior.


## Native KingdomQuestMap slot allocation recovered

The original WorldManager map allocator is now correlated byte-for-byte with
the supplied `KingdomQuest.shn` and `KingdomQuestMap.shn` rows.

`CKQServer::AllocMapLink(PROTO_KQ_INFO*)` first scans the original
`KINGDOM_QUEST` table from row zero and uses the **first** row whose `ID`
matches the scheduled definition. Its four 16-bit fields beginning at source
offset `0x66` are the four SHN columns already preserved as
`MapLink`, `Undefined 0`, `Undefined 1`, and `Undefined 2`. A value
of `0xFFFF` means no link; every other value is a zero-based
`KingdomQuestMap.shn` source-row index.

For each referenced map row, `GetEmptyMapLink` reads `NumOfMap` and scans
slot numbers from zero upward. The original allocation table is exactly ten
DWORD owners per `KingdomQuestMap` row, initialized to `0xFFFFFFFF`.
For a slot:

- if its corresponding source `Clear` byte is zero, the slot is returned
  immediately and is not owner-reserved;
- otherwise it is available only when the allocation-table owner is
  `0xFFFFFFFF`;
- when selected with nonzero `Clear`, that owner is replaced by the KQ
  Handle.

`PROTO_KQ_MAP_INFO` is then filled exactly from the selected source slot:
`MapIndex = slot`, `MapBase = BaseMap`, `MapName = Map[slot]`, and
`MapClear = Clear[slot]`. If any of the four links cannot allocate, the
original function calls `FreeMapLink(Handle)`, which scans every source row
and all ten owner cells and resets every cell owned by that Handle.

All 38 supplied NA2016 `KingdomQuestMap.shn` rows have all ten Clear bytes
set to `1`, so every selected slot in this corpus uses exclusive Handle
reservation.

`KingdomQuestMapAllocationRegistry` now reproduces exactly that native
source-slot ownership and fills the server-side `PROTO_KQ_INFO.MapLink`
array. It deliberately does **not** create a
`KingdomQuestSessionTarget`, choose a MapID, or derive an emulator
`Map.InstanceID` from native `MapIndex`. The latter remains a distinct
routing namespace until the Zone-side map-name behavior is correlated.


## Zone MapBase routing and live DoSetMakeRoom

The supplied Zone PDB/EXE now closes the remaining native-map routing
ambiguity. In `WorldManagerSession::wms_NC_KQ_W2Z_MAKE_REQ`
(`.text+0x96790`) each populated `PROTO_KQ_MAP_INFO` is handled in two
different namespaces:

- `MapName` is checked by `KingdomQuestContainer::kqc_MapUseCheck` and is
  used as the dynamic `FieldMap` identity;
- `MapBase` is passed to the original global `mapdatabox` lookup to obtain
  the static map data used to construct that field.

So `MapBase -> MapInfo.ShortName` is now an executable-proven correlation,
not a name-based guess. The resolver uses an exact ordinal string comparison
against the already source-backed `KingdomMap=1` catalog.

The exact supplied NA2016 snapshot is also locked to one active source
`MapLink` for every one of its 57 `KingdomQuest.shn` rows. Its 38
`KingdomQuestMap.shn` rows reference 23 distinct `BaseMap` names, all of
which exist in that source-backed KQ map catalog. This is deliberately treated
as a property of this provenance-locked corpus. A future source snapshot with
multiple active links is not silently reduced to one map.

The native and emulator namespaces remain separate:

```text
u32 Handle             native World KQ identity
u8  MapIndex           native slot inside one KingdomQuestMap row
MapName[12]            native dynamic FieldMap identity
MapBase[12]            native static mapdatabox identity
u16 MapID              emulator/source-backed base map ID
i16 Map.InstanceID     emulator-internal dynamic instance
```

`KingdomQuestSessionTargetRegistry.TryAllocateNative` now retains
`MapIndex/MapBase/MapName` but allocates `Map.InstanceID` independently
from a per-MapID counter beginning at 1. Instance 0 remains the emulator's
loaded/base map. No cast or arithmetic derives the internal instance from the
native Handle or MapIndex.

WorldManager's `CKQServer::DoSetMakeRoom` is also live for due scheduler
entries. The original branch is reproduced as:

```text
Status == 0 && ScheduleTime <= now
    -> AllocMapLink
       failure: Status = 8
       success: Status = 1; send NC_KQ_W2Z_MAKE_REQ
```

Before consuming a native map slot the emulator requires an active Zone that
owns the proven `MapBase` MapID. That availability check is internal
transport plumbing; when no such Zone is connected the KQ remains Status 0 and
no native slot is consumed. If enqueueing the already-prepared MAKE fails, an
explicit emulator-only rollback restores Status 0 and frees the reservation.

Zone now independently validates that the internal MAKE routing MapID resolves
to the same exact `MapBase` carried by the native request before creating the
requested internal map instance. This prevents internal transport metadata from
silently redirecting a native KQ definition to a different base map.


## Live DoSetStart gate and recovered post-countdown start sequence

The original WorldManager `CKQServer::DoSetStart` /
`KQTeam_CanKQStart` branch is now connected to the live scheduler after a
successful MAKE_ACK has placed an entry in Status 2.

The implemented decision is limited to the already-recovered executable
behavior:

```text
Status 2
  NumOfJoiner == MaxPlayers
    -> Status 3, exact 10-second countdown

  otherwise, before StartTime + StartWaitTime*60
    -> remain Status 2

  at/after the deadline:
    NumOfJoiner < MinPlayers
      -> SetDoneSkip(reason 2), Status 6

    KQTeamDivideType != 2 or no KQTeam row
      -> Status 3, exact 10-second countdown

    KQTeamDivideType == 2 (KQTD_USERSELECT):
      either team empty
        -> SetDoneSkip(reason 2), Status 6
      abs(team0-team1) > MaxMemberGap
        -> SetDoneSkip(reason 3), Status 6
      otherwise
        -> Status 3, exact 10-second countdown
```

The original PDB names the divide enum values used by these branches:
`KQTD_RANDOM=1` and `KQTD_USERSELECT=2`. The PlayerJoin-time
smaller-team branch is therefore the USERSELECT initialization path, not the
random divider. Every supplied NA2016 KQTeam row has divide type 1: players
join those KQs as neutral TeamType 2 and are divided only at start.

The participant count and team values now have an explicit combined native
membership owner. `KingdomQuestMembershipEntry` carries CharacterNumber,
Level, Class, Name and TeamType together and projects both
`KQ_JOIN_CHAR_INFO` and `PROTO_NC_KQ_JOINER`. The registry itself never
reconciles names with IDs. When a live World character supplies a new native
row, `KingdomQuestCharacterIdentity` can now source CharacterNumber from the
emulator's already-correlated `Character.ID == chrregnum/nCharNo`
representation.

The original `DoSetStart` Status-3 path is now also recovered. When the
10-second byte counter reaches zero, World writes Status 4, calls
`KQTeam_DivideRandom`, then `KQTeam_LeaveParty`, then sends
`NC_KQ_W2Z_START_CMD`.

`KQTeam_DivideRandom` runs only for `KQTD_RANDOM=1`. It computes
`floor(joiners/2)`, consumes one `RandomBox::rb_1000()` sample per native
joiner in BF order, prefers team 1 below 500 and team 0 at/above 500, and
forces the opposite team whenever the preferred team already reached the
half-size while the other has not. The odd extra player remains random after
both sides reach the half-size.

The exact random source is also correlated from the executable:
`RandomBox` seeds 16 WELL512 words from MSVCRT `rand()`; `rb_1000`
normalizes the WELL output by 2^-32, multiplies by 1e11, truncates and reduces
modulo 1000. `KingdomQuestNativeRandom` models that integer path from an
explicit original-style time32 seed; no .NET Random policy is substituted.

`KQTeam_LeaveParty` is now live for every group state this emulator can
actually represent. Original World refreshes each joiner's party/raid state,
uses `RaidLeave` for raid membership and otherwise uses normal `LeaveParty`
when a party exists. This codebase contains no Raid model or Raid creation
path; every represented grouped World session is therefore the existing normal
`Group`/Party model rather than an unknown raid surrogate.

Before Status 4 is written, the runtime resolves every native
CharacterNumber back to its already-correlated World session, verifies the
session still owns the same KQ Handle, validates any represented Party, and
resolves the source-backed KQ target to a currently connected owning Zone.
Only after that complete preflight does the recovered native order run live:

```text
Status 3 countdown expires
  -> Status 4
  -> KQTeam_DivideRandom (only KQTD_RANDOM=1)
  -> refresh each represented joiner's current Party state
  -> normal GroupManager.LeaveParty when grouped
  -> NC_KQ_W2Z_START_CMD with updated CharacterNumber/TeamType roster
```

The random source is one persistent RandomBox-compatible WELL512 stream for
the World KQ runtime, seeded once from the original-style `_time32` startup
value; a new RNG is not created per KQ. Zone already consumes the exact W2Z
START body and changes the represented KQ state from Made to Started.

`KingdomQuestStartCountdownRegistry` retains the ten-second deadline and
`KingdomQuestDoneSkipRegistry` retains raw Status-6 reason bytes.

SetDoneSkip cleanup and old-schedule deletion are now recovered and live. The
original `CKQServer::SetDoneSkip(index, reason)` at `0x004548C0`
performs, in order:

```text
Status = 6
Send_NC_KQ_W2Z_DESTROY_CMD(Handle)
FreeMapLink(Handle)
Send_NC_KQ_NOTIFY_CMD_ToJoiner(...)  // reason-specific
FreeJoiner(index)                    // session nKQHandle = 0xFFFFFFFF
Send_NC_KQ_JOINING_ALARM_END_CMD(index)
```

The notification source is the supplied original
`MsgWorldManager.shn` (SHA-256
`36b573c6f604a693cf0d0c7533fc90615233ae4a8be62f3ced9bd4119f25884e`),
which has exactly three `Desc` rows. Reason 2 uses row index 2:

```text
Kingdom Quest - %s has been canceled due to lack of participants(%d/%d).
```

with Title, NumOfJoiner and MinPlayers. Reason 3 asks `GetMsg(3)` and
`GetMsg(4)`; both are beyond the three-row source table, and the original
`WorldManagerServer::GetMsg` returns its empty fallback string for each.
The emulator therefore sends the same two empty NOTIFY bodies instead of
inventing missing localization text.

`DelOldShceduleList` at `0x00454080` runs after `DoSetStart`.
For every entry whose raw Status is in the original unsigned range 5..10, it
looks for another entry with the same KQ ID, also Status 5..10, but a later
`ScheduleTime`. Only then is the older entry marked Status 11. A second
pass frees map ownership, clears any remaining joiner-session KQ handles and
deletes that Handle from the scheduler arrays. The runtime now mirrors that
two-pass boundary; Status 7/9/10 are deliberately left as raw numeric states
rather than being assigned guessed names.


## Character DB prison state and native JOIN_CANCEL

The original NA2016 Character DB backup now supplies the missing provenance for
the JOIN prison pre-check. `tCharacter.nPrisonMin` is returned by
`p_Char_GetAllData` and written by the original prison procedures using a
`smallint` minute value. WorldManager's stored
`PROTO_NC_CHAR_BASE_CMD::prisonmin` is the same value tested by
`fc_NC_KQ_JOIN_REQ`.

The emulator now carries that field as nullable `Character.PrisonMinutes`
and loads `characters.PrisonMin` in both character load paths. The original
table default expression is not yet independently decoded; therefore
`NULL` is deliberately an emulator-only "unknown source value" sentinel and
is not converted to zero. This removes the previous structural absence of
`prisonmin` without inventing new-character admission semantics.

Original `fc_NC_KQ_JOIN_CANCEL_REQ` also proves the ACK values:
`0x09A1` when `PlayerDisjoin` removes the session's current membership and
`0x09A2` when there is no current membership to remove. The request's Handle
is echoed in the ACK, but native `PlayerDisjoin` itself uses the
session-owned current KQ Handle.

That path is now live. A successful disjoin updates the combined membership,
broadcasts exact `NC_KQ_PLAYER_DISJOIN_CMD(Handle, CharacterNumber)` bytes
to every Zone, broadcasts the refreshed JOIN_LIST to remaining joiners, then
clears the session KQ Handle. Zone mirrors the original handler by finding the
KQ Handle and deleting CharacterNumber from its represented player list.


## Native membership identity boundary

The original World joiner buffer contains both client-visible character shape
and server-side identity. To prevent accidental reconstruction across separate
registries, `KingdomQuestMembershipRegistry` is now the authoritative
combined representation for lifecycle work. Each row owns:

```text
u32 CharacterNumber
u8  Level
u8  Class
Name5[20]
u8  TeamType
```

It can project the 23-byte client `KQ_JOIN_CHAR_INFO` and 5-byte
World-to-Zone `PROTO_NC_KQ_JOINER` without lookup. Participant-only
mutations invalidate nonempty server identity projections rather than guess a
CharacterNumber. Empty scheduler/SetJoining rosters remain authoritative empty
membership in all views.

PDB `CWMClientSession::GetCharRegNo` proves that native CharacterNumber is
the first DWORD `PROTO_NC_CHAR_BASE_CMD::chrregnum`. The original Character
server names the corresponding `PROTO_NC_CHAR_CHARDATA_REQ` value
`nCharNo`, and the original DB's `p_Char_Create` returns
`nCharNo = @@IDENTITY`. Independently, the emulator already writes
`Character.ID` into the first `PROTO_AVATARINFORMATION::chrregnum` field
for CharacterList/Create responses. This closes the identity correlation:
`KingdomQuestCharacterIdentity` uses the existing persisted
`Character.ID` as native CharacterNumber; no KQ-only remapping is created.


## Native JOIN_LIST result and cooldown closed

Original `CKQServer::Recv_NC_KQ_JOIN_LIST_REQ` resolves the remaining
JOIN_LIST response semantics. The request body remains `u32 Handle`, but
when the session's current KQ is Status 4 the server substitutes that
session-owned Handle before looking up the KQ buffer.

The exact ACK results are `0x3118` success, `0x3119` missing KQ buffer,
and `0x311A` cooldown. The supplied original `SingleData.shn` contains
`KQPlayerList_ResetListCoolTime = 5`, so the live handler enforces the same
five-second interval between successful JOIN_LIST replies. Only success
updates the per-session timestamp; error replies carry an empty roster.

This removes the old `KingdomQuestJoinListReplyRegistry` dependency from
the client handler. The participant payload remains the native
`KQ_JOIN_CHAR_INFO` projection of the source-owned membership state.


## Live JOIN admission for known prison state

With CharacterNumber/chrregnum and the combined membership owner correlated,
the original JOIN mutation sequence can now run without an ID guess.

For a character whose DB-backed `PrisonMin` is known, the client handler
reproduces `fc_NC_KQ_JOIN_REQ` ordering:

```text
read requested Handle
  -> prisonmin != 0                 => 0x0999
  -> already in requested Handle    => 0x099A
  -> PlayerDisjoin(current session KQ), return value ignored
  -> PlayerJoin(requested Handle)
  -> JOIN_ACK = 0x0991 + PlayerJoin result
```

`PlayerJoin` uses the source-proven Status/capacity/level/class/gender
checks, creates one `KingdomQuestMembershipEntry` from the live character's
existing native chrregnum/Character.ID, level, class, Name5 and initial
TeamType, then atomically projects that row into client JOIN_LIST, STATUS names
and World-to-Zone START joiners. On success the native JOIN_LIST broadcast is
sent before JOIN_ACK, matching the original call order.

The one remaining prison caveat is provenance, not gameplay inference:
`PrisonMin == NULL` means the original value/default was not supplied. Such a
JOIN is fail-closed with no mutation and no fabricated Fiesta Error. Explicit
zero/nonzero source values follow the recovered native admission branches.


### START transport and disconnect boundary

The emulator performs a routing preflight that is internal plumbing rather
than new gameplay policy: a Status-3 KQ is not advanced unless its existing
session target resolves to a connected Zone that owns the same source-backed
MapID. This prevents World from entering Status 4 when the emulator has no
authoritative destination for the native START body.

If a joiner session disappears before the countdown expires, START remains at
Status 3 rather than fabricating a replacement session, party state or
CharacterNumber.

The native logout edge is now correlated. `CWMClientSession::Logout`
passes the session-owned KQ Handle to `CKQServer::InKQStatusRunning`.
That helper returns true only when the Handle exists and the KQ Status is
exactly 4. Logout calls `PlayerDisjoin(session)` only when the result is
false. The emulator now applies the same rule on both socket disconnect and
Zone-reported disconnect: pre-start/non-running memberships are removed using
the already-live native PlayerDisjoin sequence, while Status-4 membership is
deliberately retained for reconnect. Login restoration remains a separate persistence/write step, but its
validation is now fully bounded. Original `p_Char_GetKQMap` supplies
`nKQHandle`, `sKQMap`, KQ X/Y and the saved KQ date. Before
`JoinerInfoUpdateByLogin`, World calls
`CKQServer::IsExisted(nKQHandle, sKQMap, dKQDate)`.

`IsExisted` rejects `0xFFFFFFFF`, finds the live Handle, requires
`sKQMap` to match one of that definition's four native `MapName[12]`
slots and resolves the same map in the live map data. It then decodes the
packed `SHINE_DATETIME`, adds exactly ten minutes to `tm_min`, lets
`mktime` normalize it, and succeeds only while
`current_time < saved_time + 10 minutes`. There is no native lower-bound
or additional freshness policy.

`KingdomQuestReconnectRules` now models that exact packed-date/map/expiry
predicate. The emulator additionally requires the existing
`KingdomQuestSessionTarget` to carry the same native MapName; this is an
internal routing-authority gate, not new gameplay semantics.

The subsequent `JoinerInfoUpdateByLogin` does not recreate membership. It
finds the already-retained joiner row for the persisted Handle by Name5,
rebinds the new World session to that Handle and updates the live player/session
registration field. Persisting/loading the original Character-DB KQ fields and
invoking that rebind are still intentionally separate work; no handle-only
reconnect is activated.


## Original KQ script and regen corpus boundary

The supplied `Server.zip` is now locked as an independent runtime-source
provenance snapshot (SHA-256
`b83bf92c7193578a772fcebf4d8b7c8c2a9a642cf0d50d33506f77a75b0e211d`).
`docs/KINGDOM_QUEST_RUNTIME_SOURCE_MANIFEST.tsv` records only exact
case-sensitive source presence under `Server - Kopie/9Data/Shine`, including
path, SHA-256 and byte size for files that are actually present. Absence is
recorded as absence; no alternate filename is promoted to an alias.

Across the 57 exact `KingdomQuest.shn` rows there are 27 distinct
`ScriptLanguage` keys. The original Zone runtime does not equate
`ScriptLanguage` with a Lua filename.

The MAKE-relevant owner is `ScenarioBookShelf`, not `KQScriptManager`.
`ScenarioBookShelf::sbs_LoadScripts` opens
`../9Data/Shine/World/PineScript.txt`, reads the `PineScript` table's
`ScriptName` column, and calls `sbs_Read` for each row. `sbs_Read`
tries the exact paths in this order:

```text
../9Data/Shine/ScenarioBookShelf/<ScriptName>.ps
../9Data/Shine/LuaScript/<ScriptName>.lua
```

Only after the selected ScenarioBook successfully loads is its name inserted
into the ScenarioBookShelf lookup tree. The native MAKE branch tests
`ScenarioBookShelf::sbs_GetScenarioBook(PROTO_KQ_INFO.ScriptLanguage, ...)`;
a null lookup is the proven `0x098C` condition.

`KQScriptManager::kqsm_Load` is a separate system. It reads the
`DialogFile` table from the same `World/PineScript.txt` container and
loads ShineScript text paths under `World/<...>/Script` with a separate
fallback. Its 64-entry limit belongs to that manager and is **not** evidence
for the MAKE ScenarioBookShelf capacity.

Every one of the 27 ScriptLanguage keys used by this KQ snapshot is present in
that original script-source universe:

- 18 resolve to exact `LuaScript/<ScriptLanguage>.lua` entrypoints;
- 9 are the older PineScript form and resolve to exact
  `ScenarioBookShelf/<ScriptLanguage>.ps` files:
  `GordonMaster`, `Honeying`, `KQHBat1..5`, `UnderHall` and
  `UnderHall2`.

The nine PineScript-backed definitions were previously reported as "missing
Lua scripts". That interpretation was too narrow and is now removed. They are
source-present; what remains unimplemented is an emulator runtime equivalent
of the original script container/ScenarioBookShelf loader.

The same 57 definitions use 18 distinct `KingdomQuestMap.BaseMap` values.
Fifteen have an exact
`MobRegen/KingdomQuest/<BaseMap>.txt` source. Three do not:

```text
KDArena
KDMine
KDSpring
```

Those three maps do have substantial original KQ Lua trees in the archive,
but the original Zone loader now closes the static-regeneration boundary.
`KQRegenTable::kqrt_Load(char*)` at `0x004B26B0` first tries

```text
../9Data/Shine/MobRegen/KingdomQuest/%s.txt
```

and, only if that load fails, tries

```text
../9Data/Shine/MobRegen/Instant/%s.txt
```

The no-argument loader at `0x004B3220` likewise enumerates the
`KingdomQuest/*.txt` directory first and `Instant/*.txt` second.
The supplied archive contains exactly nine Instant regen basenames:
`AdlF`, `AdlFH`, `Leviathan`, `Siren`, `Tower01`,
`Tower02`, `Tower03`, `UrgDragon`, and `WarN`. None is
`KDArena`, `KDMine`, or `KDSpring`.

Therefore those three used KQ base maps have no static `KQRegenTable`
source in either original lookup directory. Their Lua trees are a separate
runtime source family and are **not** an implicit fallback in this loader.

CI now derives the 27 ScriptLanguage keys and 18 used BaseMap keys directly
from the provenance-locked SHN SQL, checks them against the runtime-source
manifest, locks the exact 18-Lua/9-Pine script backend split and the 15/3
static-regen presence boundary, and rejects any guessed replacement for absent
regen input. The original `World/PineScript.txt` SHA-256 is also locked.
This establishes **what original runtime source is present** without pretending
that source presence is the same thing as successful runtime script loading.


## Zone MAKE result branches recovered

The original Zone `WorldManagerSession::wms_NC_KQ_W2Z_MAKED_CMD`
now closes four native MAKE_ACK results:

```text
0x0981  success
0x0982  ERR_KINGDOMQUEST_MAKE_DUPLICATEHANDLE
0x0983  ERR_KINGDOMQUEST_MAKE_TOOMANYQUEST
0x098C  ERR_KINGDOMQUEST_MAKE_SCRIPTNOTFOUND
```

The ScriptLanguage branch copies the 32-byte
`PROTO_KQ_INFO.ScriptLanguage` field and calls
`ScenarioBookShelf::sbs_GetScenarioBook`. A null runtime lookup returns
`0x098C`. The original shelf is mixed-backend: `sbs_Read` tries
PineScript `.ps` first and Lua `.lua` second. Source-file/catalog presence
therefore still does not prove that the runtime lookup succeeded; the emulator
needs an equivalent loaded ScenarioBookShelf before it can emit `0x098C` or
success from that condition.

Zone now classifies duplicate Handles atomically inside
`KingdomQuestZoneRuntimeRegistry.TryMake` and returns the exact
`0x0982` ACK.

The native container-full boundary is now correlated as well. RTTI on the
global Zone KQ owner identifies
`KingdomQuest::KingdomQuestContainer : List<KQElement>`; its fixed-array
construction/destruction path walks exactly `0x12C` elements, i.e. **300
native KQ slots**. The MAKE branch that logs
`WorldManagerSession::wms_NC_KQ_W2Z_MAKED_CMD : Buffer full` returns
`0x0983`. The Zone registry therefore models the same 300-entry capacity and
can classify `NativeContainerFull`.

That classification is deliberately not yet converted into a live
`0x0983` ACK. In the original MAKE order, ScriptLanguage runtime lookup
occurs before the container-full test. Until the mixed Lua/Pine script
container lookup is represented, emitting `0x0983` from capacity alone
could invert the original error precedence. `0x098C` likewise remains gated
on that real runtime lookup rather than file-presence heuristics.


## Native Zone END and World SetDone direction

The previous emulator transport had `NC_KQ_Z2W_END_CMD (0x5810)`
available in the wrong direction. The original
`CParserZone::fc_NC_KQ_Z2W_END_CMD` at `0x0042AAC0` validates the
incoming native packet, reads its `u32 Handle`, and calls
`CKQServer::SetDone(Handle)`.

`SetDone` at `0x00454860` is exact and small:

```text
find Handle; if absent return 0
Status = 5
Send_NC_KQ_W2Z_DESTROY_CMD(Handle)
FreeMapLink(Handle)
FreeJoiner(scheduleIndex)
return 1
```

It does not delete the scheduler entry and does not send the skip-only
NOTIFY/JOINING_ALARM_END messages. Deletion remains owned by the recovered
`DelOldShceduleList` pass.

The internal transport now matches those directions: Zone owns the
`0x5810` sender, World owns the END receiver, and World responds with the
existing W2Z DESTROY broadcast after entering Status 5. `FreeJoiner` is
represented by clearing only currently live matching sessions'
`KingdomQuestHandle`; the combined KQ membership rows remain intact until
old-schedule deletion, matching the native joiner-buffer lifetime.
