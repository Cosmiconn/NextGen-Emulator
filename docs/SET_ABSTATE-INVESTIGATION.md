# SET_ABSTATE investigation — 2026-09-11

## Status

`SET_ABSTATE` remains **UNRESOLVED**. No emulator implementation is added by this note.

## Important correction: 0x1056

The Zone binary does emit `WORD 0x1056` from `0x00591DE0`.

For `QuestPlayer_ScenarioRun` at `0x005BD2F0`, QSC types 16 through 26 call `0x00591DE0` with the QSC type as the second argument. The packet constructed at `0x00591DE0` is 10 bytes:

- `+0x00`: `WORD 0x1056`
- `+0x02`: result of `0x00428390(player)` (`WORD`)
- `+0x04`: result of player vtable slot `+0x344` (`DWORD`)
- `+0x08`: second argument to `0x00591DE0`, i.e. the QSC type (`BYTE`)
- `+0x09`: result of player vtable slot `+0x4D8` (`BYTE`)

Thus for `SET_ABSTATE` (QSC type 25), byte `+0x08` is indeed `25`.

However, the original `Character.exe` contains the receiver `fc_NC_CHAR_CLASSCHANGE_REQ` at `0x00412D60` (the error string `fc_NC_CHAR_CLASSCHANGE_REQ ERROR:CharNo=%d, Class=%d` is referenced from this function). That handler reads the same packet layout, treats `+0x08` as a class value, and sends `0x1057` as the response.

Therefore the `0x1056` path cannot currently be used as proof of the SET_ABSTATE semantic path. It is a class-change command in the supplied Character binary. No implementation should be based on the assumption that `0x1056` is the AbState receiver.

## Proven AbState-related server path

The supplied Zone PDB/EXE contains:

`WorldManagerSession::wms_NC_CHAR_ZONE_ABSTATE_CMD`

with the exact function address:

`0x0044B8B0`

The function signature present in the PDB is:

`wms_NC_CHAR_ZONE_ABSTATE_CMD(NETCOMMAND, H)`

The function reads the packet as follows:

- `NETCOMMAND + 0x02`: `WORD`, used as character lookup key
- `NETCOMMAND + 0x04`: `DWORD`, compared against the character's value returned by vtable slot `+0x344`
- `NETCOMMAND + 0x08` onward: passed unchanged to the player vtable slot `+0x68C`

The handler therefore establishes a real Zone/WorldManager AbState command path, but the exact payload structure beginning at `+0x08` is not yet proven.

## Additional PDB evidence

The Zone PDB contains these AbState-related symbols:

- `WorldManagerSession::wms_NC_CHAR_ZONE_ABSTATE_CMD`
- `ShineObjectClass::ShinePlayer::so_ply_AbstateMatchEquip`
- `ShineObjectClass::ShinePlayer::so_ply_PassiveSetAbstate`
- `Abstate2WMS`
- `SubAbstatePriority::PriorityBase::bp_AbStateChange`

These establish that AbState handling is present in the Zone/WorldManager path, but they do not by themselves prove the `+0x08` payload fields.

## Current next investigation

Trace the `NETCOMMAND + 0x08` payload of `wms_NC_CHAR_ZONE_ABSTATE_CMD` to its sender/producer and identify the implementation behind player vtable slot `+0x68C` without assigning semantics from names alone.

Only after the payload is proven should `SET_ABSTATE <Name> <Strength> <KeepTime>` be implemented in the emulator.
