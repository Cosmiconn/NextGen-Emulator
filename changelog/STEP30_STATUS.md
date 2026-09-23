# Step 30 Status — `GetNewQuestStatus` binary audit

## Ergebnis

Step 30 untersucht `CQuest::GetNewQuestStatus` weiter anhand der originalen `Zone.pdb`/`Zone.exe`-Daten.

Neu gesichert:

- `Quest.obj` ist im DBI-Modulbestand identifiziert und verwendet Symbolstream 330.
- Im PDB existieren zwei Overloads von `CQuest::GetNewQuestStatus`:
  - `GetNewQuestStatus(unsigned short)`
  - `GetNewQuestStatus(QUEST_DATA*)`
- Die Quest-State-Mutationszone um `0x0062F320..0x0062F680` wurde direkt disassembliert.
- `0x0062F4B0` findet einen Quest-Eintrag, prüft `QUEST_DATA +0x12` und schreibt bei nicht-null `+0x02 = 4`; die übrigen Hilfsfelder werden zurückgesetzt.
- Bei `QUEST_DATA +0x12 == 0` wird über `0x0062F350` der passende Player-Quest-Eintrag entfernt.
- `0x0062F320` setzt mehrere Hilfsfelder eines Player-Quest-Records zurück, ohne das Statusbyte `+0x02` zu schreiben.
- Die DB-Evidence bestätigt unabhängig: `4 = PQS_REPEAT`.

## Wichtige Korrektur / Vorsicht

Die rohen PDB-Symbol-/Source-Record-Adressen der beiden `GetNewQuestStatus`-Overloads lassen sich derzeit nicht konsistent auf die bereits unabhängig verifizierten Funktionsgrenzen abbilden. Deshalb wird **keine** der beiden PDB-Adressen als endgültige Implementierungsadresse behauptet.

Das ist absichtlich **UNRESOLVED**. Es wird kein Statusverhalten aus dem Namen `GetNewQuestStatus` geraten.

## Runtime

- Keine Runtime-Änderung.
- Keine neue Quest-/Status-Semantik implementiert.
- Keine SQL-Migration geändert.

## Validierung

- Original `Zone.exe` disassembliert.
- Original `Zone.pdb` und DBI/CodeView-Material untersucht.
- Build/Runtime-Tests in Step 30 nicht durchgeführt.
