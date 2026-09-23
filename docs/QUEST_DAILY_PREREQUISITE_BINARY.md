# Daily prerequisite eligibility — native binary audit

## Scope

This audit closes the previously unresolved status-2 branch inside
`CQuest::IsSoonableQuest` and isolates the only remaining daily-reset
persistence requirement.

Evidence source: the matching original NA2016 `Zone.exe` / `Zone.pdb`.

## Predecessor branch in IsSoonableQuest

`CQuest::IsSoonableQuest` is at `0x0062FC80`.

For `Start.bQuest != 0`, the native code at
`0x0062FD1E..0x0062FD63`:

1. loads `Start.QuestID`;
2. looks up the corresponding 32-byte `PLAYER_QUEST_INFO` record through
   `0x0062F210`;
3. reads status byte `PLAYER_QUEST_INFO + 0x02`;
4. rejects statuses other than raw `2` and `4`;
5. raw status `4` passes directly;
6. raw status `2` calls virtual slot `CQuest vtable + 0x80`;
7. if that virtual returns `1`, eligibility fails; otherwise it passes.

## Identity of virtual slot +0x80

The `CQuest` constructor at `0x006301D0` installs base vtable
`0x00707F54`. `CQuestZone` installs derived vtable `0x006FBCD4`.

Both vtables contain `0x00630130` at slot `+0x80`.

The PDB contains the virtual signature:

`?IsSoonableDailyQuest@CQuest@@UAEEPAUPLAYER_QUEST_INFO@@@Z`

and the class type record places `IsSoonableDailyQuest` after the eight
player-query virtuals overridden by `CQuestZone`. Therefore the `+0x80`
call is directly identified as:

`CQuest::IsSoonableDailyQuest(PLAYER_QUEST_INFO*)` -> `0x00630130`.

## Exact IsSoonableDailyQuest behavior

The native routine:

- returns false for a null `PLAYER_QUEST_INFO*`;
- resolves the quest definition from `PLAYER_QUEST_INFO.QuestID`;
- returns false if the quest definition cannot be resolved;
- returns false when `QUEST_DATA.Type != 10`;
- reads the byte at `QUEST_DATA + 0x13` (`nDailyQuestType`);
- for raw values 1..4, selects one of four `CQuest` reset-time DWORDs:
  - raw 1 -> `CQuest + 0xB0`
  - raw 2 -> `CQuest + 0xAC`
  - raw 3 -> `CQuest + 0xA8`
  - raw 4 -> `CQuest + 0xA4`
- raw 0 or values above 4 return false;
- compares the signed 64-bit completion timestamp stored at
  `PLAYER_QUEST_INFO + 0x0B/+0x0F` with the selected reset-time value;
- returns true exactly when the stored completion timestamp is older than the
  selected reset boundary.

`CQuest::SetQuestDone` / the completion mutation at `0x0062F610` proves
the timestamp identity: it calls the original 64-bit time helper
`0x00659159` and stores EAX/EDX at `PLAYER_QUEST_INFO +0x0B/+0x0F`.

Thus the status-2 predecessor rule is:

- predecessor Type != 10 -> eligible;
- predecessor Type == 10 and daily check false -> eligible;
- predecessor Type == 10 and last completion is older than the relevant reset
  boundary -> no longer satisfies the predecessor gate.

## Reset-time storage

`CQuestZone` initializes `+0xA4/+0xA8/+0xAC/+0xB0` to zero in its
constructor. Reset-time network handlers later copy four DWORD reset-boundary
values into those fields. The raw daily subtype dispatch selects them in reverse
address order as shown above.

The PDB exposes the related enum names:

- `DQT_NONE`
- `DQT_DAY`
- `DQT_WEEK`
- `DQT_MONTH`
- `DQT_YEAR`

and the field name `nDailyQuestType`.

This audit does not rely on guessed calendar math for those reset boundaries;
the original server receives/stores explicit reset-time values.

## Supplied-corpus impact

The supplied QuestData corpus contains:

- 2304 quests total;
- 1390 quests with `Start.bQuest != 0`;
- 31 quests with `Type == 10`;
- all 31 Type-10 quests have raw `nDailyQuestType == 1`;
- only two prerequisite edges point to a Type-10 predecessor:
  - quest 20037 -> predecessor 20036
  - quest 20048 -> predecessor 20047

Therefore the emulator can now reproduce status-2 prerequisite eligibility for
all other predecessor chains exactly. Only these two supplied chains need the
normalized daily completion/reset timestamp state.

## Emulator state

`QuestNpcStartResolver` now accepts status-2 predecessors exactly when their
quest type is not 10. Type-10/status-2 predecessors still use the safe legacy
fallback until the native completion timestamp and reset-boundary state are
persisted in normalized SQL.

No calendar/reset schedule is guessed.
