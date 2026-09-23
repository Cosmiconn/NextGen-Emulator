# Step 36 — QuestProgressUpdate packet correlation from newly available captures

## Scope

The newly available `mitschnitt.zip` was extracted and the four original NA2016 loopback captures (Versuch 4–7) were parsed directly. Client-to-server payloads were framed before applying the existing Fiesta XOR stream cipher, matching the protocol handling already documented in the repository.

The most useful quest-progress evidence is in Versuch 7, on Zone port 9022.

## Proven packet family

`SH17Type` type `13` is a 7-byte packet including the 2-byte opcode, hence a 5-byte body:

`0d 44 | <5-byte body>`

The packet occurs exactly once per relevant mob kill in the observed quest runs.

### Quest 956

The following body repeats exactly five times in each of the two client streams:

`63 01 bc 03 00`

Cross-reference against the already extracted quest-objective table:

- `0x0163 = 355`, the target ID of Quest 956, slot 2.
- `0x03BC = 956`, the QuestID.
- final byte is `00`.

The objective table independently records Quest 956, slot 2, target 355, `HasToBeKilled=1`, amount 5.

This is a direct binary/data correlation, not a semantic guess.

### Quest 10

The body repeats exactly five times:

`01 01 00 0a 00`

Cross-reference:

- `0x0001 = 1`, the target ID of Quest 10, slot 2.
- `0x000A = 10`, the QuestID.
- final byte is `00`.

The objective table independently records Quest 10, slot 2, target 1, `HasToBeKilled=1`, amount 5.

### Quest 8 / target-ID-zero anomaly

The body repeats exactly five times:

`01 00 00 08 00`

The second little-endian word is `8`, matching QuestID 8. Quest 8's slot-2 objective independently has target ID `0`, `HasToBeKilled=1`, amount 5.

The first word in the packet is therefore **not yet normalized as the target ID**: the capture contains `1`, while the extracted objective row contains `0`. This is recorded as an unresolved encoding/sentinel discrepancy. It is not promoted to an `ID+1` rule because Quest 10 and Quest 956 do not support such a blanket transformation.

## Strongly supported packet layout

For the non-zero-target examples, the capture supports:

- bytes 0..1: objective target identifier
- bytes 2..3: QuestID
- byte 4: `00` in all observed samples

This layout is **proven for the two non-zero target examples**. The exact meaning of byte 4 and the zero-target encoding remain unresolved.

## Relationship to quest-state mutation

The capture evidence proves that `QuestProgressUpdate` is emitted on each observed kill. It does **not** by itself prove a `tQuest.nStatus` transition. The five progress packets remain the same on each kill, while the quest objective count is represented by the number of repeated packets in the capture.

Therefore this step does not implement or rename any quest-state mutation routine.

The next useful correlation is the NPC turn-in after these five kills: match the final `QuestProgressUpdate` sequence with the following Header 17 NPC dialog packets, reward packets, and quest-list refreshes. That is the capture-side evidence needed to connect the observed gameplay event to the server-side status mutation cluster from Step 35.

## Runtime / SQL

No runtime or SQL files were changed in Step 36.
