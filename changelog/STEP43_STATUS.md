# Step 43 Status

## Focus
Scenario client-dispatch hierarchy correction and evidence consolidation.

## Proven

- Fiesta.bin outer callback dispatcher at `0x004BE490` routes outer command value **27** to `0x004C036A`.
- `0x004C036A` is the Scenario dispatcher.
- Scenario Type **17** routes to `On_NC_SCENARIO_NPCCHAT_CMD` (`0x004BB040`).
- Scenario Type **18** routes to `On_NC_SCENARIO_MESSAGE_CMD` (`0x004B6F60`).
- NPCCHAT reads `WORD [packet+0x21]` as ScenarioID.
- Zone `Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` reads `WORD [packet]` as ScenarioID and passes it into the ScenarioDone path.
- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` remains proven by PDB as taking a single `unsigned short` ScenarioID.

## Correction

The previously documented Scenario Type 1/2 mapping was wrong. The independently parsed client table proves 17/18 for NPCCHAT/MESSAGE. Outer value 27 is a command-family value, not yet a proven network opcode.

## Not implemented

No Scenario runtime bridge was added because the wire opcode/framing for the server run command is still unresolved. No speculative packet implementation was introduced.

## Next

Trace the network decoder/registration path that produces outer value 27, then correlate with the Zone ScenarioRun callsites and a capture containing a real Scenario execution.
