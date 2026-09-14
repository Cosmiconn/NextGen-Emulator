# Step 36 Status — QuestProgressUpdate capture correlation

## Result

The previously unavailable PCAPNG captures are now available and were parsed from `mitschnitt.zip`. This resolves an important capture-side gap in the quest investigation.

### Newly proven

- Zone port 9022 in Versuch 7 carries `SH17Type` type 13 as a 7-byte packet (`0d 44` + 5-byte body).
- Quest 956 progress packets repeat exactly five times with body `63 01 bc 03 00`.
  - `0x0163 = 355` matches Quest 956 slot-2 target ID.
  - `0x03BC = 956` matches QuestID.
- Quest 10 progress packets repeat exactly five times with body `01 01 00 0a 00`.
  - `0x0001 = 1` matches Quest 10 slot-2 target ID.
  - `0x000A = 10` matches QuestID.
- Quest 8 progress packets repeat exactly five times with body `01 00 00 08 00`.
  - `0x0008 = 8` matches QuestID.
  - Quest 8's extracted target ID is 0, while the packet contains 1 in the corresponding first word. This is an unresolved encoding/sentinel case.
- For the non-zero-target examples, the first two words are directly cross-correlated as `TargetID` and `QuestID`; the final byte is observed as zero but its semantic meaning is not assigned.

### Important limitation

The repeated progress packet proves a per-kill quest-progress notification, not a `tQuest.nStatus` transition. The capture does not yet prove whether the status changes on the fifth kill, only that the client receives the same progress notification once per kill.

### Runtime / SQL

No runtime code changed.
No SQL changed.
No quest-status enum was changed.

## Next step

Use the exact capture timeline after the fifth kill and the subsequent NPC turn-in to correlate dialog responses, reward packets, quest-list refreshes, and the Step 35 mutation cluster. Keep the Quest 8 zero-target discrepancy explicitly unresolved until another non-zero/zero target pair proves its encoding.
