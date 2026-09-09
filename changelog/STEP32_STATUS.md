# Step 32 Status — `GetNewQuestStatus` caller audit

## Ergebnis

Step 32 followed all direct `Zone.exe` call sites for the two PDB-associated `CQuest::GetNewQuestStatus` procedures.

Proven:

- `0x005BDA57`, `0x005BE9F4`, `0x005BEAF4`, and `0x005BF34A` call `0x0062F320` with a compact 32-byte player-quest record.
- These callers explicitly construct/copy a record whose quest ID is at `+0x00` and whose status byte is at `+0x02`.
- The status byte is explicitly set to raw `4` or `6` before the call at the observed sites.
- The callee at `0x0062F320` mutates exactly this compact record shape and, on append, copies exactly 32 bytes into the quest list.
- `0x005BF43B` calls `0x0062F4B0` with one unsigned-short quest ID, directly agreeing with the PDB's unsigned-short overload.
- The return value of the `0x0062F4B0` call is not consumed at that caller.
- Raw `4 = PQS_REPEAT` and `6 = PQS_ING` remain independently proven.

## Important new contradiction

The PDB TPI linkage identifies the `0x0062F320` overload as:

`CQuest::GetNewQuestStatus(QUEST_DATA*) -> PLAYER_QUEST_STATUS`

The executable evidence instead shows its argument being used as a 32-byte player-quest state record. Treating that argument as a full `QUEST_DATA` object is therefore not safe.

The exact reason for this source-type/debug-info contradiction is **UNRESOLVED**. No explanation such as stale symbols, linker provenance, or original source typedef mismatch is selected without further evidence.

## Address anomaly

The second PDB procedure record still reports code offset `0x0062F4A0` / size `0x28`, while the obvious callable prologue is at `0x0062F4B0`. This remains recorded as a binary layout/provenance anomaly.

## Runtime

- No runtime changes.
- No SQL changes.
- No quest-status implementation added.

## Validation

- Original `Zone.exe` disassembled around both procedures.
- All direct call targets to `0x0062F320` and `0x0062F4B0` located by full executable disassembly.
- Caller-side argument construction inspected at each relevant site.
- No build/runtime tests performed.
