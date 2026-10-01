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

## Historical first implementation slice — superseded

The block originally began by loading only the static KQ team/vote metadata.
That bootstrap slice is retained here for provenance, but its former
zero-list/definition-scheduler TODO is superseded: the exact KQ source corpus,
native definition projection, live schedule publication and delta-based
LIST_REFRESH path are implemented in the later sections below.

## Guardrails

1. KQ definitions/schedules come from project source data, not hard-coded
   recreation from description text.
2. Captured packet fields are reproduced only after byte-level correlation.
3. Temporary Zone allocation and transfer use the existing World->Zone
   architecture; no fixed port is inferred from captures.
4. Vote-kick behavior stays disabled until its native policy/error/mutation
   ordering is source-correlated; the KQ session state itself is now live.
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

`NC_KQ_JOIN_REQ (0x5805)` is now live with the recovered native
`0x0991..0x099A` result family and admission order. The original
`prisonmin` value is loaded from character storage; emulator rows whose
original value is unknown remain fail-closed rather than being treated as zero.
Cross-KQ replacement preserves the native precheck -> PlayerDisjoin ->
PlayerJoin ordering.

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
The response is now the native 40-byte ServerTime + struct tm body. Type 29 is
the live `LIST_ADD_ACK`; source-backed refresh deltas are emitted in the
recovered 53-entry batching threshold rather than as the historical empty stub.

STATUS_REQ/ACK is corrected to u32 Handle + u8 Status + u16 joinerCount +
20-byte Name5 entries.

See docs/KINGDOM_QUEST_PROTOCOL_NATIVE.md for the field table. JOIN,
JOIN_CANCEL, JOIN_LIST and TEAM_SELECT now use recovered native
admission/membership state. The remaining disabled Header-22 request family is
vote policy, whose success/failure mutation ordering is handled separately.

## Native KQ definition wire model

The client-visible PROTO_KQ_INFO_CLIENT structure is now represented explicitly
as KingdomQuestClientInfo with its native 141-byte layout. The full
PROTO_KQ_INFO server/session structure is represented as KingdomQuestProtocolInfo
with the native 377-byte layout.

The model preserves the original field boundaries: Handle/Status/Joiner count,
ID/title/timers, level/player limits, repeat/revival gates, demand quest/item/
class/gender, server scheduling fields, four 26-byte map links, script language/
init strings and two team regeneration coordinates.

This model does not map KingdomQuest.shn columns by assumption. The later
source-projection layer fills it only from the provenance-locked KQ tables, and
the live scheduler publishes those definitions. LIST_REFRESH now compares the
visible Status-0..4 snapshot per session and emits native LIST_ADD/UPDATE/DELETE
deltas rather than the historical empty LIST_ADD stub.

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

The LIST_REFRESH handler serializes the snapshot held by this registry through
the native delta serializers. The source-backed scheduler now supplies the live
entries; an actually empty registry still serializes as the native empty list
without any handler-side hard-coded definition data.

This keeps source ingestion, scheduling and protocol serialization separate
without schedule guesses.


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
zeroes those fields when no KQTeam row exists. Team division is resolved in
the later runtime layer: PDB fixes `KQTD_RANDOM=1` and
`KQTD_USERSELECT=2`, with RANDOM division executed at Status-3 expiry and
the USERSELECT start gate modeled separately. This scheduler projection still
does not perform either runtime mutation.

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

This closes list-selection/refresh semantics. JOIN admission, scheduler
publication, status transitions and map lifecycle are now implemented by their
separate source-backed runtime owners described below; the list/refresh layer
does not duplicate those decisions.


## Native JOIN admission and MAKE result closed

Original WorldManager code now closes the previously blocking JOIN_ACK value.
`CParserClient::fc_NC_KQ_JOIN_REQ` at `.text+0x12CC0` emits
`0x0991 + CKQServer::PlayerJoin(...)`; PlayerJoin returns zero on success.
The resulting exact ACK range is 0x0991 success, then 0x0992 handle/BF,
0x0993 capacity, 0x0994 status, 0x0995 level, 0x0996 class, 0x0997 gender,
0x0998 unexpected fallback, 0x0999 nonzero `prisonmin`, and 0x099A
already joined to that Handle.

The emulator already serializes Character.Job and LookInfo.Male into the same
original shape bits used by PlayerJoin. The original
`PROTO_NC_CHAR_BASE_CMD::prisonmin` source is now correlated to
`tCharacter.nPrisonMin` and loaded as nullable provenance state. JOIN is live
when that original value is known, and remains fail-closed for an emulator row
whose source value is unknown. Cross-KQ JOIN performs the recovered
PlayerDisjoin-before-PlayerJoin sequence instead of defaulting absent state to
zero.

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

The recovered `CKQServer::AddNewScheduleList` ownership is live and remains
cleanly separated from the later map-allocation lifecycle.
`KingdomQuestScheduleRuntime`
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

As a result, LIST/SCHEDULE/STATUS have a real source-backed scheduler owner
instead of an external registry feeder. The later sections supersede the old
map-routing TODO: `AllocMapLink`, dynamic MapName/MapBase routing, MAKE and
the source-backed start gate are now live.


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

The separate USERSELECT request path is now recovered directly from
`CKQServer::Recv_NC_KQ_TEAM_SELECT_REQ`. It uses errors `0x31F0..0x31F7`,
requires Status 2 and KQTD_USERSELECT=2, rejects same-team requests, applies
the strict `newTarget < floor(MaxPlayers/2)` gate and then the directional
`newTarget-newOld <= MaxMemberGap` gate. Success changes the combined
membership TeamType atomically, ACKs the requester, then sends
`NC_KQ_TEAM_SELECT_CMD` to other represented KQ sessions. An out-of-range
request team is fail-closed because the original indexes a two-element team
counter without a safe validation branch.

All eight supplied KQTeam rows remain KQTD_RANDOM=1 / MaxMemberGap=1, so this
generic USERSELECT success mutation is currently unreachable from the supplied
content; those definitions return the proven wrong-divide-type `0x31F7`.

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
deliberately retained for reconnect. Login restoration is now wired through
the persisted KQ suffix and exact retained-joiner rebind, while its validation
remains bounded by the original predicate. Original `p_Char_GetKQMap` supplies
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

Character.exe closes the inverse conversion. Its `p_Char_GetKQMap` reader
binds SQL `dKQDate` as a timestamp and packs the outgoing 32-bit field as:

```text
(year & 0x0F)
| ((month  << 4)  & 0x000000F0)
| ((day    << 8)  & 0x00001F00)
| ((hour   << 13) & 0x0003E000)
| ((minute << 18) & 0x00FC0000)
| ((second << 24) & 0x3F000000)
```

The year really is only four bits. WorldManager reconstructs it as
`2000 + nibble`, so the original format wraps after 2015. This native
limitation remains explicit instead of receiving an emulator-only epoch fix.

`KingdomQuestReconnectRules` now models both this exact Character-side
packing and the World-side packed-date/map/expiry predicate. The emulator additionally requires the existing
`KingdomQuestSessionTarget` to carry the same native MapName; this is an
internal routing-authority gate, not new gameplay semantics.

The subsequent `JoinerInfoUpdateByLogin` does not recreate membership.
`GetJoinerIndex` compares the entire Name5 buffer as five DWORDs (20 bytes),
then the rebind writes the retained Handle into the new World session and
updates only the live session-registration field in that existing joiner row.

The emulator now activates the representable part of that path:
persisted Handle/Map/date must pass `IsExisted`, an already-retained
membership row must match the character's exact 20-byte ASCII Name5, and the
existing session target must still own the same dynamic MapName. Only then is
the World session rebound. The saved KQ X/Y and explicit internal
MapID/Map.InstanceID are carried over the emulator's World->Zone transfer so
Zone can enter the dynamic instance without overwriting the separately stored
normal return location. A persisted Handle alone can never recreate a joiner.

WorldManager.pdb and the PlayerJoin write sequence correct the earlier
single-DWORD interpretation of the native joiner tail. `KQ_JOINER_BF`
contains three distinct vote fields after `CharInfo`:
`bInVote` is a DWORD at struct offset `+0x20`, `bBan` is a DWORD at
`+0x24`, and `nVotingCount` is a byte at `+0x28`. PlayerJoin
initializes all three independently to zero.

Native `CheckCharBannedInLogin` tests **bBan at +0x24** exactly against
value `1`; it does not test the +0x20 in-vote field. The combined membership
owner therefore preserves `InVoteRaw`, `BanRaw` and `VotingCount`
separately and clones all three through team/start mutations. The raw DWORDs
stay raw because later original vote paths use both exact-value and nonzero
tests rather than one generic managed boolean.

The vote-success/result/disjoin ordering is recovered separately below before
those fields are used to drive live policy.

The core vote bookkeeping is source-modeled in
`KingdomQuestVoteCoordinator`. It preserves the exact inactive
`KQ_VOTE_INFO` state, start/voting error families, same-team voter
eligibility, initial Cancel reservation, YES/NO byte arithmetic, majority-rate
clamping by target `nVotingCount`, integer YES percentage, result audience,
target vote-count increment and success `bBan=1`.

The client transport is now live as well. Direct WorldManager disassembly
closes the request bodies and observable ordering: VOTE_START reads fixed
`Name5 Target[20] + u8 VoteType + u8 Len + bytes[Len]`, sends its ACK
before VOTE_VOTING_CMD, and uses the exact SingleData deadlines below.
VOTE_VOTING consumes the 32-bit `KQ_VOTING_TYPE` value and returns the
recovered 0x3110..0x3113 ACK family. VOTE_START_CHECK sends no reply for an
invalid/non-running session; a valid running session receives 0x3120, 0x3121
for an active vote, or 0x3122 while its suggest cooldown is still active.

The one-second KQ runtime now executes the recovered VoteProcessing order.
Already-banned live joiners receive `LINK_TO_FORCE_BY_BAN` with the current
four native MapName fields **before** expired votes are resolved. Expiry then
broadcasts RESULT_SUC/RESULT_FAIL to the same-team audience; success sends
VOTE_BAN_MSG to the target after the result and sets `bBan=1`, so that
target first receives the force-link on the following processing tick.
PlayerDisjoin also reproduces the native target-only cancellation edge: when
the leaving joiner is the active, not-yet-banned vote target it clears all
current `bInVote` values, sends VOTE_CANCEL_CMD to every other live joiner,
clears KQ_VOTE_INFO, and only then performs normal membership removal. A
starter-only disjoin does not cancel the vote.

The final login-ban edge is closed as well. Native
`CheckCharBannedInLogin` at WorldManager address `0x00454EE0` resolves
the retained joiner and returns true only when its `bBan` DWORD is
**exactly 1**. The character-login caller first runs
`JoinerInfoUpdateByLogin`, then on that result sets CWMClientSession field
`+0x1DF74=1` and calls `PlayerDisjoin`; the saved KQ coordinates are not
restored on that branch. A later login-continuation check sends the empty
`VOTE_BAN_MSG_LOGOFF (0x5830)` when that field equals 1 and immediately
clears it.

The emulator mirrors that deferred ordering. Reconnect rebinds the retained
Handle, tests `BanRaw == 1`, suppresses KQ-position restoration, sets an
explicit pending-logoff session flag, executes the same PlayerDisjoin path,
then emits 0x5830 and clears the flag. The native session deadline at
`+0x1DF70` is initialized on character login with
`KQVote_LoginCoolTime=300`; successful vote suggestions later overwrite
that same deadline with the separately sourced 300-second SuggestCoolTime.

The previously unresolved vote timings are now closed by the original
`SingleData.shn` from the supplied Server.zip. The exact file is SHA-256
`8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88`
and decodes to **41 rows / 2 columns**, with 36-byte default records:
`SingleDataIDX[32]` plus `u16 SingleDataValue`. The KQ-relevant rows are
`KQVote_VoteLimitTime=60`, `KQVote_SuggestCoolTime=300`,
`KQVote_LoginCoolTime=300` and
`KQPlayerList_ResetListCoolTime=5`. WorldManager accesses these through
`CSingleDataMap::GetValue`; VoteLimit is the active vote duration and
SuggestCool is the starter cooldown. The full 41-row snapshot is locked in
`docs/KINGDOM_QUEST_SINGLEDATA_SOURCE.tsv`, so these durations no longer
block live vote transport.



### Native character save-location persistence boundary

The original Zone PDB names `PROTO_NC_CHARSAVE_LOCATION_CMD` fields as
`chrregnum`, `coord`, `kqhandle`, `map_kq` and `coord_kq`.
`ShinePlayer::so_SaveLocation` writes a **48-byte** structure with these exact
offsets:

```text
0x00  u32 chrregnum
0x04  coord: char MapName[12] + i32 X + i32 Y
0x18  u32 kqhandle
0x1C  char map_kq[12]
0x28  coord_kq: i32 X + i32 Y
0x30  end
```

The corresponding original Character DB procedure `p_Char_SaveLocation`
receives the KQ handle/map/X/Y alongside the normal login location and writes
`nKQHandle`, `sKQMap`, `nKQX`, `nKQY`, while setting
`dKQDate=GetDate()`. `p_Char_GetKQMap` later returns that state for the
World login reconnect path.

`CharacterSaveLocationProtocolInfo` models this wire boundary byte-for-byte.
The KQ suffix is now also persisted live. Direct Zone disassembly shows that
after the normal-coordinate selection completes, `so_SaveLocation` always
writes `FieldMap::fm_GetKQhandle()`, the current FieldMap name and current
player X/Y into the KQ half of the 48-byte structure. `FieldMap` construction
initializes that handle to `0xFFFFFFFF`, so non-KQ maps have the native
no-KQ value; represented KQ instances save their exact dynamic `MapName`
rather than the emulator's base-map short name.

The supplied Character DB procedure proves the storage boundary:
`p_Char_SaveLocation` receives `nKQHandle int`,
`sKQMap nvarchar(16)`, `nKQX/nKQY int`, writes those four columns, and
sets `dKQDate=GetDate()`. The emulator schema/storage/Zone save path now
mirrors that suffix and uses the database timestamp.

The first/normal `coord` remains deliberately on the emulator's existing
save policy. Native `so_SaveLocation` selects it through additional
rollback-event, guild-tournament, regen-city-link and other special-map
branches. Those branches are not replaced with a guessed "return map" merely
to make reconnect appear complete.

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

Direct Zone.exe disassembly closes an important loader detail. Once either
source file exists, `sbs_Read` allocates the corresponding ScenarioBook and
calls its virtual `sb_Load(ScriptName)`, but it does **not** test that boolean
return. It immediately inserts the key into the shelf index and the object into
the shelf vector. Only absence of both the `.ps` and `.lua` file makes
`sbs_Read` return false. The native MAKE branch then tests
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
Lua scripts". That interpretation was too narrow and is now removed. The new
`KINGDOM_QUEST_SCENARIOBOOK_SOURCE.tsv` additionally locks all **32** KQ/*
rows from the original PineScript catalog: 9 PineScript + 23 Lua, with an exact
source file for every row. The 27 ScriptLanguage values used by the supplied
KQ definitions are all members of that proven shelf projection.

The emulator can therefore reproduce the MAKE-time shelf membership test
without pretending to execute a script. What remains unimplemented is the
later ScenarioBook/PineScript/Lua **execution** path driven by
`CinemaComplex::cc_PlayFilm`, not the source-backed MAKE lookup itself.

The Lua execution scope is now narrowed to the supplied KQ definitions rather
than the whole shelf. `KingdomQuestLuaScenarioScope` contains exactly the
**18** Lua ScriptLanguage keys referenced by `KingdomQuest.shn`, with each
original `LuaScript/<key>.lua` archive path and SHA-256 locked against
`KINGDOM_QUEST_SCENARIOBOOK_SOURCE.tsv`. The other five Lua books present in
the 23-row shelf are not activated merely because they exist. The accompanying
`IKingdomQuestLuaScenarioHost` is intentionally an execution boundary only:
it does not substitute the community Lua function catalog and it does not map
`ScriptInitValue` to an inferred Lua entry function. Exact original API
registration / entry semantics remain required before live Lua execution.

The first execution-side source layer is now represented for all **nine**
Pine-backed KQs used by the supplied definitions. The original `.ps` bytes
were reduced only by removing comments/blank lines and normalizing executable
text to ASCII/LF; command text, expressions and ordering are unchanged. The
canonical executable bundle is SHA-256
`390c0e948eb035aee62078327cb63d81ddef54aa9e28c38d642d02c86a9f9e57`.

`KingdomQuestPineScriptSource` validates per-script hashes and parses the
whole used Pine corpus into named blocks plus explicit Command / If / Infinite /
named Scope nodes. Both source syntaxes found in the originals are preserved:
separate `if ...` + `then open`, and GordonMaster's inline
`if ... then` + `open`. All nine sources parse with their exact locked
block/node counts.

The next execution layer is now partially source-modeled from direct Zone.exe/
PDB recovery. `KingdomQuestPineControlRuntime` reproduces the native
32-frame ProcessStack plus Block / IF / INFINITE / CALL / BREAK behavior,
including the exact `0x270F` block-exit index used by BREAK.

The used corpus now also preserves Pine's variable statements instead of
misclassifying their physical source lines as gameplay commands. The nine
scripts contain exactly **15 native var statements / 98 declarations** and
**48 assignments**. Zone.exe `VariableStack::vs_Push` at `0x004D6A90`
admits at most **0x7F = 127 entries**, stores a 0x100-byte identifier token
next to a 0x100-byte value token (0x200-byte entry stride), while
`vs_FindVariable` at `0x004D69F0` scans newest-to-oldest. Duplicate names
therefore shadow older values. `StateVarDeclear::sa_Step` at `0x004DA040`
per declaration performs identifier -> push -> initializer calculation, and
`StateAssignment::sa_Step` at `0x004DB110` resolves the LHS, finds the
existing newest variable, calculates the RHS directly into that value token,
then pops. `KingdomQuestPineVariableStack` and the control runtime now model
that ordering without rollback invention.

The host-free expression subset needed by those statements is executable as
well. Native `String::sa_Load` at `0x004DB330` removes surrounding
quotation marks through `PineScriptToken::pst_RemoveQuatator` at
`0x004D6310`. `Number::sa_Calculate` and `String::sa_Calculate` then
share `0x004D6710` and copy the resulting 0x100-byte stored token unchanged.
Quoted source operands such as `"ResultList"` therefore reach runtime
system functions as `ResultList`, not as a token containing quote bytes.
Simple identifiers copy the resolved VariableStack value. The native `PineScriptToken::operator+` /
`operator-` paths at `0x004D7390/0x004D74B0` parse the trailing decimal
suffix, preserve any left-hand prefix, perform 32-bit arithmetic and append
the result through native `"%d"` formatting. This closes all **98**
declaration initializers plus literal/direct-copy/simple +/- assignments in the
used KQ Pine corpus. The first stateful system-function boundary is now recovered as well. The
used Pine corpus contains exactly one `@Random(0 99)`, in GordonMaster.
`SysFuncRand::sfb_Calculate` at `0x004D77D0` evaluates the two operands,
calls the linked MSVC CRT `rand()` at `0x006594B2`, and computes
`min + rand() % (max-min+1)`; the upper bound is inclusive. The linked
`rand()` transition is the same `state*0x343FD+0x269EC3` / 15-bit output
already recovered for CardDeck. That math now lives in the common
`MsvcCrtRand` primitive used by the World CardDeck model and available to
Zone Pine. Neither layer chooses a seed or claims ownership of the native
**thread-local** Zone CRT stream; authoritative native-thread assignment and
per-thread consumer ordering remain explicitly unresolved before live
`@Random` or reward CardDeck execution.

The two used GordonMaster `@DistanceBetween(...)` expressions are now
recovered without identifying native ShineObject handles with emulator object
IDs. `SysFuncShineDistance::sfb_Calculate` at `0x004E5F40` evaluates both
operands, applies `pst_GetNumber`, resolves each through
`ShineObjectManager::som_GetObject` at `0x0054FD10`, subtracts X/Y and calls
`DirectDistanceTable::ddt_Distance` at `0x004012D0`.
`ddt_Distance` repeatedly halves both signed deltas toward zero whenever
either lies outside **[-1024,1024]**, doubling a scale each time. Its
2049x2049 cache was built by `ddt_Initialize` at `0x0045FC80` as
`floor(sqrt(dx²+dy²))`; the final native result is that integer times the
scale. `KingdomQuestPineNativeDistance` reproduces this result directly.
`KingdomQuestPineDistanceExpression` keeps the native handle→coordinate
lookup behind an explicit resolver, so no `MapObjectID` equivalence is
invented.

Pine IF comparison syntax is now closed from the original parser rather than
mapped from C-like intuition. `CompareOperator::sa_Load` and its three parser
helpers at `0x004DB1B0/0x004DAA50/0x004DABB0/0x004DAC50` assign exactly
eight native modes. `Condition::sa_Calculate` at `0x004D87A0` uses
`pst_GetNumber` for modes 1..6: `==`, `!=`, `<`, `>`, `<=`,
`>=`. The Fiesta-specific `===` and `=!=` syntax maps to modes 7/8 and
compares complete NUL-terminated Pine token text. The supplied nine scripts
contain **26 IFs**: 11 token `===`, 5 numeric `<`, 4 numeric `==`, 3
numeric `>`, and 3 token `=!=`. `KingdomQuestPineConditionExpression`
models all eight modes, and the control runtime now evaluates recovered
condition syntax itself after calculating both operands; only unknown syntax
still falls through to the explicit host boundary.

The original `@CharName` system function is now source-modeled too.
`SysFuncShineCharName::sfb_Calculate` at `0x004E45A0` evaluates its
single operand, takes the low **u16** of `pst_GetNumber`, resolves that native
handle through `som_GetObject` at `0x0054FD10`, then calls the virtual
`ShineObject::so_CharName()` identified by Zone.pdb at vtable offset
`+0x56C`. The returned `TName5` is copied as **20 payload bytes + NUL**.
A missing native object yields an empty Pine token, not a calculation failure.
The supplied Pine corpus uses this exact function **10 times**: five
`PlayerHandle` and five `LooterHandle` calls.
`KingdomQuestPineCharNameExpression` preserves that boundary behind an
explicit native-handle→name resolver; it does not equate the handle with an
emulator `MapObjectID`.

The three source-proven system-function families now compose directly into
`KingdomQuestPineControlRuntime` through
`KingdomQuestPineUsedExpressionRuntime`: the exact used `@Random`,
`@DistanceBetween` and `@CharName` forms are attempted before the
generic expression host fallback. Their native dependencies remain explicit:
the caller supplies the appropriate native-thread `MsvcCrtRand` state and the
native-object coordinate/name resolvers. A recognized used expression with a missing
dependency is an invalid calculation and faults fail-closed; it is not silently
reinterpreted by the generic host. This closes the runtime composition boundary
without inventing native-thread CRT ownership or a native-handle-to-emulator-ID
mapping.

The canonical Pine bundle is now audited into an exact **44-verb**
command inventory. The already-recovered one-step families are composed before
the generic command host by `KingdomQuestPineUsedCommandRuntime`:
`regengroup` (243), `interruptset` (225),
`interruptclear` (65), `endofkq` (19),
`interrupterase` (14), `timelimit` (14) and
`questresult` (9) plus `scriptfile` (19), for **608 source command occurrences**. Their actual
map/packet/title/scenario mutation remains behind explicit tick, regen-source
and effect-sink interfaces; the dispatcher itself owns no wall clock, map
manager, packet send or database mutation. A recognized command with a missing
dependency faults fail-closed instead of falling through to guessed semantics.

`KingdomQuestPineLocalCommandState` now supplies the concrete local owner for
the one-step state that is already fully native-correlated: the current
`Movie::TimeLimit` plus the fixed **20-entry** interrupt manager. A later
`timelimit` replaces the current time-limit state; `interruptset` obeys
the recovered capacity; `interrupterase` performs the exact 16-byte-name
erase pass across all matches; and `interruptclear` empties the active list.
The external effects of `regengroup`, `questresult` and `endofkq`
are still delegated through a separate sink rather than being approximated by
this local state owner.

All **202** used `pause` commands now execute directly in
`KingdomQuestPineControlRuntime` as well. The first step builds the recovered
10-Hz native deadline from the explicit tick source, later steps remain active
while `deadline >= currentTick`, and the command frame pops only when
`deadline < currentTick`. The **55** `waitinterrupt` commands are now
handled as a shared multi-step control primitive: all 55 use exactly
`waitinterrupt InterruptBlock "InterruptArg".` followed immediately by
`call InterruptBlock.`. `KingdomQuestPineWaitInterrupt` validates that
shape, uses the same active `KingdomQuestPineInterruptRegistryState` as its
producer; the selected entry may already have been removed by native
BlastCheck. It copies only that plan's
source-backed ActionBlock and the provider's opaque argument into the two Pine
variables, then lets the following dynamic CALL resolve the chosen block. The
provider still owns native BlastCheck/event selection and ordering.

Other system functions and dynamic `#(...)` identifiers still fall through
to the fail-closed host rather than being guessed. The exact 44-verb source
inventory is now guarded more strictly as well: after the eight executable
one-step families, the separately recovered `call`/`break`/`pause`
control paths, and the shared `waitlogin`/`waitinterrupt` handoffs, the
remaining **31 source-used command verbs** are recognized explicitly as
source-used-but-unrecovered. They return an invalid/fail-closed resolution
before the generic command host can reinterpret them. This includes
broadcast/chat UI, summon/mob, door, item, NPC and link actions. Native
side-effect recovery can enable those families one at a time without allowing
guessed fallback semantics.

The exact unresolved shape is locked per script as well. After removing the
shared wait primitives, `UnderHall` needs **6** unresolved verb families
(`broadcast/linkto/mobregen/questmobkill/reward/summonmob`);
`UnderHall2` uses those same core families plus `chatwin`, for **7**.
The five Warrior's Code scripts `KQHBat1..5` share **11** unresolved
families. Their source layer is now unified: all **205/205** external command
occurrences (**41 per script**) are locked by script, canonical line, top-level
block, verb and exact command text in
`KINGDOM_QUEST_KQHBAT_EXTERNAL_SOURCE.tsv`. The five scripts share the same
line/block skeleton; only their two return `linkto` sites vary by stage
(Eld, Urg, Urg_Alruin or Adl).
`KingdomQuestKQHBatCommandRuntime` intercepts those 11 families for all five
scripts before the generic unrecovered-verb guard, rebuilds an immutable exact
source plan and crosses only an explicit KQHBat owner sink. Missing owners or
changed source text remain Invalid/fail-closed.

Zone.exe/PDB recovery now closes **60/205 occurrences across seven verb
families** before that live owner. `battlestart PK` / `battlestop PK`
write the native map PK state byte to 1/0 and broadcast
`so_SendProtocol` Header **6**, Type **18/19** over the current map.
`sendquestresult Suc/Fail PlayerHandle` evaluates the Pine handle, resolves it
through `ShineObjectManager::som_GetObject`, then sends only Header **22**,
Type **18/19** to that object; unlike `questresult`, it performs no KQ title
hook and no map clear. `invidualreward` resolves the same native handle,
calculates its reward-index expression, and calls virtual
`so_ply_KQRewardIndex(char*)` at vtable **+0x770**. The previously unresolved
Pine `%` operator is now recovered as `PineScriptToken::pst_Merge`
(`0x004D63E0`): it concatenates token text, so the two Warrior's Code forms
produce the actual `HERO_<stage>_<rank>` IndexString. Finally,
`revival all` traverses the current map, accepts only native Player object
type **2**, and invokes `ShinePlayer::sp_ReviveReqProcess` for each selected
player. These paths are represented as typed immutable native plans; native
ShineObject handles remain opaque and are never mapped to emulator MapObjectID.

Two more shared source families are now native-closed. The exact Warrior's Code
`abstateset all "StaCommonStun01" 1 3000 LooterHandle` resolves the original
AbState source identity **ID 8500 / native ABSTATEINDEX 306**, builds
`AxialListPlayerStateSet` with strength **1** and **3000 ms**, resolves the
opaque `LooterHandle`, and runs that player-state iterator on the handle's
current map. The ten `linkto all` sites preserve the literal native
`LinkInformTemplete` inputs (server/client map token plus X/Y), construct
`AxialListLinkTo`, and traverse the current theater map; each visited object
receives the native link virtual at **vtable +0x3FC**. No emulator
`ChangeMap` shortcut is substituted.

The item families are now native-closed too, bringing Warrior's Code to
**85/205 occurrences across nine verb families**. The original ItemInfo source
(SHA-256
`7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494`)
fixes `KQ_InvincibleHammer` to ItemID **57000** and `KQ_Ice01` to
ItemID **57001**. The Pine identifier `Maul` is not treated as an item name:
the exact runtime variable must resolve to `KQ_InvincibleHammer`.

`itemerase all` is recovered as
`ShineItemErase::sa_Step` (**0x004F0570**) ->
`AxialListItemErase4All` ctor (**0x004285A0**) ->
`so_AllInMap` -> `ali_Work` (**0x00429750**), which calls each visited
object's vtable **+0x8B8** with `(ItemID, -1)`. All 20 KQHBat erase
occurrences use only this all-map branch.

The five `itemdrop InterruptArg Maul 1000000.` sites are recovered through
`ShineItemDrop::sa_Step` (**0x004EFF00**). The first operand remains an
opaque Pine/ShineObject reference; native takes its `WhoIsKillMe` handle,
resolves the killer through `som_GetObject`, preserves the killer Character
Registration Number, gates the drop with WELL512 against **1,000,000**, clears
and creates the ITI registration number, dispatches
`ItemAttributeClass::iac_DropItemMake` through vtable **+0x2C** with a
separate WELL512(1000) sample, applies the optional RandomOptionTable path,
builds `ShineMultiTypeHandle` for the killer and finally calls
`ShineObject::so_IsDropping` (**0x004B0A20**). The source-object virtual
slot **+0x708** is retained as opaque native metadata rather than assigned a
guessed gameplay name. No native handle is mapped to emulator MapObjectID and
no legacy emulator inventory/drop pipeline is substituted.

The remaining **120/205 occurrences across only two families**
(`broadcast/chatwin`) stay on the explicit unresolved owner path until their
native text/UI boundaries are equally proven. `Honeying` currently needs
**13** families and `GordonMaster` **23**. This per-script matrix is derived directly from the
hash-locked canonical Pine bundle and is CI-guarded, so implementation can
close the smallest source-equivalent slice first without hiding broader gaps.

The complete UnderHall source slice is source-locked as well.
`KingdomQuestUnderHallCommandRuntime` now owns **only the six remaining
external families / 25 source occurrences / 19 distinct source forms**.
`scriptfile` is owned by the generic one-step runtime, and the shared control
runtime owns `waitlogin` / `waitinterrupt` for the complete nine-script
corpus. The obsolete UnderHall-only wait-delivery path has been removed, so a
fired interrupt cannot accidentally be rejected by the old, native-invalid
"must still be registered" check after BlastCheck removal. The six external
families are parsed through `KingdomQuestUnderHallCommandSource` and
forwarded only as validated, source-routed plans to
`IKingdomQuestUnderHallCommandSink`. A changed UnderHall form or missing
native side-effect owner remains Invalid/fail-closed before the generic host.

UnderHall's interrupt dataflow is now a specialization of that shared
primitive. It declares `InterruptBlock=""` and `InterruptArg=""`, contains
**19** of the 55 exact wait/call pairs, and its 61 `interruptset`
registrations use only **HPLow=5, PlayerEliminate=19, Sec=18, TimeOut=19**;
every final ActionBlock resolves to a real UnderHall top-level block. The
generic delivery handoff is tied to the same
`KingdomQuestPineInterruptRegistryState` owned by the local Pine command
state. Direct native recovery corrects an earlier guard here: a firing
`sib_BlastCheck` decrements RepeatCount and may remove its own list entry
**before** returning the selected ActionBlock, so `waitinterrupt` must not
require the selected plan to remain registered after delivery.

The complete UnderHall manager/predicate behavior is now source-modeled from
the original Zone.exe/PDB:

- `ScriptInterruptManager::sim_Alloc` (`0x00509B10`) allocates through
  `List::l_AllocZ` (`0x005D08C0`), so registrations append at the Z/tail;
- `sim_InterruptBlast` (`0x0050C100`) walks from the A/head side and returns
  on the **first** `sib_BlastCheck` that fires: first registered due entry
  wins;
- `ScriptInterruptInterval::sib_BlastCheck` (`0x0050AB90`) advances the
  next deadline from the prior deadline before RepeatCount decrement/removal;
- `ScriptInterruptHPLow::sib_BlastCheck` (`0x0050ACA0`) resolves the
  **runtime object handle** stored at registration, fires immediately when the
  object no longer exists, otherwise caches MaxHP and fires inclusively when
  `currentHP * 1000 / maxHP <= threshold`;
- `ScriptInterruptPlayerEleminate::sib_BlastCheck` (`0x0050A860`) scans
  the current map with `AxialListMobCounter::ala_SearchPly` and fires exactly
  when its qualifying-player counter is zero.

`ShineInterruptSet::sa_Step` routes HPLow through the helper using
`si_HPLow`: it evaluates the first source operand at registration and stores
its low WORD as the runtime ShineObject handle, evaluates the second operand as
the DWORD threshold, and initializes cached MaxHP to zero. Therefore the five
UnderHall source forms still correlate to MobInfo **1068 / KQ_BossRobo** as
the spawned type, but **1068 is not substituted for the runtime handle**.
`KingdomQuestPineInterruptRuntimeEntry` snapshots the Pine variable value at
`interruptset` time and keeps mutable RepeatCount, interval deadline and
HPLow MaxHP cache. All 61 UnderHall registrations have exact `RepeatCount=1`.

The remaining event-side live dependency is now split into two explicit native
providers: ShineObject-handle -> HP/MaxHP resolution and the
AxialListMobCounter::ala_SearchPly qualifying-player count. The runtime composes
those providers without mapping a native handle to an emulator MapObjectID or
inventing the still-unrecovered player qualification filter; comparison,
selection order and manager mutation are no longer unresolved.


`InterruptArg` itself is no longer a control-flow blocker for this supplied
script: the canonical source contains exactly one declaration and the 19
`waitinterrupt` write sites, with **no consumer anywhere else in UnderHall**.
The native provider still supplies/writes the opaque value, but no UnderHall
branch or command reads it.

UnderHall's one `waitlogin Wait.` is source-locked in its surrounding
control flow: `KQ_BossRobo 0.` precedes it and the next condition is exactly
`if Wait == 0 -> call "QuestFail".`. The same source pattern is present in
all **9/9** supplied Pine KQs: each has exactly one
`waitlogin <identifier>.` and immediately tests that variable against zero.
`KingdomQuestPineWaitLogin` and the control runtime now resolve this common
target-variable/frame-state handoff generically. The injected
`IKingdomQuestPineWaitLoginSource` still owns the native value, wake predicate
and timing, so no player-count interpretation is invented.

The remaining six non-`waitinterrupt`/non-`waitlogin` UnderHall families
are source-resolved one step further before any gameplay owner can see them.
`KingdomQuestUnderHallExternalPlanBuilder` converts the **25 occurrences /
19 distinct forms** into typed immutable plans and correlates the checked-in
MapInfo/MobInfo identities where source permits it: the Elderine target is
MapID 9 at exact source coordinates 17214/13445, and mob/summon identifiers
resolve through the 11-row UnderHall MobInfo catalog. Numeric operands whose
native meaning is not independently recovered remain explicitly raw rather
than being renamed as guessed gameplay fields. The external sink receives only
these resolved plans; it no longer interprets raw Pine strings.

The later interrupt recovery also closes an important identity distinction in
those plans. In `mobregen KQ_BossRobo "KQ_BossRobo" ...` and every
`summonmob KQ_BossRobo "<mob>" <count>`, the unquoted
`KQ_BossRobo` operand is preserved as the Pine **runtime-handle variable**.
The quoted token is the MobInfo identity. `KingdomQuestUnderHallExternalPlan`
therefore carries `RuntimeHandleIdentifier` separately and the command-state
dispatcher resolves that identifier to the exact Pine token before invoking
the external owner. MobInfo ID 1068 is never substituted for the runtime
ShineObject handle captured later by HPLow.

- `linkto`: only canonical QuestSuc/QuestFail lines **388/402**, target
  `all`, Elderine MapID **9**, aliases `Eld`/`Eld`, coordinates
  **17214/13445**;
- `mobregen`: only canonical `Nineteenth` line **339**,
  `KQ_BossRobo` / MobID **1068**, coordinates **2300/2500**, raw numeric
  operands **90/1000** and raw text `Normal`;
- `summonmob`: only the exact **12** canonical Summon1..5 sites, with each
  source MobInfo identity and count locked to that line.

For mobregen/summonmob the resolved VariableStack value is copied as the full
**0x100-byte PineScriptToken** into the immutable owner plan; it is not parsed
as an emulator MapObjectID. MobInfo and MobInfoServer must also agree on the
numeric MobID before the plan crosses the boundary. Thus all six UnderHall
external families now reach external code only through fixed plans. This does
**not** claim native `linkto`, mob creation, spawn scheduling or handle
semantics: no native link/spawn/regen mutation is inferred until Zone.exe/PDB
can be inspected again.


Their **top-level block ownership is now source-locked as well**.
`KingdomQuestUnderHallSourceFlow` accepts each external plan only at its exact
canonical source site: `mobregen` is in `Nineteenth`, the eleven summon calls
are in `Summon1..5`, and the return paths
in `QuestSuc` / `QuestFail`. The former `scriptfile` site is no longer an
external UnderHall side effect: it is executed by the generic, native-recovered
Pine one-step runtime. Most importantly, UnderHall has exactly one
`reward KingdomQuest.` occurrence and it is canonical line 378 inside
`QuestSuc`; `QuestFail` has **zero** reward occurrences. Both success and
failure perform their own broadcast/link return sequence. The UnderHall
**scenario-to-reward source trigger location** is therefore fixed independently
of the reward implementation.

Direct Zone.exe/PDB recovery now closes the command-side semantics of
`reward KingdomQuest` as well. `ShineReward::sa_Step` at
`0x004EF650` obtains the current FieldMap KQ handle, constructs
`AxialListKQReward(handle)` and traverses the current map. The iterator
constructor resolves that handle through `KingdomQuestContainer::kqc_SearchHandle`;
the embedded `PROTO_KQ_INFO` begins at KQElement `+0x04`, so the words
read at `+0x97` / `+0x99` are exactly `RewardIndex` /
`DemandMobKill`. For every traversed ShineObject,
`KQContributeList::kqcl_GetMobKill` keys the contribution lookup with the
KQ Handle and virtual `so_GetCharRegistNumber` (+0x344). Contribution
`>= DemandMobKill` invokes virtual `so_ply_KQRewardStruct` (+0x76C) with
the same KQElement; lower contribution invokes virtual
`so_SendErrorCode` (+0x308) with Header 22, PDB type
`NC_KQ_NOREWARD_CMD=0x23`, error `0x1104`. The player implementation of
that error path emits exactly a four-byte packet (two-byte opcode + u16 error).
`KingdomQuestPineKqRewardCommandNative` now projects this exact gate and
wire without enumerating emulator map objects or executing the downstream
GameDB reward transaction. The UnderHall dispatcher now requires the current Pine KQ Handle and wraps
this command in `KingdomQuestUnderHallRewardNativePlan` before the external
owner boundary, so the raw `KingdomQuest` operand cannot be reinterpreted
downstream. The live contribution/target adapters and native reward persistence
remain separate owners.

The immediately following UnderHall command
`questmobkill 2668 "Daliy_Check" 1.` is now native-recovered as well.
`ShineQuestMobkill::sa_Step` at `0x004F8270` resolves operand 1 to
QuestID **2668**, resolves `Daliy_Check` through the native mob data box
(source MobID **50000**, "Kingdom Quest Clear "), and reads the third operand
as the repeat bound **1**. It constructs
`AxialListNearScanObjectType(2, 0)`: native object type 2 is Player and the
zero flag disables the optional extra scan predicate. For every selected
non-empty player (`so_IsEmpty`, vtable +0x300) with a CQuestZone
(`so_ply_GetQuestZone`, +0x808), native uses
`min(selectedPlayerCount, repeatOperand)` as the per-player repeat limit.
Because UnderHall's literal is 1, this is exactly one call whenever at least
one player was selected. QuestID 2668 is not 0xFFFF, so the call is the direct
`CQuestZone::QuestPlayer_ScriptMobKill(2668, 50000)` path at
`0x005C0B40`, not the `_All` variant at `0x005C0CF0`.
`KingdomQuestUnderHallQuestMobKillNative` models these identities and loop
bounds without enumerating map players or mutating CQuestZone. The UnderHall
dispatcher also requires the exact canonical QuestSuc line 379 and source/data
plan before the command reaches an external owner; the concrete live
player/quest adapter remains separate.

The eight UnderHall `broadcast all "<key>".` calls are now closed through
the next native/source boundary too. Direct Zone.exe/PDB recovery shows the
`all` branch of `ShineBroadcast::sa_Step` constructing an
`AxialListWall` for the resolved message and traversing the current map
through `ShineObject::so_AllInMap`. `AxialListWall::ali_Work` dispatches
the notice virtual at vtable `+0x784`; the ShinePlayer override is
`so_ply_Notice`. The original, already hash-locked
`Script/KQUnderHall.txt` (SHA-256
`9b6dff7ca269bf43437fcb2aa0e34eb46610d35a75c6de6b1478a56c3cfed4c0`)
resolves the four used keys exactly to:

- `KQReturn30` -> `Move to Elderine in 30 seconds.`
- `KQReturn20` -> `Move to Elderine in 20 seconds.`
- `KQReturn10` -> `Move to Elderine in 10 seconds.`
- `KQReturn5` -> `Move to Elderine in 5 seconds.`

The downstream player notice is Header 8 / type 17
(`SH8Type.GmNotice`), consistent with the supplied fail capture. The UnderHall
dispatcher now validates this native broadcast plan before crossing the
external-owner boundary, so no live sink sees a raw `KQReturn*` key. The native
body has an additional category byte before its string length, but the exact
value set by this KQ call path is not yet independently recovered.
`KingdomQuestUnderHallBroadcastNative` therefore locks the audience,
virtual dispatch, source-key/message mapping and opcode family while leaving
that category byte explicitly unresolved; it does not reuse the legacy
`Handler8.SendAdminNotice` helper or synthesize a packet.

UnderHall2 has now entered the same source-first closure path. Its canonical
hash-locked source contains exactly **74** occurrences of the six command
families shared with UnderHall: **12 broadcast, 3 linkto, 1 mobregen,
2 questmobkill, 2 reward and 54 summonmob**. Every occurrence is locked by
canonical line, top-level block and exact command text in
`docs/KINGDOM_QUEST_UNDERHALL2_EXTERNAL_SOURCE.tsv`; the matching
`KingdomQuestUnderHall2SourceFlow` routes all 74 line/kind pairs.

That inventory is now typed rather than text-only. The reusable
`KingdomQuestPineKqExternalSyntax` parser recognizes the six shared command
shapes without assigning gameplay meaning. `KingdomQuestUnderHall2SourceCatalog`
locks both used MapInfo rows (Eld=9, Urg=17) and all 13 referenced MobInfo
identities, including Daliy_Check=50000. The Uruga MapInfo regen point
6293/5477 is kept distinct from the script's explicit return coordinates
5835/6397. `KingdomQuestUnderHall2ExternalPlanBuilder` then projects all 74
canonical sites into immutable typed plans and exact per-line summon/link
expectations. This is still mutation-free: no transfer, spawn, quest, reward or
notice side effect is activated yet.

The first Native-semantic reuse is now explicit instead of duplicated.
`KingdomQuestPineUsedQuestMobKillNative` owns the common source-used
`questmobkill 2668 "Daliy_Check" 1` identities, Player scan/vtable metadata,
MobInfo 50000 validation and repeat bound for both UnderHall and UnderHall2.
The original UnderHall wrapper now delegates to that shared primitive.
UnderHall2 lines 543/559 wrap the existing native KQ reward-command plan, while
lines 544/560 wrap the shared questmobkill plan through
`KingdomQuestUnderHall2CommonNative`. Source-site ownership remains distinct;
no player enumeration, CQuestZone mutation or reward persistence is activated.

The native `broadcast all` audience is shared now as well.
`KingdomQuestPineBroadcastAllNative` owns the common
so_AllInMap -> AxialListWall -> player notice-vtable +0x784 metadata and
Header-8/type-17 opcode family. UnderHall keeps its four source-recovered
message strings. UnderHall2's 12 sites instead bind only the exact
`KQUnderHall2` script-file path/hash and four `KQReturn*` keys through
`KingdomQuestUnderHall2BroadcastNative`; their actual string-table values are
still UNRESOLVED in the checked-in source projection. The wrapper therefore
does not borrow UnderHall's Elderine text and does not synthesize a packet.

The remaining three UnderHall2 shared families now stop at the same explicit
fail-closed owner boundary rather than growing script-specific mutation code.
`KingdomQuestPineLinkToOwnerPlan`,
`KingdomQuestPineMobRegenOwnerPlan` and
`KingdomQuestPineSummonMobOwnerPlan` are script-neutral immutable carriers.
Both UnderHall and UnderHall2 external-sink contracts now inherit the same
`IKingdomQuestPineSharedExternalOwner`, so the eventual native link/regen/
summon mutation implementation cannot diverge into two script-specific owners.
`KingdomQuestUnderHall2OwnerPlanBuilder` validates the exact typed source
plan, rechecks MobInfo against both live DataProvider projections, and snapshots
the complete 0x100-byte Pine runtime-handle token for regen/summon. No map
transfer, MobHatchery spawn, scheduling or handle interpretation is performed.

UnderHall now terminates at those exact same three plan types as well. Its
script-specific builder still enforces the original Elderine return sites,
KQ_BossRobo regen operands and all 12 summon line/block/mob/count identities,
but no duplicate UnderHall-only Link/Regen/Summon plan class remains. The
native mutation owner can therefore be implemented once for both scripts.

UnderHall2 is now composed into the live Pine control boundary as well.
`KingdomQuestUnderHall2CommandRuntime` intercepts its **seven** unresolved
external verbs only when `ScriptLanguage == "KQ/UnderHall2"`; the 74
six-family sites plus all **four** exact `chatwin` sites (**78 total**) route
through `KingdomQuestUnderHall2CommandState`.
`KingdomQuestUnderHall2ChatWinSource` locks those four commands at lines
394/396/398/400 in block `TwelveTwo`, preserving only their two literal
tokens (`KQ_GB_Spider/Spider01`, `KQ_GB_Spider/Spider02`,
`RouTownChiefRoumenus/Roumenus01`, `RouTownChiefRoumenus/Roumenus02`).
No speaker/dialog/window semantics are assigned until Zone.exe/PDB recovery
supports them. That state rebuilds the exact typed
source plan, resolves runtime-handle variables for regen/summon, upgrades each
family to the strongest existing native/source-resolved plan, then crosses an
explicit `IKingdomQuestUnderHall2ExternalCommandSink`. Missing owner,
runtime handle, current KQ Handle, message text, or any source/data mismatch is
Invalid/fail-closed before the generic Pine host. The dispatcher itself performs
no map transfer, spawning, quest mutation, reward persistence or packet send.

Across all nine supplied Pine KQs the only common top-level block is
`main`, and all **9/9** scripts contain it. Direct Zone.exe recovery now
closes the native entry rule rather than relying on that corpus coincidence.
`PineEventScriptNode::Script::sa_Step` at **0x004D8100** copies the literal
`main`, resolves that named block and pushes it on the ProcessStack. In
`Movie::Theater::t_PlayFilm` at **0x005081F0**, the Pine branch first calls
`ProcessStack::ps_Ready` at **0x004D6B40**, then pushes the literal
VariableStack name `InitFlag` through **0x004D6BD0 -> vs_Push 0x004D6A90**.
If that push succeeds, it copies exactly **0x40 DWORDs / 0x100 bytes** from the
supplied `ScriptInitValue` PineScriptToken into the new value token before
the root script is stepped. Therefore `ScriptInitValue` is Pine initialization
data, not a block selector. `KingdomQuestPineScenarioRuntime` models this
exact `main` + `InitFlag` start boundary.

Two terminal KQ commands are now projected exactly without substituting old
emulator gameplay paths. `ShineQuestResult::sa_Step` at `0x004EF450`
lower-cases its single token and compares it with the PDB global `index_suc`.
A match constructs Header 22 type 18 (`NC_KQ_COMPLETE_CMD`); every other
valid token constructs type 19 (`NC_KQ_FAIL_CMD`). The two-byte empty wire is
closed by EXE/PDB plus the supplied Lost Mini Dragon fail capture.

Direct Zone.exe recovery now closes the result audience and post-result cleanup
too. `AxialListKQEnd::ali_Work` at `0x00429E00` is called by the current
FieldMap object-list walker and acts only when `so_ObjectType == 2`, i.e.
**ShinePlayer**. For each selected player it invokes
`so_ply_KQSuccess`/ `so_ply_KQFail` first and sends the already-built result
packet second. After that traversal `ShineQuestResult::sa_Step` calls
`FieldMap::fm_ClearObject(0x1B0)` before popping the Pine frame.
`KingdomQuestPineQuestResultPlan` now preserves this exact map-level order.

The original object type mask is no longer ambiguous. Vtable recovery gives
AxialFlag=0, DropItem=1, Player=2, MiniHouse=3, NPC=4, Mob=5,
MagicField=6, Door=7, Bandit=8, EffectObject=9, Servant=10, Mover=11 and
Pet=12. `AxialListObjectClear::ali_Work` at `0x00495940` calls
`so_ObjectType` through vtable +0x4D0, tests `1 << type`, and calls
`so_RetrateFromMap` through +0x3F4 for selected types. Therefore
**0x1B0 = NPC|Mob|Door|Bandit** and the `endofkq` mask
**0xB0 = NPC|Mob|Door**. Native mobile/door clear implementations set retreat
state, so the emulator must not replace this with an immediate collection
delete.

The KQ title hook itself is source-projected separately rather than routed
through `ZoneCharacter.AdvanceTitleTier`. ShinePlayer wrappers
`0x0055AE10/0x0055AE30` reach CharacterTitleZone at player offset
`0x29638`; `CT_KQSuccess` at `0x005CBFB0` increments the qword at
`+0x5E8/+0x5EC`, sets dirty dword `+0x5F0=1`, then evaluates category 21.
`CT_KQFail` at `0x005CBFD0` does the same at
`+0x5F8/+0x5FC`, dirty `+0x600=1`, category 22, through common evaluator
`0x005CB5F0`. Concrete persistence/adaptation of this native title state is
still a live boundary.

`ShineEndOfKingdomQuest::sa_Step` at `0x004F5FC0` reads the current
FieldMap KQ handle, calls `WorldManagerSession::wms_EndOfKQPacket(handle)`,
then calls `FieldMap::fm_ClearObject(0xB0)` before popping its Pine frame.
`KingdomQuestPineEndRuntime` already preserves that END-before-clear order;
the remaining live work is a native-equivalent retreat owner, not mask
interpretation.

The used Pine `regengroup` boundary is also executable-source resolved.
All **243** calls in the nine supplied Pine KQs use exactly two quoted operands;
they cover **225 unique (source key, group index) pairs across five static
regen sources**, and every pair resolves to exactly one `MobRegenGroup` plus
at least one matching `MobRegen` row in the original corpus (for these used
pairs, exactly one row each). `KingdomQuestPineRegenGroupResolver` models
that command-to-source projection. `KingdomQuestPineRegenRuntimePlanBuilder`
now resolves every source `MobIndex` one step further against both original
`MobInfo` and `MobInfoServer` projections and requires both tables to agree
on the exact numeric MobID before the plan can reach an external owner. All
original MobNum/KillNum/RegStandard/RegMin/RegMax/RegDelta*/RegSec* values and
group geometry are preserved unchanged. Native `ShineRegenGroup::sa_Step` at
`0x004EE0F0` then reaches
`PineScriptMobRegenerator::psmr_find` and
`MobHatchery::mh_ScriptBreed`. The generic `regengroup` path is now narrowed one layer further before that
live owner. `KingdomQuestPineMobHatcheryBreedPlanBuilder` materializes every
resolved source row through `KingdomQuestPineRegenNativeLayout` and locks the
recovered native order **t_MapNameServer -> psmr_find -> mh_ScriptBreed** for
all 243 used calls; the supplied corpus has zero optional geometry overrides.
The external command sink now receives this MobHatchery-ready plan rather than
the looser runtime projection. Actual MobHatchery object creation, random
placement, regeneration scheduling and kill/rebreed behavior remain the next
live layer; none is replaced by the emulator's unrelated persistent
`MobBreedLocation`/Mobspawn model.


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

### Native START -> ScenarioBook -> regen execution boundary

The original Zone runtime does **not** turn a KQ START into an unconditional
static mob load. `KingdomQuest::KQElement::kqe_QuestStart` walks the four
native KQ map slots. For every populated map it obtains the KQ element's
`ScriptLanguage` token, drops any current film with
`CinemaComplex::cc_DropFilm`, closes all doors through
`MapDoorArray::mda_CloseAllDoor`, and then calls
`CinemaComplex::cc_PlayFilm` with the stored `ScriptLanguage` and
`ScriptInitValue` tokens. MAKE created those two `PineScriptToken` values
from the corresponding fields of `PROTO_KQ_INFO`.

The Zone lifecycle preserves that recovered START boundary explicitly in
`KingdomQuestScenarioStartPlan`. W2Z START records the native action order
**DropFilm -> CloseAllDoors -> PlayFilm(ScriptLanguage, ScriptInitValue)** in
the KQ runtime state and carries it unchanged through later disjoin/end state
updates. The Pine backend start is now separately executable-source modeled by
`KingdomQuestPineScenarioRuntime`: it enters `main` and creates `InitFlag`
from the exact supplied init token before any Pine step.
`KingdomQuestZonePineFilmBridge` now connects that factory to an actual
**Started** Zone KQ state: it revalidates the three-action native start order,
resolves only the hash-locked Pine source for that ScriptLanguage, rejects a
mismatched explicit KQ handle in the command context, and returns a
`KingdomQuestZonePineFilmSession`. Each session `Step()` performs exactly
one native-style Pine top-frame step. Scheduler cadence and every unresolved
host/map/reward dependency remain external and fail-closed; Lua ScenarioBooks
are explicitly not treated as Pine fallbacks.

The earlier `ScriptInitValue` entry-block ambiguity is therefore closed for
Pine. The canonical `KQ/UnderHall` value `"10"` remains a useful regression
guard because native assigns it to `InitFlag`; it is never interpreted as a
block name. Lua remains a separate backend: the recovered Pine rule is not
reused to infer a Lua entry function or API semantics.

Static regen is reached later and lazily by the running scenario. The recovered
PineScript `regengroup` node follows this exact path:

```text
ShineRegenGroup::sa_Step
  -> PineScriptMobRegenerator::psmr_find(map/source key, group index)
     -> cache miss: PineScriptMobRegenerator::psmr_Load(source key)
        -> KQRegenTable lookup
        -> OptionReader tables MobRegenGroup + MobRegen
     -> MobHatchery::mh_ScriptBreed
```

`KQRegenTable::kqrt_Load` has a native **50-element** source-file capacity.
Each element stores a **12-byte source key** plus the loaded `OptionReader`.
The KingdomQuest path is tried first and Instant second, as documented above.
This separates three different limits that must not be conflated: the 300-slot
live KQ element container, the unrelated 64-entry KQScriptManager, and this
50-entry static KQRegenTable.

All fifteen static regen files actually used by this KQ snapshot use the same
modern source shape: seven fields in `MobRegenGroup` and sixteen fields in
`MobRegen`. Together they contain exactly **732 MobRegenGroup rows** and
**766 MobRegen rows**. The rows retain group geometry/family state,
`MobNum`/`KillNum`, and the full regeneration timing curve; flattening them
into the emulator's simple persistent spawn-point table would discard native
semantics.

`KingdomQuestRegenSourceLoader` preserves the exact 7/16-field rows,
the 12-byte key constraint, the 50-entry native table boundary, and
KingdomQuest -> Instant path order. The Pine command layer now resolves used
`regengroup` calls into those exact group/mob rows, but deliberately does
**not** create maps, mobs, timers or scenario outcomes. Live
`mh_ScriptBreed` object/timer behavior, the remaining Pine host commands,
and the Lua ScenarioBook execution path remain the next runtime layer.


CI now derives the 27 ScriptLanguage keys and 18 used BaseMap keys directly
from the provenance-locked SHN SQL, checks them against the runtime-source
manifest, locks the exact 18-Lua/9-Pine script backend split and the 15/3
static-regen presence boundary, and rejects any guessed replacement for absent
regen input. The original `World/PineScript.txt` SHA-256 is also locked.
This establishes **what original runtime source is present** without pretending
that source presence is the same thing as successful runtime script loading.


## Native KQ reward source and dice boundary

Zone PDB/EXE correlation now closes the data shape used by
`ShinePlayer::sp_KQReward`. Native `KINGDOM_QUEST_REW` is exactly
**128 bytes**: `ID`, 32-byte `IndexString`, 32-byte `KQBoxItemIDX`,
then **15 u16 Reward handles** followed by **15 u16 RewardRate values**.
The SHN stores those 30 values as signed 16-bit columns, so the raw source
model remains signed while `KingdomQuestNativeRewardInfo` performs the
explicit bit-preserving `unchecked(ushort)` projection used by the runtime.

For every one of the 15 slots, `sp_KQReward` calls
`well512_GetRandom(1000)` first and selects the slot only when the returned
value is less than its native RewardRate. A selected handle is then resolved
through `RewardData::rd_FindHandle`; a missing handle is skipped. The new
pure reward-dice model preserves one 0..999 sample per slot and deliberately
does not resolve handles or grant anything yet.

The reward-table lookup itself is now executable-proven as well.
`KQRewardDataBox::operator[](ushort)` scans loaded
`KINGDOM_QUEST_REW` rows and compares the zero-extended request directly
with the row's DWORD `ID`; it never indexes by source-row position.
`operator[](char*)` separately compares `IndexString[32]` byte-by-byte and
returns only after a matching NUL is encountered before byte 32.
`so_ply_KQRewardStruct(KQElement*)` reads the exact
`PROTO_KQ_INFO.RewardIndex` word at KQElement offset `0x97` and calls the
ID overload. `so_ply_KQRewardIndex(char*)` calls the string overload.

That distinction explains a formerly suspicious source edge without any
fallback inference. The 57 supplied KQ definitions use 22 distinct
`RewardIndex` values. Fifteen resolve to a `KingdomQuestRew.ID`; exactly
seven do not: `45, 51, 57, 63, 71, 79, 83`. Native ID lookup returns null
for those values. The original scenarios independently show why several of
these sessions do not depend on a default numeric reward: the five Warrior's
Code PineScripts use `invidualreward ... "HERO_<stage>_<rank>"`, KDSpring
calls `cKQRewardIndex` with `REW_KQ_SPRING_WIN/DRAW/LOSE`, and KDArena
uses named `REW_KQ_ARENA_*` reward strings. No missing numeric ID is
silently redirected to a neighboring row.

`KingdomQuestRewardSourceResolver` now mirrors both native lookup overloads
and rejects row-index fallback. All supplied `IndexString` values are safely
NUL-terminated within the native 32-byte field; the longest is 20 bytes.


The referenced handle source is the original `ShineReward.shn`, independently
decoded as SHA-256
`09acc18d24877fc5dfa9ab431d8dd45561e36ffd48b518cbdf05ddd1810a325f`,
**435 rows / 16 columns**. The World source layer now loads this table only when
the manifest matches that exact snapshot and the runtime table has all 435
rows. `ShineRewardSourceRow` preserves every source field and
`TryFindShineRewardByHandle` models RewardData's handle lookup without
using physical SHN row positions.

Across all 64 supplied `KingdomQuestRew` rows there are exactly **300
distinct nonzero reward handles**. Every one resolves in this exact
`ShineReward` snapshot. Their used-type split is **247 ITEM, 39 EXP,
14 MONEY**; none of the KQ reward handles in this snapshot resolves to HONOR
or the later reward types.

The exact 435-row source is now checked in as
`sql/data/data_kq_source_60_shinereward.sql`. To keep the generated source
compact without changing values, its five always-required fields are explicit
on every row; trailing fields default to zero and the **18** source rows with
nonzero trailing values are patched explicitly. CI locks the LF-normalized generated SQL
content (SHA-256
`9fa4fc1ce2db998cc61f01dc0ef6ba46575a68162efa71c467b9904032c65a88`),
all 435 source ordinals, the 18 patches, and the 300-handle/type coverage. Its shape matches the PDB `ShineReward` struct:
`RewardHandle u16`, `RewardType u8`, `Argument[33]`, `Quantity u32`,
`Upgrade i16`, nine additional i16 fields, `OptionDegree u16`, and
`TitleDegree u32`. The source dumper now includes this SHN as an optional
reward dependency; it is intentionally not added to the four-file
`--require-main` scheduler requirement.

PDB enum values are now source-modeled exactly:
`NONE=0, ITEM=1, EXP=2, MONEY=3, HONOR=4, HP_SOUL_STONE=5,
SP_SOUL_STONE=6, GURAD_SOUL_STONE=7, ATTACK_SOUL_STONE=8,
CLASS_CHANGE=9, PET=10, MAX=11`. The recovered `sp_KQReward` switch has
positive accumulation/build branches only for 0..4: ITEM enters
`TreasureChestMaker`, EXP accumulates the later `sp_GainExp` amount,
MONEY accumulates `cen`, and HONOR accumulates `fame`.

The same function constructs native `NC_KQ_REWARD_REQ` opcode `0x5815`.
Its fixed payload base is 35 bytes: `u32 fame + u64 cen` plus the 23-byte
base of `PROTO_NC_ITEMDB_CREATEITEMLIST_REQ`, followed by its variable item
list. PDB sizes also close `REWARDSUC_ACK` at 8 bytes and
`REWARDFAIL_ACK` at 10 bytes. Their exact native opcodes are `0x5816`
and `0x5817`. Both begin with the six-byte `NETPACKETZONEHEADER`
(`u16 clienthandle + u32 charregistnumber`), followed by `u16 lockindex`;
FAIL appends `u16 error`.

The original Zone ACK handlers additionally close the transaction identity
boundary. Both resolve `clienthandle` back to a player and require the
player's CharacterNumber to equal the ACK `charregistnumber` before touching
the item store. Success passes the ACK LockIndex into one item-store virtual
transaction method; Fail passes the same LockIndex into a different virtual
method.

That common post-resolution identity gate is now executable in
`KingdomQuestRewardAckIdentity`. It accepts only an already-resolved native
ClientHandle/CharacterNumber pair, rejects either mismatch and projects only
the validated `LockIndex` plus ACK kind into
`KingdomQuestRewardAckTransactionRef`. The failure ACK's trailing `error`
is intentionally not copied into that transaction reference because the
recovered native failure handler never reads it.

The item-store transaction semantics behind that identity gate are now
recovered as well. `ShinePlayer::sp_KQReward` obtains the embedded
`InventoryCellLockList` through `so_ply_GetInventoryLockList`
(`0x0055A1D0`, player offset `+0xFAA8`). Native serializes the current
lock index into `NC_KQ_REWARD_REQ` and sends it to GameDB **before** staging
MONEY and FAME on that same lock index, then advances the lock index and calls
`sp_GainExp` immediately. EXP is therefore outside the later ACK rollback
boundary. Success ACK `0x5816` invokes
`InventoryCellLockList::icl_Apply_N_Free` (vtable `+0x1C`,
`0x0048BE90`) with apply mode 0; failure ACK `0x5817` invokes
`icl_Free` (vtable `+0x28`, `0x0048B2C0`) and never reads the packet's
trailing Error. The Cen/Fame releasers apply the staged mutation only on the
success path.

`KingdomQuestRewardNativeTransaction` now locks this pre-ACK ordering and
projects validated ACK identities into exact `ApplyAndFree` versus
`FreeWithoutApply` actions. It deliberately does **not** map those native
lock cells onto the emulator's legacy direct-MySQL Inventory implementation;
live GameDB/lock-list integration remains a separate implementation boundary.
The wire-level `error` field remains modeled in
`KingdomQuestRewardFailAckInfo` because it is still part of the native
packet structure.

The new `KingdomQuestRewardSelectionPlan` now closes the pure selection
layer between dice and mutation. It consumes all 15 dice results, performs the
source-backed RewardHandle lookup, skips missing handles exactly as native
`sp_KQReward` does, ignores NONE, and separates ITEM / EXP / MONEY / HONOR
from later reward types. It deliberately does **not** aggregate with guessed
overflow rules, generate inventory items, award stats/currency, or persist
anything. Types 5..10 remain preserved as unresolved later-type entries rather
than being promoted to KQ effects.

The ITEM branch is now correlated through the actual native classifier rather
than an ItemInfo-only approximation. `sp_KQReward` calls
`TreasureChestMaker::tcm_ItemMake(7, ShineReward*, classGroup)`, which passes
`ShineReward.Argument` to `ItemGroupClassifier::igc_Getitem`.
`igc_Getitem` first searches the ItemDataBox directly. Only a direct miss
consults the classifier's group tree; a second miss returns `0xFFFF`.
Direct lookup therefore wins even when the same string is also a group name.

The group's original source is also closed. Zone.exe
`ItemGroupClassifier::igc_Load` has exactly two `igc_Store` call sites per
ItemInfoServer row: `DropGroupA` at record offset `0x39` and
`DropGroupB` at `0x61`. Original `ItemInfoServer.shn` is SHA-256
`d8cf2b411783822908e6ecbfc833b4ae104d2f3ece1b3cc2250aafa5aa714c10`;
`ItemInfo.shn` is
`7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494`.
`ItemDropGroup.txt` is a separate source and is **not** a fallback for this
classifier.

For the 247 KQ-used ITEM handles the exact result is now **161 direct item
lookups, 82 group lookups and 4 native misses**. That is 95 direct arguments,
33 group-only arguments and three miss arguments. The four misses are handle
89 / `NamedOP3Armor6`, handle 280 / `GiantHoneyingNewReward`, and
handles 906 + 924 / `BestHighProduct`. There are 59 names present in both
direct ItemInfo and classifier-group namespaces; the native direct-first order
resolves those as items. The 33 KQ-used group-only keys cover 791 source
candidate rows / 790 unique ItemIDs.

`docs/KINGDOM_QUEST_ITEM_GROUP_SOURCE.tsv` locks that KQ-relevant
group/miss key boundary and its source provenance. The shared classifier and
`KingdomQuestRewardItemPlan` now distinguish exact item, classifier group,
native classifier miss, and defensive duplicate-ItemInfo ambiguity. A native
miss is a proved `0xFFFF` outcome, not an unresolved alias.

The native group-candidate algorithm is now closed from the supplied
`Zone.exe`/`Zone.pdb`. `igc_Store` inserts every valid
ItemInfoServer DropGroupA/DropGroupB item at the CardStack top and immediately
calls `cs_Suffle(1)`. Across the complete original ItemInfoServer source that
is **5,758 valid stores / 789 distinct groups**. The 33 KQ group-only keys
retain their exact **791 assignment rows / 790 unique ItemIDs**, including
source row, A/B column, native global store ordinal and the matching original
ItemInfo `UseClass`. The compact canonical candidate corpus is locked by
SHA-256 `2944aecbb00d305929075f54b9571fca78252b64c2e0316d3d4e1fa7264fee35`.

`CardDeck::CardStack::cs_Suffle(1)` consumes exactly two MSVC CRT
`rand()` values, takes each modulo the current card count and swaps those two
card values. The linked CRT implementation is exact:
`state = state * 0x343FD + 0x269EC3` modulo 2^32 and
`rand = (state >> 16) & 0x7FFF`.
`igc_Getitem` performs one such shuffle, then examines at most the original
deck count. Every examined top card is moved to the bottom *before* filtering;
the first candidate whose
`ccdb_UseClassTypeToBit(ItemInfo.UseClass) & classGroup` is nonzero wins.
A complete pass without a compatible card returns native `0xFFFF`.

`KingdomQuestNativeItemGroupClassifierState` now reproduces this deck/RNG
state machine, and `KingdomQuestRewardItemCandidatePlan` composes it after
the existing direct-first classifier result. No framework randomness is used.
The model deliberately requires explicit CRT state at both native
`igc_Load` start and reward lookup. Direct Zone.exe recovery corrects the
earlier ownership wording: the statically linked `srand` at
`0x006594A0` and `rand` at `0x006594B2` both obtain the current CRT
thread-data block through `0x006637A8` and write/read the RNG DWORD at
offset `+0x14`. The stream is therefore **native-thread-local**, not
process-global. Three `srand` callsites are present in this Zone.exe: two
seed from the native time routine at `0x0065910B`; the third is the Lua
random-seed path and consumes one `rand()` immediately after reseeding. One
time-seeded path then consumes sixteen CRT values before handing that vector to
the WELL512 initialization boundary.

`MsvcCrtRand` now models the exact per-thread state transition plus direct
`srand`-style `Seed()` replacement. It does not choose a native execution
thread, synthesize wall-clock seed timing, or merge independent native thread
streams. An observed startup time is therefore **not** promoted to an
authoritative later CardDeck state.

`sp_KQReward` passes the result of
`sp_GetItemWhoEquip_ClassGroup()` into
`TreasureChestMaker::tcm_ItemMake(7, ShineReward*, classGroup)`. That helper
is now recovered too. Its native class getter feeds a switch that recognizes
only family roots **1, 6, 11, 16, 21, 26** and expands them respectively to
bits **1..5, 6..10, 11..15, 16..20, 21..25, 26..27**. Every other input
returns the native fallback mask `1`. `KingdomQuestRewardClassGroup`
models exactly that bit construction, and the candidate plan can consume an
explicit native player-class byte through `TryBuildFromNativeClass`.

Candidate selection is therefore source-modeled through class-family masking.
The TreasureChest call boundary is now recovered far enough to preserve its
RNG ordering as well. `TreasureChestMaker` stores exact **111-byte
ItemTotalInformation** entries. Slot 0 is the chest itself and construction
starts with item-count 1. The KQ reward overload at `0x00595D00` requires
`count < 8` **before** calling `igc_Getitem`; consequently at most seven
successfully created reward contents can follow the chest. Capacity-rejected
later ITEM calls consume no classifier/CardDeck RNG. The candidate plan now
models that pre-classifier rejection and increments its successful-content
count only for source-resolved items.

For an accepted ITEM call the recovered native order is
`igc_Getitem -> ItemDataBox validation -> iti_mkregnum ->
ItemAttributeClass::iac_itemcreate -> optional RandomOptionTable lookup /
iac_GetItemOptionStruct / rot_FillOption -> embed child registration number ->
count++`. The chest ITI holds each child's 8-byte registration number at the
native `count*8+3` location. `KingdomQuestRewardTreasureChestNative`
locks this layout, the distinct raw-ITI overload's `count <= 8` guard, and
the exact call-stage order without constructing guessed item bytes.

The reward-specific `ItemAttributeClass::iac_itemcreate` overloads are now
recovered for every KQ-reachable native item class. The supplied reward/item
correlation reaches exactly classes **0, 3, 4, 5, 6, 7, 8, 10, 11 and 14**
across 884 unique candidate ItemIDs. `KingdomQuestRewardItemAttributeNative`
locks their reward-write targets/offsets. Weapon/armor/shield/boot consume an
explicit `well512_GetRandom(1000)` sample and select the native grade from
`Upgrade + Undefined0..8`; quantity/grade writes, option-storage offsets,
decoration clear behavior and the skill-scroll no-op overload are preserved.
All 247 supplied ITEM rewards have `Upgrade=0`, `OptionDegree=0` and
`TitleDegree=0`. The original ItemOptions corpus has no degree-zero cards, so
the optional option-card source path is empty for this supplied reward shape,
although the native lookup/call boundary remains represented.

The exact reachable native item-class shape is now locked against the original
ItemInfo/ItemInfoServer corpus. The **791** KQ CardDeck candidate rows split as
`{0:59, 5:537, 6:118, 7:2, 8:66, 14:9}`; the **95** direct ITEM arguments
split as `{0:13, 3:5, 4:57, 6:4, 8:4, 10:3, 11:1, 14:8}`. Across the
deduplicated reachable KQ ITEM space this yields
`{0:72, 3:5, 4:57, 5:537, 6:120, 7:2, 8:70, 10:3, 11:1, 14:17}`.
Therefore native weapon class **5** is genuinely reachable, but only through
the group/CardDeck path; no direct KQ ITEM argument resolves to class 5.

`KingdomQuestRewardConstructionPlan` now composes the recovered
candidate stage into the reward-specific ItemAttributeClass stage. It derives
the native item class directly from source-backed `ItemInfo.Class`, consumes
one explicitly supplied WELL512 sample only for native classes **5..8**, and
consumes none for classifier misses or TreasureChest capacity rejection.
Every successfully selected item remains explicitly marked as requiring normal
per-class item-create/registration, while only native class **5** carries the
additional weapon-socket-rate requirement.

Live authoritative ITI emission therefore still requires native **thread
assignment and per-thread consumer order** for CRT/CardDeck work plus the
remaining normal per-class base item-create/registration bytes. The class-5
weapon socket stage is now source-backed separately:
`KingdomQuestRewardWeaponSocketRateNative` resolves the exact original
EnchantSocketRate row by ItemGradeType, performs the recovered one-or-two
WELL512 roll sequence and writes the final socket count at native ITI offset
`0x43`. Inventory mutation/persistence remains separate.

The reward row's separate `KQBoxItemIDX` field is now source-resolved as
well. Of the 64 exact `KingdomQuestRew` rows, 58 carry a nonempty box
index and all 58 values are distinct. Every one resolves to exactly one
`ItemInfo.inxname`; there are no misses or duplicate-name ambiguities.
All 58 resolved ItemInfo rows have the same raw source shape:
`type=1, class=15, maxlot=1, equip=0, ItemUseSkill=UsePresentBox,
ItemFunc=0`. The remaining six reward rows have an empty box index.

`KingdomQuestRewardBoxPlan` preserves only that mapping. It deliberately
does not treat `KQBoxItemIDX` as one of the fifteen reward contents, does
not open the box and does not choose TreasureChest results. This separates the
source-backed reward container item from the later TreasureChest item-
construction/transaction layer. The classifier candidate algorithm itself is
now represented separately as described above.

The scalar side is now closed exactly without activating mutation.
Direct Zone.exe disassembly of `ShinePlayer::sp_KQReward` proves three
DWORD locals for EXP, MONEY and HONOR. Every selected scalar
`ShineReward.Quantity u32` is added with a 32-bit x86 add, so each total
wraps modulo **2^32**. When native `NC_KQ_REWARD_REQ (0x5815)` is built,
the MONEY accumulator is written as the low DWORD of its `u64 cen` field
and the high DWORD is explicitly zero; EXP is later passed as that u32 value
to `sp_GainExp(..., 0xFFFF, 0xFFFF)`.

`KingdomQuestRewardScalarPlan` now models those exact u32 accumulators with
unchecked wraparound and exposes `MoneyPacketCen` as the proven zero-extended
wire value. In this exact snapshot no wrap occurs: all 39 used EXP handles and
all 14 used MONEY handles have empty Arguments, there are no used HONOR
handles, the largest possible per-reward sum is **150,000,000 EXP** or
**5,000 MONEY**, and only reward IDs **56** and **62** contain more than
one EXP handle.

The scalar projection itself performs no mutation. The native timing is now
known separately: EXP is gained immediately after the request lock index is
advanced, while MONEY/FAME are staged and become visible only through the
success `ApplyAndFree` ACK path. Scalar arithmetic and ACK transaction timing
are therefore no longer unresolved reward boundaries.

The recovered reward stages now have a single mutation-free composition point:
`KingdomQuestRewardPreparationPlan` chains the exact RewardSource row,
15-slot dice/handle resolution, ITEM argument plan, KQ box-item identity and
scalar sums. It exposes the remaining boundary explicitly:
`RequiresTreasureChestRuntime` is true for every selected ITEM branch because
native `sp_KQReward` routes ITEM through `TreasureChestMaker`. Direct
items, classifier groups and proven `0xFFFF` misses are now distinct source
outcomes; future source ambiguity (duplicate ItemInfo name, missing/ambiguous
box, or a currently-unproven later reward type) is surfaced separately and
never converted into a grant.

The preparation plan still performs no live item creation, character mutation,
database write or network send. For the supplied source snapshot the remaining
live-reward blockers are now limited to authoritative native-thread
assignment/per-thread CRT consumer ordering, completion of the remaining base
ITI/registration stage, live GameDB/InventoryCellLockList integration, and the
native scenario reward-command side effect that starts reward processing.
TreasureChest layout/cap/call order, reward-specific ItemAttribute writes,
class-5 weapon socket-rate construction, scalar mutation timing and ACK
apply/free semantics are no longer unresolved.

The packet/transaction structures are modeled byte-for-byte, but the emulator
does not activate a guessed GameDB-equivalent persistence path before those
remaining construction/integration boundaries are source-equivalent.


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
`ScenarioBookShelf::sbs_GetScenarioBook`. A null lookup returns `0x098C`.
Direct disassembly now proves that `sbs_Read` inserts a catalog key whenever
its preferred `.ps` or fallback `.lua` file exists; the virtual
`sb_Load` result is ignored by this insertion path. This is a MAKE lookup
rule only and says nothing about successful later script execution.

The same MAKE function also fixes the relevant native error precedence:
duplicate Handle is tested first (`0x0982`), then ScenarioBook lookup
(`0x098C`), then the fixed KQ container capacity (`0x0983`). RTTI on the
global owner identifies
`KingdomQuest::KingdomQuestContainer : List<KQElement>`; its fixed-array
construction/destruction path walks exactly `0x12C` elements, i.e. **300
native KQ slots**.

`KingdomQuestScenarioBookShelfSource` now projects the exact KQ subset of the
original startup shelf from the provenance-locked catalog/files. All 27
ScriptLanguage values used by the supplied definitions resolve; the full KQ
catalog subset is 32 keys (9 Pine + 23 Lua), all source-present. Zone therefore
now emits the exact live MAKE_ACK family in native order: duplicate
`0x0982`, source-backed script miss `0x098C`, container full `0x0983`,
or success `0x0981`. Emulator-only MapID/instance routing rejection still
has no invented native Error mapping and remains fail-closed.

This closes MAKE-time script lookup and capacity precedence. It does **not**
activate `ScenarioBook::sb_Load`, `CinemaComplex::cc_PlayFilm`, objective
logic, mob breeding, or KQ completion policy.


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
