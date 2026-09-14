# Quest Turn-in / Completion Capture Audit — Step 37

## Scope

Step 37 examines the newly supplied PCAPNG set with priority on the QuestProgressUpdate → objective completion → NPC turn-in/completion path. No runtime implementation is changed by this audit.

Sources used:

- `mitschnitt.zip` containing attempts 4–7
- current `Client.zip`, including `ressystem/QuestData.shn`, `QuestDialog.shn`, and `MobInfo.shn`
- current server `Zone.exe` / `Zone.pdb` archive
- existing Step 35/36 quest mutation documentation
- existing emulator source, especially `NetCrypto`

## Capture inventory

| Capture | Size | SHA256 |
|---|---:|---|
| Versuch 4 | 2,237,992 B | `e2d2e84bb0064aff1a5b256b583023d32200e6b01b05c47beb979d023b5d01af` |
| Versuch 5 | 2,473,916 B | `75c432bb2498274933d0b9779e4433341ae2a30a3d4ef424cae00ea6b594f09e` |
| Versuch 6 | 3,020,664 B | `2b94e82da3ffdf6b5f0a4dab62fa803b01b1440be51d933ee891402f1f6bcae6` |
| Versuch 7 | 5,844,168 B | `d06599b75d876342676004038b1a9509ad4a01bd3881c14d6ac65e846a6f200e` |

Attempt 7 is the primary capture for this step.

## Attempt 7: Zone 9022 connections

Three client connection generations are present on port 9022:

- `56439/56440` — handshake key positions 8 / 9
- `63075/63076` — handshake key positions 10 / 11
- `64278/64279` — handshake key positions 12 / 13

The server→client handshake is visible as `07 08 <xorpos:u16le>`. The existing `NetCrypto` implementation provides the exact XOR table and rolling 499-byte position mechanism needed to decrypt client→server payloads. Client→server traffic was therefore reassembled and decrypted before packet classification.

## QuestProgressUpdate: exact completion-objective sequence

For stream `9022 → 63075`:

```text
06:42:53.132366  0d 44 01 63 01 bc 03
06:43:20.532574  0d 44 01 63 01 bc 03
06:43:30.159348  0d 44 01 63 01 bc 03
06:44:09.237123  0d 44 01 63 01 bc 03
06:44:15.949877  0d 44 01 63 01 bc 03
```

For stream `9022 → 63076`:

```text
06:43:20.532618  0d 44 01 63 01 bc 03
06:43:30.146808  0d 44 01 63 01 bc 03
06:44:09.237155  0d 44 01 63 01 bc 03
06:44:15.929145  0d 44 01 63 01 bc 03
06:44:23.443002  0d 44 01 63 01 bc 03
```

The seven-byte packet is therefore independently reproduced as:

```text
opcode = 0x440D
body   = 01 63 01 BC 03
```

with little-endian interpretation:

```text
word 0 = 0x0163 = 355
word 1 = 0x03BC = 956
byte   = 0x01
```

The existing Step 36 correlation remains valid for the capture: five occurrences correspond to the objective amount 5.

## Important source/capture consistency check

The current `QuestData.shn` still parses to 2304 quest records. Its quest 956 record contains an active objective entry with:

```text
Target/objective ID = 355
HasToBeKilled        = 1
Amount               = 5
```

`MobInfo.shn` / the migrated `MobInfo.sql` identify mob ID 355 as `Mandragora`.

The quest script associated with quest 956 is:

```text
Start:
SAY 104822 NPC
SAY 104823 ME
SAY 104824 NPC
SAY 104825 ME
SAY 104826 NPC
SAY 104827 ME
SAY 104828 NPC
...
ACCEPT
END

Action:
SAY 104835 NPC
END

Finish:
SAY 104829 NPC
SAY 104830 ME
SAY 104831 NPC
SAY 104832 ME
SAY 104833 NPC
SAY 104834 ME
DONE
LINK 957
END
```

The existing migration also maps quest 956 to the quest-start NPC ID 31, which is `Skill Master Ruby` in `MobInfo`.

This conflicts with the human capture description, which labels the earlier five-Slime task as Zach's quest. The binary/resource evidence instead associates the observed `0x440D` / QuestID 956 sequence with the Mandragora objective. **Do not resolve this conflict by guessing.** It is now explicitly tracked as a source/capture narrative inconsistency.

Quest 10 is independently mapped to mob ID 1 (`MushRoom`) with amount 5, matching the later five-progress sequence in the same capture. This makes the 956→10 ordering particularly relevant, but does not by itself prove the NPC interaction or completion trigger.

## Turn-in interval after the fifth 956 progress

For client 63075, the fifth 956 progress update occurs at:

```text
06:44:15.949877
```

For client 63076, the fifth 956 progress update occurs at:

```text
06:44:23.443002
```

The first subsequent QuestProgressUpdate for Quest 10 occurs at:

```text
63075: 06:44:42.258219
63076: 06:44:42.245979
```

Thus both clients enter the next objective approximately 19–27 seconds after their fifth 956 progress packet.

### Client→server result

After TCP reassembly and `NetCrypto` decryption, the client streams were classified packet-by-packet. No packet with header 17 was observed in the relevant client→server interval, and no decoded `0x4401`/`0x4402` NPC-dialog packet was observed there.

The relevant decoded client→server traffic between the fifth 956 progress and the first Quest 10 progress is dominated by movement/position traffic. In particular, no proven quest-selection/quest-completion request can currently be assigned to the interval.

Therefore:

- no client-side Header-17 turn-in request is proven in this capture;
- no ACK-field mapping is added;
- no quest completion packet semantics are invented;
- the completion trigger remains **UNRESOLVED**.

## Server-side completion code correlation

The existing Step 35 binary audit proves that `0x0062F610` mutates a player quest record:

- non-repeatable quest → raw status `2` (`PQS_DONE`)
- repeatable quest → raw status `3` (`PQS_SOON`)
- completion/count field increment at record `+0x13`
- timestamp fields updated
- virtual callback invoked

A newly rechecked binary path at `0x006302B0` scans the player's 32-byte quest records by quest ID and calls `0x0062F610` with the matching record pointer; if no matching record is found it can call `0x0062F610` with null. This confirms a direct internal completion path, but does not establish that this path was reached by the observed PCAP event.

Known direct callers remain:

```text
0x005BBFFF -> 0x0062F610
0x005BF451 -> 0x0062F550
0x005BF4C6 -> 0x0062F680
0x005BF42A -> 0x0062F3B0
0x006302E0 -> 0x0062F610
0x006302F4 -> 0x0062F610
0x00630306 -> 0x0062F610
```

The PDB range anomaly documented in Step 35 remains in force; no symbol reassignment is made from these addresses alone.

## Current Step 37 conclusion

**PROVEN**

1. Attempt 7 contains two independent clients on Zone port 9022.
2. Client→server encryption can be decrypted using the existing `NetCrypto` algorithm and the per-connection handshake positions.
3. QuestProgressUpdate `0x440D` occurs exactly five times per client for the observed QuestID 956 sequence.
4. The observed payload is byte-for-byte `01 63 01 BC 03` after the opcode.
5. QuestID 956 and objective target 355/amount 5 are consistent with the current QuestData resource.
6. Server binary `0x0062F610` contains the proven raw completion mutation 2/3.
7. `0x006302B0` directly searches the player quest list and invokes `0x0062F610` on a matching record.

**UNRESOLVED**

1. The actual QuestID-956 NPC/dialog turn-in packet is not isolated in attempt 7.
2. The exact completion trigger between the fifth progress update and the next quest is not proven.
3. The actual path from capture event to `0x0062F610` is not yet correlated.
4. Reward application and reward packet semantics are not proven by this capture interval.
5. The human capture description's Zach/Slime narrative conflicts with the current resource mapping of Quest 956 → Ruby/Mandragora; this must be resolved from source evidence before implementation.
6. `PQS_REWARD`, reward selection, and quest-list refresh remain unresolved.

## Runtime impact

None. No runtime quest logic, SQL schema, packet layout, or quest-state transition was changed in Step 37.
