# Quest Script Semantics Review – Step 16

## Ergebnis

Die Prüfung der `data_quest_script`-Daten zusammen mit den zugehörigen `data_questdialog`-Texten liefert belastbare Evidenz dafür, dass `StartScript`, `ActionScript` und `FinishScript` separate Einstiegspunkte eines gemeinsamen Quest-Script-Kontexts sind und nicht drei voneinander isolierte Label-Namensräume.

Das ist keine Aussage darüber, wie der originale Server intern die drei Einstiegspunkte speichert. Für die beobachteten Sprünge ist aber ein gemeinsamer Quest-Kontext mit eindeutig auflösbaren Cross-Stage-Labels die bislang konsistenteste Repräsentation.

## Belegte Beispiele

### Wiederholbare Quests

Quests 24, 25, 96, 97, 100, 111, 112, 116, 118 und 228 besitzen im `StartScript` ein `:MARK1` mit `ACCEPT`. Das jeweilige `FinishScript` führt nach `DONE` ein weiteres `SAY` mit Auswahl aus und enthält anschließend `IF RESULT == 1 GOTO MARK1`.

Beispiel Quest 24:

- `Start`: `:MARK1` → `ACCEPT` → `END`
- `Finish`: `DONE` → `SAY 2457 NPC` → `IF RESULT == 1 GOTO MARK1`
- Dialog `2457` bietet ausdrücklich eine Wiederholungsentscheidung an.

Damit ist der Cross-Stage-Sprung zurück zum Start-Einstiegspunkt durch den Dialoginhalt gestützt.

### Quest 60102 / 60108 / 60024

Diese Quests enthalten im `StartScript` `IF VAR1 < 1 GOTO MARK100`. Das Ziel `MARK100` steht im `FinishScript` und führt zu `SAY 108 ME`. Dialog 108 beschreibt ausdrücklich den Fall eines vollen Inventars.

Zusätzlich enthalten die Startskripte `IF RESULT == 2 GOTO MARK2`. `MARK2` existiert in keinem der drei Stage-Felder. Der zugehörige Dialog `9210` besitzt tatsächlich eine zweite Auswahl. Die Folge dieser Auswahl bleibt offen; es wird kein Ersatzlabel erfunden.

## Interpreter-Folgerung

Der Interpreter verwendet deshalb ein Quest-Graph-Modell:

1. Label im aktuellen Stage zuerst.
2. Falls dort nicht vorhanden: gesamter Quest-Kontext.
3. Cross-Stage nur bei eindeutigem Ziel.
4. Mehrdeutige globale Labels werden nicht geraten.
5. Fehlende Ziele bleiben offen.

Der bisherige stage-lokale Parser bleibt als Bestandteil der drei Einstiegspunkte erhalten; der Graph löst den Laufzeitkontrollfluss darüber hinaus auf.

## Offene Datenanomalien

Acht Sprungziele bleiben ohne gleichnamiges Label im gesamten Quest:

- Quest 85 → `MARK100`
- Quest 108 → `MARK100`
- Quest 229 → `MARK100`
- Quest 416 → `MARK100`
- Quest 2313 → `MARK100`
- Quest 60102 → `MARK2`
- Quest 60108 → `MARK2`
- Quest 60024 → `MARK2`

Diese werden nicht automatisch repariert.

## Weiterhin nicht behauptet

- `LINK N` wird nicht als QuestID-Sprung interpretiert.
- `RESULT` wird nur als Ergebniswert modelliert; die Erzeugung des Wertes durch das Netzwerk/Dialogsystem ist noch nicht implementiert.
- `ACCEPT`, `DONE`, Inventar-, Item-, Reward-, Scenario- und AbState-Operationen bleiben Runtime-Kommandos ohne erfundene Seiteneffekte.
