# Quest Script ↔ Native QSC Corpus Audit

## Scope

This audit compares the verified complete 2304-record QuestData script corpus with the native quest command-name table and dispatch recovered from the original `Zone.exe`. The CI fixture is authoritative for corpus counts; SQL statements and prose are not counted as quest commands.

## Corpus findings

| Textual command | Corpus occurrences | Native QSC relation | Emulator handling | Status |
|---|---:|---|---|---|
| `GET_PLAYER_EMPTY_INVENTORY VAR1` | 676 | `QSC_GET_PLAYER_EMPTY_INVENTORY = 0x1B` | `QuestRuntime.GetEmptyInventorySlots` → low 8-bit script variable | **PROVEN / ALIGNED**; Handler17 now stores the native byte-width result |
| `CREATE_ITEM <id> <lot>` | 206 | `QSC_CREATE_ITEM = 0x0E` | `QuestRuntime.CreateItem` | **PROVEN / ALIGNED**; DWORD lot width, stack splitting, and real ItemID 0 are handled |
| `DELETE_ITEM <id> <lot/ALL>` | 1474 | `QSC_DELETE_ITEM = 0x0D` | `QuestRuntime.DeleteItem` | **CORPUS PATH COVERED**; both forms plus real ItemID 0 are handled; exact native internal return/packet semantics remain the evidence gap |
| `ACCEPT [QuestID]` | 2410 | quest parser command; explicit QuestID form proven | `QuestRuntime.Accept` | **PROVEN** |
| `LINK <id>` | 350 | native command 11; `0x005BE0EE` | exact target QuestID + effective-status stage switch | **PROVEN / IMPLEMENTED** |
| `SCENARIO <id>` | 52 | scenario execution path proven; not the general QSC opcode | packet `0x440E` | **PROVEN** |
| `DONE` | 2603 | quest completion command | `QuestRuntime.Complete` | **PROVEN** for current supported flow |
| `SET_ABSTATE <name> <strength> <keepTime>` | 51 | `QSC_SET_ABSTATE = 0x1E` | `QuestRuntime.SetAbstate` → dedicated in-place `Buffs.SetBuff` refresh | **PROVEN / ALIGNED for mapped inputs + refresh rule**; internal native timer/callback/packet representation remains outside the claimed parity boundary |
| `RESET_ABSTATE <name>` | 0 | `QSC_RESET_ABSTATE = 0x1F` | `QuestRuntime.ResetAbstate` | Native mapping proven; no occurrence in this corpus slice |
| `GET_ITEM_LOT <id>` | 104 | `QSC_GET_ITEM_LOT = 0x21` | `QuestRuntime.GetItemLot` | **PROVEN / ALIGNED**; Handler17 now exposes the native low 16-bit result to the script state |
| `GET_PLAYER_RACE` | 0 | `QSC_GET_PLAYER_RACE = 0x17` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_CLASS` | 0 | `QSC_GET_PLAYER_CLASS = 0x18` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_LEVEL` | 0 | `QSC_GET_PLAYER_LEVEL = 0x19` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_GENDER` | 0 | `QSC_GET_PLAYER_GENDER = 0x1A` | not implemented | **UNRESOLVED textual usage** |
| `SET`, `ADD`, `SUB` | no confirmed quest-script command occurrences in this fragment | `QSC_SET = 0x14`, `QSC_ADD = 0x15`, `QSC_SUB = 0x16` | not implemented as textual commands | **UNRESOLVED** |
| `DROP_ITEM` | 0 | `QSC_DROP_ITEM = 0x0F` | not implemented | **UNRESOLVED** |
| `CANCEL` | 12 | native command 7; distinct from `REPEAT_QUEST_GIVE_UP = 28` | `QuestRuntime.Cancel` | **PROVEN / IMPLEMENTED** against the reconstructed SetQuestCancel repeatable/non-repeatable behavior |
| `IS_ABSTATE` | 0 | `QSC_IS_ABSTATE = 0x20` | not implemented | **UNRESOLVED textual usage** |

## ItemID 0 is a real item, not a sentinel

The QuestData corpus and the imported ItemInfo table cross-confirm low ItemIDs:

- ItemID 0 = `LeatherBoots`
- ItemID 1 = `LeatherHelmet`
- ItemID 2 = `LeatherPants`
- ItemID 3 = `LeatherShirt`

This is runtime-relevant, not merely structural:

- Quest 103 Start executes `CREATE_ITEM 0000 1`.
- Quest 244 End requires ItemIDs 0,1,2,3 and its Finish script deletes each one.
- The CI quest audit now verifies the low ItemInfo rows and the exact Quest 103/244 zero-ID command occurrences.

Accordingly `QuestRuntime.CreateItem` and `QuestRuntime.DeleteItem` must not reject `itemId == 0`. Item existence is determined by `DataProvider.GetItemInfo`, not by a numeric nonzero convention.

## DELETE_ITEM corpus shape

All 1,474 supplied occurrences use exactly one of the two forms already accepted by the runtime:

- 1,035 lines: `DELETE_ITEM <ItemID> ALL`
- 439 lines: `DELETE_ITEM <ItemID> <numeric lot>`

No numeric-lot occurrence uses zero. This validates the textual parser surface, but does not by itself prove the native return value.

### DELETE_ITEM can exceed the End.ItemLot minimum

Cross-checking the generated QuestData SQL against the Finish scripts proves that an atomic "requested lot must exist before deleting anything" rule would reject valid source flows. Examples:

- Quest 225: End item condition requires Item 3106 lot 3, while Finish executes `DELETE_ITEM 3106 5`.
- Quest 444: End item conditions require Item 3085 lot 5 and Item 3100 lot 5, while Finish deletes 12 and 8 respectively.
- Quest 460: End item conditions require Item 2600 lot 15 and Item 2601 lot 20, while Finish deletes 20 and 25 respectively.

The native rewardability gate therefore does not guarantee that the later numeric DELETE_ITEM request is fully satisfiable. The emulator intentionally keeps consume-up-to-available behavior for numeric deletion and does not add an atomic preflight. `QuestRuntime.DeleteItem` still returns false when the full requested lot was not available, but the textual quest command has no proven branch/result consumer and Handler17 continues the script, matching the source shape conservatively.

This narrows the remaining native evidence gap: exact item-removal ordering/packet behavior and the command's internal return convention are unresolved, but insufficient-quantity atomic rejection is specifically **not** a safe assumption for this corpus.

## Native width constraints

### GET_PLAYER_* family

The native implementations call the player getter, read its `AL` return value, zero-extend it, and store it at `[player + 0xD0 + QSC.Data * 4]`. These native results are therefore byte-width values. `STRUCT_QSC.Data` is the destination quest-variable index.

### GET_ITEM_LOT

The native implementation reads a `WORD ItemID`, calls the player's item-lot getter, then zero-extends `AX` before storing it in the quest result slot. The native QSC result is therefore limited to the low 16 bits.

### CREATE_ITEM

The native QSC structure is `WORD nItemID` at offset `+0` and `DWORD nLot` at offset `+2`. Handler17 now accepts the lot as `uint`, matching the native DWORD field. `QuestRuntime.CreateItem` forwards that quantity to `ZoneCharacter.GiveItemLots`, which splits it according to the item's SQL-loaded `MaxLot`. The `Item` constructor also preserves the requested stack amount.

## ParserNext breakthrough: `0x006387F0`

`0x006387F0` is the parser-state advancement routine used by `CQuestZone::QuestEnd` and other quest-parser paths. It loads the current command/state from the parser object at `+0x7D0`, advances through a 16-entry dispatch table at `0x00639330`, and returns the parser object pointer used as the next QSC state.

The routine also proves that the parser object carries multiple parsed arguments in fields around `+0x7D5` and that the current token stream is held in the parser buffer at `+0x53C`.

A later routine beginning around `0x0063936?` is reached by the parser flow and contains the six relational comparison cases. Its generated machine code is unambiguous:

- equality: `==`
- inequality: `!=`
- less-than: `<`
- greater-than: `>`
- less-or-equal: `<=`
- greater-or-equal: `>=`

The same routine also performs variable assignment/add/subtract on quest-variable slots at `parser + 0x18 + index*4` in its surrounding cases. This confirms that the emulator's existing IF comparison model is directionally consistent with native code, but it does **not** by itself prove that native QSC commands `SET/ADD/SUB` have the same textual syntax.

The parser routine contains explicit token-buffer length checks (including a 0x40-byte bound for one parsed string and a 0x20-byte bound for another), so string-bearing quest commands have native bounds that should not be silently ignored in a strict emulation.

## Important negative finding

The verified 2304-record corpus contains no `GET_PLAYER_RACE`, `GET_PLAYER_CLASS`, `GET_PLAYER_LEVEL`, `GET_PLAYER_GENDER`, `IS_ABSTATE`, `DROP_ITEM`, `SET`, `ADD`, or `SUB` lines. `GET_ITEM_LOT` is present 104 times and is implemented. Absence from this supplied corpus does not prove the other commands never occur in another QuestData version.

The original command-name table directly distinguishes textual `CANCEL` (command 7) from `REPEAT_QUEST_GIVE_UP` (command 28). Likewise, the corrected table identifies LINK as command 11; command 29 is `UNKNOWNED`, not LINK.

## Next evidence targets

1. Keep the full-corpus opcode, DELETE_ITEM operand-shape, known missing-label, and LINK-topology audits mandatory in CI.
2. Preserve the proven IF/GOTO and item operand widths.
3. Do not assign runtime semantics to commands absent from the supplied corpus solely because their native names are known.
4. Keep malformed/blank source operands as source anomalies instead of auto-repairing them.
