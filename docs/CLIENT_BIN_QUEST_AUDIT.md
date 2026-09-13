# Client-Bin Quest Audit — Step 34

## Input

`Client-Bin-DLLs.zip` was inspected as an additional clean-room reference.

Relevant executable:

- `Fiesta.bin` — PE32 i386, image base `0x00400000`, timestamp `2016-06-02 08:20:09`.
- SHA256: `01196b20abe4fb542ee8c4685f57224ef29051b2bf24196eed80a9b2a95ae4fb`

## `cGetQuestStatus` XRef — proven client registration

The string `cGetQuestStatus` is at VA `0x0096A88C` in `.rdata`.

There is exactly one direct 32-bit immediate reference to that address in `.text`, at `0x00424778`.

The surrounding code is a repeated command/resource registration sequence. The relevant sequence is:

- `0x00424772`: load an object vtable entry from `[esi]` / `[vtable+0x18]`.
- `0x00424775`: push callback address `0x00422A00`.
- `0x0042477A`: push string address `0x0096A88C` (`cGetQuestStatus`).
- `0x0042477F`: `ecx = esi`.
- `0x00424781`: indirect call through the vtable entry.

Therefore the client binary directly proves that `cGetQuestStatus` is registered with callback/function address `0x00422A00` through the same command/resource registration mechanism used by neighboring `cGet*` identifiers.

The callback at `0x00422A00` consumes one stack argument and calls internal client routines at `0x00739E70`, `0x006ACDB0`, `0x00728DD0`, and `0x0073A440`. The current evidence does **not** prove that any of these calls are the Header17 quest-status network operation, so no packet semantics are assigned.

## `quest_ack` XRef — two distinct client uses

### 1. `quest_ack` command/name table entry

The string `quest_ack` at `.rdata` VA `0x00978740` has one direct 32-bit immediate reference at `0x00496C51`.

The surrounding code performs a bytewise string comparison against a local buffer. This establishes a command/name matching path, but does not by itself establish a network packet ID or quest-status field layout.

### 2. `quest_ack` string-comparison path

A second string `quest_ack` is at VA `0x009A9CD0`, with the nearby format string `quest_ack %d` at `0x009A9D00`.

The first is referenced at `0x005F4504` by code which calls `0x0079CD29` with the input pointer in `EAX`, the string `quest_ack`, and length `9`.

`0x0079CD29` is directly shown to be a bytewise bounded string comparison routine: it compares the supplied buffer with the supplied string for the supplied length and returns zero on equality. Thus this use of `quest_ack` is a **string comparison**, not proof of a packet opcode.

The surrounding function at `0x005F44A0` subsequently invokes another internal method with argument `0x0C` only after this string comparison succeeds. The exact higher-level protocol meaning of that method remains unresolved.

The `quest_ack %d` format string at `0x009A9D00` is referenced at `0x005F83CD`; the surrounding routine is a client-side processing/UI path. No direct Header17/`0x4410` packet construction was established from this reference.

## Packet correlation result

Raw byte occurrences corresponding to `0x440F` / `0x4410` remain present in `Fiesta.bin`, but the client XRefs above do not establish a unique packet-builder call site or field layout.

Accordingly:

- `cGetQuestStatus` is proven to be a registered client command/resource callback.
- `quest_ack` is proven in two string/name handling paths.
- Neither result proves numeric `PLAYER_QUEST_STATUS` mappings.
- Neither result proves the Header17 `0x440F` request field order.
- Neither result proves the Header17 `0x4410` ACK field order.

## Relevance to current server blocker

This client pass does **not** resolve the server PDB/machine-code contradiction around `CQuest::GetNewQuestStatus`.

The highest-value remaining evidence is therefore the original server-side mutation/caller cluster (`SetQuestDone`, `GetNewQuestStatus`, `IsDoingQuest`, `DoingQuestUpdateStatus`) plus direct capture correlation for the quest-status packets. The client binary should not be used to invent missing server semantics.

## Runtime

No runtime changes.
No SQL changes.
No client binary modifications.
