# Step 32 Status — `GetNewQuestStatus` caller and return-behavior audit

## Ergebnis

Step 32 followed direct `Zone.exe` call sites and then audited the actual return-register behavior of both PDB-associated procedures.

Proven:

- `0x0062F320` is called with a compact 32-byte player-quest record at all observed direct call sites.
- That record has QuestID at `+0x00` and status at `+0x02`; callers explicitly set status `4` or `6` before the call.
- `0x0062F320` clears the observed auxiliary fields and, on the non-null path, leaves the original argument pointer in `EAX`; it therefore does not behave as a `PLAYER_QUEST_STATUS` return.
- `0x0062F4B0` receives one unsigned-short quest ID and returns only `1` or `0` in the observed machine code.
- The surrounding PDB procedure ranges overlap/cross independent callable machine-code regions; the anomaly is therefore broader than the single second-record discrepancy.

## Important conclusion

The PDB's declared `PLAYER_QUEST_STATUS` return type for the two `GetNewQuestStatus` records is directly contradicted by the observed executable return behavior. Likewise, the `QUEST_DATA*` parameter declaration for the `0x0062F320` record is contradicted by its actual player-quest-record use.

The cause of the mismatch remains **UNRESOLVED**. No source-level reassignment or guessed explanation is made.

## Runtime

- No runtime changes.
- No SQL changes.
- No quest-status implementation added.

## Validation

- Original `Zone.exe` disassembled around `0x62F260–0x62F550`.
- Direct call sites to `0x62F320` and `0x62F4B0` inspected.
- PDB symbol records around the cluster enumerated from Quest module stream 330.
- No build/runtime tests performed.
