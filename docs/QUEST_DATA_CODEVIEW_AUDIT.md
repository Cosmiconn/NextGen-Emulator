# `QUEST_DATA` CodeView field audit — Step 39

## Scope

Original `Zone.pdb` / `Zone.exe` were cross-referenced further at the native `Occure_*` routines. This step focuses on the exact event signatures and the byte-level behavior of the shared `QUEST_END_CONDITION` arrays. No emulator runtime code is changed.

## Proven outer structure

`QUEST_DATA` has concrete size `0x2A8`.

- `Start` = `QUEST_START_CONDITION` at `+0x18`
- `End` = `QUEST_END_CONDITION` at `+0x58`
- `Action` = `QUEST_ACTION` at `+0xC4`
- `Reward` = `QUEST_REWARD` at `+0x204`

The end block is `0x68` bytes and therefore ends at `QUEST_DATA + 0xC0`.

## Proven end-block offsets

The original CodeView/offset cross-reference gives:

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

The two repeated arrays have these proven sizes from the binary cursor arithmetic:

- NPC/Mob area: 5 entries × `0x08` bytes, first event cursor at `+0x5E`.
- Item area: 5 entries × `0x06` bytes, first event cursor at `+0x86`; the preceding byte/word at `+0x84` is the first entry's gate/leading field.

The exact source-level names of the individual NPC/Mob element members beyond the outer `NPCMobList` name are **not used as evidence below** unless their byte role is directly established by the instruction stream.

## `IsRewardAbleQuest(PLAYER_QUEST_INFO*)` — `0x0062FF40`

The binary proves this routine is the full end-condition eligibility check.

### End level

- `+0x59` is tested as an enable byte.
- player vtable `+0x64` supplies current level.
- current level is compared with byte `+0x5A`.
- current level below the stored threshold returns false.

### NPC/Mob array

The loop starts with cursor `QUEST_DATA + 0x60`, then accesses:

- cursor `-0x04` → `QUEST_DATA + 0x5C`, the list's leading field/gate area
- cursor `+0x00` → target WORD at `QUEST_DATA + 0x60` for the first logical element
- cursor advances by `0x08`
- exactly five iterations

For an enabled entry, the target WORD is passed to player vtable `+0x7C`. If that call returns `1`, the reward condition fails.

The same routine then calls player vtable `+0x68` for the target WORD and compares the returned WORD against the entry WORD at `cursor + 0x02`; a lower player value fails.

This establishes the native behavior of the reward check but **does not by itself assign source names to every byte in the 8-byte entry**.

### Item array

The loop starts at `QUEST_DATA + 0x86` and advances by `0x06` for five entries.

For each entry:

- gate = byte at cursor `-2` (`QUEST_DATA + 0x84` for entry 0)
- item ID = WORD at cursor `+0`
- required lot = WORD at cursor `+2`

Player vtable `+0x7C` is called first with the item ID. A return value of `1` immediately fails. Otherwise player vtable `+0x68` is called with the same item ID and the returned lot is compared with required lot; below-required fails.

### Location

The routine tests `End.bLocation` at `+0xA2` and, when enabled, reads:

- map/location `+0xA4` (WORD)
- X `+0xA8` (DWORD)
- Y `+0xAC` (DWORD)
- range `+0xB0` (DWORD)

The player's location accessor is vtable `+0x60`; the location/range predicate is vtable `+0x28`. A false predicate returns false.

### Scenario/runtime flag

`+0xB4` is tested as the scenario-condition gate. The reward routine does not compare `+0xB6` itself. Instead it checks `PLAYER_QUEST_INFO + 0x1D` bit `0x02` when the scenario gate is enabled.

The scenario event routine separately proves that `+0xB6` is the scenario ID used for matching.

### Race / class

- `+0xB8` gate; `+0xB9` required race; player vtable `+0x6C` supplies current race.
- `+0xBA` gate; `+0xBB` required class; player vtable `+0x70` supplies current class.

A mismatch returns false.

### Time limit

- `+0xBC` gate.
- `+0xBE` WORD time limit.
- `PLAYER_QUEST_INFO + 0x1E` is the current stored quest-time/progress counter.

The reward check passes this condition when `TimeLimit <= PLAYER_QUEST_INFO + 0x1E`; `TimeLimit > current` returns false.

## Native event routines — exact signatures and behavior

The PDB contains these exact native symbols:

```text
?Occure_NPCMobKill@CQuest@@UAEXGGHH@Z
?Occure_TakeItem@CQuest@@UAEXGGGG@Z
?Occure_DestroyItem@CQuest@@UAEXGGGG@Z
?Occure_CheckLocation@CQuest@@UAEXGG@Z
?Occure_ScenarioDone@CQuest@@UAEXGG@Z
?Occure_RaceChange@CQuest@@UAEXGE@Z
?Occure_ClassChange@CQuest@@UAEXGE@Z
?Occure_TimeProcess@CQuest@@UAEXGGG@Z
```

Here `G` is the MSVC unsigned-short/WORD encoding and `H` is the signed-int encoding; `E` is the unsigned-char/BYTE encoding. The function bodies below were checked directly in `Zone.exe`.

### `Occure_NPCMobKill` — `0x006306D0`

The native body is now sufficiently precise to establish the event-side NPC/Mob progress semantics:

1. iterate active player quest entries (`PLAYER_QUEST_INFO` stride `0x20`);
2. only status `6` entries are processed;
3. resolve `QUEST_DATA` by quest ID;
4. start at `QUEST_DATA + 0x5E` and iterate exactly five 8-byte entries;
5. require entry gate at `+0xFE` relative to the cursor (i.e. cursor `-2`) to equal `1`;
6. require entry target WORD to equal the event's NPC/Mob ID argument;
7. require entry byte at `+0x02` to equal `1`;
8. read the required count from entry byte `+0x03`;
9. read current quest progress from `PLAYER_QUEST_INFO + 0x18`;
10. if current progress is below required count, increment it by one;
11. invoke the quest progress/status update path;
12. when the resulting status changes from `6` to `8` or `7`, invoke the corresponding native reward/failure handling path.

This proves that the event-side NPC/Mob progress path uses **entry `+0x02 == 1` as an additional action/type discriminator** and **entry `+0x03` as the per-entry required count**. The source-level names of those two fields remain **UNRESOLVED** because the byte roles are proven but not the names.

### `Occure_TakeItem` — `0x00630810`

PDB signature has four WORD arguments after `this`.

The body:

- processes only status-6 quests;
- resolves the quest data;
- iterates five 6-byte item entries beginning at `QUEST_DATA + 0x86`;
- requires entry gate at cursor `-2` to be `1`;
- matches the event item ID against cursor `+0`;
- reads required lot at cursor `+2`;
- compares the event amount argument against required lot;
- when sufficient, invokes the quest progress/status update path.

This establishes the same 6-byte item array is used by the event path, not merely by the reward eligibility check.

### `Occure_DestroyItem` — `0x00630920`

The structure walk is the same five-entry, 6-byte walk beginning at `+0x86`.

It matches the item ID and compares the event quantity against the required lot. Unlike `Occure_TakeItem`, its native vtable callback is at vtable `+0x38`; the progress/status path is then invoked for status-6 quests.

### `Occure_CheckLocation` — `0x00630A60`

The routine obtains current player location through vtable `+0x60`, then processes active status-6 quests.

For each quest it requires:

- `End.bLocation == 1`
- current map/location == `End.Location` (`+0xA4`)
- location predicate through vtable `+0x28` using `End.LocationX/Y/Range` (`+0xA8/+0xAC/+0xB0`)
- `PLAYER_QUEST_INFO + 0x1D` bit `0x01` not already set

When the predicate succeeds it sets bit `0x01`, calls the location-related native callback at vtable `+0x3C`, and enters the common progress/status update path.

This proves that location completion is **edge-triggered by a runtime flag bit** rather than repeatedly incrementing progress every location tick.

### `Occure_ScenarioDone` — `0x00630BA0`

For each active status-6 quest:

- require `End.bScenario == 1`;
- compare event ScenarioID with `End.ScenarioID` at `+0xB6`;
- require `PLAYER_QUEST_INFO + 0x1D` bit `0x02` to be clear;
- set that bit;
- call the scenario callback at vtable `+0x40`;
- enter the common progress/status update path.

This is direct proof of the `+0xB4/+0xB6` scenario mapping.

### `Occure_RaceChange` — `0x00630C90`

For active status-6 quests:

- require `End.bRace == 1` at `+0xB8`;
- compare event race BYTE with `End.Race` at `+0xB9`;
- call the race-change callback at vtable `+0x44`;
- enter the common progress/status update path.

### `Occure_ClassChange` — `0x00630D70`

For active status-6 quests:

- require `End.bClass == 1` at `+0xBA`;
- compare event class BYTE with `End.Class` at `+0xBB`;
- call the class-change callback at vtable `+0x48`;
- enter the common progress/status update path.

### `Occure_TimeProcess` — `0x00630E50`

The native function:

- obtains current time from `0x65910B`;
- compares it against `[this + 0x14]` and returns immediately when equal;
- performs the observed 16-bit subtraction `previous - current` and zero-extends the result;
- stores current time at `[this + 0x14]`;
- processes only status-6 quests;
- resolves quest data;
- requires `QUEST_DATA + 0xBC == 1`;
- compares `PLAYER_QUEST_INFO + 0x1E` against `QUEST_DATA + 0xBE`;
- adds the computed delta to `PLAYER_QUEST_INFO + 0x1E` when the limit has not been exceeded;
- invokes vtable `+0x4C` with quest ID, updated counter and time limit;
- runs the common status update path.

The exact high-level unit/name of `PLAYER_QUEST_INFO + 0x1E` remains **UNRESOLVED**; it must not be renamed to `ElapsedTime`, `Seconds`, `RepeatCount`, etc. without additional evidence.

## Important correction to previous audit

The earlier Step-38 wording that described the individual NPC/Mob and item element fields as fully CodeView-resolved source names was too strong for the evidence currently available in this working environment. The **outer `QUEST_END_CONDITION` member names and all relevant offsets are proven**, and the inner entry byte roles are proven by instruction-level analysis. Inner source-level names are therefore marked **UNRESOLVED** until independently recovered from the CodeView type records.

Likewise, `IsRewardAbleQuest(PLAYER_QUEST_INFO*)` at `0x0062FF40` is the full end-condition check; `0x0062FEE0` is the separate `QUEST_DATA*` overload.

## Implementation consequence

The current emulator's generic `data_quest_objective` model is not yet equivalent to the original packed end-condition block. In particular, the native model contains:

- five NPC/Mob slots with an action/type byte and count byte;
- five item slots with gate, ID and required lot;
- location/scenario/race/class/time-limit end gates;
- runtime completion flags at `PLAYER_QUEST_INFO + 0x1D`;
- a time counter at `PLAYER_QUEST_INFO + 0x1E`.

Therefore **no SQL schema rewrite is made in this step**. The correct next action is to recover the remaining inner CodeView element names and then compare the real `.shn`/SQL corpus against these exact slots before changing `QuestRuntime.cs`.

## Status

No emulator runtime behavior changed. This commit contains only evidence corrections and binary cross-reference documentation.
