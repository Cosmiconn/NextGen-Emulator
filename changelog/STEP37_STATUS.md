# Step 37 Status — Quest Turn-in / Completion Capture Reconstruction

Date: 2026-09-10
Branch: `nextgen-step14-quest-scripts`
Base: `e434ed38d91ff20502e94f47ce47c3f06284080b`

## Scope

Capture-first investigation of the quest completion interval after the fifth QuestProgressUpdate, with QuestID 956 as the primary correlation target. No runtime implementation was performed.

## Completed

- Inventoried attempts 4–7 from the newly supplied `mitschnitt.zip`.
- SHA256 recorded for all four PCAPNGs.
- Reassembled TCP streams for Zone port 9022.
- Confirmed the per-connection XOR key-position handshake (`0x0807`) and used the existing `NetCrypto` implementation to decrypt client→server traffic.
- Enumerated the complete QuestProgressUpdate sequence for both clients.
- Confirmed five `0x440D` packets for QuestID 956 per client.
- Confirmed body `01 63 01 BC 03`, giving the observed values 355 / 956 / 0x01.
- Rechecked the current client `QuestData.shn`: quest 956 contains objective target 355 with kill flag 1 and amount 5.
- Rechecked `MobInfo`: mob 355 is `Mandragora`.
- Rechecked quest 956 script: finish sequence is SAY 104829–104834 followed by `DONE` and `LINK 957`.
- Rechecked server binary completion path: `0x006302B0` searches the player quest list and calls `0x0062F610` for the matching record; `0x0062F610` performs the proven raw 2/3 completion mutation.

## Critical finding

The human description of attempt 7 labels the earlier five-Slime task as Zach's quest, while the current QuestData/resource evidence maps QuestID 956 to the Mandragora objective and NPC ID 31 / Skill Master Ruby. The capture description and resource mapping therefore cannot both be accepted as the same QuestID-956 event without additional evidence.

This is explicitly left unresolved. No mapping or runtime correction is made by assumption.

## Turn-in result

The interval between the fifth QuestID-956 progress packet and the first QuestID-10 progress packet was fully checked for decoded client→server packets. No Header-17 quest dialog/selection request was isolated there. Consequently the actual completion/turn-in trigger is not yet proven from the available capture.

The next quest starts with QuestID 10 progress at approximately 06:44:42 for both clients, but the intervening completion and reward transition is still not packet-to-code correlated.

## Status

### Proven

- QuestProgressUpdate count and payload for QuestID 956.
- QuestData objective correlation 355 / amount 5.
- Server-side completion mutation raw 2/3.
- Internal quest-record lookup at `0x006302B0`.

### UNRESOLVED

- NPC turn-in request packet.
- Header-17 field semantics in this capture.
- Exact completion trigger.
- Capture event → `0x0062F610` call-path correlation.
- `PQS_REWARD` transition.
- Reward item selection/application packet.
- Quest-list/status refresh packet.
- Resolution of the Zach/Slime versus QuestID-956/Ruby/Mandragora inconsistency.

## Runtime / SQL

No runtime code changed.
No SQL schema changed.
No `.shn` runtime loading introduced.
No quest status semantics beyond already proven Step 35 facts were implemented.

## Tests

No build or runtime test was claimed. This step is a static/capture audit only.
