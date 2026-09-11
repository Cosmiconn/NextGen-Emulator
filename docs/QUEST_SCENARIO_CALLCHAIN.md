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

## Client binary evidence (Fiesta.bin)

The supplied `Fiesta.bin` provides an independent client-side confirmation of the Scenario protocol family.

### Scenario command dispatcher

At client VA `0x004C0385`, the Scenario command dispatcher routes its first Scenario command type to `On_NC_SCENARIO_NPCCHAT_CMD` at `0x004BB040`. The following command type routes to `On_NC_SCENARIO_MESSAGE_CMD` at `0x004B6F60`.

The dispatcher uses a command-type value in the range `1..34` (the shown switch subtracts one and bounds the index against `0x21`). This is a **Scenario subcommand/type**, not the network opcode; it must not be confused with the Quest packet opcodes already reconstructed elsewhere.

### NPCCHAT packet field

`On_NC_SCENARIO_NPCCHAT_CMD` at `0x004BB040` reads:

```text
WORD [packet + 0x21] -> ScenarioID
```

The value is zero-extended as a 16-bit integer and is passed into the client Scenario manager. The same `WORD [packet + 0x21]` is subsequently used again to resolve Scenario state.

Therefore the client binary independently proves that the Scenario NPC-chat command carries a 16-bit `ScenarioID` at offset `0x21` within its command packet structure.

### MESSAGE packet

`On_NC_SCENARIO_MESSAGE_CMD` at `0x004B6F60` reads the first two words of its packet (`+0x00` and `+0x02`) and passes them through the Scenario/message handling path. This is a separate Scenario subcommand from NPCCHAT.

The exact meaning of those two fields and the full message structure remain `UNRESOLVED`.

### Important limitation

The client findings above do **not** yet identify the Quest-side Scenario RUN network opcode. The observed client dispatcher is a Scenario-command dispatcher after a Scenario packet has already been decoded. Therefore it cannot safely be used to claim that any particular Quest opcode (for example `0x440E`) is the Scenario RUN opcode.

## PCAP correlation

The available attempt-7 capture contains two clients on Zone port 9022 and has been parsed at TCP-payload level. The capture clearly contains normal quest progress traffic, including opcode bytes for the known QuestProgressUpdate packet, but attempt 7's recorded gameplay does not contain a confirmed quest using the special `SCENARIO` command. Therefore it cannot safely supply the missing SCENARIO opcode/layout.

No scenario packet is inferred merely from a numeric byte sequence in the capture. This avoids falsely identifying ordinary world traffic as a scenario command.

## Runtime conclusion

The supported flow is:

`Quest Script SCENARIO <ScenarioID>` → scenario run command → client-side Scenario command dispatch/execution → client scenario-done request → server scenario-done handling with QuestID + ScenarioID.

The exact Quest-side run packet framing/opcode, the exact client-done request framing, and the exact quest-state mutation after completion are not yet sufficiently reconstructed from the available evidence. They remain `UNRESOLVED` and must not be guessed.

## Implementation decision

No SCENARIO runtime implementation is added in this step. The existing interpreter must not fabricate scenario completion or silently convert SCENARIO into SAY.

## Next evidence target

Continue from the client Scenario dispatcher into the network registration/packet construction path and correlate it with the Zone PDB symbols. In parallel, obtain/correlate a capture that actually executes one of the known quest scenarios (for example scenario IDs 8, 10, 12–14 or 16–27), then identify the Quest-side run packet and matching client-done request.
