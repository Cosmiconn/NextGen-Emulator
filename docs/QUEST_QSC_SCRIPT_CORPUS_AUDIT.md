# Quest Script ↔ Native QSC Corpus Audit

## Scope

This audit compares the textual quest-script corpus currently present in `sql/data/data_questscript_fragments.sql` with the native QSC command enum and dispatch recovered from the original `Zone.exe`.

The corpus scan counts command tokens at script-line boundaries; SQL statements and prose are not counted as quest commands.

## Corpus findings

| Textual command | Corpus occurrences | Native QSC relation | Emulator handling | Status |
|---|---:|---|---|---|
| `GET_PLAYER_EMPTY_INVENTORY VAR1` | 156 | `QSC_GET_PLAYER_EMPTY_INVENTORY = 0x1B` | `QuestRuntime.GetEmptyInventorySlots` → script variable | **PROVEN command present**; native result is an 8-bit value and exact truncation still needs implementation alignment |
| `CREATE_ITEM <id> <lot>` | 133 | `QSC_CREATE_ITEM = 0x0E` | `QuestRuntime.CreateItem` | **PROVEN command present**; native QSC lot is DWORD, while current handler parses `ushort` |
| `DELETE_ITEM <id> <lot/ALL>` | 19 | `QSC_DELETE_ITEM = 0x0D` | `QuestRuntime.DeleteItem` | **PROVEN command present** |
| `ACCEPT [QuestID]` | 4 | quest parser command; explicit QuestID form proven | `QuestRuntime.Accept` | **PROVEN** |
| `LINK <id>` | 4 | parser command; native semantics not yet fully cross-referenced | not executed by Handler17 | **UNRESOLVED** |
| `SCENARIO <id>` | 3 | scenario execution path proven; not the general QSC opcode | packet `0x440E` | **PROVEN** |
| `DONE` | 2 | quest completion command | `QuestRuntime.Complete` | **PROVEN** for current supported flow |
| `SET_ABSTATE <name> <strength> <keepTime>` | 1 | `QSC_SET_ABSTATE = 0x1E` | `QuestRuntime.SetAbstate` | **PROVEN native mapping**; refresh semantics still being aligned |
| `RESET_ABSTATE <name>` | 0 | `QSC_RESET_ABSTATE = 0x1F` | `QuestRuntime.ResetAbstate` | Native mapping proven; no occurrence in this corpus slice |
| `GET_ITEM_LOT <id>` | 0 in this fragment | `QSC_GET_ITEM_LOT = 0x21` | `QuestRuntime.GetItemLot` | Native mapping proven; current result-width handling needs alignment |
| `GET_PLAYER_RACE` | 0 | `QSC_GET_PLAYER_RACE = 0x17` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_CLASS` | 0 | `QSC_GET_PLAYER_CLASS = 0x18` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_LEVEL` | 0 | `QSC_GET_PLAYER_LEVEL = 0x19` | not implemented | **UNRESOLVED textual usage** |
| `GET_PLAYER_GENDER` | 0 | `QSC_GET_PLAYER_GENDER = 0x1A` | not implemented | **UNRESOLVED textual usage** |
| `SET`, `ADD`, `SUB` | no confirmed quest-script command occurrences in this fragment | `QSC_SET = 0x14`, `QSC_ADD = 0x15`, `QSC_SUB = 0x16` | not implemented as textual commands | **UNRESOLVED** |
| `DROP_ITEM` | 0 | `QSC_DROP_ITEM = 0x0F` | not implemented | **UNRESOLVED** |
| `CANCEL` | 0 command-line occurrence in the fragment scan | `QSC_REPEAT_QUEST_GIVE_UP = 0x1C` is not proven equivalent to textual CANCEL | not implemented | **UNRESOLVED** |
| `IS_ABSTATE` | 0 | `QSC_IS_ABSTATE = 0x20` | not implemented | **UNRESOLVED textual usage** |

## Native width constraints

### GET_PLAYER_* family

The native implementations call the player getter, read its `AL` return value, zero-extend it, and store it at `[player + 0xD0 + QSC.Data * 4]`. These native results are therefore byte-width values. `STRUCT_QSC.Data` is the destination quest-variable index.

### GET_ITEM_LOT

The native implementation reads a `WORD ItemID`, calls the player's item-lot getter, then zero-extends `AX` before storing it in the quest result slot. The native QSC result is therefore limited to the low 16 bits.

### CREATE_ITEM

The native QSC structure is `WORD nItemID` at offset `+0` and `DWORD nLot` at offset `+2`. The current Handler17 parser accepts the lot as `ushort`; this is a proven width mismatch.

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

The absence of `GET_PLAYER_RACE`, `GET_PLAYER_CLASS`, `GET_PLAYER_LEVEL`, `GET_PLAYER_GENDER`, `IS_ABSTATE`, `GET_ITEM_LOT`, `DROP_ITEM`, `SET`, `ADD`, or `SUB` in this specific extracted script fragment does not prove those commands never occur in another quest-script source/version.

Likewise, textual `CANCEL` must not be equated with native `QSC_REPEAT_QUEST_GIVE_UP` merely from naming. The native QSC 28/29 dispatch enters the shared parser-state path at `0x005BE1A1`; no direct wire give-up operation was observed there.

## Next evidence targets

1. Map the 16 parser-state dispatch entries at `0x00639330` to their textual command names using the original parser's string/token tables.
2. Correlate `0x0063936?` comparison/variable operations with the exact `IF` grammar already observed in SQL scripts.
3. Resolve the shared QSC 15/28/29 path via the parser-state transition it receives from `0x006387F0`.
4. Align proven byte/word widths in Handler17.
5. Only implement additional textual commands once syntax and native semantics are proven.
