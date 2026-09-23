# Step 42 Status

## Goal

Use the supplied client `Fiesta.bin` to independently constrain the Scenario protocol before implementing `SCENARIO` runtime behavior.

## Findings

- `On_NC_SCENARIO_NPCCHAT_CMD` is present at client VA `0x004BB040`.
- It reads `WORD [packet + 0x21]` as a `ScenarioID` and passes the 16-bit value into the Scenario manager.
- `On_NC_SCENARIO_MESSAGE_CMD` is present at client VA `0x004B6F60` and is a distinct Scenario command handler.
- The Scenario command dispatcher at `0x004C0385` routes command type 1 to NPCCHAT and command type 2 to MESSAGE; the dispatcher bounds the Scenario subcommand index to 34 (`1..34`).
- This is client-side Scenario subcommand/type information, not proof of the Quest-side network opcode.
- No Quest Scenario RUN opcode is inferred from incidental `0x440E` byte occurrences.

## Decision

Documentation only. No Scenario runtime implementation is added because the Quest-side wire framing/opcode and completion-state mutation remain `UNRESOLVED`.

## Repository change

Updated `docs/QUEST_SCENARIO_CALLCHAIN.md` with the Fiesta.bin evidence.
