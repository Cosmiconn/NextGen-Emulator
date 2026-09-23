# Quest Scenario Wire Audit — Step 44

## Proven original server contract

Direct CodeView/PDB mapping and disassembly of the original `Zone.exe` establish:

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` → `0x005BB200`
  - writes opcode `0x440E`
  - writes `WORD scenarioID` at packet offset `+0x02`
  - sends exactly `4` bytes
  - therefore Header 17 / Type 14 / ScenarioID `u16`
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` → `0x005BDAD0`
  - reads `WORD [packet]` as `scenarioID`
  - dispatches into the player/quest scenario-completion path
- downstream completion path at `0x005BD751` calls `0x005BB0C0` after its virtual quest-state callback succeeds
- `CQuestZone::Send_NC_QUEST_CLIENT_SCENARIO_DONE_ACK` → `0x005BB0C0`
  - writes opcode `0x440C`
  - writes the same `WORD scenarioID` at `+0x02`
  - sends `4` bytes
  - therefore Header 17 / Type 12 / ScenarioID `u16`

The exact PDB symbol for `0x005BD751` remains intentionally unresolved. Its direct behavior is sufficient to establish the request/ACK wire contract without naming that internal helper.

## QuestPlayer_ScenarioRun

`CQuestZone::QuestPlayer_ScenarioRun` maps to `0x005BD2F0`.

The QSC dispatcher subtracts `8` from the QSC type and dispatches through a 20-entry table covering types `8..27`. The `STRUCT_QSC_SCENARIO` case is reached through the scenario-script QSC type and constructs the scenario command state before entering the scenario execution path.

## Client-side correlation

The client binary contains:

- outer command/family value `27` → `0x004C036A`
- Scenario dispatcher at `0x004C036A`
- Scenario Type `17` → `On_NC_SCENARIO_NPCCHAT_CMD` (`0x004BB040`)
- Scenario Type `18` → `On_NC_SCENARIO_MESSAGE_CMD` (`0x004B6F60`)
- NPCCHAT reads `WORD [packet+0x21]` as `ScenarioID`

The exact internal decoder mapping from wire opcode `0x440E` to the client's outer command value `27` is not required for the server wire implementation and remains separately documented as an internal client-dispatch correlation.

## Emulator implementation

Step 44 adds:

- `CH17Type.ScenarioDoneReq = 13` (`0x440D`)
- `SCENARIO <ScenarioID>` handling in `Handler17`
- server → client ScenarioRun packet `0x440E`, payload `u16 ScenarioID`
- per-dialog pending scenario state; the quest script pauses at `SCENARIO`
- client → server ScenarioDone request `0x440D`, payload `u16 ScenarioID`
- matching server ACK `0x440C`, payload `u16 ScenarioID`
- quest script continuation after a matching ScenarioDone request

No numeric scenario-to-ScenarioBookShelf mapping is invented here. The ScenarioID is transmitted exactly as supplied by the quest script, matching the original server's `u16` contract.
