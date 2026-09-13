# Quest Script ↔ Native QSC Corpus Audit

## Scope

This audit compares the textual quest-script corpus currently present in `sql/data/data_questscript_fragments.sql` with the native QSC command enum and dispatch already recovered from the original `Zone.exe`.

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

## Native width constraints that are now proven

### GET_PLAYER_* family

The native implementations call the player getter, read its `AL` return value, zero-extend it, and store it at:

`[player + 0xD0 + QSC.Data * 4]`

Therefore these five native results are byte-width values. The QSC `Data` field is the destination quest-variable index.

### GET_ITEM_LOT

The native implementation reads a `WORD ItemID`, calls the player's item-lot getter, then zero-extends `AX` before storing it in the quest result slot. Therefore the native QSC result is limited to the low 16 bits of the getter result.

The current emulator's `QuestRuntime.GetItemLot` returns a `uint`, so the Handler17 result assignment should eventually apply the native 16-bit truncation.

### CREATE_ITEM

The native QSC structure is:

- `WORD nItemID` at `Data + 0`
- `DWORD nLot` at `Data + 2`

The current Handler17 parser accepts the lot as `ushort`. This is an emulator-width mismatch even though all currently observed script examples use small lot values.

## Important negative finding

The absence of `GET_PLAYER_RACE`, `GET_PLAYER_CLASS`, `GET_PLAYER_LEVEL`, `GET_PLAYER_GENDER`, `IS_ABSTATE`, `GET_ITEM_LOT`, `DROP_ITEM`, `SET`, `ADD`, or `SUB` in this **specific extracted script fragment** does not prove those commands never occur in another quest-script source/version. They remain unresolved until the complete original script corpus/parser representation is cross-checked.

Likewise, textual `CANCEL` must not be equated with native `QSC_REPEAT_QUEST_GIVE_UP` merely from naming. The native QSC 28/29 dispatch currently enters the shared parser-state routine at `0x005BE1A1`; no direct wire give-up operation was observed there.

## Next evidence targets

1. Correlate the complete quest-script corpus against `CQuestParserScript::CommandRun` / `ParserNext` and the native QSC structures.
2. Resolve the shared parser-state routine at `0x006387F0`, especially its 16-entry state/command table.
3. Align byte/word widths in Handler17 where native return-width evidence is already definitive.
4. Only implement additional textual commands once their script syntax and native semantics are proven.
