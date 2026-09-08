# Quest Script Interpreter – Schritt 14

Die normalisierte Tabelle `data_quest_script` wird als datengetriebenes Quest-Programm dargestellt. Die Implementierung ist side-effect-free: Kontrollfluss wird ausgewertet, `SAY` und Gameplay-Befehle werden als Schritte exponiert.

Unterstützt werden Labels, `GOTO`, `IF ... GOTO`, `RESULT`/Variablen, `== != < <= > >=`, `END` und `SAY`. Befehle wie `ACCEPT`, `DONE`, `CREATE_ITEM`, `DELETE_ITEM`, `GET_PLAYER_EMPTY_INVENTORY`, `GET_ITEM_LOT`, `SET_ABSTATE`, `SCENARIO` und `LINK` werden nicht semantisch erfunden, sondern als neutrale Commands ausgegeben.

`LINK` wird nicht als QuestID interpretiert. Inventar-/Reward-/Quest-State-Operationen bleiben Sache eines späteren Runtime-Adapters. Die Netzwerk-Anbindung an Handler17 bleibt bewusst aus, bis Dialog-/Branch-Semantik gegen die Mitschnitte bestätigt ist.

Der Corpus-Audit erkennt 2304 Quest-Script-Datensätze, 2986 Labels und keine unbekannten Opcodes. 21 IF/GOTO-Ziele besitzen im jeweiligen Skript kein passendes Label; diese Fälle bleiben offen und werden nicht automatisch repariert.

Quest 1 / Baby Steps enthält `SAY 202 NPC`, `SAY 203 NPC`, `IF RESULT == 1 GOTO MARK1`, `:MARK1`, `ACCEPT`, `END`.
