# Quest `SET_ABSTATE` — Original Binary Cross-Reference

## Scope

This document records only semantics directly established from the supplied original `Zone.exe`, `Zone.pdb`, and server-side `Shine/World/QuestParser.txt` resources.

## Correct QSC dispatch

`CQuestZone::QuestEnd` dispatches the current `STRUCT_QSC` command through a jump table at `0x005BF020`. The command value is read from the current QSC object at offset `+0x00`, decremented by one, and used as the table index.

The table entry for command value **30** targets `0x005BE325`. That target contains the original log string:

`CQuestZone::QuestEnd - QSC_SET_ABSTATE`

Therefore, in this original build:

- `QSC_SET_ABSTATE = 30`
- It is **not** the Scenario ID value 25.

The earlier interpretation of Scenario ID 25 as QSC type 25 is rejected.

## `STRUCT_QSC_SET_ABSTATE` field layout observed in `QuestEnd`

At `0x005BE339` the current QSC pointer is loaded. The handler reads:

- `QSC + 0x05`: NUL-terminated AbState name
- `QSC + 0x09`: strength byte
- `QSC + 0x0A`: DWORD KeepTime

The handler resolves the name through the original AbState dictionary (`0x00418F80`).

The strength byte is clamped to the inclusive range **1..40**.

If the QSC KeepTime DWORD is zero, the handler falls back to the selected strength entry's stored duration at the resolved AbState/SubAbState record (`+0x2B` in the runtime record used here).

## Native application path

The handler resolves the AbState definition and invokes the player virtual method at vtable slot `+0x638`. The supplied PDB identifies that method as:

`ShineObjectClass::ShinePlayer::so_AbnormalState_Set`

The native setter then reaches:

`AbnormalStateContainer::AbstateListInObject::asl_AbstateSet`

This is the actual in-memory AbState mutation path.

## Runtime parameter mapping

For the QSC handler, the values passed into the native setter are built as follows:

- target/caster: current player
- state ID: resolved AbState runtime ID (`AbState record +0x22`)
- strength: QSC `+0x09`, clamped to 1..40
- AbState definition: resolved dictionary entry
- start time: original server time source
- empower keep time: `0`
- rest count: `0`
- runtime KeepTime: QSC `+0x0A`, or SubAbState default when QSC KeepTime is zero
- enchant source: `0`
- set-data pointer: `NULL`

The exact native packet/update side effects are intentionally not duplicated here; only the parameter mapping needed by the quest runtime is recorded.

## Scenario distinction

`CQuestZone::QuestPlayer_ScenarioRun` is a separate dispatcher. Its Scenario IDs include class-change scenarios. Scenario ID **25** reaches `0x00591DE0(...,25)`, which creates `0x1056`; the original Character path then invokes `p_Char_ChangeClass`. This is a Scenario/class-change path and must not be used to implement QSC `SET_ABSTATE`.

## Implementation consequence

The emulator should implement quest `SET_ABSTATE <Name> <Strength> <KeepTime>` using the AbState runtime already present in `NextGen.Zone`, not through packet `0x1056`.

The original binary evidence now directly establishes the three quest-script inputs and their runtime behavior:

`Name → AbState dictionary entry → state ID`

`Strength → clamped 1..40 strength level`

`KeepTime → explicit milliseconds; zero means use the selected SubAbState default`
