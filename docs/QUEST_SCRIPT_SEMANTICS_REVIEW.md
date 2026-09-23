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

Acht Sprungziele bleiben ohne gleichnamiges Label im gesamten Quest. Der
vollständige Korpus-Audit hat inzwischen auch die exakten Trigger klassifiziert:

- Quest 85, Finish: `IF VAR1 < 1 GOTO MARK100`
- Quest 108, Finish: `IF VAR1 < 1 GOTO MARK100`
- Quest 229, Finish: `IF VAR1 < 1 GOTO MARK100`
- Quest 416, Finish: `IF VAR1 < 1 GOTO MARK100`
- Quest 2313, Start: `IF VAR1 < 1 GOTO MARK100`
- Quest 60024, Start: `IF RESULT == 2 GOTO MARK2`
- Quest 60102, Start: `IF RESULT == 2 GOTO MARK2`
- Quest 60108, Start: `IF RESULT == 2 GOTO MARK2`

Damit sind fünf Anomalien reale Inventar-voll-Pfade und drei reale
Dialogauswahl-Pfade. Sie dürfen nicht als bloß tote/unreachable Quelldaten
abgehakt werden. Native `CommandRun` beweist für einen fehlenden GOTO-Lookup
einen failure-Rückgabewert; die vollständige ursprüngliche UI-/Caller-Folge ist
nicht weiter aufgelöst.

Für den Emulator ist die Grenze deshalb abgeschlossen, ohne Quelldaten zu
erfinden: ein unresolved/ambiguous Sprung wird geloggt, beendet die lokale
Script-Session und führt keine erfundene Questmutation aus. CI fixiert Quest,
Stage, Ziel und Originalkommando aller acht Fälle. Die genaue native
Fehlerpräsentation bleibt Fidelity-Debt, nicht mehr Runtime-Blocker.

## Spätere Laufzeit-Auflösungen

Seit diesem frühen Semantik-Review wurden mehrere damals offene Punkte direkt gegen das Original geschlossen:

- Textuelles `LINK <QuestID>` ist als native Command-11-Operation belegt und routet anhand des effektiven Zielquest-Status in Start/Action/Finish.
- `RESULT` wird im Dialogpfad aus der tatsächlichen Client-Antwort gesetzt und treibt IF/GOTO wie im Questgraphen vorgesehen.
- `ACCEPT`, `CANCEL`, `SCENARIO`, Item-Operationen, `DONE` im unterstützten Finish/Reward-Pfad und `SET_ABSTATE` besitzen inzwischen explizite Runtime-Implementierungen mit separaten Binary-Audits.

Weiterhin nicht automatisch repariert werden die acht oben aufgeführten fehlenden Labels. Ebenso werden Script-Befehle, die im gelieferten Corpus nicht vorkommen, nicht allein aufgrund ihres nativen Enum-Namens mit erfundenen Textsyntax-Regeln ergänzt.
