# NextGen-Emulator

NextGen-Emulator is an open source C# server emulator for Fiesta Online,
modernized to .NET 10 (LTS).

## Lineage

This project's codebase is directly descended from **Estrella**
(github.com/Temperament/Estrella), which itself was derived from
**DragonFiesta** (github.com/DragonFiestaTeam/DragonFiesta), which in turn
was derived from **Zepheus** (github.com/Zepheus/Zepheus_Fiesta).

Thanks to the Zepheus, DragonFiesta and Estrella authors and all
contributors there.

## Client Support

Client currently targeted: Fiesta Gamigo NA 2016 (TeamNG client).
Protocol/opcode compatibility against the real client is **not yet
verified** — see `DOCUMENTATION.md`.

## What changed vs. Estrella

See `DOCUMENTATION.md` for the full, itemized changelog: SQL-injection
remediation (all known string-concatenated queries parametrized), removal
of dead EF6/WCF/WinForms code, the .NET Framework → .NET 10 SDK-style
project migration, and the `MySql.Data` → `MySqlConnector` driver swap.

**This modernization has not been build-verified with a real .NET SDK.**
Read the "Verification status" section in `DOCUMENTATION.md` before
relying on it.
