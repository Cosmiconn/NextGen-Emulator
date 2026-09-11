# Quest SCENARIO Callchain

## Status

`SCENARIO` is confirmed as a distinct Quest Script command. The original Zone symbols establish a dedicated scenario execution and completion path. The client binary now also establishes the Scenario command-family hierarchy after packet decoding.

## Confirmed original symbols

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD`
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`
- `CQuestZone::Send_NC_QUEST_CLIENT_SCENARIO_DONE_ACK`
- `CQuest::Occure_ScenarioDone`
- `CQuest::_Send_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`

`CQuest::Occure_ScenarioDone` has parameters including `nQuestID` and `nScenarioID`.

## Script corpus

The extracted quest corpus contains 52 `SCENARIO` commands. Observed values include 8, 9, 10, 12, 13, 14, 16 through 27. The values therefore behave as scenario identifiers, not as ordinary dialog message IDs.

## Server packet-structure evidence

The Zone PDB gives an important narrowing result:

- `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` has the native signature `QAEHG@Z`, i.e. it receives one `unsigned short` scenario argument (`nScenarioID`).
- `CQuestZone::QuestPlayer_ScenarioRun` has signature `QAEHGPAUSTRUCT_QSC@@`; its `STRUCT_QSC` input is the parsed quest-script command and the separate numeric argument is an `unsigned short`.
- `CQuestZone::Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` receives a `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_REQ*`.
- The PDB exposes `nScenarioID` as a member associated with `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_REQ`.
- The PDB also exposes the matching `PROTO_NC_QUEST_CLIENT_SCENARIO_DONE_ACK` type.

Direct Zone disassembly of `Recv_NC_QUEST_CLIENT_SCENARIO_DONE_REQ` shows the packet's first field is the 16-bit ScenarioID:

```text
005BDAE2  mov eax,[ebp+8]        ; packet
005BDAE5  movzx edx,WORD PTR [eax]
005BDAE9  push edx
005BDAEE  call 005BD750
```

Thus the client→server Scenario-done request begins with `uint16 ScenarioID`. The exact packet header/opcode and any following fields remain unresolved.

## Client binary evidence (Fiesta.bin)

### Correct command-family hierarchy

A large client callback/dispatcher at `0x004BE490` performs a first-level dispatch using the value at `[packet/context + 0x08]`:

```text
004BE4C8  mov eax,[ebp+0x08]
004BE4CB  add eax,-2
004BE4CE  cmp eax,0x33
004BE4D7  jmp [eax*4+0x4C10A8]
```

The entry for **outer command value 27** resolves to `0x004C036A`.

`0x004C036A` is the Scenario-family dispatcher:

```text
004C036A  mov eax,[ebp+0x0c]
004C036D  dec eax
004C036E  cmp eax,0x21
004C0377  jmp [eax*4+0x4C1CC0]
```

Its Scenario subcommand table has 34 entries. The independently parsed table establishes:

- **Scenario Type 17** → `0x004C037E` → `On_NC_SCENARIO_NPCCHAT_CMD` at `0x004BB040`
- **Scenario Type 18** → `0x004C038F` → `On_NC_SCENARIO_MESSAGE_CMD` at `0x004B6F60`

Therefore the currently proven hierarchy is:

```text
Outer client command value 27
        ↓
Scenario dispatcher 0x004C036A
        ↓
Scenario Type 17 → NPCCHAT
Scenario Type 18 → MESSAGE
```

**Important correction:** earlier documentation that treated Scenario Types 1/2 as NPCCHAT/MESSAGE was incorrect. The proven values are **17/18**. The value **27 is an outer client command/family value**, not yet a proven wire opcode.

### NPCCHAT packet field

`On_NC_SCENARIO_NPCCHAT_CMD` at `0x004BB040` reads:

```text
WORD [packet + 0x21] -> ScenarioID
```

The value is zero-extended as a 16-bit integer and is passed into the client Scenario manager. The same `WORD [packet + 0x21]` is subsequently used again to resolve Scenario state.

Therefore the client binary independently proves that the Scenario NPC-chat command carries a 16-bit `ScenarioID` at offset `0x21` within that command packet structure.

### MESSAGE packet

`On_NC_SCENARIO_MESSAGE_CMD` at `0x004B6F60` is a separate Scenario subcommand and routes through the Scenario/message manager. Its exact payload semantics remain unresolved.

### Network opcode limitation

The client Scenario hierarchy is reached after an outer value has already been decoded into the callback dispatcher. It therefore does **not** prove that outer value 27 is the network opcode, and it does not prove any particular Quest opcode for `Send_NC_QUEST_SCENARIO_RUN_CMD`.

The remaining high-value target is the decoder/registration path that produces outer value 27, followed by correlation with the Zone `Send_NC_QUEST_SCENARIO_RUN_CMD` callsite.

## PCAP correlation

The available attempt-7 capture contains two clients on Zone port 9022 and has been parsed at TCP-payload level. It contains normal quest progress traffic, but the recorded gameplay does not contain a confirmed execution of a quest using the special `SCENARIO` command. No scenario packet is inferred merely from a coincidental byte sequence.

## Runtime conclusion

The supported flow is:

`Quest Script SCENARIO <ScenarioID>` → server scenario run path → client outer Scenario command family → Scenario subcommand dispatch → client scenario-done request → server scenario-done handling with ScenarioID and Quest context.

The exact Quest-side run packet framing/opcode, the exact client-done request framing beyond its leading `uint16 ScenarioID`, and the exact quest-state mutation after Scenario completion remain `UNRESOLVED`.

## Implementation decision

No SCENARIO runtime implementation is added merely from the dispatcher hierarchy. The emulator must not fabricate scenario completion or silently convert `SCENARIO` into `SAY`.

## Next evidence target

1. Trace the client decoder/registration path that creates outer command value 27.
2. Trace callers of `CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD` and correlate its output with the client Scenario family.
3. Obtain/correlate a capture that actually executes one of the known quest scenarios (for example scenario IDs 8, 10, 12–14 or 16–27).
4. Once the wire contract is proven, implement the Scenario bridge and verify it with GitHub Actions.
