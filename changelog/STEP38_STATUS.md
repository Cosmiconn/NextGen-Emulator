# Step 38 Status — Quest Script Runtime Commands

## Implemented

- `GET_ITEM_LOT` updates `QuestScriptState.Result` from the character inventory.
- `GET_PLAYER_EMPTY_INVENTORY` updates the requested script variable.
- `CREATE_ITEM` creates the requested stack through `ZoneCharacter.GiveItem`.
- `DELETE_ITEM` removes a requested quantity or all matching item stacks and persists/synchronizes the mutation.
- Reward-selection completion resumes the paused quest script after `DONE`, allowing the following script path to continue.

## Deliberately unresolved

- Parameterized `ACCEPT` remains unresolved.
- `SCENARIO`, `SET_ABSTATE`, and `CANCEL` remain neutral commands.
- Non-kill objective completion is not broadened in this step.
- Repeatable SQL normalization remains unresolved; no raw serialized-field shortcut is introduced.

## Verification

Step 38 code and documentation are committed to `nextgen-step14-quest-scripts`. GitHub Actions is the authoritative build environment. The step is not marked build-complete until the CI run for the current commit is green on Windows and Ubuntu and the quest audit can execute with the required SQL corpus.
