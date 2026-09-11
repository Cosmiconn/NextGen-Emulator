# Quest SCENARIO Callchain

## Status

`SCENARIO` is confirmed as a distinct Quest Script command. The original Zone symbols establish a dedicated scenario execution and completion path.

## Confirmed original symbols

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD`
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`
- `CQuestZone::Send_NC_QUEST_CLIENT_SCENARIO_DONE_ACK`
- `CQuest::Occure_ScenarioDone`
- `CQuest::_Send_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`

`CQuest::Occure_ScenarioDone` has parameters including `nQuestID` and `nScenarioID`.

## Script corpus

The extracted quest corpus contains 52 `SCENARIO` commands. Observed values include 8, 9, 10, 12, 13, 14, 16 through 27. The values therefore behave as scenario identifiers, not as ordinary dialog message IDs.

## Packet-structure evidence

The Zone PDB gives an important narrowing result:

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` has the native signature `QAEHG@Z`, i.e. it receives one `unsigned short` scenario argument (`nScenarioID`).
- `CQuestZone::QuestPlayer_ScenarioRun` has signature `QAEHGPAUSTRUCT_QSC@@`; its `STRUCT_QSC` input is the parsed quest-script command and the separate numeric argument is an `unsigned short`.
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` receives a `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_REQ*`.
- The PDB exposes `nScenarioID` as a member associated with `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`.
- The PDB also exposes the matching `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_ACK` type.

This strongly narrows the semantic payload to a 16-bit scenario identifier on both the server→client run path and the client→server completion request. It does **not** by itself prove the packet header, opcode, length byte, padding, or any additional fields.

## PCAP correlation

The available attempt-7 capture contains two clients on Zone port 9022 and has been parsed at TCP-payload level. The capture clearly contains normal quest progress traffic, including opcode bytes for the known QuestProgressUpdate packet, but attempt 7's recorded gameplay does not contain a confirmed quest using the special `SCENARIO` command. Therefore it cannot safely supply the missing SCENARIO opcode/layout.

No scenario packet is inferred merely from a numeric byte sequence in the capture. This avoids falsely identifying ordinary world traffic as a scenario command.

## Runtime conclusion

The supported flow is:

`Quest Script SCENARIO <ScenarioID>` → scenario run command → client-side scenario execution → client scenario-done request → server scenario-done handling with QuestID + ScenarioID.

The exact packet framing/opcodes and the exact quest-state mutation after completion are not yet sufficiently reconstructed from the available evidence. They remain `UNRESOLVED` and must not be guessed.

## Implementation decision

No SCENARIO runtime implementation is added in this step. The existing interpreter must not fabricate scenario completion or silently convert SCENARIO into SAY.

## Next evidence target

Obtain/correlate a capture that actually executes one of the known quest scenarios (for example scenario IDs 8, 10, 12–14 or 16–27), then identify the run packet and matching client-done request. In parallel, continue binary reconstruction of the packet constructors/receivers to resolve opcode, framing and any fields beyond `nScenarioID`.
