# Quest Script Runtime — Step 38

## Scope

Runtime execution of four quest-script commands whose semantics are directly observable from the supplied QuestData scripts and the existing emulator inventory API:

- `GET_ITEM_LOT <ItemID>` → `RESULT` receives the total quantity currently held.
- `GET_PLAYER_EMPTY_INVENTORY <VAR>` → the named script variable receives the number of currently empty normal inventory slots.
- `CREATE_ITEM <ItemID> <Amount>` → creates the requested item through the existing inventory persistence path.
- `DELETE_ITEM <ItemID> <Amount|ALL>` → removes the requested quantity, or all matching stacks, and persists/synchronizes the inventory mutation.

## Control-flow integration

`Handler17` executes these commands while advancing a `QuestScriptMachine`. `DONE` remains the completion boundary. When completion requires a selectable reward, execution pauses at `DONE`; a later reward-selection packet resumes the same script session after `DONE`.

## Evidence boundaries

The implementation deliberately does not reinterpret unresolved syntax or commands:

- `ACCEPT` with arguments remains unresolved; corpus examples include forms such as `ACCEPT 6`.
- `SCENARIO` remains neutral.
- `SET_ABSTATE` remains neutral.
- `CANCEL` remains neutral.
- Non-mob objective completion is not broadened by inference in this step.

No SHN file is read at runtime. No new QuestData field semantics are inferred here.

## Verification

GitHub Actions is the authoritative build environment for this project. Step 38 changes were committed to `nextgen-step14-quest-scripts`; the resulting Windows/Ubuntu CI run must be green before this step is marked complete. The Quest script audit is also expected to run when the required SQL corpus is present in the branch.
