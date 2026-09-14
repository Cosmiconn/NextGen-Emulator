# `QUEST_DATA` CodeView field audit — Step 40

## Scope

Original `Zone.pdb` / `Zone.exe` were cross-referenced further at the native `CQuest::Occure_*` routines. This step resolves the previously missing source-level names of the inner `NPCMobList` element and records the native `QUEST_NPC_MOB_ACTION` enum. No emulator runtime or SQL schema changes are made from inference.

## Proven outer structure

`QUEST_DATA` has concrete size `0x2A8`.

- `Start` = `QUEST_START_CONDITION` at `+0x18`
- `End` = `QUEST_END_CONDITION` at `+0x58`
- `Action` = `QUEST_ACTION` at `+0xC4`
- `Reward` = `QUEST_REWARD` at `+0x204`

`QUEST_END_CONDITION` is `0x68` bytes and ends at `QUEST_DATA + 0xC0`.

## Proven end-block offsets

| `QUEST_DATA` offset | Proven member |
|---:|---|
| `+0x58` | `End.bIsWaitListProgress` |
| `+0x59` | `End.bLevel` |
| `+0x5A` | `End.Level` |
| `+0x5C` | `End.NPCMobList` storage |
| `+0x84` | `End.ItemList` storage |
| `+0xA2` | `End.bLocation` |
| `+0xA4` | `End.Location` |
| `+0xA8` | `End.LocationX` |
| `+0xAC` | `End.LocationY` |
| `+0xB0` | `End.LocationRange` |
| `+0xB4` | `End.bScenario` |
| `+0xB6` | `End.ScenarioID` |
| `+0xB8` | `End.bRace` |
| `+0xB9` | `End.Race` |
| `+0xBA` | `End.bClass` |
| `+0xBB` | `End.Class` |
| `+0xBC` | `End.bTimeLimit` |
| `+0xBE` | `End.TimeLimit` |

The repeated arrays are proven as:

- `NPCMobList`: 5 elements × `0x08` bytes; storage begins at `+0x5C`.
- `ItemList`: 5 elements × `0x06` bytes; storage begins at `+0x84`.

Native event/reward loops use an element cursor at `+0x5E` for `NPCMobList` (the `NPCMobID` field) and `+0x86` for `ItemList` (the `ItemID` field).

## Newly resolved: `NPCMobList` element fields

The original `Zone.pdb` CodeView records contain the complete source names for the inner NPC/Mob element:

| Element offset | CodeView source member | Type/role |
|---:|---|---|
| `+0x00` | `bNPCMob` | BYTE enable/gate |
| `+0x02` | `NPCMobID` | WORD target ID |
| `+0x04` | `NPCMobAction` | `QUEST_NPC_MOB_ACTION` |
| `+0x05` | `NPCMobCount` | BYTE required count |
| `+0x06` | `TargetGroup` | WORD |

The element is exactly `0x08` bytes.

This reconciles the earlier instruction-level cursor notation:

- native cursor `QUEST_DATA + 0x5E` = element `+0x02` (`NPCMobID`);
- cursor `-0x02` = element `+0x00` (`bNPCMob`);
- cursor `+0x02` = element `+0x04` (`NPCMobAction`);
- cursor `+0x03` = element `+0x05` (`NPCMobCount`).

Therefore the formerly unresolved byte roles in `Occure_NPCMobKill` are now source-name resolved.

## `QUEST_NPC_MOB_ACTION` enum recovered from CodeView

The original PDB records contain these enumerators:

| Value | Enumerator |
|---:|---|
| `0` | `QUEST_ACTION_IF_NONE` |
| `1` | `QUEST_ACTION_IF_MOB_KILL` |
| `2` | `QUEST_ACTION_IF_GATHER` |
| `3` | `QUEST_ACTION_IF_TOUCH_OBJECT` |
| `4` | `QUEST_ACTION_IF_AREAINFO` |

This is direct CodeView evidence. The native `Occure_NPCMobKill` test `NPCMobAction == 1` therefore means `QUEST_ACTION_IF_MOB_KILL`; it is not an invented generic “type” value.

`TargetGroup` is source-name resolved by CodeView but its runtime semantic is **UNRESOLVED** in this step because the analyzed event/reward paths do not establish how the value is consumed.

## `IsRewardAbleQuest(PLAYER_QUEST_INFO*)` — `0x0062FF40`

The binary proves this is the full end-condition eligibility check.

### End level

- `End.bLevel` `+0x59` is the enable byte.
- player vtable `+0x64` supplies current level.
- current level is compared with `End.Level` `+0x5A`.
- current level below the stored threshold returns false.

### NPC/Mob array

The loop starts with cursor `QUEST_DATA + 0x60`, i.e. `NPCMobList[0].NPCMobID`, and advances by `0x08` for five entries.

For each enabled entry:

- `bNPCMob` must be `1`;
- `NPCMobID` is passed to player vtable `+0x7C`; return `1` fails the reward condition;
- player vtable `+0x68` is called for `NPCMobID`;
- the returned WORD is compared against `NPCMobList[i].NPCMobAction`-adjacent quantity field at cursor `+0x02`, which is the native WORD at element offset `+0x04` only if interpreted through the actual cursor arithmetic. **Do not use this sentence as a source-level field mapping:** the exact source-level meaning of the reward-path WORD comparison is still being kept at the instruction-level representation pending a second independent cross-reference.

The safe, proven event-side mapping is the table above. No new semantic meaning is assigned to `TargetGroup` here.

### Item array

The loop starts at `QUEST_DATA + 0x86` and advances by `0x06` for five entries.

For each entry:

- gate = `ItemList[i].bItem` at element `+0x00`;
- item ID = `ItemList[i].ItemID` at element `+0x02`;
- required lot = `ItemList[i].ItemLot` at element `+0x04`.

Player vtable `+0x7C` is called first with the item ID. A return value of `1` immediately fails. Otherwise player vtable `+0x68` is called with the item ID and the returned lot is compared with required lot; below-required fails.

## Native event routines

PDB signatures directly identify:

```text
?Occure_NPCMobKill@CQuest@@UAEXGGHH@Z
?Occure_LevelChange@CQuest@@UAEXGGG@Z
?Occure_TakeItem@CQuest@@UAEXGGGG@Z
?Occure_DestroyItem@CQuest@@UAEXGGGG@Z
?Occure_CheckLocation@CQuest@@UAEXGG@Z
?Occure_ScenarioDone@CQuest@@UAEXGG@Z
?Occure_RaceChange@CQuest@@UAEXGE@Z
?Occure_ClassChange@CQuest@@UAEXGE@Z
?Occure_TimeProcess@CQuest@@UAEXGGG@Z
```

`Occure_NPCMobKill` at `0x006306D0` is now fully named at the inner-entry level:

1. active status-6 quest only;
2. resolve `QUEST_DATA`;
3. iterate `NPCMobList[0..4]`;
4. require `bNPCMob == 1`;
5. match event NPC/Mob ID against `NPCMobID`;
6. require `NPCMobAction == QUEST_ACTION_IF_MOB_KILL` (`1`);
7. use `NPCMobCount` as the required count;
8. compare against `PLAYER_QUEST_INFO + 0x18` progress;
9. increment progress by one while below `NPCMobCount`;
10. run the common progress/status update path.

The exact reward/failure callback behavior following a resulting status `8`/`7` remains as previously documented.

`Occure_TakeItem`, `Occure_DestroyItem`, `Occure_CheckLocation`, `Occure_ScenarioDone`, `Occure_RaceChange`, `Occure_ClassChange`, and `Occure_TimeProcess` retain the previously proven behavior and offsets. In particular, location completion uses runtime flag bit `0x01` at `PLAYER_QUEST_INFO + 0x1D`, scenario completion uses bit `0x02`, and time processing updates `PLAYER_QUEST_INFO + 0x1E` using the observed native 16-bit delta calculation.

## `Occure_LevelChange` status

The PDB signature is now independently confirmed as:

```text
?Occure_LevelChange@CQuest@@UAEXGGG@Z
```

with source locals named `nQuestID`, `nPlayerLevel`, and `nDoneLevel` in the CodeView records.

The exact native body/address mapping for this individual routine is **UNRESOLVED in Step 40**. It is therefore deliberately not assigned an address or guessed comparison semantics here. The next binary pass must locate the exact procedure by its CodeView procedure record and then establish whether `nPlayerLevel` is compared against `End.Level`, how `nDoneLevel` is derived, and how the common progress/status path is invoked.

## Implementation consequence

The `NPCMobList` schema can now be represented without invented names:

```text
bNPCMob       BYTE
NPCMobID      WORD
NPCMobAction  QUEST_NPC_MOB_ACTION
NPCMobCount   BYTE
TargetGroup   WORD
```

However, **no SQL schema rewrite is made yet**. The next required step is to locate `Occure_LevelChange` exactly and then cross-reference these resolved names against the actual `.shn` corpus and existing SQL rows before modifying `QuestRuntime.cs` or SQL.
