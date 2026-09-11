# Step 41 — SCENARIO packet narrowing

## Result

The SCENARIO investigation was advanced without adding speculative runtime behavior.

## Evidence established

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` has native signature `QAEHG@Z`: one `unsigned short` scenario argument.
- `CQuestZone::QuestPlayer_ScenarioRun` receives the parsed `STRUCT_QSC` plus an `unsigned short` argument.
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` receives `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_REQ*`.
- The PDB exposes `nScenarioID` on the client-scenario-done request type.
- The matching `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_ACK` type is present.
- Attempt 7 PCAP contains normal quest traffic but no confirmed execution of one of the known SCENARIO quest commands; therefore no SCENARIO opcode/layout is inferred from it.

## Interpretation

A 16-bit `nScenarioID` is strongly supported as the semantic payload of the scenario run path and as a field of the client-done request. Packet framing, opcodes, padding/additional fields, and the exact quest-state mutation remain `UNRESOLVED`.

## Runtime decision

No SCENARIO implementation was added. `SCENARIO` remains intentionally unresolved in the runtime rather than being converted to `SAY` or fabricated as an immediate completion.

## Next target

Use a capture that actually runs a known scenario and correlate it against the Zone scenario symbols; continue constructor/receiver reconstruction for exact wire format.
