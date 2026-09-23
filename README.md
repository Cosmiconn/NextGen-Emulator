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

Protocol compatibility is verified selectively against original binaries/PDB
and real packet captures. The SQL-backed Quest block is complete for the
supplied 2304-record NA2016 QuestData corpus; other game systems remain partial.
See `docs/PROJECT_BLOCK_STATUS.md` for the current authoritative status and
`DOCUMENTATION.md` for the chronological reverse-engineering log.

## What changed vs. Estrella

See `DOCUMENTATION.md` for the full, itemized changelog: SQL-injection
remediation (all known string-concatenated queries parametrized), removal
of dead EF6/WCF/WinForms code, the .NET Framework → .NET 10 SDK-style
project migration, and the `MySql.Data` → `MySqlConnector` driver swap.

The solution is continuously built with .NET 10 on GitHub Actions for both
Linux and Windows. Feature completeness is tracked separately from build
health in `docs/PROJECT_BLOCK_STATUS.md`.
