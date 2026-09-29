# Current emulator block status

## Scope and authority

This is the current authoritative feature-block list for the NA2016 target.
`DOCUMENTATION.md` is a chronological engineering log and intentionally
contains historical statements that were later superseded.

Status is based on the current branch implementation, the supplied NA2016 data,
the original binary/PDB audits already committed to this repository, and the
real capture reconstructions documented in sections 53/54 of
`DOCUMENTATION.md`.

## Completed block

### Quest — COMPLETE for supplied NA2016 corpus

The SQL-backed Quest block is complete for all 2304 supplied QuestData records.
Runtime does not read QuestData.shn. The exact opcode/IF/DONE/ACCEPT/SAY/LINK
corpus, missing-label anomalies, reward-selection recovery and SQL-only runtime
are CI-locked.

See `docs/QUEST_BLOCK_COMPLETION.md`.

The Quest block has no remaining implementation gaps for the supplied corpus.
Original helper/caller identities that do not alter observable emulator
behavior are retained only as reverse-engineering metadata and do not reopen the
block.

## Remaining major blocks

### 1. Kingdom Quests — IN PROGRESS

The former zero-KQ World stub is superseded. The current branch has a
source-backed KQ scheduler and native Header-22/server lifecycle model tied to
the supplied NA2016 SHN, PDB/EXE and capture evidence.

Implemented and CI-locked on the KQ branch:

- exact main KQ source/provenance loading plus native schedule/Handle ownership;
- LIST/SCHEDULE/STATUS serialization and refresh deltas;
- JOIN/JOIN_CANCEL/JOIN_LIST admission and membership state;
- source-backed map-link allocation, dynamic Zone instance routing and transfer;
- native start gate, random team division, complete USERSELECT TEAM_SELECT
  request/error/mutation/broadcast path, and W2Z MAKE/START + Z2W END/DESTROY;
- save-location/reconnect state and native KQ date packing;
- exact ScenarioBookShelf KQ catalog/file projection plus native MAKE
  duplicate/script/capacity ACK precedence, without equating shelf membership
  with successful later script execution;
- original static-regen source boundaries and lazy scenario-owned regen path;
- deterministic reward preparation through KQ reward lookup/dice,
  ShineReward lookup, ITEM classifier source resolution, exact native
  CardDeck candidate shuffle/rotation/UseClass filtering plus native
  class-family mask expansion from explicit CRT/class state, box identity and
  exact scalar accumulation;
- native reward ACK packet/identity validation plus recovered
  InventoryCellLockList transaction semantics: request lock index is serialized
  before send, MONEY/FAME are staged on that lock, EXP is granted immediately,
  success ACK applies/frees the staged cells and failure ACK frees without
  apply; no emulator persistence path is substituted for the native lock list;
- complete source-backed World vote transport: 60-second vote duration,
  300-second login/suggest cooldowns, start/voting/check ACK families,
  result/ban processing, force-ban map links, target-disjoin cancellation and
  exact-one login-ban PlayerDisjoin -> deferred VOTE_BAN_MSG_LOGOFF.

Still required for block completion:

- source-equivalent execution inside `CinemaComplex::cc_PlayFilm` for
  PineScript/Lua ScenarioBooks and the live scenario-driven
  mob/objective/success/failure flow. The W2Z START lifecycle already preserves
  the native DropFilm -> CloseAllDoors -> PlayFilm(ScriptLanguage,
  ScriptInitValue) order. Direct Zone.exe recovery now closes the Pine film
  entry rule as well: PineEventScriptNode::Script enters literal top-level
  block `main`, while Theater::t_PlayFilm pushes `InitFlag` into the native
  VariableStack and copies the complete 0x100-byte ScriptInitValue token before
  the first script step. `KingdomQuestPineScenarioRuntime` models that exact
  entry/init boundary. The smallest Pine slice, UnderHall, now has exact command
  routing, generic 9/9
  `waitlogin <variable>` handoff and a generic **55/55** `waitinterrupt
  InterruptBlock "InterruptArg". -> call InterruptBlock.` handoff tied to the
  active interrupt registry. Those shared waits now have a single runtime owner;
  the obsolete UnderHall-only delivery path and its stale post-BlastCheck
  registration requirement have been removed. Sec/TimeOut due-candidate
  evaluation, typed source
  plans for the five HPLow and 19 PlayerEliminate predicates, and source-resolved
  MapInfo/MobInfo plans for its remaining six external command families are
  also modeled. The generic 243-call Pine `regengroup` path now resolves its
  source MobIndex values through both original MobInfo/MobInfoServer projections
  and requires an exact MobID match before handing a runtime plan to the
  MobHatchery owner; only actual spawn/scheduling/kill-rebreed behavior remains.
  Direct Zone.exe/PDB recovery now closes the UnderHall interrupt
  manager itself: registrations append at the native Z/tail, BlastCheck scans
  in registration order and stops at the first fire, RepeatCount/removal and
  Sec deadline advancement are mutable runtime state, HPLow uses the
  registration-time runtime object handle and fires for a missing object or
  `currentHP*1000/maxHP <= threshold`, and PlayerEliminate fires when the
  native qualifying-player count is zero. The observation boundary is now
  split explicitly into a native ShineObject-handle health resolver and the
  native qualifying-player-count source; neither may equate the native handle
  with an emulator MapObjectID or invent the ala_SearchPly filter. Supplying
  those live observations plus the six external command side effects remains
  open; `InterruptArg` has no consumer in the supplied UnderHall source and is
  no longer a control-flow blocker. Their exact
  top-level source sites are also locked: UnderHall's sole
  `reward KingdomQuest` occurs only in `QuestSuc` while `QuestFail` has
  no reward command. Direct Zone.exe/PDB recovery now also closes that
  command's native contribution gate: the current KQ Handle resolves the
  KQElement, per-object KQ mob-kill contribution is compared against the
  native `DemandMobKill` word, eligible objects dispatch
  `so_ply_KQRewardStruct`, and lower contribution dispatches exact
  `NC_KQ_NOREWARD_CMD` (Header 22/type 35) with u16 error `0x1104`.
  The four-byte no-reward wire and RewardIndex/DemandMobKill KQElement offsets
  are CI-locked; live contribution/target adapters plus downstream GameDB
  persistence remain open. The `questresult` COMPLETE/FAIL client wire is also
  closed as an exact empty Header-22 type 18/19 packet. Direct Zone.exe
  recovery now also closes the audience and cleanup semantics:
  AxialListKQEnd visits current-FieldMap objects but acts only on native
  ShinePlayer type 2, performs CT_KQSuccess/CT_KQFail before the send, and
  ShineQuestResult then clears native mask 0x1B0. The object-type vtables close
  0x1B0 as NPC|Mob|Door|Bandit and the `endofkq` 0xB0 mask as
  NPC|Mob|Door. CT_KQSuccess/Fail native qword counters, dirty writes and
  category 21/22 evaluator calls are now projected explicitly; concrete native
  title-state persistence and so_RetrateFromMap-compatible live owners remain.
  The `endofkq` terminal order remains Z2W END followed by that exact
  FieldMap clear behind explicit owners. A Started Zone KQ can now be bound to a real
  `KingdomQuestZonePineFilmSession` through the exact stored
  DropFilm -> CloseAllDoors -> PlayFilm envelope, and each explicit session
  `Step()` advances one native-style Pine top-frame step. The remaining live
  gap is the authoritative CinemaComplex scheduling/host ownership plus native
  event/command side effects; the separate Lua backend execution also remains
  fail-closed. The Pine `cc_PlayFilm` entry binding itself is no longer
  unresolved;
- exact native-thread assignment/consumer ordering for the thread-local Zone
  CRT rand stream used by CardDeck, authoritative normal per-class
  ItemTotalInformation base itemcreate/registration, and live native
  GameDB/InventoryCellLockList integration. Original-source correlation proves
  native item class 5 is reached by 537 of the 791 CardDeck candidate rows and
  by none of the 95 direct ITEM arguments. The class-5 weapon-socket stage is
  now closed separately: the exact five-row EnchantSocketRate source snapshot
  is exported/loaded and `KingdomQuestRewardWeaponSocketRateNative` models the
  native one-or-two WELL512 roll path and ITI socket-count write. The optional
  OptionCard path is source-empty for the supplied corpus. Reward-specific
  ItemAttributeClass writes for all 10 reachable native item classes,
  TreasureChest layout/cap/call order, reward request envelope and GameDB ACK
  lock-list transaction order are source-modeled. Live native GameDB/lock-list
  integration and the native `reward KingdomQuest` command side effect remain;
  UnderHall's source-side success trigger location is already fixed to
  `QuestSuc`;

See `docs/KINGDOM_QUEST_BLOCK.md` for the current evidence and implementation
boundary. Unknown behavior remains fail-closed rather than inferred.

### 2. Combat / skills / AbState fidelity — PARTIAL

Core melee, targeted skills, AoE damage and many AbState effects exist, but
several concrete gaps remain:

- `Skill.Write()` still sends a hard-coded `60000` cooldown;
- `BuffActionResolver` explicitly treats RATE and PLUS effects as the same
  additive model even where the source enum distinguishes them;
- AoE damage currently uses the older direct damage path and does not share all
  targeted-skill/`MapObject.Damage()` mechanics (damage type, AbState
  application, shield/miss/reflect handling);
- shield consumption is aggregated rather than tracked per contributing buff;
- exact party-range/dispel/persistence behavior is not complete;
- the no-buff revive fallback remains flat `HP = 50`, while a real capture
  proves a percentage-like recovery in at least one observed case.

Passive skill books are **already implemented** in the current code; older
documentation saying they can only be inserted manually is superseded.

### 3. Character titles — PARTIAL

All title table data is loadable, but runtime progress triggers currently cover
six categories:

- mob kills (11);
- PvP/guild-war kills (12);
- NPC sell (23);
- NPC buy (24);
- friend count (34);
- total titles/fame meta category (44).

Most of the 127 gameplay categories still lack event counters. The client
notification opcode for a newly earned title is also still unknown; the
character title list itself is sent.

### 4. Guild Tournament subsystem — DATA/PARTIAL ONLY

Normal guild/guild-academy infrastructure exists. Guild Tournament is not a
complete game loop.

Current code loads `data_guildtournamentskill` and can reuse normal AbState
definitions. Community documentation supplied the structures of the wider
tournament table family, but the project does not have the required complete
NA2016 value set for scheduling, scoring, occupation and rewards.

### 5. Auction House — NOT IMPLEMENTED

The repository contains SHN documentation for AuctionCost, AuctionGroupView,
AuctionLimit and AuctionPeriod, but no current auction-house runtime block was
found in the server implementation. Protocol, listing persistence, search,
purchase/settlement and expiration behavior remain to be reconstructed.

### 6. Lucky House / gambling — PROTOCOL RESEARCH ONLY

CH47/SH47 opcodes and several packet shapes are documented from real captures
(object interaction, enter, bet/roll, leave, periodic table data), but there is
no Handler47 gameplay implementation. Rules, state machine, currency mutation
and authoritative result generation remain open.

## Cross-cutting fidelity backlog

These are smaller than the major gameplay blocks above and should normally be
handled alongside the block that consumes them:

- CharacterInfo field alignment around class/job data still has capture-level
  uncertainty;
- SH17 long companion payloads used around SHOW_REWARD pages (types 6/10 in
  the historical capture notation) are not fully decoded;
- Header 36 remains unidentified from only two observed packets;
- exact default revive formula needs another authoritative data point or binary
  reconstruction;
- some skill-learning/title visual broadcasts are still missing despite the
  server-side state mutation working.

## Recommended execution order

1. **Kingdom Quests** — strongest combination of missing gameplay impact and
   existing capture evidence.
2. **Combat/skills/AbState parity** — close cooldown, RATE/PLUS and AoE damage
   architecture before adding more effect types.
3. **Titles** — add event counters systematically from CharacterTitleData and
   then resolve the visible award notification.
4. **Guild Tournament** — after the needed NA2016 table values are available or
   reconstructed.
5. **Auction House**.
6. **Lucky House / gambling**.

The Quest block should not be reopened unless the supplied QuestData corpus
changes, a CI invariant fails, or new evidence proves an implemented Quest
behavior wrong.
