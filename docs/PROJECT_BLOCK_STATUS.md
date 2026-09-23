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

The remaining Quest notes about the original post-0x4412 wake-up caller,
missing-label UI presentation and the internal comma preprocessing routine are
archival native-fidelity questions. They do not leave an executable Quest path
undefined.

## Remaining major blocks

### 1. Kingdom Quests — NOT IMPLEMENTED

This is the largest clearly unimplemented gameplay block.

Current code still answers the World-side KQ initialization with
`p3.WriteShort(0) // zero kingdom quests!` in
`NextGen.World/Handlers/Handler22.cs`.

Evidence already available:

- real World-server KQ list traffic (SH22 type 29);
- instance-detail and registration requests (CH22 types 3 and 5);
- registration responses, countdown traffic and instance-state broadcasts;
- a complete failed KQ session including temporary Zone transfer and
  `KingdomQuestFailed`;
- KQ description/vote data exports.

Still required:

- authoritative KQ definition/schedule/session data or reconstruction;
- exact list serialization and instance lifecycle;
- registration/team assignment;
- temporary map-instance allocation/transfer;
- success/failure objectives, respawn policy and reward completion;
- persistence/reconnect behavior.

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
