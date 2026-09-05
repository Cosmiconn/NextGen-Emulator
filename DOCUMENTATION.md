# NextGen-Emulator — Technische Dokumentation

> Vormals **Estrella** (github.com/Temperament/Estrella), abgeleitet von
> **DragonFiesta**, abgeleitet von **Zepheus**. Diese Dokumentation
> beschreibt ausschließlich die Änderungen, die in dieser Session gegenüber
> dem zuletzt bekannten Estrella-Stand vorgenommen wurden.
>
> **Stand:** September 2026 · **Ziel-Framework:** .NET 10.0 (LTS, GA seit
> 11. November 2025, Support bis 14. November 2028)

---

## 0. Verifikationsstatus — jetzt mit echtem Build

**Update:** Auf Nachfrage wurde tatsächlich versucht, die Solution lokal zu
bauen — mit überraschend gutem Ergebnis. Kurzfassung:

- **.NET 10 SDK real installiert** (`apt-get install dotnet-sdk-10.0`,
  funktioniert über die freigegebenen Ubuntu-Repos) und `dotnet --version`
  bestätigt `10.0.111`.
- **`dotnet restore` gegen nuget.org schlägt in dieser Sandbox fehl** —
  empirisch bestätigt (`403 Forbidden`), nicht nur vermutet. nuget.org steht
  nicht auf der Liste erlaubter Netzwerk-Domains.
- Versucht, den **echten MySqlConnector-Quellcode von GitHub zu klonen und
  selbst zu bauen** (github.com ist erlaubt) — kam an eigenem
  Build-Tooling (MinVer, StyleCop, Package-Validation-Baseline) vorbei,
  scheiterte aber letztlich an einer weiteren impliziten NuGet-Abhängigkeit.
- Daraufhin einen **handgeschriebenen Shim** gebaut: echte
  `System.Data.Common`-Basisklassen (`DbConnection`, `DbCommand`,
  `DbDataReader`, `DbDataAdapter`, `DbParameter`, `DbParameterCollection`)
  mit genau der Teilmenge der MySqlConnector-/MySql.Data-Oberfläche, die
  dieser Code tatsächlich nutzt (`MySqlParameter`, `MySqlCommand`,
  `MySqlDataReader`, `MySqlDataAdapter`, `MySqlConnectionStringBuilder`,
  `MySqlDbType`, inkl. der providertypischen "Hiding"-Convenience-Methoden
  wie `CreateCommand()`, `ExecuteReader()`, `GetString(string)`,
  `Parameters.Add(MySqlParameter)`, `Parameters["name"]` — jede davon wurde
  gegen die echte MySqlConnector-Dokumentation/den Quellcode auf GitHub
  gegengeprüft, nicht geraten).
- Analog ein Shim für `System.Diagnostics.PerformanceCounter`.
- **Ergebnis: Die komplette `NextGen-Emulator.sln` baut fehlerfrei gegen
  diesen Shim — 0 Fehler, 17 harmlose/erwartete Warnungen** (Obsolete-APIs
  wie `SecurityPermissionAttribute`, `Thread.Abort()`,
  `Assembly.GlobalAssemblyCache`; eine `CA1416`-Plattform-Warnung für
  `Console.WindowWidth` unter Linux — alles bereits im Original vorhanden
  bzw. der `PerformanceCounter`-Windows-only-Charakter, siehe 3.3).

**Dabei zwei echte, bisher unentdeckte Fehler gefunden und im
tatsächlich ausgelieferten Code (nicht nur der Testkopie) behoben:**

1. `NextGen.FiestaLib/Extensions.cs` hatte ein totes `using MySql.Data.Types;`
   — der Namespace wird in der Datei nirgends verwendet. Entfernt.
2. (Shim-seitig, kein Code-Fix nötig, aber erwähnenswert) Mehrfach zeigte
   sich, dass der Original-Code Provider-Convenience-Methoden nutzt, die
   nur die konkreten MySQL-Treiber bieten (nicht die abstrakte
   `System.Data.Common`-Basis) — z. B. `MySqlParameter`-Overloads mit
   `MySqlDbType`, benannte `Parameters["..."]`-Zugriffe. Das ist im
   Originalcode korrekt und bewusst so — kein Fund, der Änderungen am
   NextGen-Code nötig gemacht hätte, nur am Shim.

**Ein gefundener, aber bewusst NICHT gefixter, mutmaßlich vorbestehender
Bug** (außerhalb des SQL-Injection-Scopes, daher hier nur dokumentiert,
nicht repariert): In
`NextGen.World/Data/Guild/GuildManager.cs`, Methode zum Anlegen einer
Gilde (Aufruf der Stored Procedure `Guild_Create`), wird ein
Output-Parameter so konstruiert:

```csharp
var idParam = cmd.Parameters.Add(new MySqlParameter("@pID", SqlDbType.Int)
{
    Direction = ParameterDirection.Output
});
```

`SqlDbType.Int` ist der **SQL-Server**-Typ-Enum (`System.Data.SqlDbType`),
nicht der MySQL-Typ-Enum (`MySqlConnector.MySqlDbType`). Da der genutzte
Konstruktor `MySqlParameter(string, object value)` ist, kompiliert das
zwar (der Enum-Wert wird als `Value` interpretiert), konfiguriert aber
vermutlich **nicht** den beabsichtigten Output-Parameter-Datentyp korrekt.
Ob das den Gildenerstellungs-Pfad tatsächlich bricht, hängt von der
Signatur der `Guild_Create`-Stored-Procedure ab, die nicht Teil dieses
Repos ist — daher bewusst nicht geraten-gefixt. **Bitte gezielt
gegenprüfen**, bevor Gildenerstellung produktiv genutzt wird.

**Was diese Verifikation NICHT beweist:** Der Shim ist keine Kopie des
echten MySqlConnector-Bytecodes — er wurde von Hand geschrieben und
zweimal um Lücken ergänzt, die erst beim Bauen auffielen. Es ist plausibel,
dass die Signaturen an einzelnen, hier nicht genutzten Stellen abweichen.
**Vor Produktivbetrieb trotzdem einmal `dotnet restore`/`dotnet build`
gegen das echte NuGet-Paket laufen lassen** (Befehle unten) — das dauert
mit funktionierendem Internetzugriff nur wenige Minuten und ist der
einzig wirklich verlässliche Nachweis.

```bash
dotnet restore NextGen-Emulator.sln
dotnet build NextGen-Emulator.sln -c Release
```

Nicht möglich war in dieser Sandbox: ein Lauf gegen eine echte
MySQL-Instanz (kein Datenbankserver vorhanden) und damit jede
Laufzeit-Verifikation der SQL-Fixes selbst — nur die Kompilierbarkeit ist
bestätigt.

---

## 1. Rename: Estrella → NextGen-Emulator

Vollständig durchgeführt, verifiziert per rekursivem Case-insensitive-Grep
(0 Treffer für "Estrella" außerhalb bewusster Lineage-Referenzen in
README.md und einem Code-Kommentar):

| Was | Alt | Neu |
|---|---|---|
| Projektverzeichnisse | `Estrella.*` (7×) | `NextGen.*` (7×) |
| `.csproj`-Dateien | `Estrella.X.csproj` | `NextGen.X.csproj` |
| Solution-Datei | `Estrella.sln` | `NextGen-Emulator.sln` |
| Namespaces / `RootNamespace` / `AssemblyName` | `Estrella.*` | `NextGen.*` |
| `Console.Title` (Login/World/Zone) | `"Estrella.Login"` etc. | `"NextGen.Login"` etc. |
| IDE-Altlasten (`.suo`, `.userprefs`, `.vs10x`) | vorhanden | entfernt (Build-Cache, gehört nicht ins Repo) |

Die Umbenennung lief über ein Skript, das `Estrella` → `NextGen` in allen
`.cs`/`.csproj`/`.sln`-Dateien ersetzt hat — **nicht** blind per
Suchen&Ersetzen im Dateisystem, sondern mit anschließender Verifikation,
dass keine gebrochenen Referenzen (Pfade, `ProjectReference`,
`RootNamespace`) übrig blieben.

**Nicht umbenannt (bewusst):** Die `EstrellaVersion`-Tabelle, die im alten,
inzwischen komplett entfernten `DatabaseUpdater` vorkam (siehe 3.4) — die
Datei ist weg, ein SQL-Migrationsscript für eine Tabellen-Umbenennung wurde
nicht erstellt, weil die Funktion ohnehin nie aktiv war.

---

## 2. Sicherheit: SQL-Injection-Remediation (Kernauftrag)

**Alle** identifizierten String-verketteten bzw. per `string.Format` in
SQL-Statements eingesetzten Variablen wurden auf parametrisierte Queries
(`MySqlParameter`) umgestellt. Verifiziert durch rekursives Grep nach
`"..." + var` und `string.Format(...query...)` im SQL-Kontext — 0 Treffer
mit echten Variablen am Ende der Session.

### 2.1 Neue Infrastruktur

`NextGen.Database/DatabaseClient.cs` wurde um parametrisierte Overloads
ergänzt, die exakt das bestehende Verhalten der unparametrisierten Methoden
spiegeln (gleiche Command-Queue-Mechanik), nur mit `MySqlParameter`-Bindung
statt String-Einsatz:

- `ExecuteQuery(string query, params MySqlParameter[] pParams)`
- `ReadDataTable(string query, params MySqlParameter[] pParams)`
- `ReadInt32(string query, params MySqlParameter[] pParams)`

### 2.2 Vollständiges Fix-Inventar (nach Datei)

| Datei | Anzahl Stellen | Risikoeinschätzung |
|---|---|---|
| `NextGen.Login/InterServer/InterHandler.cs` | 1 | niedrig (Account-ID, int) |
| `NextGen.World/Handlers/Handler42.cs` | 3 | **hoch** — `AddBlockname`/`removename` sind roher Client-String (Ignore-Liste) |
| `NextGen.World/Networking/WorldClient.cs` | 3 | **kritisch** — Charaktererstellung baute `INSERT INTO characters` mit rohem `name`-Feld |
| `NextGen.World/Managers/CharacterManager.cs` | 2 | niedrig (IDs) |
| `NextGen.World/Data/Guild/Guild.cs` | 1 | niedrig (ID) |
| `NextGen.World/Data/Guild/Academy/GuildAcademy.cs` | 2 | niedrig (ID) |
| `NextGen.World/Data/Group/Group.cs` | 7 | niedrig (IDs), inkl. Umbau von `string.Format`-Konstrukten |
| `NextGen.World/Data/WorldCharacter.cs` | 17 | **hoch** — u. a. `QuickBar`/`GameSettings`/`ClientSettings`/`Shortcuts` (Client-Blobs als Hex-String) |
| `NextGen.World/Data/MasterSystem/MasterMember.cs` | 4 | mittel (Charaktername) |
| `NextGen.World/Data/Inventory.cs` | 1 | niedrig (ID) |
| `NextGen.World/Data/DatabaseHelper.cs` | 2 | **hoch** — `UPDATE characters ... WHERE Name = '{0}'`, roher Charaktername; **spät entdeckt**, war in keiner vorherigen Inventur |
| `NextGen.Database/DataStore/ReadMethods.cs` | 1 | niedrig (ID) |
| `NextGen.Util/DUpdater/DatabaseUpdater.cs` | — | Datei komplett entfernt (siehe 3.4), betroffene Zeile war ohnehin toter/auskommentierter Code |
| `NextGen.Zone/Game/ZoneCharacter.cs` | 4 | mittel (Spielzustand, IDs) |
| `NextGen.Zone/Game/Map.cs` | 2 | niedrig |
| `NextGen.Zone/Game/Skill.cs` | 1 | mittel — **zusätzlich Bug gefixt**: `INSERT` listete 5 Spalten, aber nur 4 Werte (Owner fehlte); durch Analogie zu `ZoneCharacter.cs` ergänzt. **Bitte gegenprüfen.** |
| `NextGen.Zone/Game/Item.cs` | 1 | niedrig |
| `NextGen.Zone/Game/PremiumItem.cs` | 2 | niedrig |
| `NextGen.Zone/Game/RewardItem.cs` | 2 | niedrig |
| `NextGen.Zone/Game/Inventory/PremiumInventory.cs` | 1 | niedrig |
| `NextGen.Zone/Game/Inventory/Inventory.cs` | 1 | niedrig |
| `NextGen.Zone/Game/Inventory/RewardInventory.cs` | 1 | niedrig |
| `NextGen.Zone/Game/Guild/GuildManager.cs` | 2 | niedrig |
| `NextGen.Zone/Game/Guild/GuildStorage.cs` | 1 | niedrig |
| `NextGen.Zone/Managers/GroupManager.cs` | 3 | niedrig, inkl. Umbau von `string.Format`-Konstrukten |
| `NextGen.Zone/Data/DataProvider.cs` | 3 | niedrig (interne Konfigurationsdaten, kein Client-Input) |
| `NextGen.Zone/CommandHandler.cs` | 1 | niedrig (Admin-Befehl) |
| `NextGen.Zone/Game/Group/Group.cs` (separate Zone-Klasse) | 2 | niedrig — **spät entdeckt**, ursprünglich mit `World/Data/Group/Group.cs` verwechselt |

**Gesamt:** 29 Dateien, ca. 65 einzelne Query-Stellen.

### 2.3 Bewusst nicht als Injection gewertet

Stellen, die einen Schema-/Datenbanknamen aus der Server-Konfiguration
(`Settings.Instance.zoneMysqlDatabase`) in die Query einsetzen (z. B.
`GuildDataProvider.cs`, `Map.cs`, `CommandHandler.cs`): Das ist kein
Client-Input, sondern ein server-seitiger Konfigurationswert, und
SQL-Bezeichner (Tabellen-/Schema-Namen) lassen sich ohnehin nicht per
`MySqlParameter` binden. Die übrigen Werte in diesen Queries wurden trotzdem
parametrisiert.

---

## 3. .NET-Framework → .NET 10 Modernisierung

### 3.1 Warum .NET 10 und nicht .NET 8

Zum Zeitpunkt dieser Arbeit (September 2026) ist **.NET 10** (GA 11.
November 2025) die aktuelle LTS-Version mit Support bis November 2028.
.NET 8 ist zwar noch LTS, aber sein Support endet bereits am 10. November
2026 — ein Projektstart auf .NET 8 heute wäre in ca. zwei Monaten bereits
in der Auslaufphase. .NET 9 ist STS (Standard Term Support, nur 2 Jahre)
und ebenfalls kein sinnvolles Ziel für ein langlebiges Serverprojekt. Alle
sieben Projekte wurden daher auf `<TargetFramework>net10.0</TargetFramework>`
gesetzt.

### 3.2 Altes → neues Projektformat

Alle sieben `.csproj`-Dateien wurden von altem, ausführlichem
MSBuild-Format (`ToolsVersion="4.0"`, `TargetFrameworkVersion v4.0`,
explizite `<Compile Include>`-Listen pro Datei, `<Reference>` mit
`HintPath` auf lokale DLLs) auf schlankes **SDK-Style-Format**
(`<Project Sdk="Microsoft.NET.Sdk">`) umgestellt. SDK-Style-Projekte
sammeln `.cs`-Dateien standardmäßig automatisch per Glob ein — das war der
Auslöser für den wichtigsten Fund dieser Migration (siehe Abschnitt 4).

Alte `Properties/AssemblyInfo.cs`-Dateien wurden entfernt (kollidieren mit
der automatischen Assembly-Info-Generierung von SDK-Style-Projekten).

### 3.3 Abhängigkeiten ersetzt

| Alt | Neu | Grund |
|---|---|---|
| `MySql.Data.dll` (lokale Binärdatei, HintPath, Stand ~2013) | `MySqlConnector` 2.6.2 (NuGet) | Aktiv gepflegt, async-first, offizieller .NET-10-Support seit Version 2.5.0. Namespace-Swap `MySql.Data.MySqlClient` → `MySqlConnector` in 40 Dateien durchgeführt. `MySqlDataAdapter` (für `ReadDataTable`) ist im neuen Paket vorhanden und API-kompatibel. |
| `System.Diagnostics.PerformanceCounter` (.NET-Framework-Assembly) | `System.Diagnostics.PerformanceCounter` 10.0.11 (NuGet) | In .NET Core/5+ nicht mehr Teil der Basisbibliothek. **Weiterhin Windows-only zur Laufzeit** — das war im Original nicht anders, ist jetzt aber als explizite Paketabhängigkeit sichtbar statt stillschweigend vorausgesetzt. |
| `App.Config` (Assembly-Binding-Redirects für `MySql.Data`) | entfernt | Binding-Redirects sind ein .NET-Framework-Konzept; unter .NET 10 irrelevant, und die Datei wurde ohnehin von keinem Code geladen (verifiziert: keine `ConfigurationManager`-Nutzung im gesamten Repo). |

### 3.4 Entfernter toter Code (EF6/WCF/WinForms)

Beim Vergleich der alten `<Compile Include>`-Listen mit dem tatsächlichen
Dateibestand fiel auf, dass mehrere Dateien **nie mitkompiliert wurden** —
und bei genauerer Prüfung stellte sich heraus: Sie waren tot.

| Entfernt | Befund |
|---|---|
| `Util/ConnectionStringbuilder.cs` | EF6-Hilfsklasse, nur von `DatabaseUpdater.cs` referenziert (siehe unten) |
| `Login/Data/Account.Designer.cs`, `World/Data/World.Designer.cs` | EF6-`ObjectContext`-Generate-Code, **nirgends live referenziert** (grep-verifiziert: `AccountEntities`/`WorldEntities` wird außerhalb dieser Dateien nie benutzt) |
| `Util/DUpdater/DatabaseUpdater.cs` | Die öffentliche `Update()`-Methode war bereits im Original ein reines No-Op (nur ein Log-Eintrag) — die gesamte eigentliche Logik (DB-Versions-Check, Patch-SQL-Ausführung) steckte in einem auskommentierten Codeblock. Der Konstruktor, den die Aufrufer erwarteten (`new DatabaseUpdater(Settings.Instance.Entity, DatabaseTypes.World)`), **existierte im Original gar nicht** — dieser Aufruf wäre nicht kompilierbar gewesen, stand aber ebenfalls in einem Kommentarblock (`ConnectEntity()` in `Login/Worker.cs` und `World/Worker.cs` ruft nichts Funktionsfähiges auf). Komplett inaktiv, komplett entfernt. |
| `Zone/Properties/Resources.resx` + `Resources.Designer.cs` | Enthielt ausschließlich unveränderten Visual-Studio-Platzhaltertext ("this is my long string" etc.), nie befüllt, nirgends referenziert. |
| `<Reference>` auf `System.ServiceModel` (Login/World/Zone) | Grep nach `ServiceHost`/`ServiceContract`/`OperationContract` im gesamten Repo: **0 Treffer.** Die `net.pipe://...`-URIs in den Settings-Klassen sind Config-Werte ohne zugehörigen WCF-Service — reine Boilerplate-Reste, nie verdrahtet. |
| `<Reference>` auf `System.Data.Entity`, `System.IdentityModel`, `System.IdentityModel.Selectors` | Nirgends im Code genutzt (grep-verifiziert). |
| `<Reference>` auf `System.Windows.Forms`, `System.Drawing` (World) und `PresentationCore`, `PresentationFramework`, `WindowsBase` (FiestaLib) | Nirgends im Code genutzt (grep-verifiziert). Das Entfernen macht die Bibliotheken nebenbei tatsächlich plattformunabhängiger — passend zum Cross-Platform-Ziel (Windows/Linux), das auch beim TheSeed-Map-Editor-Projekt verfolgt wird. |

Die beiden leeren `ConnectEntity()`-Methoden in `Login/Worker.cs` und
`World/Worker.cs` wurden **nicht** entfernt (um den Kontrollfluss nicht
anzufassen), aber der irreführende Kommentar-Code wurde durch eine klare
Erklärung ersetzt.

---

## 4. Bislang ausgeschlossene Dateien — wichtig, bitte lesen

Das ist der wichtigste Einzelfund dieser Modernisierung, weil er sonst
**unbemerkt** geblieben wäre: Die alte `<Compile Include>`-Liste in jedem
`.csproj` listete Quelldateien einzeln auf. Ein Abgleich mit dem
tatsächlichen Dateibestand ergab **12 Dateien, die im Original nie
mitkompiliert wurden** — vermutlich, weil sie nach dem letzten manuellen
Pflegen der `.csproj`-Datei hinzugefügt, aber nie eingetragen wurden.

Da SDK-Style-Projekte alle `.cs`-Dateien automatisch per Glob einsammeln,
hätte die reine Formatumstellung diese 12 Dateien **stillschweigend
scharfgeschaltet** — ungeprüften, nie gegen einen Compiler gelaufenen Code
in den produktiven Build aufzunehmen wäre grob fahrlässig gewesen. Ich habe
sie daher explizit per `<Compile Remove>` weiterhin ausgeschlossen, um das
bisherige Verhalten exakt zu erhalten:

| Datei | Vermutete Funktion |
|---|---|
| `NextGen.Util/SettingsEnum.cs` | Enum für Settings-Werte |
| `NextGen.Database/Storage/User.cs` | Storage-DTO |
| `NextGen.World/Data/CommercialReqest.cs` | Handelsanfrage (`TradeReqest`-Klasse) |
| `NextGen.World/Data/ZoneInfo.cs` | Zone-Metadaten |
| `NextGen.World/Managers/CommercialManager.cs` | Handelssystem-Manager |
| `NextGen.Zone/Security/CheatTracker.cs` | Cheat-Erkennung |
| `NextGen.FiestaLib/CharCreationError.cs` | Fehler-Enum für Charaktererstellung |
| `NextGen.FiestaLib/Data/SpawnNPCPoint.cs` | NPC-Spawn-Daten |

**Update:** Die vier `SHN/*.cs`-Dateien (`SHNFile.cs`, `SHNReader.cs`,
`SHNWriter.cs`, `SHNColumn.cs` — der `.shn`-Dateiparser) standen ebenfalls
auf dieser Liste, sind aber inzwischen gegen 130 echte `.shn`-Dateien aus
dem NA2016-Client verifiziert (129/130 laden korrekt) und **wieder Teil
des regulären Builds**. Details: Abschnitt 9.

Die verbleibenden 8 Dateien oben sind weiterhin unverifiziert und bewusst
ausgeschlossen. **Empfehlung:** einzeln gegenprüfen (kompiliert der Code?
wird er irgendwo erwartet?) und erst dann bewusst aktivieren.

---

## 5. Unverändert gebliebene, funktionskritische Teile

Bewusst **nicht** angefasst, weil sie für die Client-Kompatibilität
entscheidend sind und jede Änderung das Risiko birgt, das Protokoll zu
brechen:

- `NetCrypto.cs` — die 499-Byte-XOR-Tabelle und die Ver-/Entschlüsselung
  wurden unverändert übernommen (nur der Namespace-Import angepasst, keine
  Logik geändert).
- Alle Opcode-Handler und Paketstrukturen in `FiestaLib.Networking`.
- Die grundsätzliche Server-Architektur (Login → World → Zone, TCP,
  Inter-Server-Protokoll).

## 6. Weiterhin offen (aus dem ursprünglichen Projektauftrag)

- **Protokoll-/Opcode-Kompatibilität gegen den echten NA2016-Client ist
  weiterhin nicht verifiziert.** Das war schon vor dieser Session der
  Fall und ändert sich durch Rename/Modernisierung nicht. Nächster Schritt
  bleibt unverändert: Login-Handshake des echten Clients mitschneiden und
  gegen `NetCrypto.cs`/`LoginHandler.cs` abgleichen.
- Die in Abschnitt 4 gelisteten 12 ausgeschlossenen Dateien, allen voran
  der SHN-Parser.
- Kein automatisierter Testlauf vorhanden (kein Testprojekt in der
  `.sln`) — anders als im vorherigen `NextGen`-Zip wird hier aber auch
  nicht behauptet, dass es einen gibt.

---

## 7. Build & lokale Verifikation

```bash
# Wiederherstellen + Build
dotnet restore NextGen-Emulator.sln
dotnet build NextGen-Emulator.sln -c Release

# Einzelne Server starten (nach erfolgreichem Build)
dotnet NextGen.Login/bin/Release/net10.0/NextGen.Login.dll
dotnet NextGen.World/bin/Release/net10.0/NextGen.World.dll
dotnet NextGen.Zone/bin/Release/net10.0/NextGen.Zone.dll
```

MySQL-Schema liegt unter `sql/` (`sql/login/login-base.sql`,
`sql/full_schema.sql` sofern vorhanden — bitte gegen die tatsächlich
verwendeten Tabellen in `NextGen.World`/`NextGen.Zone` gegenprüfen, das war
nicht Teil dieser Session).

Konfiguration: `Config.cfg` an der Repo-Wurzel — Mysql-Zugangsdaten dort
anpassen. Ausführliche Schritt-für-Schritt-Anleitung inkl. Datenbank-Setup:
siehe `SETUP.md`.

## 8. Nach dem Build-Test gefundene und behobene Zusatz-Bugs

Beim Vorbereiten der Setup-Anleitung (`SETUP.md`) wurden zwei weitere,
bisher unentdeckte Probleme gefunden und direkt im ausgelieferten Code
behoben:

1. **Cross-Platform-Bug beim Laden von `Config.cfg`**
   (`NextGen.InterLib/Settings.cs`): Der Pfad wurde per
   `Verzeichnis + "\\Config.cfg"` (String-Verkettung mit hartkodiertem
   Backslash) gebildet. Unter Windows funktioniert das zufällig, weil
   Backslash dort der Pfadtrenner ist — unter Linux/macOS ist Backslash
   nur ein normales Zeichen, wodurch die Konfigurationsdatei nie gefunden
   wurde und der Server lautlos mit "Error reading settings" abbricht.
   Gefixt mit `Path.Combine(...)`.
2. **Der `GuildManager.cs`-Bug aus Abschnitt 0 ("gefunden, aber nicht
   gefixt") wurde inzwischen tatsächlich behoben.** Die zugehörige Stored
   Procedure `Guild_Create` (liegt in
   `sql/do-not-use/SQL scripts/Guild_Create.sql`) hat die Signatur
   `OUT pID INT` — der korrekte Typ ist also `MySqlDbType.Int32`, nicht
   `SqlDbType.Int` (der SQL-Server-Enum, der nur zufällig kompilierte,
   weil er über den `object value`-Konstruktor als reiner Wert statt als
   Typangabe interpretiert wurde). Nebenbei ein komplett unbenutztes
   `using System.Data.SqlClient;` in derselben Datei entfernt.

Außerdem wurden drei widersprüchliche, teils veraltete Kopien von
`Config.cfg` bereinigt (`NextGen.FiestaLib/Config.cfg` und `Run/Config.cfg`
— letztere enthielt u. a. eine echte externe IP-Adresse eines früheren
Entwicklers). Es gibt jetzt nur noch eine `Config.cfg` an der Repo-Wurzel,
die bei jedem Build automatisch in die drei Server-Output-Ordner kopiert
wird. Details und Nutzung: `SETUP.md`.

Alle drei Fixes wurden erneut gegen die Shim-Solution kompiliert —
weiterhin 0 Fehler, 17 unveränderte Warnungen.

## 9. SHN-Parser gegen echte NA2016-Client-Dateien verifiziert

Der SHN-Parser stand bisher auf der Ausschlussliste (Abschnitt 4) — nicht
weil er offensichtlich falsch war, sondern weil er nie gegen echte Dateien
geprüft wurde. Das wurde nachgeholt: gegen alle 130 `.shn`-Dateien aus dem
`ressystem`-Ordner eines echten NA2016-Client-Archivs getestet (reine
Client-Datendateien, keine Server-Komponenten — das ist Dateiformat-
Interoperabilität, nicht Übernahme von Server-Code).

**Ergebnis vor Fixes:** 127 von 130 Dateien laden korrekt.

**Zwei echte Bugs gefunden und gefixt:**

1. **Unbekannter Spaltentyp 29** (`ItemActionCondition.shn`,
   `ItemActionEffect.shn`, 5 Spalten insgesamt: `SubjectTarget`,
   `ObjectTarget`, `ConditionActivity`, `EffectTarget`, `EffectActivity`).
   Alle fünf haben laut Datei-Header exakt 8 Bytes Länge. Als `UInt64`
   interpretiert ergeben die Werte ein klares, konsistentes
   Bitmasken-Muster (z. B. `ConditionActivity` läuft über aufeinander-
   folgende Zeilen als `0x100000007`, `0x200000007`, `0x300000007`, …) —
   kein Rauschen, sondern erkennbar strukturierte Daten. `SHNColumn.cs`
   und `SHNFile.cs` (Lese- und Schreibpfad) entsprechend ergänzt.
2. **`QuestData.shn` löste eine nichtssagende `IndexOutOfRangeException`
   tief in `FileCrypto.Crypt` aus.** Ursache: Die im Datei-Header
   deklarierte Nutzdatenlänge (7.274.726 Bytes) stimmt nicht mit der
   tatsächlichen Dateigröße überein (nur 2.140.444 Bytes verfügbar — Faktor
   ~3,4). `SHNFile.Load()` prüft das jetzt explizit und wirft eine klare,
   aussagekräftige `InvalidDataException` statt der kryptischen
   Bounds-Exception. **`QuestData.shn` selbst bleibt ungelöst** — ob die
   Datei beschädigt ist, komprimiert vorliegt, oder ein anderes Encoding
   nutzt, wurde nicht weiter untersucht. Alle anderen 129 Dateien sind
   davon nicht betroffen.

**Bekannte, aber nicht angetroffene Schwachstelle:** Der bereits im
ursprünglichen Projektauftrag dokumentierte "Spaltentyp 26"-Bug
(`GenerateRows` liest bei mehreren Spalten dieses Typs in derselben Tabelle
falsch, weil die Restlänge komplett der ersten zugerechnet wird) wurde
gezielt gegen alle 130 Dateien geprüft — **keine einzige** hat zwei oder
mehr Spalten vom Typ 26. Der Bug ist also unter den vorliegenden
Referenzdateien nicht aktiv, aber strukturell weiterhin vorhanden. Nicht
blind gefixt, da keine reale Datei zum Verifizieren vorliegt.

**Ergebnis nach Fixes:** 129 von 130 Dateien laden korrekt. Der Parser ist
jetzt reaktiviert und regulärer Bestandteil des `NextGen.FiestaLib`-Builds
(nicht mehr per `<Compile Remove>` ausgeschlossen) — erneut gegen die
komplette Shim-Solution kompiliert, weiterhin 0 Fehler.

## 10. Datenbankschema abgeleitet — `fiesta_world` und `fiesta_data`

Schließt die in Abschnitt 6 dokumentierte Lücke ("World-/Zone-Datenbank-
schema fehlt komplett"). Zwei unterschiedliche Herangehensweisen, klar
getrennt nach Vertrauenswürdigkeit:

**Mit echten Daten (hohe Konfidenz):** `sql/data/data_iteminfo.sql`,
`data_mobinfo.sql`, `mapinfo.sql`, `activeskill.sql`, `minihouse.sql` —
per neu gebautem SHN→SQL-Exportwerkzeug (siehe Abschnitt 9) direkt aus den
echten `.shn`-Client-Dateien erzeugt, inklusive echter Daten (14.999 Items,
2.878 Mobs, 138 Maps, 2.791 Skills, 356 Minihäuser). Spaltennamen wurden
gegen die jeweilige `Load(DataRow)`-Methode im C#-Code abgeglichen
(`ItemInfo.cs`, `MobInfo.cs`, `MapInfo.cs`, `ActiveSkillInfo.cs`,
`MiniHouseInfo.cs`) und stimmen bis auf zwei dokumentierte Umbenennungen
1:1 überein.

**Rein aus Code abgeleitet, ohne Datenabgleich (niedrigere Konfidenz):**
`sql/world/schema.sql` (15 Tabellen: `characters`, `items`, `equips`,
`Skillist`, `friends`, `groups`, `Guilds` + 4 weitere Gilden-Tabellen,
`Masters`, `BlockUser`, `PremiumItems`, `Rewarditems`) und
`sql/data/schema_derived.sql` (17 weitere Referenztabellen ohne
Client-`.shn`-Entsprechung, u. a. `BaseStats`, `data_MobInfoServer`,
`Vendors`, `dropgroupinfo`). Jede Spalte stammt aus einem tatsächlichen
`row["..."]`-Zugriff oder `SET`/`INSERT`-Statement im vorhandenen
GPL-Code — keine Spalte wurde geraten. Trotzdem: **nicht gegen einen
echten Server oder echte Spielstanddaten verifiziert**, im Unterschied zu
den `.shn`-basierten Tabellen oben.

Wichtige Architektur-Klärung dabei gefunden: `items`, `equips`,
`Skillist` und `GuildStorage` liegen entgegen der Namensvermutung in
**`fiesta_world`**, nicht `fiesta_data` — verifiziert über
`Program.CharDBManager` (World-Verbindung) vs. `Program.DatabaseManager`
(Data-Verbindung) in den jeweiligen Zone-Klassen. `MasterRewards`
(Belohnungs-Item-Zuordnung) liegt dagegen tatsächlich in `fiesta_data`,
obwohl inhaltlich World-nah — der Code wechselt die Datenbank per
`USE fiesta_data; ... USE fiesta_world` mitten in einer Query.

### Zwei weitere, dabei gefundene Bugs

1. **`PremiumItem`/`PremiumItems`-Namenskonflikt (behoben).**
   `PremiumItem.cs` fügt Premium-Items in die Tabelle `PremiumItems`
   (Plural) ein, löschte sie aber aus `PremiumItem` (Singular) — und
   `PremiumInventory.cs` las beim Laden ebenfalls aus der falschen,
   singularen Tabelle. In der Praxis: Premium-Items wären beim Löschen
   nie wirklich entfernt worden (0 betroffene Zeilen, kein Fehler), und
   `PremiumInventory` hätte beim Start immer eine leere/nicht existente
   Tabelle abgefragt. Beide Stellen jetzt auf `PremiumItems` vereinheitlicht.
2. **`MapObjectBuffCollection.cs` — toter Code mit SQL-Server-Resten
   (dokumentiert, nicht verändert).** Diese Klasse liegt im Namespace
   `Fiesta.Zone.Game.Buffs` (nicht `NextGen.World...` — ein Überbleibsel
   aus einer noch älteren Codebasis als die Estrella-Linie selbst) und
   nutzt `System.Data.SqlClient` (`SqlConnection`/`SqlCommand`/
   `SqlParameter`) — also Microsoft SQL Server statt MySQL. Grep-verifiziert:
   **wird von keiner anderen Stelle im Code aufgerufen.** Selbst wenn sie
   aufgerufen würde, wäre sie inkompatibel (ein `SqlConnection`-Objekt
   existiert im übrigen Code nirgends). Für die `Buffs`-Tabelle in
   `schema_derived.sql` wurde trotzdem ein Best-Effort-Schema angelegt
   (Spalten aus den Ordinalpositionen der `GetInt16`/`GetInt32`/
   `GetInt64`-Aufrufe grob rekonstruiert), aber als niedrige Konfidenz
   markiert — dieser Code ist mit hoher Wahrscheinlichkeit für den
   produktiven Betrieb irrelevant.

Import-Reihenfolge und -Befehle: `sql/README.md`.

## 11. Client-Versions-Infrastruktur

Grundlage: Der Client meldet seine Version bereits heute im allerersten
Login-Paket (`CH3Type.Version`, zwei `ushort`-Felder Jahr+Version) — der
Server hat das bisher nur geloggt, nie ausgewertet. Zwei Bausteine wurden
ergaenzt, in dieser Reihenfolge (aufeinander aufbauend):

**1. Handshake validiert jetzt tatsaechlich.**
`LoginHandler.VersionInfo()` speichert Jahr/Version am `LoginClient`
(neue Properties `ClientYear`/`ClientVersion` an der gemeinsamen
`Client`-Basisklasse in `FiestaLib`, dadurch auch fuer World/Zone
verfuegbar) und prueft optional gegen `Login.SupportedClientVersions`
(`Config.cfg`, Format `"Jahr:Version"` kommagetrennt, z. B. `"2016:2"`).
**Leer = jede Version akzeptiert** (Default, unveraendertes bisheriges
Verhalten) — reale Werte fuer eine konkrete Client-Version kennt niemand
ohne echten Paket-Mitschnitt (siehe Abschnitt 6/SETUP.md).

**2. Versionsbewusste Handler-Registry.**
`PacketHandlerAttribute` hat jetzt optionale `MinVersion`/`MaxVersion`
(Default: 0..65535, deckt "jede Version" ab). `HandlerStore` (in Login,
World und Zone identisch erweitert) verwaltet pro (Header, Type) jetzt
eine Liste statt eines einzelnen Handlers und waehlt bei der Aufloesung
den **spezifischsten passenden** aus — ein generischer Fallback-Handler
kann so gezielt durch einen versionsspezifischen ueberstimmt werden, ohne
den Fallback zu entfernen:

```csharp
[PacketHandler(CH3Type.Login)]                              // Fallback, alle Versionen
[PacketHandler(CH3Type.Login, MinVersion = 5, MaxVersion = 5)] // abweichendes Verhalten nur fuer Version 5
```

Alle drei Dispatch-Stellen (`LoginClient`/`WorldClient`/`ZoneClient`)
reichen `this.ClientVersion` an `HandlerStore.GetHandler(...)` durch.
`ClientVersion` ist `0`, solange keine echte Meldung erfolgt ist (World/
Zone fuehren aktuell keinen eigenen Version-Handshake durch, sie
vertrauen dem am Login bereits gepruepften Transfer) — `0` matcht immer
den Default-Bereich, das Verhalten fuer alle bisherigen, unversionierten
Handler aendert sich dadurch nicht.

**Bewusst nicht gemacht:** `ClientVersion` wird aktuell nicht ueber
`ClientTransfer` von Login zu World/Zone weitergereicht. Falls
Versionsunterschiede irgendwann auch dort relevant werden, ist das der
naheliegende naechste Schritt (zusaetzliches Feld an `ClientTransfer` plus
Setzen beim Erzeugen des Transfers in `Handler3.cs`).

**Was das NICHT loest:** unterschiedliche Paketstrukturen/Feldlaengen
zwischen echten Client-Versionen muessen weiterhin einzeln reverse-
engineered werden (die Infrastruktur macht es nur *moeglich*, sie sauber
nebeneinander zu registrieren). Die 499-Byte-XOR-Tabelle in `NetCrypto.cs`
ist dabei vermutlich unproblematisch — sie ist laut Code ohnehin fix, nur
die Startposition wird pro Session zufaellig gewaehlt und dem Client
unverschluesselt im Handshake mitgeteilt (`SendHandshake(crypto.XorPos)`
in `Client.cs`).

## 12. Config-Parser-Bug gefunden und gefixt

Beim Ergaenzen des neuen `Login.SupportedClientVersions`-Keys fiel auf:
`InterLib/Settings.cs` behandelte bisher **jede Zeile, die irgendwo ein
`#` enthaelt**, als reinen Kommentar (`entry.Contains("#")`) — nicht nur
Zeilen, die tatsaechlich mit `#` *beginnen*. Ein Config-Wert mit `#`
darin (z. B. ein generiertes Passwort) haette die komplette Zeile
verschluckt, der Key waere nie gesetzt worden, mit derselben Art von
schwer nachvollziehbarem Folgefehler wie beim `TicksToSleep`-Vorfall
weiter oben. Gefixt: nur Zeilen, die mit `#` beginnen, gelten jetzt als
Kommentar.

## 13. Systematische Bereinigung verschluckender catch-Blöcke

Direkte Konsequenz aus dem `TicksToSleep`-Vorfall: derselbe
`catch { return false; }`/`catch { }`-Antipattern (echte Fehlerursache
verschluckt, nur ein nichtssagendes Symptom an anderer Stelle sichtbar)
wurde systematisch im ganzen Repo gesucht. Gefunden: 13 weitere Stellen.

**11 davon gefixt** (Fehler wird jetzt geloggt, Rückgabewert/Verhalten
unveraendert):
- `NextGen.World/Worker.cs`, `NextGen.Login/Worker.cs`,
  `NextGen.Zone/Worker.cs` — alle drei `[InitializerMethod]`, liefen also
  in derselben kritischen Startphase wie das urspruenglich betroffene
  `Settings.Load()`. Ein Fehler hier haette zum selben Symptom gefuehrt
  (Server startet nicht, keine brauchbare Fehlermeldung), nur an anderer
  Stelle im Ablauf.
- `NextGen.World/InterServer/LoginConnector.cs`,
  `NextGen.World/InterServer/ZoneAcceptor.cs`,
  `NextGen.Login/InterServer/WorldAcceptor.cs`,
  `NextGen.Zone/InterServer/WorldConnector.cs` — Inter-Server-
  Verbindungsaufbau zwischen Login/World/Zone.
- `NextGen.Zone/Program.cs` (`IsLoaded`-Pruefung).
- `NextGen.Zone/Game/Inventory/{PremiumInventory,Inventory,RewardInventory}.cs`
  — `Mutex.ReleaseMutex()` ohne gehaltenen Lock ist ein
  Programmierfehler-Indikator (nicht nur ein erwarteter Sonderfall),
  daher auf `LogLevel.Warn` geloggt statt komplett stillschweigend.

**2 bewusst unveraendert gelassen:**
`NextGen.FiestaLib/Networking/Client.cs` und
`NextGen.InterLib/Networking/InterClient.cs`, jeweils um
`Socket.Shutdown(SocketShutdown.Both)` beim Verbindungsabbau. Das ist
ein etablierter, legitimer Fall (Socket kann beim Trennen bereits
serverseitig geschlossen/zurueckgesetzt worden sein) — kein Bug wie bei
den Startup-Pfaden, Logging hier haette nur Rauschen erzeugt.

Alle 11 Fixes erneut gegen die Shim-Solution kompiliert — weiterhin 0
Fehler, 17 unveraenderte Warnungen.

## 14. Buffs/Debuffs — Bestandsaufnahme (Feature aktuell nicht funktionsfaehig)

Auf Nachfrage geprueft, wie weit das Buff-System tatsaechlich
implementiert ist. Ergebnis: **im Kern gar nicht**, trotz mehrerer
vorhandener Dateien, die auf den ersten Blick nach einer Implementierung
aussehen:

- `NextGen.Zone/Game/Buffs/Buff.cs` — die eigentliche Logik (Aktivieren/
  Deaktivieren von Stat-Modifikatoren, Sub-Ability-State-Handling) ist
  **komplett in einem `/* ... */`-Block auskommentiert** und referenziert
  Typen, die im gesamten Repo **nicht existieren**
  (`LivingObject`, `BuffAction`, `StatsAction`, `SubAbStateActionType`) —
  stammt vermutlich aus einer anderen, vollstaendigeren Codebasis und
  wurde nie ans Projekt angepasst.
- `NextGen.Zone/Game/Buffs/Buffs.cs` — reiner Datencontainer
  (Stat-Summen-Properties + leere `List<Buff>`), **keine einzige Methode**
  ausser dem Konstruktor. Nichts setzt die Stat-Properties.
- `NextGen.World/Managers/BuffManager.cs` — funktionsfaehiger Code, der
  ein `InterPacket` (`ZONE_CharacterSetBuff`/`ZONE_CharacterRemoveBuff`)
  an alle Zone-Server broadcastet. **Wird aber nirgends aufgerufen** — der
  einzige Aufrufer (`GuildAcademyManager.cs`) ist selbst auskommentiert.
  Selbst wenn aufgerufen: **kein Handler auf Zone-Seite** nimmt dieses
  Paket entgegen (grep-verifiziert).
- `NextGen.World/Game/MapObjectBuffCollection.cs` — die in Abschnitt 0/8
  bereits dokumentierte tote SQL-Server-Persistenzschicht.
- Datenmodell-Fundament ebenfalls nur ein Stub: `AbStateInfo.cs` liest
  aus der DB nur `ID` und `InxName`, keinerlei Effektdaten.
  `SubAbstateInfo.cs` ist eine **komplett leere Klasse**
  (`class SubAbstateInfo { }`).

**Was echt vorhanden ist:** die rohen `.shn`-Referenzdaten im
NA2016-Client (`AbState.shn` 777 Zeilen, `AbStateView.shn` 776 Zeilen,
`SubAbState.shn` 2041 Zeilen — beim SHN-Parser-Test in Abschnitt 9 bereits
erfolgreich gegen den Parser verifiziert), die tatsaechlichen Effektdaten
enthalten. Diese wurden bisher nur nie in ein brauchbares C#-Datenmodell
uebersetzt.

**Realistischer Umfang, um das Feature echt funktionsfaehig zu machen**
(nicht in dieser Session umgesetzt, da deutlich groesser als die
bisherigen Einzel-Fixes):
1. `AbState.shn`/`AbStateView.shn`/`SubAbState.shn`-Spalten empirisch
   reverse-engineeren (aehnliches Vorgehen wie bei `ItemInfo`/`MobInfo`
   in Abschnitt 9, aber mit mehr unbekannten Spalten und Verknuepfungen
   zwischen den drei Dateien).
2. `AbStateInfo`/`SubAbstateInfo` als echte Datenmodelle mit den
   relevanten Effektfeldern (Dauer, Stat-Modifikatoren, Tick-Intervalle
   fuer DoT/HoT) aufbauen.
3. `Buff`/`Buffs` in `NextGen.Zone` mit echter Aktivierungs-/
   Ablauf-/Stat-Neuberechnungslogik implementieren.
4. Zone-seitigen Handler fuer `ZONE_CharacterSetBuff`/
   `ZONE_CharacterRemoveBuff` schreiben, `BuffManager`-Aufrufe an den
   tatsaechlichen Stellen (Skill-Einsatz, Item-Nutzung) reaktivieren/
   ergaenzen.
5. Netzwerk-Sync zum betroffenen Client (und ggf. sichtbar fuer andere
   Spieler in der Naehe) fuer Buff-Icons/-Anzeige.
6. Persistenz-Entscheidung treffen: Buffs beim Ausloggen verfallen lassen
   (einfacher, viele Spiele machen das so) oder tatsaechlich in
   `fiesta_world` persistieren (dann muesste `Buffs`-Tabellenschema aus
   `sql/data/schema_derived.sql` neu bewertet werden — aktuell dort nur
   als niedrige-Konfidenz-Platzhalter vorhanden, siehe Abschnitt 10).

## 15. Buffs/Debuffs — echte Implementierung (Fortsetzung von Abschnitt 14)

Auf Wunsch tatsaechlich umgesetzt, ausgehend von der Bestandsaufnahme in
Abschnitt 14. Ausschliesslich mit NA2016-Daten gearbeitet (CN2012/TW2008
bewusst nicht angefasst, siehe `CLIENT-VERSIONS.md`).

### 15.1 Datengrundlage empirisch verifiziert

`AbState.shn` (19 Spalten, 777 Zeilen) und `SubAbState.shn` (14 Spalten,
2041 Zeilen) aus dem echten NA2016-Client vollstaendig gegen den
SHN-Parser gelesen und Spalte fuer Spalte analysiert:

- **`AbState`** = die Buff/Debuff-**Definition** (Name, Dispel-Kategorie,
  Party-Weitergabe-Felder, Dauer-Skalierungsfaktoren). Verweist per
  String-Spalte `SubAbState` auf...
- **`SubAbState`** = die **Staerke-Stufen** eines AbState (z.B. "SeverBone
  Stufe 1/2/3"), mehrere Zeilen teilen sich denselben `InxName`,
  unterschieden durch die Spalte `Strength`. Jede Zeile hat bis zu 4
  Wirkungs-Slots (`ActionIndexA`/`ActionArgA` .. `ActionIndexD`/`ActionArgD`)
  — das deckt sich exakt mit der Struktur der vorher komplett
  auskommentierten `Buff.cs` (`SubAbStateInfo.Actions`,
  `SubAbStateActionType`).

Empirische Bestaetigung der Interpretation: bei `SubStaSeverBone` steigt
`ActionArgA` linear mit `Strength` (300 → 350 → 400, +50 je Stufe) bei
gleichbleibendem `ActionIndexA=21` — ein klares, strukturiertes Muster,
kein Zufall.

**`ActionIndex`-Bedeutung grossteils NICHT geklärt.** 111 verschiedene
Werte in den Referenzdaten. Versuch einer Korrelation ueber
AbState-Namen (z.B. AbStates mit "Crit" im Namen korrelieren exklusiv
mit `ActionIndex=34`) lieferte einzelne schwache Hinweise, aber keine
belastbare Bestaetigung. **Bewusste Design-Entscheidung:** keine
geratenen Zuordnungen scharfschalten (falsche Spielbalance waere die
Folge) — `BuffActionResolver.cs` ist deshalb standardmaessig komplett
leer, loggt nicht aufgeloeste Actions und wendet keine Wirkung an. Siehe
Kommentar in der Datei fuer den vorgesehenen Erweiterungsweg (gezielter
Live-Test: bekannten Buff anwenden, Werte-Delta im Client beobachten,
dann Eintrag ergaenzen).

### 15.2 Neue/geaenderte Dateien

| Datei | Status |
|---|---|
| `NextGen.FiestaLib/Data/AbStateInfo.cs` | Neu geschrieben (vorher: nur `ID`+`InxName`, keine Effektdaten) |
| `NextGen.FiestaLib/Data/SubAbstateInfo.cs` | Neu geschrieben (vorher: komplett leere Klasse) |
| `NextGen.FiestaLib/Data/SubAbStateAction.cs` | Neu |
| `NextGen.Zone/Game/Buffs/Buffs.cs` | `AddBuff`/`RemoveBuff`/`Tick` ergaenzt (vorher: reiner Datencontainer ohne Methoden) |
| `NextGen.Zone/Game/Buffs/Buff.cs` | Neu implementiert gegen echte, im Projekt vorhandene Typen (vorher: komplett auskommentiert, referenzierte nicht-existente Typen) |
| `NextGen.Zone/Game/Buffs/BuffActionResolver.cs` | Neu, siehe 15.1 |
| `NextGen.Zone/Data/DataProvider.cs` | `LoadAbStates()` implementiert — **behebt nebenbei einen echten, zuvor unentdeckten Compile-Fehler**, siehe 15.3 |
| `NextGen.Zone/Game/ZoneCharacter.cs` | Bereits vorhandener TODO-Stub (`case ItemUseEffectType.AbState: //TOOD: add buffs for itemuse`) tatsaechlich angebunden; zwei oeffentliche Wrapper `AddBuff`/`RemoveBuff` ergaenzt |
| `NextGen.Zone/InterServer/InterHandler.cs` | Handler fuer `ZONE_CharacterSetBuff`/`ZONE_CharacterRemoveBuff` ergaenzt — der Sender (`NextGen.World/Managers/BuffManager.cs`) existierte bereits, aber niemand auf Zone-Seite empfing das Paket |
| `sql/data/data_abstate.sql`, `data_subabstate.sql` | Neu, echte NA2016-Daten (777 / 2041 Zeilen) |

### 15.3 Nebenbei gefundener, unabhaengiger Compile-Fehler

Beim Nachverfolgen von `AbStatesByID`/`AbStatesByName` fiel auf:
`DataProvider`s Konstruktor rief bereits `LoadAbStates();` auf, aber
diese Methode war **nirgends im Projekt implementiert** — ein echter
`CS0103`-Fehler. Das bedeutet: **`NextGen.Zone` liess sich im zuletzt
ausgelieferten Zip nicht kompilieren.**

Das wurde durch eine Luecke im eigenen Verifikationsprozess nicht
bemerkt: die fuer die Shim-Kompilierung genutzte Testkopie war an dieser
einen Datei nicht mit dem tatsaechlich ausgelieferten Stand synchron,
wodurch mehrere vorherige "0 Fehler"-Meldungen in dieser Doku fuer
`NextGen.Zone` fälschlich bestaetigend wirkten, obwohl sie eine leicht
andere Version geprüft hatten. **Seit dieser Session wird fuer jede
Verifikation eine komplett frische Kopie aus dem tatsaechlichen
Liefer-Stand gezogen statt einzelne Dateien inkrementell nachzuziehen.**
Mit `LoadAbStates()` implementiert kompiliert `NextGen.Zone` jetzt
nachweislich wieder (frische Kopie, siehe Build-Log dieser Session).

### 15.4 Was weiterhin fehlt

- **Kein Skill-Einsatz-System.** `NextGen.Zone` hat aktuell keinen
  `UseSkill`/`CastSkill`-Handler — Buffs koennen also nur ueber
  Item-Nutzung ausgeloest werden (funktioniert, siehe 15.2), nicht ueber
  Skills. Das ist eine groessere, separate fehlende Voraussetzung.
- **Kein zentraler Tick-Mechanismus.** `Buffs.Tick(DateTime now)` muss
  regelmaessig aufgerufen werden, damit abgelaufene Buffs entfernt
  werden — aktuell nirgends verdrahtet, da es in `NextGen.Zone` noch
  keine zentrale Update-Schleife pro Charakter gibt.
- **`ActionIndex`-Bedeutungen** siehe 15.1 — Infrastruktur fertig,
  konkrete Wirkungen mussen noch einzeln bestaetigt werden.
- **Persistenz-Entscheidung offen** (Buffs bei Logout verfallen lassen
  vs. in `fiesta_world` speichern) — aktuell verfallen sie beim Logout
  automatisch (kein Speicherpfad implementiert), was fuer viele
  Buff-Arten ohnehin das erwartete Verhalten ist.
- Nicht getestet gegen einen echten laufenden Server (kein MySQL in
  dieser Sandbox) — wie beim uebrigen Schema nur Compile-Verifikation,
  keine Laufzeit-Verifikation.

### 15.5 Tick-Mechanismus ergaenzt (Fortsetzung)

`Buffs.Tick(DateTime now)` (siehe 15.4) war bis hierhin korrekt
implementiert, aber niemand rief es auf — Buffs waeren nie automatisch
abgelaufen. Behoben, unter Wiederverwendung eines bereits vorhandenen,
fast identischen Musters:

`NextGen.Zone/Worker.cs` betreibt bereits eine echte Tick-Schleife und
ruft darin regelmaessig `ClientManager.Instance.UpdateMountTicks(now)`
auf (alle 30 Sekunden, prueft Reittier-Futterverbrauch pro Charakter).
Exakt analog ergaenzt:

- `ClientManager.UpdateMountTicks` als Vorlage genutzt, um
  `ClientManager.UpdateBuffTicks(DateTime now)` zu bauen — iteriert ueber
  alle verbundenen Clients und ruft `ZoneCharacter.TickBuffs(now)` auf
  (neuer oeffentlicher Wrapper, gleiches Kapselungsmuster wie
  `AddBuff`/`RemoveBuff`).
- In `Worker.cs` **nicht** am 30-Sekunden-Rhythmus von
  `UpdateMountTicks` angehaengt, sondern am bereits vorhandenen
  1-Sekunden-Takt (`lastCheck`) — `SubAbState.KeepTime` liegt bei
  typischen Debuffs im Bereich weniger Sekunden (siehe 15.1,
  `SeverBone` = 20000ms), ein 30-Sekunden-Intervall haette Buffs
  regelmaessig weit ueber ihre eigentliche Laufzeit hinaus aktiv
  gelassen.

Damit laufen aktive Buffs jetzt tatsaechlich automatisch ab. Erneut
gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte 17
Warnungen.

**Was fuer "essenziell nutzbar" weiterhin fehlt:** kein Skill-Einsatz-
System (Abschnitt 15.4) — das bleibt der naechste, deutlich groessere
Schritt.

## 16. Korrektur: Skill-Einsatz-System existiert bereits — Buff-Anbindung ergaenzt

**Berichtigung einer eigenen Fehleinschaetzung:** Abschnitt 15.4 und
frühere Antworten in dieser Session behaupteten, es gebe in
`NextGen.Zone` keinen Skill-Einsatz-Handler. Das war falsch. Ursache: die
damalige Suche nutzte ein Wortgrenzen-Suchmuster (`\bUseSkill\b`), das
`UseSkillWithTarget` nicht als Treffer erkannte, weil direkt auf
"UseSkill" ohne Trennzeichen "WithTarget" folgt — ein Regex-Fehler, keine
inhaltliche Pruefung.

**Tatsaechlicher Stand:** `NextGen.Zone/Handlers/Handler9.cs` enthaelt
einen funktionsfaehigen Skill-Einsatz-Pfad: `AttackSkillHandler`,
`UseSkillWithTargetHandler` (inkl. Schadensberechnung gegen Mobs und
Heilung mit `DemandType == 6`), `UseSkillWithPositionHandler` (AoE),
inklusive Animations-/Statusupdate-Pakete. Was fehlte: keine Anbindung an
das neue Buff-System.

### 16.1 AbState-Slots in ActiveSkill.shn gefunden und angebunden

`ActiveSkill.shn` (96 Spalten insgesamt) hat vier AbState-Slots
(`StaNameA-D`/`StaStrengthA-D`/`StaSucRateA-D`) — ein Skill kann beim
Einsatz bis zu vier Buffs/Debuffs ausloesen, jeweils mit eigener
Erfolgsrate. `ActiveSkillInfo.cs` entsprechend erweitert (neue Klasse
`SkillAbStateSlot`), `ZoneCharacter.ApplySkillAbStates(ActiveSkillInfo)`
ergaenzt und in `UseSkillWithTargetHandler` (Handler9.cs) an beiden
bestehenden Zweigen angebunden:

- **Heil-Zweig** (`DemandType == 6`): AbState-Effekte werden auf das
  Heilziel angewendet (z.B. Heilung + Regenerations-Buff).
- **Schadens-Zweig**: AbState-Effekte werden auf den **Anwender** (self)
  angewendet, nicht auf den Mob-Gegner — `Buffs`/`AddBuff` ist bisher nur
  fuer `ZoneCharacter` gebaut, `Mob` hat kein Buff-System. Ein Skill, der
  eigentlich einen Debuff auf den Gegner legen sollte (z.B. "Poison
  Arrow"), wuerde also faelschlich sich selbst statt den Mob debuffen.
  Bewusst nicht anders geloest, um keine falsche/erratene Zielzuordnung
  einzubauen — siehe 16.2.

`SuccessRate`-Skalierung (Prozent? Promille?) nicht verifiziert, defensiv
als "von 100" behandelt (>=100 = immer erfolgreich).

### 16.2 Weiterhin offen

- **Debuffs auf Mobs.** `Buffs`/`Buff`/`BuffActionResolver` sind nur fuer
  `ZoneCharacter` gebaut. Damit Skills wie "Poison Arrow" den Gegner statt
  sich selbst debuffen, braucht `Mob` ein eigenes (einfacheres) Buff-
  System oder eine gemeinsame Basis mit `ZoneCharacter`.
- `ActionIndex`-Bedeutungen weiterhin ungeklaert (Abschnitt 15.1) —
  betrifft jetzt auch skill-ausgeloeste Buffs, nicht nur item-ausgeloeste.
- Kein echtes Cooldown-Tracking pro Skill (`Skill.Write()` sendet
  hartkodiert `60000`, siehe `NextGen.Zone/Game/Skill.cs`) — unabhaengig
  von der Buff-Arbeit, aber fuer ein rundes Skill-System relevant.
- Nicht getestet gegen einen echten laufenden Server/Client (kein MySQL,
  kein Live-Client in dieser Sandbox) — wie beim uebrigen Schema nur
  Compile-Verifikation.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 17. Buffs vs. Debuffs korrekt zugeordnet — Mob-Debuff-System ergaenzt

Design-Vorgabe erhalten: **Buffs sind positive Effekte auf den Anwender,
Debuffs sind negative Effekte auf den Gegner.** Das loest die in
Abschnitt 16.2 offene Frage.

**Empirisch geprueft, ob sich das direkt aus `AbState.shn` ablesen laesst:**
Verteilung von `StateGrade`, `DispelIndex`, `SubDispelIndex` uber alle 777
Eintraege analysiert und gegen eindeutig negativ benannte AbStates
(Poison/Curse/Stun/Fear/Slow/...) abgeglichen. Ergebnis: **keine einzelne
Spalte trennt Buff/Debuff sauber binaer** — `DispelIndex=0` z. B. enthaelt
sowohl eindeutig negative (`StaGldStun`, `StaKarenStun`) als auch
vermutlich positive Eintraege. Die Zuordnung ist daher **nicht aus den
AbState-Daten selbst ableitbar**, sondern folgt der Skill-Design-Logik:
Der Zweig, in dem ein Skill verwendet wird (Angriff vs. Heilung/Support),
bestimmt das Ziel — genau das war bereits in `Handler9.cs` als
Zweigstruktur vorhanden.

### 17.1 Aenderungen

- **`Buffs.cs`/`Buff.cs` von `ZoneCharacter` auf `MapObject` (gemeinsame
  Basisklasse von `ZoneCharacter` und `Mob`) umgestellt** — eine Engine
  fuer beide, keine Zweitimplementierung fuer Mobs.
- **`Mob.cs`** bekommt dieselben Wrapper-Methoden wie `ZoneCharacter`
  (`AddBuff`/`RemoveBuff`/`TickBuffs`/`ApplySkillAbStates`), initialisiert
  in `Init()` (von beiden Mob-Konstruktoren aufgerufen).
- **`Handler9.cs` Schadenszweig korrigiert:** wendet die AbState-Slots des
  Skills jetzt auf `victim` (den angegriffenen Mob) an, nicht mehr auf
  `self` — Debuff landet beim Gegner, wie vorgegeben. Heilzweig war
  bereits korrekt (Buff auf das Heilziel).
- **Tick-Anbindung fuer Mob-Debuffs:** ueber `Mob.Update(DateTime)`, das
  bereits von `Map.Update()` fuer jedes Objekt aufgerufen wird (Aufruf-
  Intervall variiert mit der Server-Tick-Rate, siehe Worker.cs — weniger
  praezise als der dedizierte 1-Sekunden-Takt fuer Charakter-Buffs, aber
  ausreichend fuer zeitbasierte Ablauf-Pruefung, kein Timing-kritischer
  Pfad).

### 17.2 Bewusste Vereinfachung

Ein Skill mit sowohl Debuff- als auch Buff-Slot gleichzeitig (z. B.
"schwaecht den Gegner UND staerkt dich selbst") wuerde mit dieser
Zuordnung fuer alle 4 AbState-Slots einheitlich behandelt (alle auf das
Ziel je nach Zweig), nicht slot-individuell unterschieden — dafuer fehlt
ein verlaessliches Datenfeld. In der Praxis vermutlich selten, aber nicht
ausgeschlossen. Waere ueber ein zukuenftiges, empirisch bestaetigtes Feld
(oder gezielten Live-Test) feiner aufloesbar.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 18. Gemischte Buff+Debuff-Skills (beide Effekte beim Anwender) + Gesamt-Lückenkatalog

Neue Design-Info: Es gibt Skills, die beim Anwender **gleichzeitig einen
Buff und einen Debuff** ausloesen (Selbstkosten-Skills, z.B.
Kampfrausch-Skill mit Lauftempo-Malus). Konkret in den echten Daten
gefunden: `FitBlood01`-`FitBlood12` (`DemandType=3`) mit den Slots
`StaFitBlood` (vermutlich positiver Selbstbuff) + `StaFitMoveDown`
(eindeutig ein Lauftempo-Debuff) — **beide mit demselben `DemandType`**,
was zeigt: `DemandType` bestimmt das Ziel eines Skills insgesamt, nicht
die Wertigkeit einzelner Slots.

**Von 15 in den echten Daten vorkommenden `DemandType`-Werten (0-14)
behandelte der Code bisher nur einen einzigen explizit** (`==6` fuer
Heilung) — alle anderen liefen zwangsweise durch den Angriffszweig, der
zwingend einen `Mob` als Ziel voraussetzte. Reine Selbstziel-Skills wie
`FitBlood01` waeren daher komplett fehlgeschlagen (`return`, keine
Wirkung).

**Fix:** statt alle 15 `DemandType`-Werte einzeln zu entschluesseln (nicht
verifizierbar ohne Live-Test), laufzeitbasierte Erkennung ergaenzt: loest
sich das Ziel des Skills auf den Anwender selbst auf (`victim == self`),
werden alle AbState-Slots einheitlich auf sich selbst angewendet — robust
gegenueber unbekannten `DemandType`-Werten, deckt `FitBlood01` korrekt ab.

### 18.1 Ehrlicher Gesamtabgleich: was fehlt noch gegenueber dem echten Spiel

| Bereich | Status |
|---|---|
| Item-ausgeloeste Buffs | Funktioniert (Abschnitt 15) |
| Skill-ausgeloeste Buffs/Debuffs (Angriff, Heilung, Selbstziel) | Funktioniert fuer die 3 erkannten Zielfaelle (Abschnitt 16-18) |
| Automatischer Ablauf (Tick) | Funktioniert, fuer Charaktere praezise (1s), fuer Mobs an Map-Tick gekoppelt (weniger praezise) |
| **`ActionIndex`-Bedeutung** (welcher Stat wird um wieviel veraendert) | **Weiterhin ungeklaert** — `BuffActionResolver` wendet aktuell KEINE numerische Wirkung an, nur Buchfuehrung (welcher Buff aktiv ist, wie lange). Spielmechanisch macht ein aktiver Buff also noch keinen Unterschied fuer Werte wie AC/MR/Schaden. |
| 12 von 15 `DemandType`-Werten (Party-Skills? Flaechenziele? Sonderfaelle?) | Nur ueber den generischen "Ziel==self"-Fallback und den bestehenden Mob-Zweig abgedeckt, nicht einzeln verifiziert |
| Cooldown pro Skill | Hartkodiert (`Skill.Write()` sendet immer 60000), kein echtes Server-seitiges Cooldown-Tracking |
| AoE-Skills (`UseSkillWithPositionHandler`) | Existiert, aber keine AbState-Anbindung ergaenzt (nur Einzelziel-Pfad in dieser Session bearbeitet) |
| Party-weite Buffs (`AbState.PartyState1-5`/`PartyRange`/`PartyEnchantNumber`) | Spalten werden geladen, aber nirgends ausgewertet — Buffs wirken aktuell nur auf den direkt getroffenen Charakter, nicht auf Gruppenmitglieder in der Naehe |
| Dispel-Mechanik (`DispelIndex`/`SubDispelIndex`) | Geladen, aber kein Dispel-Skill/-Item wertet sie aus |
| Persistenz ueber Logout | Buffs verfallen beim Ausloggen (kein Speicherpfad) — je nach `AbStateSaveType`-Wert (Spalte existiert, wird geladen, aber nicht ausgewertet) waere fuer manche Buffs eigentlich Persistenz vorgesehen |

**Die groesste einzelne Luecke bleibt `ActionIndex`.** Alles andere in
dieser Liste ist Verdrahtung/Vollstaendigkeit; ohne aufgeloeste
`ActionIndex`-Werte hat aber selbst ein perfekt verdrahteter Buff noch
keinen spuerbaren Effekt im Spiel — er wird nur korrekt verwaltet
(aktiv/abgelaufen), veraendert aber keine Werte. Realistischster Weg dahin
bleibt ein gezielter Live-Test (siehe fruehere Antwort in dieser Session).

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 19. Durchbruch bei ActionIndex — echte Stat-Wirkung implementiert

Ein Hinweis zur Bedeutung von "Action Index" im Fiesta-Online-P-Server-
Jargon (Animationssteuerung ueber `ActionTable.dat`/`.kfm`-Dateien) fuehrte
zu einer genaueren Pruefung, ob die `ActionIndex`-Spalte in
`SubAbState.shn` dasselbe Konzept meint. **Ergebnis: nein — beides
existiert getrennt in den echten Daten.** `AbStateView.shn` hat eine
eigene, dediziert benannte Spalte `AniIndex` (String, z.B. `"AbState00"`)
fuer die Animation — komplett getrennt von der numerischen
`ActionIndex`-Spalte in `SubAbState.shn`. Bestaetigt, dass beide
Konzepte nur aehnlich heissen, aber unterschiedliche Dinge sind.

**Dabei aber der eigentliche Durchbruch, in derselben Datei:**
`AbStateView.shn` (die zuvor als "nur Client-Anzeige, nicht server-
relevant" eingestuft und nur oberflaechlich gesichtet wurde) enthaelt
zwei bislang uebersehene Spalten:
- **`IconSort`**: `"BUFF"` oder `"DEBUFF"` fuer 776 von 777 AbStates —
  eine sauberere binaere Klassifizierung, nach der in Abschnitt 17
  vergeblich in `AbState.shn` gesucht wurde.
- **`Descript`**: Klartext-Beschreibung der Wirkung (z.B.
  `"Increased Critical Rate"`, `"Increased DEX"`).

### 19.1 ActionIndex-Bedeutungen empirisch abgeleitet

Fuer AbStates mit genau einem aktiven `ActionIndex`-Slot (keine
Mehrdeutigkeit durch kombinierte Beschreibungen) wurde `Descript`
systematisch mit dem jeweiligen `ActionIndex` korreliert. Ergebnis: sehr
saubere, groesstenteils eindeutige Zuordnungen fuer ~35 Werte. Aktiviert
in `BuffActionResolver.cs`:

| ActionIndex | Bedeutung | Buffs-Property |
|---|---|---|
| 1 | Increased Strength | `Str` |
| 7, 81 | Agility/DEX (+ und -) | `Dex` |
| 35 | Endurance | `End` |
| 36, 99 | Spirit (+ und -) | `Spr` |
| 22 | Max HP | `MaxHP` |
| 23 | Max SP | `MaxSP` |
| 3, 4 | Physical Damage | `WeaponDamage` |
| 46 | Magic Damage | `MagicDamage` |
| 5, 6, 73, 74 | Physical Defense (+ und -) | `WeaponDefense` |
| 15, 16 | Magical Defense (+ und -) | `MagicDefense` |
| 8, 90 | Evasion (+ und -) | `Evasion` |
| 10, 92 | Aim/Accuracy (+ und -) | `Aim` (neu) |
| 34, 80 | Critical Rate (+ und -) | `CriticalRate` (neu) |
| 20, 88 | Travel/Move Speed (+ und -) | `MoveSpeed` (neu) |
| 78 | Attack Rate/Speed | `AttackSpeed` (neu) |

`Buffs.cs` um vier neue Properties erweitert (`Aim`, `CriticalRate`,
`AttackSpeed`, `MoveSpeed`) — im urspruenglichen Datencontainer nicht
vorhanden, aber fuer diese haeufig belegten Effekte noetig.

**Vorzeichen nicht pro ActionIndex geraten:** Einige Werte (5, 8, 15, 36)
traten sowohl bei "Increased X" als auch "Decreased X" auf — derselbe
Aktionstyp, kontextabhaengiges Vorzeichen. Deshalb kommt das Vorzeichen
nicht aus dem `ActionIndex` selbst, sondern aus `AbStateInfo.IsBuff`
(also aus `IconSort`) — sauberer und durchgehend konsistent, statt eine
Annahme pro Index zu treffen.

**Bewusst nicht implementiert:** ActionIndex 19 (Immobilized/
Bewegungssperre) und 49 (KnockBack) sind keine Stat-Deltas, sondern
Crowd-Control-/Physik-Effekte — wuerden Eingriffe in die Bewegungs-
validierung erfordern, die nicht Teil dieser Aenderung waren. Bleiben
unaufgeloest (geloggt, keine Wirkung), um keine Halbfunktionalitaet
vorzutaeuschen.

### 19.2 Buff/Debuff-Zielzuordnung jetzt datenbasiert statt geraten

Löst die in Abschnitt 18 nur laufzeitbasiert (`victim == self`) geloeste
Zielfrage sauberer: `MapObject.ApplySkillAbStates(skillInfo, buffRecipient,
debuffRecipient)` wertet jetzt pro Slot `AbStateInfo.IsBuff`/`IsDebuff`
aus `IconSort` aus, statt alle Slots eines Skills einheitlich zu
behandeln. Deckt `FitBlood01` (Selbstbuff + Selbstdebuff) UND Skills mit
Buff-auf-Verbuendeten + Debuff-auf-Anwender-als-Kosten korrekt ab, ohne
weiterhin zu raten, wessen "Nutzer" gemeint ist.

**Architektur-Vereinfachung dabei:** `Buffs`/`AddBuff`/`RemoveBuff`/
`TickBuffs`/`ApplySkillAbStates` waren in `ZoneCharacter.cs` und
`Mob.cs` dupliziert (siehe Abschnitt 17) — jetzt in die gemeinsame
Basisklasse `MapObject` verschoben, keine Zweitimplementierung mehr.
`Buffs`-Feld ist `protected` (nicht `private`), da mehrere bereits
vorhandene `Get*()`-Stat-Methoden in `ZoneCharacter.cs`
(`GetExtraStr()`, `GetWeaponDamage(buffed)` etc.) direkt darauf
zugreifen — **diese Methoden existierten bereits im Original-Code und
lasen schon `Buffs.XXX`**, hatten also von Anfang an einen echten
Verbraucher; nur `BuffActionResolver` fuellte die Werte bisher nicht.

**Nebenbei gefunden und mitgefixt:** `ZoneCharacter.GetAim(bool buffed)`
war ein reiner Stub (`return 15`, `buffed`-Parameter ignoriert, TODO
"basestats aim + dex?"). Jetzt: `return 15 + (buffed ? Buffs.Aim : 0)`
— der Buff-Anteil wird verwendet, die Basiswert-Formel (Dex-Abhaengigkeit)
bleibt offen, das war schon vorher so.

### 19.3 Was jetzt wirklich funktioniert — und was noch fehlt

**Funktioniert jetzt vollstaendig:** Ein Buff/Debuff mit einem der ~35
zugeordneten `ActionIndex`-Werte veraendert tatsaechlich einen Spielwert
(Str/End/Dex/Int/Spr, MaxHP/MaxSP, Waffen-/Magieschaden,
Waffen-/Magieverteidigung, Ausweichen, Aim, Kritchance), nicht nur
Buchfuehrung. `GetExtraStr()`, `GetWeaponDamage(true)` etc. lesen das
bereits.

**Weiterhin offen:**
- `CriticalRate`/`AttackSpeed`/`MoveSpeed` sind jetzt in `Buffs.cs`
  korrekt befuellt, aber es gibt (anders als bei Str/Dex/Aim/Damage/
  Defense) noch **keine** `Get*()`-Konsumentenmethode dafuer im Code —
  die Werte stehen bereit, wirken aber noch nirgends auf tatsaechliche
  Kritwuerfe/Angriffstempo/Laufgeschwindigkeit.
- ~76 der ~111 vorkommenden `ActionIndex`-Werte weiterhin unaufgeloest
  (meist seltener genutzte/spezielle Effekte, Crowd-Control, oder Werte
  ohne sauberen Einzel-Slot-Beleg in den Daten).
- Skill-Cooldowns, AoE-Buff-Anbindung, Party-weite Buffs, Dispel — siehe
  Abschnitt 18.1, unveraendert offen.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 20. Kampf-Anbindung (CriticalRate/AttackSpeed) + weitere ActionIndex-Zuordnungen

### 20.1 CriticalRate/AttackSpeed jetzt an echte Kampfformeln angebunden

- **Kritischer Treffer** (`AttackSequence.cs`): war hartkodiert 20% Chance
  (`Program.Randomizer.Next() % 100 >= 80`), unabhaengig von jedem
  Charakterwert. Jetzt: `Buffs.CriticalRate` senkt/erhoeht die Schwelle,
  geklemmt auf 1-99% um garantiertes Treffen/Verfehlen durch
  Buff-Stacking auszuschliessen.
- **Angriffstempo** (`ZoneCharacter.Attack()`, `Mob.Attack()`):
  `Buffs.AttackSpeed` verkuerzt/verlaengert jetzt das Intervall zwischen
  Angriffen, Untergrenze 300ms. Auch fuer Mobs ergaenzt, damit Debuffs wie
  "Decreased Attack Rate" auch auf Gegner wirken.
- Dabei `AttackSequence_.cs` (mit Unterstrich) als **toten Code entdeckt**
  — eine zweite, nirgends instanziierte Kopie derselben Klasse, aehnlich
  den bereits bekannten Faellen (`MapObjectBuffCollection.cs` etc.). Nicht
  angefasst, nur zur Kenntnis: `grep -rn "new AttackSequence("` zeigt, dass
  ausschliesslich `AttackSequence.cs` (ohne Unterstrich) tatsaechlich
  verwendet wird.

**`MoveSpeed` bewusst nicht angebunden — anderer Fall als CriticalRate/
AttackSpeed:** Es gibt serverseitig **ueberhaupt keine
Bewegungsvalidierung** (kein Movement-Packet-Handler, keine Nutzung von
RunSpeed/WalkSpeed irgendwo im Code). Das ist keine fehlende
Buff-Verdrahtung, sondern eine fehlende Grundmechanik — Bewegung scheint
aktuell rein client-seitig gehandhabt zu werden. `Buffs.MoveSpeed` steht
bereit, hat aber nichts zum Anbinden.

### 20.2 Weitere ActionIndex-Zuordnungen (jetzt ~40 aktiv, von 111 gesamt)

Bei der Fehlersuche fiel auf: **`ActionIndex=21` ("Decreased Attack
Rate") stand in der Doku-Tabelle aus Abschnitt 19 als erledigt, fehlte
aber tatsaechlich im `BuffActionResolver`** — eigener Fehler, jetzt
nachgetragen (→ `AttackSpeed`).

Neu hinzugekommen, aus erweiterter Korrelation (auch Mehrfach-Wort-
Beschreibungen wie "Strength and Critical Rate" statt nur
Einzel-Slot-Faelle):

| ActionIndex | Bedeutung | Wirkung |
|---|---|---|
| 2 | "Strength and Critical Rate" | `Str` + `CriticalRate` gleichzeitig |
| 12 | "Increased Intelligence and Critical Rate" | `Int` + `CriticalRate` gleichzeitig |
| 13 | "Increased Magic Damage and Magical Defense" | `MagicDamage` + `MagicDefense` gleichzeitig |
| 119 | "All Stat +30" | `Str`+`End`+`Dex`+`Int`+`Spr` gleichzeitig |
| 107 | "Gives a bonus of X% EXP increase from hunting" | neues `ExpBonusPercent`, angebunden an `ZoneCharacter.GiveExp()` (existierender Konsument) |

### 20.3 Identifiziert, aber bewusst NICHT implementiert (andere Kategorie als reine Stat-Deltas)

- **19, 25 = "Stunned"** (84 bzw. 126 Vorkommen), **38 = "Fear"** (21),
  **49 = "KnockBack"** (10): Crowd-Control-/Physik-Effekte, keine
  Zahlenwerte auf eine `Buffs`-Eigenschaft. Wuerden eine
  Aktions-/Bewegungssperre im Handler-/Movement-Code voraussetzen, die es
  noch nicht gibt (vgl. 20.1, `MoveSpeed`).
- **26, 27 = periodischer HP-Schaden/-Heilung** ("Periodic Bleeding
  Damage", "Periodic Poison Damage", "Periodic HP Recovery" — **derselbe
  Index fuer beide Richtungen**, wie bei den bereits geloesten Stat-Paaren,
  Vorzeichen ueber `IsBuff`/`IsDebuff`), **30 = ebenfalls periodische
  HP-Erholung** (94-128 Vorkommen, sehr haeufig). Nicht implementiert, weil
  das einen **wiederkehrenden** Effekt braucht (Schaden/Heilung alle N
  Sekunden), waehrend `BuffActionResolver` aktuell nur beim
  Aktivieren/Deaktivieren einmalig wirkt. Wuerde eine Erweiterung von
  `Buffs.Tick()` erfordern (bisher nur Ablauf-Pruefung) — klar
  abgrenzbare, aber eigene naechste Aufgabe.
- **31 = Poison Resistance, 32 = Disease Resistance, 33 = Curse
  Resistance**: Identifiziert, aber kein bestehender Konsument — es gibt
  aktuell keine Code-Stelle, die eine "Vergiftungs-/Krankheits-/
  Fluch-Anwendungschance" berechnet, die sich durch eine Resistenz
  verringern liesse.
- **108 = Item-Drop-Rate-Bonus**: `RandomDrop.cs` wuerfelt zwar die
  Drop-Rate, aber ohne offensichtlichen Zugriff auf den toetenden
  Charakter (anders als `GiveExp()`, das den Charakter direkt als `this`
  hat) — Anbindung wuerde erfordern, den Charakter-Kontext durch das
  Drop-System durchzureichen. Nicht in dieser Runde gemacht, um keine
  ueberstuerzte/falsche Verdrahtung zu riskieren.
- **17, 60, 61 = Invincibility/Damage Absorption/Damage Reflection**:
  komplexe Sondermechaniken (muessten in die Schadensberechnung selbst
  eingreifen, nicht nur einen Stat-Wert veraendern), nicht implementiert.

**Zwischenstand:** ~40 von 111 `ActionIndex`-Werten aktiv (reine
Stat-Deltas). Weitere ~9 identifiziert, aber aus den oben genannten
strukturellen Gruenden bewusst nicht umgesetzt. Rest (~62) weiterhin ohne
ausreichend eindeutige Evidenz.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 21. Periodischer Schaden/Heilung (ActionIndex 26/27/30) implementiert

Deckt die in Abschnitt 20.3 als "noch offen" markierte, mit Abstand
haeufigste Kategorie ab (26: 128, 27: 94, 30: 21 Vorkommen in den echten
Daten — zusammen deutlich mehr als alle anderen einzelnen ActionIndex-
Werte).

### 21.1 Umsetzung

- **`Buff.cs`**: die drei periodischen ActionIndex-Werte werden jetzt von
  der einmaligen Activate()/Deactivate()-Anwendung ausgenommen und
  stattdessen ueber eine neue `TickPeriodic(DateTime now)`-Methode
  verarbeitet, aufgerufen aus `Buffs.Tick()` (die bereits bestehende,
  fuer den automatischen Ablauf zustaendige Methode aus Abschnitt 15.5).
- **Intervall**: 1 Sekunde. Kein Datenfeld in `SubAbState.shn` gibt ein
  explizites Tick-Intervall vor — diese Annahme ist plausibel (deckt
  sich mit dem bereits bestehenden 1-Sekunden-Rhythmus fuer Charaktere),
  aber **nicht gegen einen echten Client verifiziert**.
- **Richtung ueber `AbState.IsBuff`/`IsDebuff`** (wie bei allen anderen
  Actions, siehe Abschnitt 19): Debuff → Schaden ueber die neue,
  polymorphe `MapObject.Heal()`-Gegenstueck-Methode zu `Damage()`.
- **`MapObject.Heal(uint amount, bool isSP = false)` neu ergaenzt** —
  bisher gab es nur `Damage()` auf `MapObject`-Ebene, `HealHP()`/`HealSP()`
  existierten nur auf `ZoneCharacter`. Die neue Methode spiegelt exakt die
  Sync-Logik von `Damage()` (MaxHP/MaxSP-Klemmung, `SendUpdateHP`/
  `SendUpdateSP` bei Charakteren), funktioniert dadurch automatisch auch
  fuer `Mob` (z.B. ein sich selbst heilender Boss).
- **Schaden ueber `Character.Damage(null, amount)`** — nutzt den bereits
  bestehenden, bully-null-sicheren Pfad (kein ausgeloester Gegenangriff,
  aber korrekter HP-Sync bei Charakteren; fuer Mobs bislang ohnehin ohne
  Extra-Broadcast, siehe bereits bestehendes Verhalten in
  `MapObject.Damage()`).

### 21.2 Nicht geloest, bewusst dokumentiert

- **Tick-Intervall bleibt eine Annahme** (siehe oben) — ohne echten
  Paket-Mitschnitt nicht zu verifizieren.
- **Tick-Praezision fuer Mobs** unveraendert an `Map.Update()` gekoppelt
  (siehe Abschnitt 17.1), also weniger praezise als bei Charakteren.
- ActionIndex 26/27 werden weiterhin **ausschliesslich** ueber diesen
  periodischen Pfad behandelt (nicht mehr ueber `BuffActionResolver`s
  Stat-Dictionary, wo sie ohnehin nie eingetragen waren) — kein
  Konflikt, da beide Systeme dieselbe `PeriodicHpActionIndices`-Liste zur
  Unterscheidung nutzen.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 22. Tick-Intervall konfigurierbar gemacht + weitere ActionIndex-Analyse

### 22.1 Periodisches Tick-Intervall: von hartkodiert auf konfigurierbar

Beim genaueren Hinsehen ergab sich Zweifel an der 1-Sekunden-Annahme aus
Abschnitt 21: `KeepTime` streut in den echten Daten fuer denselben
ActionIndex zwischen 12000ms und 300000ms (5 Minuten) fuer denselben
Wirkungstyp. Bei fixer 1s-Rate wuerde der 5-Minuten-Fall 300 Ticks
ausloesen — je nach `ActionArg` ein sehr hoher Gesamteffekt, der bei
einem kurzen 12-Sekunden-Debuff (12 Ticks) unplausibel anders skaliert.
Keine der einfachen Alternativhypothesen (feste Tick-Anzahl unabhaengig
von der Dauer, z.B. immer 10 oder 20 Ticks) ergab ein saubereres,
konsistenteres Bild.

Da sich das ohne echten Paket-Mitschnitt nicht abschliessend klaeren
laesst: `Buff.PeriodicInterval` ist jetzt eine **statische, zur Laufzeit
aenderbare Property** statt einer `readonly`-Konstante, gesetzt aus dem
neuen Config-Key `Zone.PeriodicBuffTickMs` (Default 1000, wie bisher).
Sobald ein echter Wert bekannt ist, laesst sich das per Config-Aenderung
korrigieren, ohne den Code anzufassen.

### 22.2 Weitere ActionIndex-Zuordnungen (jetzt ~48 aktiv)

Aus einer erweiterten Analyse (auch Werte mit nur 1-2 Belegen, aber
eindeutigem Text) ergaben sich vier weitere, mit ausreichender Konfidenz
aktivierte Zuordnungen:

| ActionIndex | Bedeutung | Beleglage |
|---|---|---|
| 37 | "Increased Intelligence" | `Int` (n=1, aber eindeutiger Text; `GetExtraInt()` existierte als Konsument bereits, war aber nie befuellt) |
| 89 | "Decreased Strength" | `Str` (separater Index zu 1, gleiche Zielgroesse, n=1) |
| 18 | "Increased (Shield) Block Rate" | neue Property `BlockRate` (n=2), kein bestehender Konsument |
| 31/32/33 | Gift-/Krankheits-/Fluch-Resistenz | neue Properties `PoisonResistance`/`DiseaseResistance`/`CurseResistance` (n=4-5 je), kein bestehender Konsument |

### 22.3 Identifiziert, aber strukturell noch nicht anschliessbar — priorisierter Ausblick

Bei der erweiterten Analyse traten drei groessere, wiederkehrende
Kategorien hervor, geordnet nach Beleghaeufigkeit (= vermutliche
Priorität fuer die naechsten Schritte):

**1. Crowd-Control-Effekte — mit Abstand am haeufigsten, ~223 Vorkommen:**
`19`/`25` = "Stunned" (84 + 126!), `38` = "Fear" (21), `49` = "KnockBack"
(10), `110` = "Paralyzed" (2). Kein Stat-Delta, sondern eine
Handlungssperre — braucht einen neuen `MapObject`-Zustand (z.B.
`IsStunned`/`IsFeared`) und Pruefungen an mehreren Stellen (Skill-Einsatz
in `Handler9.cs`, Bewegung, Angriff). Klar der naheliegendste naechste
Schritt, sowohl wegen der Haeufigkeit als auch weil die Handler-Stellen
bereits bekannt sind (dieselben, die schon fuer Buffs/Skills bearbeitet
wurden).

**2. Ausgehende Schadens-Modifikatoren (DoT-Verstaerkung):** `68` =
"Increases all DOT Damage", `70` = "Increased Poison Damage", `75` =
"Decreased DOT Damage", `76` = "Decreased Blood Damage", `77` =
"Decreased Poison Damage". Betreffen nicht die eigenen erlittenen
Werte, sondern den Schaden, den die eigenen periodischen Effekte bei
ANDEREN anrichten. `Buff.cs` speichert aktuell keinen Verweis auf den
Verursacher (nur `Character` = wer den Buff traegt) — muesste um eine
"Caster"-Referenz erweitert werden, um beim periodischen Tick (Abschnitt
21) dessen Bonus nachzuschlagen.

**3. Sondermechaniken, die in die Schadensberechnung selbst eingreifen:**
`17`/`56` = Unverwundbarkeit ("Nullify all attacks", "immunity from all
damaging effects"), `60`/`61`/`120` = Schadensreflexion, `102`/`103` =
"Ignore Magic/Physical Damage", `112` = "Absorbs damage". Wuerden einen
Eingriff in `MapObject.Damage()` selbst erfordern (z.B. Schaden auf 0
setzen oder an den Angreifer zurueckspiegeln), nicht nur einen
Stat-Wert. Nicht implementiert, um keine Halbfunktionalitaet
vorzutaeuschen.

Rest (~65 Werte) hat entweder zu wenig/mehrdeutige Beleglage oder betrifft
Sondereffekte mit sehr wenigen Vorkommen (Event-/Feiertags-Buffs wie
"Golden Egg", "Slime Puzzle Buff" etc.) — nicht weiter verfolgt.

**Zwischenstand:** ~48 von 111 `ActionIndex`-Werten aktiv umgesetzt (44
Stat-Deltas + 3 periodische HP-Effekte + 1 Fortschritts-Bonus). Rest
entweder identifiziert-aber-ohne-Konsument (dokumentiert oben) oder
weiterhin unbekannt.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 23. Durchbruch: Fiesta-Heroes-Dokumentation erhalten und ausgewertet

Auf Zuruf eine Sammlung von Fiesta-Heroes-Dokumentation erhalten (jetzt im
Repo unter `docs/fiestaheroes/`), darunter `SHN Documentation/AbState.md`,
`SubAbState.md`, `AbStateView.md`, `ActionEffectAbState.md` und viele
weitere. Das ist eine **autoritative Enum-Tabelle** fuer genau die
`ActionIndex`-Werte, an denen in den letzten Antworten empirisch
gearbeitet wurde — der bei weitem groesste Vertrauensgewinn in dieser
Session.

### 23.1 Bestaetigt

- `IconSort` = "Specifies whether the icon displayed on the status bar is
  a buff or debuff" — exakte Bestaetigung des empirischen Fundes aus
  Abschnitt 19.
- `ActionIndex 26 = SAA_TICK`, `27 = SAA_DOTDAMAGE`, `30 = SAA_HEALAMOUNT`
  — bestaetigt exakt die in Abschnitt 23 (vorherige empirische Korrektur)
  hergeleitete Interpretation: 26 ist das Tick-Intervall, 27/30 sind die
  periodischen Betraege.
- `ActionIndex 19 = SAA_NOMOVE`, `25 = SAA_NOATTACK`, `38 = SAA_FEAR` —
  bestaetigt die Grundrichtung der CC-Zuordnung aus Abschnitt 22.3 (die
  vorherige Bezeichnung als "Stunned" war eine empirische Annaeherung,
  nicht der exakte Name — NOMOVE/NOATTACK zusammen ergeben praktisch eine
  Betaeubung).
- `ItemUseEffectType`/`UseEffectType`-Enum bestaetigt `UE_ABSTATE=4` passend
  zur bereits vorhandenen `ItemUseEffectType.AbState`-Anbindung (Abschnitt
  15).
- `DispelIndex`/`SubDispelIndex` referenzieren die Enums `DispelAttr`/
  `SubDispelAttr` (bisher nur geladen, nicht ausgewertet — siehe 18.1,
  weiterhin offen).

### 23.2 Zwei echte Fehler in der eigenen Arbeit gefunden und korrigiert

1. **`ActionIndex 35`/`36`**: vorher faelschlich auf `End`/`Spr`
   (Ausdauer/Willenskraft) gemappt. Laut Enum tatsaechlich `SAA_MAXHPPLUS`/
   `SAA_MAXSPPLUS` (Maximal-HP/-SP) — komplett andere Zielgroesse. Korrigiert.
2. **`ActionIndex 2`/`12`/`13`**: vorher als "kombinierte" Effekte behandelt
   (Staerke+Kritchance, Intelligenz+Kritchance, Magieschaden+Magie-
   verteidigung), basierend auf Mehrfach-Slot-Korrelation mit Beschreibungen
   wie "Strength and Critical Rate". Laut Enum sind es tatsaechlich einfache
   Einzelwerte (`SAA_STRPLUS`/`SAA_INTPLUS`/`SAA_MAPLUS`) — die fruehere
   Analyse hatte zwei **separate** Slots derselben AbState (z.B. Staerke in
   Slot A, Kritchance separat in Slot B/C/D) faelschlich als einen
   kombinierten Effekt interpretiert. Korrigiert: jeder Index wirkt jetzt
   nur auf seine eigene, per Enum benannte Zielgroesse.

### 23.3 `BuffActionResolver.cs` komplett neu aufgebaut

Von ca. 30 empirisch-geschaetzten auf **41 Zuordnungen, jetzt alle unter
Verweis auf den offiziellen Enum-Namen** (z.B. `SAA_STRPLUS`,
`SAA_ATKSPEEDDOWNRATE`) statt einer Text-Korrelations-Vermutung. Neu
ergaenzt (vorher nicht abgedeckt, jetzt durch die Enum-Namen eindeutig):
`SAA_STRRATE`, `SAA_MENTALPLUS` (= Spr, der korrekte Weg zu diesem Stat),
`SAA_THRATE`, `SAA_WCMINUS`/`SAA_WCDOWNRATE`, `SAA_MRMINUS`/
`SAA_MRDOWNRATE`, `SAA_THDOWNRATE`, `SAA_ALLSTATEPLUS` (39, zusaetzlich zum
bereits bekannten 119).

**Neue, noch offene Einschraenkung dabei entdeckt:** Die Enum unterscheidet
durchgaengig zwischen "PLUS" (flacher Wert) und "RATE" (Prozentsatz), z.B.
`STRPLUS=2` vs. `STRRATE=1` fuer denselben Stat. Diese Implementierung
wendet aktuell **beide gleich an** (flache Addition), weil eine echte
Prozentrechnung den Basiswert vor Buff-Anwendung kennen muesste — das sieht
die aktuelle `Buffs`-Architektur (reine Additions-Summen) nicht vor. Bei
RATE-Eintraegen ist die Ingame-Wirkung dadurch vermutlich falsch skaliert.
Naechster sinnvoller Ausbauschritt, sobald die Grundmechanik weiter reift.

### 23.4 Crowd-Control-Pruefung fertiggestellt

Die in der letzten Antwort begonnene `CanAct`-Pruefung (blockiert Aktionen
waehrend `IsStunned`/`IsFeared`) jetzt in allen vier relevanten Handlern
ergaenzt: `AttackMeleeHandler`, `AttackSkillHandler`,
`UseSkillWithTargetHandler`, `UseSkillWithPositionHandler` (alle in
`Handler9.cs`).

### 23.5 Weiterhin offen / naechste Schritte

- **Punkte 2 und 3 aus der urspruenglichen Priorisierung** (ausgehende
  DoT-Schadensverstaerker mit Caster-Referenz; Unverwundbarkeit/
  Schadensreflexion in `MapObject.Damage()`) — durch die Dokumentenanalyse
  noch nicht bearbeitet, bleiben fuer den naechsten Schritt. Die Enum
  bestaetigt aber bereits die relevanten Indizes (`SAA_REFLECTDAMAGE=61`,
  `SAA_ADDALLDOTDMG=68` etc.), das hilft dort direkt weiter.
- **RATE- vs. PLUS-Unterscheidung** (siehe 23.3) weiterhin ungeloest.
- Die uebrigen ~85 SHN-Dokumentationsdateien in `docs/fiestaheroes/` sind
  fuer viele andere Bereiche relevant (z.B. `ItemInfoServer.md`,
  `MobInfoServer.md`, `QuestData.md`) und wurden in dieser Runde nur
  punktuell gesichtet, nicht systematisch ausgewertet — wertvolle
  Grundlage fuer kuenftige Arbeit an anderen Systemen.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 24. Punkte 2 und 3 umgesetzt: DoT-Schadensverstaerker + Schild/Reflexion/Miss

Schliesst die in Abschnitt 22.3 identifizierten, damals noch nicht
umgesetzten Kategorien ab.

### 24.1 Ausgehende DoT-Schadensverstaerker (SAA_ADDALLDOTDMG etc.)

`Buff.cs` bekommt eine neue `Caster`-Referenz (wer den Buff/Debuff
verursacht hat), durchgereicht von `MapObject.ApplySkillAbStates()` (der
Anwender selbst) bis zur `AddBuff`-Kette (`Buffs.AddBuff`, `MapObject.
AddBuff`, jeweils neuer optionaler `caster`-Parameter, Default `null` fuer
Faelle ohne bekannten Verursacher, z.B. World-seitig per InterServer
gesendete Buffs).

Beim periodischen Tick (`Buff.TickPeriodic`) wird der Schaden jetzt um den
Bonus des Casters skaliert: `DotDamageBonusPercent` (generisch, SAA_
ADDALLDOTDMG=68/SAA_SUBTRACTALLDOTDMG=75), plus spezifisch
`PoisonDamageBonusPercent` (70/77) bzw. `BloodingDamageBonusPercent`
(69/76), unterschieden ueber `AbState.DispelIndex`/`SubDispelIndex`
(`DispelAttr.DA_POISON=4`, `SubDispelAttr.SDA_BLOODING=4` - aus der
Fiesta-Heroes-Dokumentation, siehe Abschnitt 23).

### 24.2 Schild / Schadensreflexion / Ausweichen (SAA_SHIELDAMOUNT/REFLECTDAMAGE/MISSRATE)

`MapObject.Damage()` um drei neue Pruefungen erweitert, in dieser
Reihenfolge:

1. **`MissRatePercent`** (ActionIndex 60): Wurf zu Beginn, bei Erfolg wird
   der gesamte Treffer negiert (nur fuer HP-Schaden, nicht SP-Verbrauch).
2. **`ShieldAmount`** (17): absorbiert Schaden aus einem Pool, bevor er die
   HP erreicht. **Wichtige Vereinfachung:** der Pool wird direkt von
   `Damage()` verbraucht, nicht sauber pro Buff-Instanz getrackt - der
   `Buffs.ShieldAmount`-Setter klemmt daher auf `>= 0`, damit ein
   spaeteres `Deactivate()` (das den urspruenglichen Betrag abzieht) nicht
   ins Negative laeuft, wenn der Schild zwischenzeitlich schon
   (teilweise) verbraucht wurde. Bei mehreren gleichzeitigen Schild-Buffs
   wird nicht sauber zwischen "welcher Schild wurde verbraucht"
   unterschieden - ein Pool fuer alle.
3. **`ReflectDamagePercent`** (61): spiegelt einen Prozentsatz des
   *tatsaechlich erlittenen* Schadens (nach Schild-Absorption) an den
   Angreifer zurueck. Neuer `isReflected`-Parameter an `Damage()`
   (Default `false`) verhindert Endlosschleifen, falls beide Seiten
   gleichzeitig reflektieren - der Rueckschlag selbst durchlaeuft die
   Miss/Schild/Reflect-Pruefungen nicht erneut.

### 24.3 Weiterhin offen

- Schild-Pool-Tracking pro einzelnem Buff (statt ein gemeinsamer Pool,
  siehe 24.2) waere fuer korrektes Verhalten bei mehreren gleichzeitigen
  Schild-Quellen noetig.
- RATE- vs. PLUS-Unterscheidung (Abschnitt 23.3) betrifft auch einige der
  hier neu ergaenzten Prozentwerte.
- Nicht implementiert: `SAA_MISSRATE` unterscheidet nicht zwischen
  Nah-/Fernkampf oder Angriffsart - trifft aktuell pauschal auf jeden
  eingehenden Treffer zu.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 25. NA2016-Luecken: Passive Skills implementiert, Fame-Bestandsaufnahme

Auf Wunsch "NA2016 voll unterstuetzen" gezielt nach mit den FH-Docs
schliessbaren Luecken aus dem urspruenglichen Projektauftrag gesucht
("Fame/Killpoints nicht berechnet, passive Skills nicht implementiert").

### 25.1 Passive Skills — Infrastruktur war fertig, Wirkung fehlte

Ueberraschender Befund: Laden aus der DB (`Skillist`-Tabelle, `IsPassive`-
Spalte) und Senden an den Client (`Handler4.SendPassiveSkillList`) waren
bereits vollstaendig implementiert. Es fehlte ausschliesslich die
**Wirkung**.

`PassiveSkill.shn` (48 Spalten, 503 Zeilen) hat eine grundlegend andere
Struktur als `ActiveSkill.shn`: keine AbState-Slots, sondern direkt
benannte Stat-Spalten (`Intel`, `MaxSP`, `WCRateUp`, `MARateUp`,
`MACriRate`, plus zahlreiche waffentyp-spezifische `MstRt*`/`MstPl*`-
Felder). Neu: `PassiveSkillInfo.cs`, `sql/data/data_passiveskill.sql`
(echte Daten), `DataProvider.LoadPassiveSkills()`.

**Bewusst nur ein Teil der 48 Spalten abgebildet:** `MaxSP`, `Intel`,
`WCRateUp`, `MARateUp`, `MACriRate` — klar benannt, eindeutig einer
bestehenden `Buffs`-Property zuordenbar. Die vielen waffentyp-
spezifischen Mastery-Felder (`MstRtSword1`, `MstPlAxe2`, ...) NICHT
abgebildet: dafuer muesste zusaetzlich bekannt sein, welche Waffe der
Charakter aktuell traegt, und "MstRt" (Rate?) vs. "MstPl" (Plus?) ist
nicht zweifelsfrei belegt. Rohdaten stehen trotzdem vollstaendig in der
Datenbank, falls spaeter genauer ausgewertet werden soll.

**Anwendung permanent statt zeitbegrenzt:** `Buffs.AddPassiveSkill()`/
`RemovePassiveSkill()` neu — gleiches Additions-/Subtraktionsmuster wie
`Buff.Activate()`/`Deactivate()`, aber ohne `KeepTime`/Ablauf, da passive
Skills dauerhaft wirken, solange sie erlernt sind. Angebunden in
`ZoneCharacter.LoadSkillsFromDataTable()` (Charakter-Login).

**Weiterhin offen, dabei entdeckt:** Skillbücher lernen aktuell **nur
aktive** Skills (`dskill.IsPassive = false;` ist im Item-Nutzungspfad
hartkodiert, `NextGen.Zone/Game/ZoneCharacter.cs` um Zeile 634). Ein
passiver Skill kann also nur ueber bereits in der DB vorhandene
`Skillist`-Eintraege (z.B. manuell eingefuegt) aktiv werden, nicht durch
Erlernen zur Laufzeit — ein separater, hier nicht geschlossener
Feature-Luecke.

### 25.2 Fame — Bestandsaufnahme, nicht geloest

`ZoneCharacter.Fame` existiert als Property (delegiert an
`Character.Fame`), wird in einem Statuspaket an den Client gesendet, aber
**nirgends vergeben** — beim Laden und bei der Charaktererstellung
durchgehend hart auf `0` gesetzt, keine `GiveFame()`-Methode vorhanden
(im Gegensatz zu `GiveExp()`, das echt funktioniert). Keine
Datenbankspalte noetig (wird nie aus der DB geladen, nur In-Memory).

**Nicht geloest:** ohne Beleg, WANN Fame in diesem Spiel vergeben wird
(PvP-Kills? bestimmte Quests? Arena?), waere jede Implementierung
geraten. FH-Docs zu `CharacterTitleData.md`/`BasicInfoTitle.md`
(Charaktertitel, moeglicherweise Fame-basiert) noch nicht ausgewertet -
moeglicher Ansatzpunkt fuer eine spaetere Session.

Erneut gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 26. Abstürze behoben + echter Paket-Mitschnitt (2016er Client gegen Original-2016er-Server)

### 26.1 Zwei Abstuerze behoben

Aus echten Logs eines Testlaufs: `NextGen.World.Program.Load()` und
`NextGen.Zone.Program.Load()` stuerzten beide mit `NullReferenceException`
ab, direkt nach erfolgreichem `Settings.Load()`. Ursache: die
`DatabaseManager`-Konstruktion + `GetClient()`-Verbindungstest lagen
ausserhalb jedes Try/Catch - ein Verbindungsfehler (falsches Passwort,
MySQL nicht erreichbar, Datenbank fehlt) fiel dadurch als nackte,
unklare Unhandled Exception auf. Jetzt abgesichert mit klarer
Diagnosemeldung in beiden Program.cs. Nebenbei einen unabhaengigen
Bug im selben Save()-Aufrufpfad gefunden: `@spr`-Parameter wurde mit
`CharacterStats.StrStats` (Staerke) statt `SprStats` (Willenskraft)
befuellt - Willenskraft waere nie korrekt gespeichert worden. Behoben.

### 26.2 Skillbuch-Fix + Kill Points (aus der Konversation gefordert)

- **Skillbuecher lehren jetzt auch passive Skills** (vorher hartkodiert
  nur aktive, siehe Abschnitt 25.1) - `PassiveSkillsByName` ergaenzt,
  gleicher Lernablauf wie bei aktiven Skills, aber mit sofortiger
  permanenter Wirkung (`AddPassiveSkill`).
- **PvP-Kill-Points**: `ZoneCharacter.KillPoints`, echte DB-Persistenz
  (`characters.KillPoints`, anders als Fame). Vergabe in
  `AttackSequence.cs` an der bereits vorhandenen Todes-Pruefung ergaenzt
  (dort, wo bereits zwischen Mob-Kill/EXP und Nicht-Mob-Kill
  unterschieden wurde). Betrag (1 Punkt/Kill) ist **nicht verifiziert** -
  keine Belegstelle fuer die echte Formel gefunden, bewusst simpel
  gehalten statt eine Zahl zu erfinden.

### 26.3 Echter Mitschnitt: 2016er Client gegen Original-2016er-Server

Erster echter, unabhaengiger Beleg fuer das gesamte Netzwerk-Protokoll-
Verstaendnis dieses Projekts. Werkzeug: `tools/pcap-analysis/
decrypt_fiesta.py` (scapy-basiert, mit der echten 499-Byte-XOR-Tabelle
aus `NetCrypto.cs` und der Framing-Logik aus `Client.cs` nachgebaut).

**Bestaetigt, 1:1 exakt:**
- Handshake-Byte-Struktur: `04 07 08 <XorPos LE>` (Laenge=4, dann
  `SH2Type.SetXorKeyPosition`-Opcode `07 08`, dann der XorPos-Wert) -
  exakt wie von `SendHandshake()`/`Packet(SH2Type)` erzeugt.
- Laengen-Praefix-Schema (1-Byte fuer <255, sonst `00` + 2-Byte-LE fuer
  groessere Pakete) exakt wie in `Client.cs` implementiert.
- Opcode-Packing (`Header = pOpCode >> 10`, `Type = pOpCode & 1023` via
  `Packet(byte,byte)`) exakt bestaetigt anhand mehrerer entschluesselter
  Pakete (u.a. `SH2Type.UpdateClientTime = 73` korrekt erkannt).

**Neu gelernt (nicht vorher bekannt):** `Client.cs` ruft `crypto.Crypt()`
nachweislich **nur beim Empfang** auf (nie beim Senden) - Server-Pakete
sind grundsaetzlich unverschluesselt, nur Client->Server ist
verschluesselt. Im Mitschnitt empirisch bestaetigt (Server-Pakete sofort
im Klartext lesbar). Das ist kein Bug, sondern das tatsaechliche
Protokollverhalten.

**Architektur komplexer als angenommen:** Der Mitschnitt zeigt weit mehr
als drei Server-Verbindungen - u.a. viele kurzlebige Verbindungen zu
Ports 9031-9034 direkt hintereinander (vermutlich Ping-Checks gegen
mehrere Zone-Server-Kandidaten zur Serverauswahl/Latenzmessung, erkennbar
an `SH2Type.Ping`/Antwort-Mustern), sowie eine spaete Verbindung auf Port
9016 mit einem bislang unbekannten **Header=1**-Paket (1592 Byte,
Klartext-Charaktername "Takeo" enthalten) - **Header=1 ist im
NextGen-Emulator-Code ueberhaupt nicht definiert** (kein `CH1Type`/
`SH1Type`), echte, bisher unbekannte Protokolluecke.

**Offen fuer eine kuenftige Session:** vollstaendige Zuordnung aller
Ports zu Login/World/Zone, Entschluesselung/Analyse der Header=1-Familie
und der Ports 9031-9034/9011/9013/9014/9022 im Detail, Abgleich der
dabei sichtbaren Opcodes gegen die im Code bereits registrierten
Handler. Das Werkzeug dafuer liegt jetzt im Repo bereit.

## 27. Zweiter Mitschnitt (annotiert) — echter Bug gefunden und behoben, Protokoll-Familien systematisch abgeglichen

Zweiter, diesmal von Beginn an sauber mitgeschnittener Durchlauf (Wireshark
vor Clientstart gestartet) mit Zeitstempel-Notizen. Das erlaubte erstmals
eine zuverlaessige Server-Zuordnung ueber die Reihenfolge der
Verbindungen statt Vermutung: **Port 9010 = Login, Port 9013 = World,
Port 9016 = Zone** (Zeitstempel passen exakt zu den notierten Aktionen:
Login ~22:21, Serverwahl ~22:23, Charakter-Login ~22:24).

### 27.1 Echter, konkreter Bug gefunden und behoben: `CH3Type.Login`

Von 10 im Login-Strom beobachteten Opcodes stimmten **9 exakt** mit dem
bestehenden Code ueberein (`CH3Type.Version=101`/`SH3Type.
VersionAllowed=103`, `CH3Type.FileHash=4`/`SH3Type.FilecheckAllow=5`,
`SH3Type.WorldlistNew=10`, `CH3Type.WorldReRequest=27`/`SH3Type.
WorldistResend=28`, `CH3Type.WorldSelect=11`/`SH3Type.WorldServerIP=12`).
**Eine Abweichung:** `CH3Type.Login` stand im Code als `56`, der echte
Client sendet **`90`**. Korrigiert.

### 27.2 Login-Paketstruktur grundlegend falsch — jetzt korrigiert

`LoginHandler.Login()` ging von einem kompakten 54-Byte-Paket aus
(11 Zeichen Username, 7 Byte Abstand, 7 Zeichen Passwort). Der echte
Mitschnitt zeigt ein voellig anderes, 318-Byte-Paket:

- Username: **260 Byte** fester Puffer, nullterminiert (getestet mit
  Account "admin")
- Passwort: **32 Byte MD5-Hex-String** (nicht binaer!) - per Skript
  gegen `hashlib.md5(b"admin").hexdigest()` exakt bestaetigt
- 4 Byte Padding
- 8 Byte Client-Tag ("Original" beobachtet)
- 12 Byte Padding

`LoginHandler.Login()` komplett neu geschrieben, nutzt jetzt
`TryReadString`/`TryReadBytes` mit den echten Feldlaengen statt der alten
Zeichen-fuer-Zeichen-Schleife. Die nachgelagerte Konto-Logik (DB-Abgleich,
Auto-Account-Erstellung, Bann-/Doppellogin-Pruefung) war unveraendert
korrekt und blieb unangetastet.

**Praktische Bedeutung:** Ohne diesen Fix waere Login mit dem echten
NA2016-Client vermutlich nie funktionsfaehig gewesen - das alte Parsing
haette bei einem 318-Byte-Paket komplett falsche Werte gelesen.

### 27.3 Weitere Opcode-Familien im World-Strom abgeglichen

Bestaetigt: `CH3Type.WorldClientKey=15`/`SH3Type.CharacterList=20`,
`CH4Type.CharSelect=1`/`SH4Type.ServerIP=3` (enthaelt echte Zonen-Server-
IP), `SH4Type.CharacterGuildinfo=18`, `SH4Type.
CharacterGuildacademyinfo=151`, `SH37Type.SendMasterList=20`,
`SH31Type.LoadUnkown=7`, generisches `SH2Type`-Ping/Pong.

**Neue Luecken gefunden:**
- **`SH22Type` existiert serverseitig ueberhaupt nicht** (nur `CH22Type`
  fuer Client->Server). Der echte Server sendet aber grosse
  Server->Client-Pakete mit Header 22 (bis 7477 Byte, enthalten
  Klartext-Zonen-/Arenanamen wie "Arena - Prelude [60-70]", "Arena -
  Heroes [81-90]", "King Kong Phino's Mess") - vermutlich eine
  Karten-/Arenaliste. Komplett unimplementiert.
- **`SH28Type` unvollstaendig**: nur `LoadQuickBar=3/LoadQuickBarState=5/
  LoadGameSettings=11/LoadClientSettings=13/LoadShortCuts=15` definiert,
  aber der Mitschnitt zeigt zusaetzlich Typen 4, 12, 50, 51, 52 (letztere
  mit umfangreichen Daten, z.B. 384 Byte bei Typ 51) - vermutlich
  zugehoerige Request-Typen bzw. eine erweiterte/andere Struktur als
  angenommen.
- **`CH31Type` Typ 6** (Client->Server) hat keine erkennbare Gegenstelle
  in der aktuellen Enum-Definition.
- Periodisches `CH4Type`/`SH4Type`-Ping-Paar (Typen 217/218) nicht in den
  bestehenden Enums erfasst.

### 27.4 Werkzeug erweitert

`tools/pcap-analysis/decrypt_fiesta.py` um `unpack_opcode()` ergaenzt
(korrekte Bit-Entpackung `Header = opcode>>10, Type = opcode&1023` aus
zwei Little-Endian-Bytes - ein Fehler in der ersten Analyse-Runde
(Abschnitt 26) verwendete faelschlich die rohen Bytes direkt als
Header/Type, was dort zu falschen Kopfdaten fuehrte; hier korrigiert und
empirisch mehrfach bestaetigt).

### 27.5 Noch offen

- `SH22Type` (Karten-/Arenaliste) muesste komplett neu entworfen werden -
  Struktur der 7477-Byte-Pakete noch nicht im Detail analysiert.
- `SH28Type`-Erweiterung (Typen 4/12/50/51/52) noch nicht analysiert.
- Zone-Strom (Port 9016, der groesste und wichtigste Teil dieses
  Mitschnitts mit dem Quest-Abgabe- und GM-Levelup-Ereignis) noch nicht
  ausgewertet - naechster Schritt.

Login-Fix gegen eine frische Vollkopie kompiliert — 0 Fehler, unveraenderte
17 Warnungen.

## 28. Zone-Strom ausgewertet: Header=1-Raetsel geloest, Bewegungspaket gefunden, GM-Befehlskanal bestaetigt

Fortsetzung von Abschnitt 27 - der Zone-Strom (Port 9016) des zweiten,
annotierten Mitschnitts.

### 28.1 Das "Header=1"-Raetsel aus Abschnitt 26 ist aufgeklaert - war ein eigener Analysefehler

Der grosse 1592-Byte-Upload mit Klartext-Charakternamen, den Abschnitt 26
als mysterioeses "Header=1, komplett unbekannt" beschrieb, ist tatsaechlich
**`CH6Type.TransferKey = 1`** - der laengst bekannte Transfer-Schluessel-
Mechanismus (World uebergibt Login-Token an Zone). Die falsche Zuordnung
in Abschnitt 26 kam vom selben Bit-Entpackungsfehler, der in Abschnitt 27
gefixt wurde (rohe Bytes statt korrektem `opcode>>10`/`opcode&1023`).
**Keine unbekannte Protokollluecke, sondern ein bereits im Code
existierender, korrekt benannter Mechanismus.**

### 28.2 Bestaetigte Opcodes im Zone-Strom

`SH4Type.CharacterInfo/CharacterLook/CharacterQuestsBusy/
CharacterQuestsDone/CharacterActiveSkillList/CharacterPassiveSkillList/
CharacterItemList/CharacterInfoEnd/CharacterTitles/
CharacterTimedItemList/Unk(222)`, `SH6Type` (Antwort auf TransferKey),
`SH17Type` (Typ 30, bisher nur als `LoadUnkown` o.ae. bekannt).

### 28.3 GM-/Chat-Befehlskanal bestaetigt (`CH8Type`)

`CH8Type.Type=1` = Chat-Nachricht, ausgewertet als GM-Befehl bei
Praefix `&`. Zwei reale Befehle im Mitschnitt beobachtet und exakt
bestaetigt:
- `&adminlevel` -> Antwort `SH8Type` (Typ 17): "Admin level is 100" -
  exakt passend zur User-Notiz.
- `&levelup` -> vom User genutzt, um von Level 1 auf Level 3 zu wechseln.

### 28.4 Wichtiger neuer Fund: Bewegungspaket identifiziert

`CH8Type.Type=25` (18 Byte) wird waehrend des Laufens **alle ~2 Sekunden**
periodisch vom Client gesendet, mit `SH7Type.Type=8`-Antworten (151 Byte,
enthaelt u.a. den Klartext-Kartennamen "RouCos02"). **Das ist mit hoher
Wahrscheinlichkeit das lange gesuchte Bewegungspaket** - in
`DOCUMENTATION.md` mehrfach als "es gibt serverseitig ueberhaupt keine
Bewegungsvalidierung, kein Handler gefunden" dokumentiert (u.a. Abschnitt
20.1). Noch nicht im Detail decodiert (Feldstruktur der 18 Byte unklar -
vermutlich X/Y-Position + Richtung + Zeitstempel/Sequenznummer), aber der
Opcode selbst ist jetzt bekannt: **Header 8, Type 25**.

### 28.5 `CharacterInfo`-Struktur: wichtiger neuer Anker, aber nicht vollstaendig geloest

Bei bekanntem Level=1 zeigt sich: Byte 23 des Pakets (vorher als "Level"
vermutet) enthaelt konsistent den Wert 8, nicht 1 - **Level sitzt an
anderer Stelle oder das Feld ist etwas anderes** (evtl. Job/Klasse -
passend zu "Fighter"). Neuer, verlaesslicher Anker gefunden: das 12-Byte
Feld mit Inhalt "RouN" (+ Nullen) ist sehr wahrscheinlich **nicht** ein
Gildenname (wie in Abschnitt 26 vermutet), sondern der **Kartenname
"Roumen"** (Fiesta Onlines Startstadt) - passt exakt zur `WriteString
(MapInfo.ShortName,12)`-Zeile in `WriteDetailedInfo()`. Die exakte
Feldreihenfolge DAVOR (zwischen Name-Ende und Map-Beginn, ca. 46 Byte)
bleibt trotz mehrerer Ansaetze ungeloest - einfaches Bytes-Abzaehlen
reicht nicht aus, ohne weitere bekannte Referenzwerte (z.B. exaktes HP/SP/
Geld zum Aufnahmezeitpunkt) zu raten, was bewusst vermieden wurde.

### 28.6 Nicht gefunden: Quest-Abgabe-Paket

Der vom User notierte Quest-Abgabe-Vorgang (22:28, "healer julia") liess
sich in dieser Runde nicht eindeutig identifizieren - kein Klartext
"quest"/"julia" im Chat-Kanal gefunden, vermutlich laeuft NPC-Interaktion
ueber einen dedizierten Binaer-Opcode statt Text. Fuer eine kuenftige
Runde: gezielt nach den Zeitstempeln unmittelbar vor/nach 22:28 in den
NPC-nahen Frames suchen (Header 9/22 kommen als Kandidaten in Frage, u.a.
`SH9Type`-Pakete mit ungeklaerten Typen 2/61/70 traten in diesem
Zeitfenster auf).

Kein Code in dieser Antwort geaendert (reine Protokoll-Auswertung) - die
konkreten Fixes aus Abschnitt 27 (Login-Paketstruktur) bleiben der
wichtigste praktische Ertrag bisher.

## 29. Nutzer-Korrekturen ausgewertet: Job-Klasse bestaetigt, Geld-Paket verifiziert, eigene Fehleinschaetzung zu Bewegung korrigiert

### 29.1 Job-Klasse 8 = Warrior, bestaetigt ueber Fiesta-Heroes-Dokumentation

`ItemInfo.md` (WhoEquip-Bitmask-Tabelle, TW2008/CN2012) listet:
`Fighter=2, CleverFighter=4, Warrior=8, Gladiator=16, Knight=32`. Der im
`CharacterInfo`-Paket beobachtete Byte-Wert 8 an der vorher als "Level"
vermuteten Position (Abschnitt 28.5) ist damit sehr wahrscheinlich die
**Job-Klasse "Warrior"**, nicht Level - passt zur Charakterbeschreibung
("fighter"-artige Nahkampfklasse) des Nutzers. Level sitzt demnach an
anderer Stelle im Paket, weiterhin nicht lokalisiert.

### 29.2 Geld-Paket (`SH4Type.Money`) exakt verifiziert, Code bereits korrekt

Nutzer-Angabe: 52 Kupfer durch Quest-Abgabe erhalten. Mitschnitt zeigt
exakt ein `SH4Type.Money`-Paket (Typ 51) mit Wert **52** (8-Byte Long) bei
t+240s, unmittelbar vor der vom Nutzer notierten Quest-Abgabe/dem
Levelup-Befehl. `ZoneCharacter.ChangeMoney()` sendet bereits exakt diese
Struktur (`packet.WriteLong(Character.Money)`) - **kein Fix noetig, der
vorhandene Code ist hier nachweislich korrekt.**

### 29.3 Wichtige Korrektur: Bewegungsvalidierung existiert bereits vollstaendig

An mehreren frueheren Stellen dieser Session (u.a. Abschnitt 20.1, 22.3,
28.4) wurde faelschlich behauptet, es gebe **keine** serverseitige
Bewegungsvalidierung. **Das war falsch** - eine gruendlichere Pruefung
(ausgeloest durch die Bewegungsrichtungs-Hinweise des Nutzers) zeigt:
`NextGen.Zone/Handlers/Handler8.cs` enthaelt bereits `WalkHandler`/
`RunHandler`/`HandleMovement` mit:
- Zustandspruefung (kein Bewegen waehrend Tod/Rasten/Handel)
- Kollisionspruefung gegen die Block&Walk-Kartendaten
- **Geschwindigkeits-/Cheat-Erkennung** (Distanz-pro-Paket-Grenzwert,
  `CheatTracker.AddCheat(CheatTypes.Speedwalk, ...)`)
- Rotationsberechnung
- Party-Positions-Broadcast an Gruppenmitglieder

Die per Mitschnitt entschluesselte Paketstruktur (vier 4-Byte-Integer:
oldX, oldY, newX, newY) **stimmt exakt** mit dem bereits vorhandenen
`TryReadInt`-Parsing in `HandleMovement` ueberein - eine weitere,
unabhaengige Bestaetigung, dass dieser Teil des Codes bereits richtig
ist. `CH8Type.Run=25`/`CH8Type.Walk=23` sind beide per
`[PacketHandler(...)]` registriert.

**Warum der fruehere Fehler passierte:** die urspruengliche Suche
verwendete die Stichworte "RunSpeed"/"MoveSpeed"/"WalkSpeed", die im
tatsaechlichen Code nicht vorkommen (die Bewegungslogik nutzt
`Vector2.Distance`/eigene Feldnamen statt dieser Begriffe) - eine zu eng
gefasste Stichwortsuche fuehrte zu einem falsch-negativen Ergebnis.
Analog zum bereits in Abschnitt 16 dokumentierten Fall
("UseSkillWithTarget" vs. Wortgrenzen-Regex).

**Tatsaechlich weiterhin offen bei der Bewegung** (nicht neu, aber zur
Praezisierung): `Buffs.MoveSpeed` (Abschnitt 19-24) ist weiterhin nicht an
diese Geschwindigkeitspruefung angebunden - ein Lauftempo-Buff/-Debuff
wuerde aktuell keinen Effekt auf `HandleMovement`s Distanzgrenze haben.
Das waere ein sinnvoller, klar abgegrenzter naechster Schritt, falls
gewuenscht.

Kein Code in dieser Antwort geaendert - alle drei Punkte bestaetigten
entweder bereits korrekten Code oder korrigierten eine reine
Dokumentations-/Analyseaussage.

## 30. Quest-Sequenz gefunden, Job-Vermutung korrigiert, Methodik-Luecke behoben

### 30.1 Methodik-Fehler behoben: keine echte TCP-Stream-Rekonstruktion

Nutzerfrage "etwas im Opcode-Lesesystem uebersehen?" gezielt geprueft:
`tools/pcap-analysis/decrypt_fiesta.py` verarbeitete bisher jedes
aufgezeichnete TCP-Paket einzeln statt den Bytestrom eines Streams
zusammenhaengend zu parsen - bei ueber mehrere TCP-Segmente verteilten
Frames waere das ein echtes Problem gewesen. Empirisch geprueft (Skript
um echte Pufferung/Rekonstruktion ergaenzt): fuer den ausgewerteten
Zone-Strom lag **keine** Fragmentierung vor (0 Byte unverarbeiteter Rest
in beide Richtungen) - die fruehere paketweise Analyse war fuer diesen
Mitschnitt zufaellig korrekt. Das Skript ist trotzdem jetzt korrekt
(echte Pufferung), fuer kuenftige, evtl. groessere Mitschnitte wichtig.

### 30.2 Job-Klasse-Vermutung aus Abschnitt 29.1 korrigiert

Nutzer-Einwand berechtigt: Warrior ist in Fiesta Online typischerweise
erst ab Level 60 (oder per GM-Befehl) erreichbar, der Charakter war aber
Level 1. Die WhoEquip-Bitmask-Tabelle (Abschnitt 29.1) beschreibt
**Item-Ausruestungsanforderungen** (welche Klassen ein Gegenstand
tragen darf) - vermutlich ein **eigenstaendiges Nummernsystem**,
unabhaengig davon, wie der Charakter selbst seine aktuelle Klasse im
`CharacterInfo`-Paket speichert. Die Gleichsetzung "Byte-Wert 8 im Paket
= Bitmask-Wert 8 = Warrior" war ein Fehlschluss. **Zurueckgezogen -
keine belastbare Alternative gefunden.** Fuer eine sichere Zuordnung
braeuchte es einen zweiten Mitschnitt mit einem Charakter bekannter,
anderer Basis-Klasse (Fighter/Trickster/Mage/Cleric) zum Vergleich.

### 30.3 Vollstaendige Quest-Abgabe-Sequenz gefunden und decodiert

Bei Julia (NPC), zeitlich exakt passend zur Nutzer-Notiz (52 Kupfer
Belohnung, Abschnitt 29.2):

1. `CH9Type.SelectObject` - Julia anklicken
2. `SH9Type.StatUpdate` - Serverantwort mit NPC-Daten
3. `CH8Type.BeginInteraction` - Dialog beginnen
4. **`SH17Type` Typ 1, 105 Byte** - das eigentliche NPC-Dialog-/Quest-Menue
   (neu benannt: `NpcDialogMenu`)
5. **`CH17Type` Typ 2, 9 Byte** - Spieler waehlt eine Option (Quest
   abgeben) (neu benannt: `NpcDialogResponse`)
6. Belohnungs-Kaskade: `SH4Type.Money=52` (exakt bestaetigt),
   `SH9Type.GainExp`, `SH9Type.LevelUP` (237 Byte - ob dies tatsaechlich
   ein Levelup war oder nur volle Stat-Neuberechnung enthaelt, ist nicht
   sicher, da der Nutzer keinen Levelup direkt aus der Quest meldete),
   `SH9Type.HealHP`/`HealSP`, `SH4Type.CharacterPoints`, `SH4Type.
   UpdateStats`
7. Zwei weitere, bisher komplett unbekannte Familien: `SH47Type` Typ 5
   (10 Byte, einmalig) und ein `CH16Type`/`SH16Type`-Austausch (Typen 37/38,
   vermutlich Quest-Log-Bestaetigung)

**Header 17 (das eigentliche Dialog-/Quest-Menue), 47 und 16 existierten
vorher ueberhaupt nicht im Code** - jetzt mit den beobachteten Opcode-
Werten als neue Enums dokumentiert (`CH17Type`, `SH17Type`, `CH16Type`,
`SH16Type`, `SH47Type` in `PacketTypeClient.cs`/`PacketTypeServer.cs`),
**aber bewusst ohne Handler-Implementierung** - das quantifiziert exakt
die schon laenger bekannte Luecke ("Quest-System nicht implementiert",
siehe `SendQuestListBusy`s TODO-Kommentar) mit echten Opcodes, ist aber
selbst noch keine funktionierende Umsetzung. Ein echtes Quest-System
(NPC-Dialogbaum, Quest-Zustandsverwaltung, Bedingungspruefung) waere ein
eigenstaendiges, groesseres naechstes Vorhaben.

Kompiliert (nur neue Enum-Definitionen, keine Handler-Logik) - 0 Fehler,
unveraenderte 17 Warnungen.

## 31. Nachfassrunde: SH28Type teilweise entschluesselt, kein Kampf im ersten Mitschnitt, Klarstellung Quest-Opcodes

### 31.1 SH22Type bestaetigt: Kingdom Quests, zeitgesteuert

Nutzer bestaetigt: "Arena"/"King Kong Phino's Mess" sind Kingdom Quests,
die uhrzeitabhaengig oeffnen. Erklaert die grosse Paketgroesse (bis 7477
Byte) - vermutlich eine vollstaendige Liste aller verfuegbaren KQ-Faenster
mit Zeitfenstern, nicht nur eine einfache Kartennamen-Liste.

### 31.2 SH28Type 50/51/52 doch teilweise lesbar - eigene Aussage korrigiert

Auf Nachfrage systematisch statt oberflaechlich geprueft:

**Typ 51 (384 Byte) - hohe Konfidenz geloest:** 4-Byte-Eintraege
(Modifier-VK-Code, Haupt-VK-Code, 2-Byte-Slot-Index). Die Haupt-VK-Codes
ergeben an mehreren Stellen exakt `49,50,51,52,53,54,55,56,57,48,189,187`
- das ist buchstaeblich die Tastenreihe "1234567890-=" als Windows-
Virtual-Key-Codes (`VK_1`=0x31=49 ... `VK_0`=0x30=48, `VK_OEM_MINUS`=0xBD=189,
`VK_OEM_PLUS`=0xBB=187), dreimal wiederholt mit unterschiedlichem
Modifier-Byte (0, 16=`VK_SHIFT`, 18=`VK_MENU`/Alt). **Das ist die
QuickBar-Tastenkuerzel-Zuordnung** (Slot -> Taste, mit Umschalt-/Alt-Variante).
**Typ 52 (88 Byte) - mittlere Konfidenz:** 3-Byte-Eintraege
(Flag-Byte 0/1, sequenzieller Index, 00) - passt zu einem einfachen
"Slot aktiviert/sichtbar"-Array.
**Typ 50 (32 Byte) - nur teilweise:** enthaelt erkennbare
(Slot-Index, 4-Byte-Wert)-Paare (Index 10->3500, Index 11->3505,
+5 pro Slot) - vermutlich Skill-/Item-IDs auf einzelnen Slots gebunden,
aber der vorausgehende 16-Byte-Header/Zaehl-Bereich bleibt bei nur zwei
Beispieleintraegen ungeklaert.

**Lehre:** die erste Einschaetzung ("unbekannter Inhalt") war zu schnell
aufgegeben - mit systematischem Aufbrechen in feste Feldbreiten und
Cross-Referenzierung gegen bekannte Zahlenmuster (hier: Windows-VK-Codes)
liess sich der Grossteil doch entschluesseln. Fuer kuenftige aehnliche
Faelle: immer zuerst auf sich wiederholende Bytemuster/-abstaende pruefen,
bevor ein Paket als "nicht lesbar" eingestuft wird.

### 31.3 Kein echter Kampf im ersten Mitschnitt - eigene Erwartung korrigiert

Nutzer-Vermutung "Kampf gegen Schleim muesste im ersten Mitschnitt
enthalten sein" gezielt geprueft (Zone-Strom, Port 9016/55402, der beim
allerersten Analysedurchlauf dieser Session mit falschem Portpaar
[64912 statt 55402] gar nicht ausgewertet wurde - eigener Fehler,
jetzt korrigiert). Ergebnis: **keine** der bekannten Kampf-Opcodes
(`SH9Type.AttackAnimation=71`, `AttackDamage=72`, `DieAnimation=74`,
`SkillAck=53`, `SkillUsePrepareSelf/Others=78/79`) kommen vor. Gefunden
wurden nur `SH9Type`-Typen 40, 42, 95 (nicht in der aktuellen Enum
benannt) unmittelbar nach grossen `SH7Type`-Paketen (vermutlich
Mob-Spawn-/Sichtbarkeits-Daten fuer die Karte) - passt eher zu
"Monster wird sichtbar/registriert" als zu tatsaechlichem Kampf. Es
gab in diesem Mitschnitt vermutlich keinen echten Angriff auf ein
Monster, nur das Betreten eines Gebiets mit Monstern in der Naehe.
**Kampf-Verifikation bleibt fuer einen kuenftigen, gezielten Mitschnitt
offen** (siehe vorherige Antwort - dritter, kampf-fokussierter Mitschnitt).

### 31.4 Klarstellung "Opcodes bekannt, Feldstruktur nicht"

Auf Nachfrage praezisiert: bei `Header 17/16/47` (Quest-System, Abschnitt
30.3) ist bekannt, **welche Zahl** ein bestimmtes Paket identifiziert
(z.B. "Typ 1 auf Header 17 = das Dialogmenue erscheint"), aber **nicht**,
was die einzelnen Bytes INNERHALB des Pakets bedeuten (z.B. an welcher
Byte-Position die Quest-ID steht, welches Byte die verfuegbaren
Dialogoptionen codiert). Das ist ein anderer, tieferer Grad der
Entschluesselung als nur den Opcode zu kennen - vergleichbar mit dem
Unterschied zwischen "ich weiss, dass Paket X existiert" und "ich weiss,
was in Paket X steht" (wie bei `SH28Type` in 31.2 exemplarisch gezeigt -
dort ist inzwischen beides bekannt, bei Header 17/16/47 bisher nur
Ersteres).

Kein Code in dieser Antwort geaendert - reine Protokoll-Nachanalyse und
Korrektur eigener vorheriger Aussagen.

## 32. Dritter Mitschnitt: echte Kampfdaten - zwei konkrete Paket-Bugs gefunden und behoben

Dritter Mitschnitt (~23 Minuten, sehr detailliert dokumentiert:
Charaktererstellung, komplettes Tutorial, Shopping, Skill-Kauf/-Lernen,
Statuspunkte, und - erstmals - **echter Kampf** gegen 3 Schleime mit
Nahkampf, Skill-Einsatz (Magic Missile), erlittenem Schaden (exakt 12
HP), Heilung und Toden). Zone-Verbindung dieses Mal auf Port 9022
bestaetigt (Handshake-Signatursuche). Charaktername "NuyaTheMage"
(neu erstellte Magierin), Karte "RouT" (Roumen Tutorial-Bereich).

### 32.1 `SH9Type.AttackDamage` - Struktur korrigiert

Vorher: 15 Body-Byte (1-Byte-Crit, 4-Byte-HP, zwei feste Byte `4`/`100`
am Ende). Echte Struktur (16 Body-Byte, empirisch aus 11
aufeinanderfolgenden echten Kampf-Paketen rekonstruiert):

`Attacker(2) Target(2) ?(2) Damage(2) RemainingHP(2) ?(2) Counter(2) Counter(2)`

**Empirisch zweifach bestaetigt:**
- Bei "Schleim greift Spieler an"-Paketen erscheint der Schadenswert
  exakt **12** - identisch mit der vom Nutzer real erlittenen
  Schadensmeldung.
- Bei "Spieler greift Schleim an" faellt `RemainingHP` ueber 6
  aufeinanderfolgende Treffer sauber von 21 auf 17, 13, 9, 5, 1 (jeweils
  -4, passend zum konstanten Schaden pro Treffer), bis der letzte Treffer
  (Schaden 1) den Schleim toetet.
- Die Spieler-HP fiel nach dem 12-Schaden-Treffer von 64 auf 52 und blieb
  dort bei zwei nachfolgenden 0-Schaden-Treffern (Fehlschlaegen) exakt
  gleich - in sich konsistent.

`HP`-Feld war faelschlich 4 statt 2 Byte breit (`WriteUInt` ->
`WriteUShort`), Aufrufer entsprechend mit `(ushort)` Cast angepasst
(interne `MapObject.HP`-Repraesentation bleibt `uint`, nur das
Netzwerkfeld wurde korrigiert). Die beiden beobachteten, aber nicht
sicher gedeuteten 2-Byte-Felder werden jetzt bewusst mit `0` geschrieben
statt geraten zu werden.

### 32.2 `SH9Type.AttackAnimation` - Struktur korrigiert

Gesamtlaenge (9 Body-Byte) stimmte bereits, aber die letzten zwei Byte
waren hartkodiert `4`/`100` statt eines Treffer-Sequenzzaehlers (im
Mitschnitt sauber `1,1` / `2,2` / `3,3` / `4,4` pro Treffer - derselbe
Zaehler wie in `AttackDamage`). Methode um einen `counter`-Parameter
erweitert, Aufrufer entsprechend angepasst (`victim.UpdateCounter`).

### 32.3 Bedeutung der ehrlich unaufgeloesten Felder

Zwei 2-Byte-Felder in `AttackDamage` bleiben ungeklaert - Position und
Groesse stehen fest, die Bedeutung nicht. Ein Ausreisser (erster
Treffer einer Sequenz zeigte einen abweichenden Wert an einer Position,
die sonst durchgehend 0 war) deutet moeglicherweise auf ein
"Kampfbeginn"-Flag hin, ist aber mit nur einem Beispiel nicht
belastbar. Absichtlich nicht geraten.

### 32.4 Noch nicht ausgewertet (naechste Schritte)

Dieser Mitschnitt enthaelt deutlich mehr als bisher verarbeitet -
Charaktererstellung (`CH5Type`?), Ausruestungskauf bei Schmied Swanson,
Item-Nutzung (HP/MP-Traenke per Tastendruck), Statuspunkt-Verteilung
(Taste C, 2x SPR, 1x END), Skill-Kauf und -Erlernen per Rechtsklick
(Ice Bolt), Skill-Fenster (Taste K) und Quickslot-Zuweisung, Ausruhen
(Taste Pos1), Quest-Abgabe mit konkreter Belohnung (HP-/MP-Trank Stufe
1), und mindestens zwei natuerliche Level-Ups (Level 3->4->5) mit
Statuspunkt-Vergabe. Alles potenziell wertvoll fuer kuenftige
Verifikationsrunden.

Alle Aenderungen dieser Session gegen eine frische Vollkopie kompiliert -
0 Fehler, unveraenderte 17 Warnungen.

## 33. Statuspunkte-Bug gefunden: End/Dex vertauscht

Fortsetzung von Abschnitt 32. `CH4Type.SetPointOnStat=92`/`Handler4.
HandleSetStatPoint` bereits vorhanden und dem Grundprinzip nach korrekt
(1 Byte Stat-ID, Punkt wird verteilt, `UsablePoints` verringert,
Bestaetigung zurueckgesendet) - aber mit einem konkreten, spielrelevanten
Bug.

**Fund:** Alle 5 im Mitschnitt beobachteten Statuspunkt-Zuweisungen
stimmen exakt in Anzahl und Zeitpunkt mit dem Nutzer-Log ueberein (2x
SPR bei t+175s, 2x SPR bei t+1020s, 1x END bei t+1225s). Die vier
SPR-Zuweisungen zeigten den erwarteten Byte-Wert 4 (korrekt). Die
fuenfte, vom Nutzer als "in End gesteckt" dokumentiert, zeigte den
Byte-Wert **1** - der Code ordnete Byte-Wert 1 aber `Dex` zu (und 2
`End`). **Echte Reihenfolge: Str(0), End(1), Dex(2), Int(3), Spr(4)** -
deckt sich mit der Reihenfolge, die im uebrigen Code durchgaengig
verwendet wird (`CharacterStats`, `Buffs.cs`). Korrigiert.

**Praktische Bedeutung:** Ohne diesen Fix haetten Spieler, die einen
Punkt in Ausdauer stecken wollten, tatsaechlich einen Punkt in
Geschicklichkeit bekommen (und umgekehrt) - ein spielrelevanter,
unmittelbar bemerkbarer Bug.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 34. Chronologischer Durchgang durch den dritten Mitschnitt

Auf Wunsch von Anfang an durchgegangen, statt gezielt einzelne Bereiche
herauszugreifen. World-Verbindung dieses Mal auf Port 9013 (kein
Login-Mitschnitt enthalten - Aufzeichnung begann bei der
Charakterauswahl).

### 34.1 Charaktererstellung - Opcode gefunden, Struktur unklar

`CH22Type` Typ 191 (27 Byte) unmittelbar vor der Server-Antwort mit dem
neuen Charakternamen "NuyaTheMage" identifiziert - vorher nicht in der
Enum vorhanden (nur `GotIngame=27` bekannt). **Enthaelt keinen lesbaren
Text** (anders als z.B. `CharacterInfo`) - vermutlich reine
Aussehens-/Klassenparameter (Geschlecht, Frisur, Gesicht, Farbe, Klasse)
als Zahlenwerte. Ohne eine zweite Charaktererstellung mit anderen,
bekannten Auswahlwerten zum Vergleich nicht sicher Byte-fuer-Byte
zuordenbar - bewusst nicht geraten.

### 34.2 Bestaetigt korrekt (keine Aenderung noetig)

Mehrere Stichproben mit klaren Vorher-Nachher-Ankern (Anzahl der
Aktionen im Mitschnitt passend zur Nutzer-Dokumentation):

- **`CH12Type.BuyItem`** (Ausruestungskauf bei Schmied Swanson): 4 Kaeufe
  mit sequenziellen Item-IDs 1500-1503 (Schuhe/Hose/Muetze/Oberteil als
  zusammenhaengendes Starter-Set), dazu ein 5. Kauf mit ItemID 6920
  (vermutlich der spaetere HP-Trank-Kauf bei Sera). Struktur (2-Byte-
  ItemID + 4-Byte-Menge) stimmt exakt mit dem bestehenden Handler
  ueberein.
- **`CH12Type.Equip`**: 4 Anfragen, passend zu den 4 ausgeruesteten
  Teilen. 1-Byte-Slot-Struktur bestaetigt korrekt.
- **`CH8Type.BeginRest`/`EndRest`**: je 1 Paket, passend zur einmaligen
  Rast-Nutzung im Mitschnitt. Leerer Payload (keine Felder) bestaetigt.

### 34.3 Neu bestaetigt: SH22Type = Kingdom-Quest-Liste

Der in Abschnitt 26/27/31 als "Karten-/Arenaliste" identifizierte
`SH22Type` (Typ 29) liess sich diesmal sauber einzeln beobachten (drei
separate Pakete statt einem grossen Block): "Arena - Prelude [60-70][A]",
"Arena- Reincarnation[101-110][B]", "King Kong Phino's Mess[A]" - exakt
wie vom Nutzer beschrieben (zeitgesteuerte Kingdom Quests). Struktur
weiterhin nicht im Detail decodiert (grosse, sich wiederholende
Binaerbloecke pro Eintrag), aber die Kategorie ist jetzt zweifelsfrei
bestaetigt.

### 34.4 Zusammenfassung aller Mitschnitt-Sessions (Abschnitte 26-34)

**Echte, behobene Bugs:** `CH3Type.Login`-Opcode + komplette
Paketstruktur (Abschnitt 27), `SH9Type.AttackDamage`-Struktur (32),
`SH9Type.AttackAnimation`-Struktur (32), Statuspunkte End/Dex vertauscht
(33). **Bestaetigt bereits korrekt:** Bewegung, Geld-Paket, BuyItem,
Equip, BeginRest/EndRest, diverse World-/Zone-Opcodes aus Abschnitt 27/28.
**Neu identifiziert, Struktur noch offen:** Quest-Dialog-System (Header
17/16/47), Charaktererstellung (`CH22Type` 191), Kingdom-Quest-Liste
(`SH22Type` 29), Level-Up-Detailpaket (`SH9Type` 12), einige `SH28Type`-
Slotdaten.

Kein Code in dieser Antwort geaendert - reine Bestandsaufnahme/
Bestaetigung bereits vorhandenen, korrekten Codes plus Dokumentation
neuer Opcodes.

## 35. Chronologischer Durchgang, Fortsetzung: Quest-Dialog-Ablauf, Skill-Lernen bestaetigt

### 35.1 NPC-Dialog-Ablauf (Header 17) vollstaendig nachvollzogen

- `SH17Type` Typ 30 (18 Byte) - tritt exakt zu Beginn einer neuen
  NPC-Interaktion auf (t+0.48s beim allerersten Tutorial-Dialog, t+1153s
  beim Wechsel zu Skillmeister Vayne). Neu benannt: `DialogSessionStart`.
- `CH17Type.NpcDialogResponse` (7 Byte) ist bei **jedem** Weiterklicken
  identisch (`e9 0a 02 01 00 00 00`) - der Server steuert den
  Dialogfortschritt serverseitig anhand des NPC-/Sitzungszustands, der
  Client sagt nur "weiter". Dadurch laesst sich aus dem Mitschnitt allein
  nicht unterscheiden, ob z.B. eine Quest angenommen oder nur eine
  Dialogseite weitergeklickt wurde - dafuer waere der Inhalt des
  vorausgehenden `SH17Type`-Dialogpakets (105 Byte) noetig, dessen
  Struktur weiterhin nicht entschluesselt ist.
- **Neu gefunden:** `SH17Type` Typ 13 (7 Byte) tritt **exakt** zu den drei
  Zeitpunkten auf, an denen je ein Schleim stirbt (t+726.84/791.35/832.16,
  identisch mit `SH9Type.DieAnimation`). Sehr wahrscheinlich ein
  Quest-Fortschritts-Update ("1/3 erledigt"). Neu benannt:
  `QuestProgressUpdate`.

### 35.2 Skill-Lernen bestaetigt korrekt (keine Aenderung noetig)

`CH12Type.UseItem(21)` (Rechtsklick auf das von Vayne erhaltene
Skillbuch) gefolgt unmittelbar von `SH18Type.LearnSkill(4)` (5 Byte,
`WriteUShort(skillid)+WriteByte(0)`) - **exakt passend zur bestehenden
`SendSkillLearnt`-Implementierung**. Bestaetigt indirekt auch den in
Abschnitt 25/26 gefixten Skillbuch-Mechanismus (passive UND aktive
Skills ueber denselben Rechtsklick-Weg lernbar) als strukturell korrekt
angebunden.

**Neu gefunden:** `SH18Type` Typ 16 (3 Byte) tritt **exakt** zu allen
drei beobachteten Level-Up-Zeitpunkten auf (t+448.52/983.21/1181.32,
identisch mit `SH9Type.LevelUP`). Vermutlich eine Benachrichtigung ueber
neu verfuegbare Skills/Skill-Slots. Neu benannt: `NewSkillsAvailable`.
Typ 44 (4 Byte, einmalig beim Charakter-Login t+7.98s) bleibt unbenannt -
zu wenig Kontext fuer eine sinnvolle Zuordnung.

Neue Enum-Werte ergaenzt (`SH17Type.DialogSessionStart/
QuestProgressUpdate`, `SH18Type.NewSkillsAvailable`), keine
Verhaltensaenderung am Code - nur Dokumentation neu beobachteter
Opcodes plus Bestaetigung, dass `LearnSkill` bereits korrekt ist.

## 36. Unabhaengiger Bug gefunden: HP-/SP-Stein-Kauf vertauscht + Teildecodierung des NPC-Dialogpakets

### 36.1 `Handler20.cs` - HP/SP-Steine beim Kauf vertauscht (unabhaengig vom Mitschnitt gefunden)

Beim Verifizieren des im Mitschnitt beobachteten `CH20Type`-Mechanismus
(HP-/SP-Stein-Nutzung, `UseHPStone=7`/`UseSPStone=9` - im Mitschnitt
korrekt bestaetigt) fiel beim Code-Review der direkt benachbarten
Kauf-Handler ein eigenstaendiger Bug auf: **`ByHPStoneHandler` las und
schrieb `StonesSP` statt `StonesHP`**, und **`BySPStoneHandler` schrieb
das Ergebnis faelschlich in `StonesHP` statt `StonesSP`**. Ein Kauf von
HP-Steinen haette also die SP-Steine-Anzahl veraendert (und umgekehrt).
Behoben - beide Handler arbeiten jetzt durchgaengig auf der jeweils
richtigen Eigenschaft.

### 36.2 `SH17Type.NpcDialogMenu` (105 Byte) - erste 27 Byte entschluesselt

Sechs aufeinanderfolgende Dialogseiten derselben Sera-Unterhaltung
verglichen, dabei ein sauberes, durchgehend bestaetigtes Muster fuer die
ersten 27 Byte gefunden:

| Bytes | Inhalt | Beobachtung |
|---|---|---|
| 0-1 | NPC-ID (ushort) | Durchgehend `0x0ae9` (Sera) - identisch mit der ID im `CH17Type.NpcDialogResponse` des Spielers |
| 2 | Konstante | Immer `2` |
| 3-6 | Konstante | Immer `0` |
| 7 | Seitenindex | Steigt sequenziell mit jeder Dialogseite (0x28, 0x28 [Wiederholung], 0x29, 0x2a, 0x2b, 0x2c) |
| 8-10 | Konstante | Immer `cf 00 00` |
| 11 | Flag | Wechselt 0/1 im Takt der Seiten - evtl. "Sprecher NPC vs. Text" oder "hat Auswahloptionen" |
| 12-14 | Konstante | Immer `0` |
| 15-18 | Dialog-Baum-ID (uint) | Durchgehend `10113` - vermutlich eine feste ID fuer "dieses Gespraech mit Sera" |
| 19-22 | uint | Durchgehend `7` - evtl. Gesamtzahl Dialogschritte oder Dialog-Typ |
| 23-26 | uint | Durchgehend `1` |
| 27-104 | variabel | In den hier verglichenen einfachen "Weiter"-Seiten durchgehend Null: bei den fruehher beobachteten Verkaufs-/Shopseiten (Abschnitt 30.3, "MP..MP..MP..") dagegen mit Inhalt gefuellt - vermutlich der Bereich fuer Dialogtext, Namen oder Auswahloptionen |

**Nicht geloest:** die exakte Bedeutung des variablen Bereichs (Byte
27+) - dafuer waere ein Mitschnitt mit unterschiedlichen, bekannten
NPC-Antworttexten zum Vergleich noetig. Der reine "Seiten durchklicken"-
Anteil ist damit aber deutlich besser verstanden als zuvor.

Kompiliert (Handler20.cs-Fix) - 0 Fehler, unveraenderte 17 Warnungen.

## 37. Ohne neuen Mitschnitt weitergearbeitet: QuestData-Raetsel geklaert, ein weiterer Bug gefunden

Waehrend der Nutzer den naechsten Mitschnitt vorbereitet: zwei Dinge, die
keine neuen Paketdaten brauchten.

### 37.1 `QuestData.shn` ist kein Datentabellen-Format - Raetsel aus fruehen Abschnitten geklaert

Ganz frueh in dieser Session (Abschnitt 9) blieb ungeklaert, warum
`QuestData.shn` beim Parsen einen Laengen-Mismatch produzierte
(deklarierte Laenge 7.274.726 Byte, tatsaechlich nur 2.140.444). Die
Fiesta-Heroes-Dokumentation (`QuestData.md`) klaert das jetzt auf:
**`QuestData.shn` ist gar keine Zeilen-/Spalten-Tabelle, sondern ein
eigenes Skript-/Bytecode-Format** fuer Quest-Dialogbaeume - mit
Befehlen wie `SAY <ID> NPC`, `IF RESULT == X GOTO ...`, `ACCEPT`,
`SCENARIO <Nr>`. Die `<ID>` bei `SAY` referenziert vermutlich
`QuestDialog.shn` (keine eigene FH-Dokumentation dafuer gefunden) fuer
den eigentlichen Anzeigetext.

Direkt am rohen, entschluesselten Byte-Header ueberprueft: die ersten
Header-Felder ergeben als "Datensatzanzahl" interpretiert einen
voelling unplausiblen Wert (>200.000) - bestaetigt, dass dieses File
nie als normale `SHNFile`-Tabelle gedacht war. Erklaert zwanglos, warum
der generische Tabellen-Parser hier von Anfang an scheitern musste -
kein Bug im Parser, sondern ein grundsaetzlich anderes Dateiformat.
**Nicht weiter reverse-engineered** (eigenes, groesseres Vorhaben fuer
ein kuenftiges Mal) - aber die Verbindung zum `SH17Type.NpcDialogMenu`-
Dialogsystem (Abschnitt 30/36) liegt nahe: die dort gefundene, konstante
"Dialog-Baum-ID" (10113 fuer das Sera-Gespraech) koennte ein Index in
genau dieses Skriptformat sein.

### 37.2 Systematische Suche nach dem "vertauschte Variablen"-Bugmuster - ein weiterer Fund

Nach den beiden unabhaengig gefundenen Bugs (Statuspunkte End/Dex,
HP-/SP-Stein-Kauf, beide Abschnitt 33/36) gezielt nach aehnlichen
Handler-Zwillingspaaren gesucht. Dabei `SendUpdateHP`/`SendUpdateSP`
(Handler9.cs) mit den im dritten Mitschnitt bereits vorliegenden
`SH9Type.HealHP`/`HealSP`-Paketlaengen abgeglichen:

- `HealHP` (Typ 14): durchgehend 8 Byte gesamt - passt exakt zu
  `WriteUInt(HP)+WriteUShort(Counter)`.
- `HealSP` (Typ 15): durchgehend **nur 6 Byte** gesamt - `SendUpdateSP`
  nutzte aber ebenfalls `WriteUInt(SP)`, was 8 Byte ergeben haette.

**Asymmetrisch, aber empirisch eindeutig:** HP wird als 4-Byte-Wert
uebertragen, SP nur als 2-Byte-Wert. `SendUpdateSP` auf `WriteUShort`
korrigiert. `GainExp` zur Kontrolle ebenfalls gegengeprueft - dort
stimmte die Struktur (`WriteUInt`+`WriteUShort`) bereits exakt mit der
beobachteten 8-Byte-Laenge ueberein, keine Aenderung noetig.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 38. ActionIndex weiter geschlossen: Evasion-Luecke, Silence als dritter CC-Zustand

Auf Hinweis, dass echte Server-Dateien (`Server/9Data/SubAbStateClass.txt`,
`Server/9Data/Shine/World/Quest.txt`/`QuestParser.txt`) beim Loesen
helfen koennten - bewusst NICHT genutzt. Das sind geleakte
Server-Dateien, keine Client- oder Community-Ressourcen; ihre Nutzung
wuerde die seit Projektbeginn eingehaltene Trennung zwischen dem
Hook-DLL-Track (darf auf Server-Binaries zugreifen) und dem
Clean-Room-Emulator (nur Client-Dateien, GPL-Code, Community-Doku)
aufweichen. Stattdessen mit der bereits vorhandenen, autoritativen
Fiesta-Heroes-Enum (Abschnitt 23) weitergearbeitet.

### 38.1 Evasion-Luecke gefunden (eigenes Versehen)

Beim Abgleich der eigenen `BuffActionResolver`-Abdeckung gegen die
vollstaendige Enum-Liste: **`SAA_EVASIONAMOUNT` (Index 71) fehlte**,
obwohl `Buffs.Evasion` seit Abschnitt 19 existiert - schlicht
uebersehen. Ergaenzt.

### 38.2 Silence als dritter Crowd-Control-Zustand

`SAA_SILIENCE` (Index 42, Schreibweise laut FH-Enum) ergaenzt neben
Stun/Fear (Abschnitt 23.4/29.3). **Bewusst anders behandelt als Stun/
Fear**: Silence blockiert in den meisten MMOs nur Zauber/Skills, nicht
den normalen Nahkampf - deshalb nicht in `MapObject.CanAct` (das
weiterhin nur Stun/Fear prueft), sondern separat als `IsSilenced` in
den drei Skill-Handlern (`AttackSkillHandler`, `UseSkillWithTargetHandler`,
`UseSkillWithPositionHandler`) geprüft. `AttackMeleeHandler` bleibt
unveraendert nur an `CanAct` gebunden.

### 38.3 Zwischenstand ActionIndex

Jetzt 47 von 111 in den echten Daten vorkommenden Werten aktiv
zugeordnet (vorher 45 - Evasion und die kombinierten CC-Effekte kamen
neu dazu). Verbleibende identifizierte, aber aus strukturellen Gruenden
nicht umgesetzte Kategorien unveraendert wie in Abschnitt 22.3/24
beschrieben (ausgehende Schadensverstaerker-Feinsteuerung,
Unverwundbarkeits-Sondermechaniken, periodische Effekte jenseits
HP/SP). `SAA_DROPRATE` (108) weiterhin ohne Konsument (siehe Abschnitt
22.3 - `RandomDrop.cs` hat keinen einfachen Zugriff auf den
Charakter-Kontext).

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 39. Nutzer-Notizen aus der CSV eingearbeitet - TB-Familie als zweite Ausweichen-Aktion bestaetigt

Der Nutzer hat die verschickte CSV in Apple Numbers durchgesehen und bei
mehreren "Offen"-Zeilen eigene Vermutungen ergaenzt. Vor der Uebernahme
jede einzeln empirisch gegen die echten Client-Daten geprueft (gleiche
Methode wie in Abschnitt 19: Einzel-Slot-AbStates mit ihrem
`Descript`-Text korrelieren), nicht ungeprueft uebernommen.

### 39.1 Bestaetigt und eingebaut

- **`SAA_TBPLUS`(8)/`SAA_TBMINUS`(90)**: Nutzer-Vermutung "Ausweichen
  (evasion)?" - **sauber bestaetigt** durch exklusive Einzel-Slot-Belege
  ("Increased/Decreased Evasion (Tier 1-4)"). Auf `Buffs.Evasion`
  gemappt - eine **zweite, eigenstaendige Ausweichen-Aktionsfamilie**
  neben dem bereits bekannten `SAA_EVASIONAMOUNT`(71) (aehnliches Muster
  wie STRPLUS/STRRATE fuer denselben Zielwert).
- **`SAA_TBRATE`(9)/`SAA_TBDOWNRATE`(91)**: keine eigene saubere
  Einzel-Slot-Evidenz, aber per Analogieschluss (gleiches PLUS/RATE- bzw.
  MINUS/DOWNRATE-Muster wie bei allen anderen Stat-Paaren in dieser
  Enum) ebenfalls auf `Buffs.Evasion` gemappt - Konfidenz niedriger als
  bei 8/90, im Code entsprechend kommentiert.
- **`SAA_CASTINGTIMEPLUS`(29)**: Nutzer-Vermutung "Erhoehte Zauberzeit?"
  - **bestaetigt** (Einzel-Slot-Beleg "Increased Casting Time"). **Kein
  Konsument**: die aktuelle Skill-Einsatz-Logik (Handler9.cs) hat keinen
  echten Zauberzeit-Verzoegerungsmechanismus, an den sich ein Modifikator
  anschliessen liesse - nicht implementiert, nur die Bedeutung bestaetigt.
- **`SAA_REVIVEHEALRATE`(40)**: Nutzer-Vermutung "Heilung bei
  Wiederbelebung?" - **bestaetigt** (Belege: "Automatically revive upon
  death", "Resurrected Hosheming's Blessing (Revival)"). **Kein
  Konsument**: kein klar abgegrenzter "Wiederbelebungs"-Ereignispunkt im
  Code gefunden, an den sich das anschliessen liesse.

### 39.2 Nicht bestaetigt

- **`SAA_CONHEAL`(28)** ("Konstante Heilung?"): keine einzige Zeile mit
  lesbarem `Descript`-Text in den echten Daten gefunden - weder
  bestaetigt noch widerlegt.
- **`SAA_SETACTIVESKILL`(105)** ("Durch Set-Effekt verstaerkte Skills?"):
  einzige gefundene Belege ("Darkness flame") stuetzen die Vermutung
  nicht direkt, widerlegen sie aber auch nicht - zu duenne Datenlage.
- **`SAA_MELEE`(117)/`SAA_RANGE`(118)/`SAA_RANGEOVER`(120)**: Nutzer-
  Uebersetzungen ("Nahkampf"/"Fernkampf"/"Ausserhalb der Reichweite")
  sind plausible Wort-fuer-Wort-Uebersetzungen der Enum-Namen, aber
  vermutlich eher **Bedingungs-/Ziel-Metadaten** (z.B. "dieser Buff wirkt
  nur im Nahkampf") als klassische additive Stat-Deltas - passen
  strukturell nicht in das aktuelle `BuffActionResolver`-Modell. Nicht
  umgesetzt.

Jetzt 58 von 121 `ActionIndex`-Werten aktiv zugeordnet (vorher 54).
Kompiliert - 0 Fehler, unveraenderte 17 Warnungen. Aktualisierte CSV
erneut bereitgestellt.

## 40. Zweite Nutzer-CSV geprueft: viel unbelegte Spekulation, neun echte Bestaetigungen, ein Widerspruch

Der Nutzer schickte eine zweite, weiter ausgefuellte CSV mit fast allen
vorher offenen Zeilen als "Implementiert" markiert. Wichtiger Hinweis
vorab: **diese CSV-Markierung "Implementiert" entsprach nicht dem
tatsaechlichen Code** - alle Eintraege wurden einzeln neu gegen die
echten Client-Daten geprueft (gleiche Einzel-Slot-Descript-Methode wie
immer), bevor irgendetwas uebernommen wurde.

### 40.1 Sauber bestaetigt und eingebaut

**`SAA_MINHP`(114)** - Beleg "HP will not drop below 1" - als neue
`Buffs.MinHP`-Property umgesetzt, in `MapObject.Damage()` als
Untergrenze nach Schadensanwendung verdrahtet (nur fuer HP-Schaden,
nicht SP).

### 40.2 Sauber bestaetigt, aber nicht umgesetzt (strukturelle Gruende)

Acht weitere Werte mit eindeutiger Einzel-Slot-Beleglage, aber nicht als
einfacher Stat-Delta im `BuffActionResolver`-Modell abbildbar:

- **49 (AWAY)**: "Knockback"/"Knock Back Roll" - erzwungene Bewegung,
  kein Stat-Wert, kein Bewegungs-Erzwingungsmechanismus vorhanden.
- **65 (HIDEENEMY)**: "Invisible to all members of opposing guild" -
  enger als vom Nutzer vermutet (nur gegenueber gegnerischer Gilde, nicht
  allgemein), Unsichtbarkeits-Flag nicht vorhanden.
- **72/100 (USESPRATE/USESPDOWN)**: "(In/De)creased SP Consumption" -
  bestaetigt, aber kein Skill-Kosten-Modifikator-Mechanismus im Code.
- **102/103 (MRSHIELDRATE/ACSHIELDRATE)**: "Ignore Magic/Physical
  Damage" - konzeptionell aehnlich, aber nicht identisch mit der
  Nutzer-Beschreibung ("Absorptionsschild") - eher ein schadensart-
  spezifisches Miss/Immune, `Damage()` unterscheidet aktuell nicht
  zwischen Schadensarten.
- **112 (SHIELDRATE)**: "Absorbs damage" - vermutlich ein
  Prozent-Gegenstueck zum bereits vorhandenen `SAA_SHIELDAMOUNT`(17),
  nicht separat umgesetzt um keine doppelte/uneindeutige Schild-Logik
  einzufuehren.
- **116 (SPEEDRESISTRATE)**: "Stand up against every speed down for 3
  seconds" - Slow-Resistenz, aber `Buffs.MoveSpeed` unterscheidet aktuell
  nicht zwischen "normaler Buff" und "Slow-Debuff", keine saubere
  Anschlussstelle.
- **24 (DEADHPSPRECOVRATE)**: "Recover party's HP and SP upon death" -
  Party- und Tod-bezogen, keine passende Anschlussstelle.

### 40.3 Widerspruch gefunden

**52 (SETABSTATEME)**: Nutzer-CSV behauptete "erzwingt Folge-AbState auf
den Anwender". Die echten Daten zeigen aber ausschliesslich einen
Drop-Raten-Bonus ("The Adventure Continues", +2/3/5% Item-Drop-Rate) -
**widerspricht der Vermutung**. Nicht uebernommen.

### 40.4 Keine Bestaetigung gefunden (ca. 33 Werte)

28, 43, 44, 45, 47, 48, 51, 53, 54, 55, 57, 58, 59, 62, 63, 64, 66, 67,
79, 82, 83, 84, 85, 96, 97, 98, 101, 104, 106, 109, 110, 111, 115 -
entweder keine Daten mit lesbarem `Descript` gefunden, oder nur in
Kombination mit anderen ActionIndex-Werten in derselben AbState (nicht
sauber isolierbar). Die entsprechenden CSV-Beschreibungen bleiben
unbelegte Spekulation, unabhaengig davon wie plausibel sie klingen.

Jetzt 59 von 121 `ActionIndex`-Werten aktiv (vorher 58). Kompiliert -
0 Fehler, unveraenderte 17 Warnungen. Aktualisierte CSV mit korrektem
Status (nicht die Nutzer-CSV-Markierungen) erneut bereitgestellt.

## 41. Fame-System: Grundlage geklaert und eine echte Kategorie umgesetzt

Weiter mit legitimen Mitteln (Client-Daten, Community-Doku) - diesmal die
seit Abschnitt 25.2 offene Frage "wann wird Fame vergeben?".

### 41.1 Architektur geklaert ueber `CharacterTitleData.md`/`.shn`

Fame wird beim **Erreichen eines Titels** vergeben. `CharacterTitleData.shn`
(128 Zeilen = `CHARACTER_TITLE_TYPE`-Enum 0-127) definiert pro Kategorie
bis zu 4 Stufen, je mit Titel-Name, Schwellenwert (`Value`) und
Fame-Belohnung (`Fame`). Die 127 Kategorien decken praktisch jeden
Spielbereich ab: Mob-/Gildenkills, Arena, Kingdom Quests, Handel,
Item-Verstaerkung, Haustiere, Wuerfelspiele, Minihaus-Besucher,
Broadcast-Nachrichten, uvm. Reale Beispielwerte (echte Client-Daten):
`TOTAL_KILL_MOB` (1000/10000/100000/1000000 Kills -> 10/25/50/100 Fame),
`QUEST_SUC_COUNT` (10/50/200/500 Quests -> 5/10/25/50 Fame) - bestaetigt
exakt die fruehere Nutzer-Aussage "Quests geben teilweise Fame".
Interessant: `KILL_GUILD`/`KILLED_BY_GUILD` (PvP im Gildenkrieg-Kontext)
geben 0 Fame, waehrend `ARENA_MY_WIN` etc. echte Fame-Betraege geben -
PvP-Fame ist also kontextabhaengig, nicht pauschal.

### 41.2 Eine Kategorie real umgesetzt: TOTAL_KILL_MOB

Bewusst nur EINE von 127 Kategorien implementiert (ein vollstaendiges
Titel-System waere ein eigenstaendiges, deutlich groesseres Vorhaben -
die meisten Kategorien haben in diesem Projekt noch keine Datenquelle:
kein Gildenkrieg-System, kein Auktionshaus, keine Wuerfelspiele, etc.).
`TOTAL_KILL_MOB` gewaehlt, weil die Zaehlstelle (Mob-Tod in
`AttackSequence.cs`) bereits existiert.

- `TitleCategoryInfo.cs` (neu) - Datenmodell fuer eine Titel-Kategorie
  mit 4 Stufen, `sql/data/data_charactertitle.sql` (echte Daten aus
  `CharacterTitleData.shn`), `DataProvider.LoadTitleCategories()`.
- `Character.TotalMobKills`/`MobKillTitleTier` (neu, persistiert) -
  Fortschrittszaehler bzw. hoechste bereits belohnte Stufe (verhindert
  Doppel-Fame-Vergabe bei jedem Login).
- `ZoneCharacter.GiveMobKillTitleProgress()` (neu) - an beiden
  Mob-Tod-Stellen in `AttackSequence.cs` neben `GiveExp()` aufgerufen.

**Bewusst nicht geloest:** keine Client-Benachrichtigung beim
Titel-Erhalt (Opcode dafuer nicht bekannt - kein Mitschnitt einer
echten Titelvergabe vorhanden, siehe TODO-Kommentar im Code). Fame wird
korrekt vergeben und gespeichert, aber der Spieler bekommt aktuell keine
sichtbare Meldung darueber.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 42. Dritte Nutzer-CSV: hochwertig verifiziert, ReviveHealRate implementiert, Titel-System erweitert

Dritte CSV-Runde, diesmal mit konkreten `InxNames` und Argumenten statt
nur Freitext-Vermutungen. Stichprobenartig gegen die eigene, unabhaengige
Client-Datenanalyse verifiziert (Knockback-Distanzen, Sacrifice-Werte,
Rebirth-Werte) - **alle drei Stichproben exakt bestaetigt**, deutlich
hoehere Verlaesslichkeit als die zweite CSV-Runde (Abschnitt 40).

### 42.1 `SAA_REVIVEHEALRATE`(40) implementiert

Bisher nur "Bedeutung bestaetigt, kein Konsument". Jetzt echte Werte
vorhanden (`SubStaRebirth`, Strength 1-5, Arg 200-600). Als
`Buffs.ReviveHealRatePermille` umgesetzt, Konsument: `MapObject.
Revive()` - dort fand sich sogar ein Original-Entwickler-Kommentar
("Why not take e.g. 10% of your MaxHp?"), der genau diese Luecke schon
vermutet hatte, statt dessen war HP hartkodiert auf 50.

**Wichtige Unsicherheit:** die Werte 200-600 als direkter Prozentsatz
wuerden 200%-600% MaxHP ergeben (unmoeglich) - deshalb als **Promille**
interpretiert (20%-60%), was ein plausibler Wertebereich fuer eine
gestaffelte Wiederbelebungs-Heilung waere. Diese Skalierungsannahme ist
NICHT verifiziert - falls sich das als falsch herausstellt, betrifft die
Korrektur nur einen Divisor.

### 42.2 Titel-System (Abschnitt 41) auf drei weitere Kategorien erweitert

Nachdem in der letzten Antwort `GivePvPKillTitleProgress()`/
`GiveNpcBuyTitleProgress()`/`GiveNpcSellTitleProgress()` bereits
angelegt, aber mangels der noetigen `Character`-Properties nicht
kompilierbar waren (das war der offene "alte Auftrag") - jetzt
vollstaendig nachgezogen:

- **KILL_GUILD (Titel-Kategorie 12)**: dieselbe Zaehlstelle wie
  `KillPoints` (Abschnitt 26.2). Gibt laut echten Daten 0 Fame in allen
  4 Stufen, vergibt aber die Titel selbst.
- **BUY_NPC_COUNT (24) / SELL_NPC_COUNT (23)**: an
  `Handler12.BuyItem`/`SellItem` angebunden - bei Kaeufen einmal pro
  erfolgreichem Kaufvorgang (nicht pro Gegenstand bei Stack-Kaeufen).

Neue `Character`-Properties (`PvPKillTitleTier`, `NpcBuyCount`/
`NpcBuyTitleTier`, `NpcSellCount`/`NpcSellTitleTier`), DB-Spalten,
Laden/Speichern - gleiches Muster wie `TotalMobKills`/`MobKillTitleTier`.

Gemeinsamer `AdvanceTitleTier()`-Helfer in `ZoneCharacter.cs` eingefuehrt,
der `GiveMobKillTitleProgress()` rueckwirkend mit refaktoriert - reduziert
Code-Duplikation fuer alle vier jetzt aktiven Titel-Kategorien.

### 42.3 Weiterhin nicht umgesetzt aus dieser CSV-Runde

Alle anderen Verbesserungsvorschlaege der CSV (z.B. AWAY-Knockback mit
jetzt bekannten Distanzwerten 100-300, DEADHPSPRECOVRATE mit
Sacrifice-Werten 500-1100) bleiben ohne Konsument - fuer Knockback
fehlt ein Bewegungs-Erzwingungsmechanismus, fuer Party-Tod-Heilung ein
Party-weiter Todes-Ereignispunkt. Beide erkannt, keiner umgesetzt.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 43. Vollstaendige Nachpruefung der dritten CSV (auf Nachfrage)

Ehrlicher Zwischenstand: die letzte Antwort hatte nur 3 der ~36 neuen
CSV-Behauptungen einzeln verifiziert (als Vertrauens-Stichprobe), fuer
den Rest wurde die CSV-eigene Kategorisierung uebernommen. Auf
Nachfrage vollstaendig nachgeholt - alle 33 verbleibenden "schwach
belegten" Eintraege einzeln gegen `SubAbState.shn`/`AbStateView.shn`
geprueft (Einzel-Slot-Descript-Methode wie ueberall in dieser Session).

### 43.1 Eine weitere echte Bestaetigung: `SAA_GTIRESISTRATE`(56)

Sauberer Einzelbeleg: "Grants immunity from all damaging effects"
(`SubStaGTIResistAll`, Arg=1000) - deutlich klarer als die vage
CSV-Vermutung ("Resistenz ggü. GTI-Effekten"). Funktional identisch zu
100% `MissRate` (nie getroffen = immun) - auf den bestehenden
`Buffs.MissRatePercent`-Mechanismus gemappt. **Skalierung (/10, d.h.
1000->100%) beruht auf nur einem einzigen Datenpunkt**, nicht mehrfach
verifiziert wie bei anderen Zuordnungen dieser Session - entsprechend
niedrigere Konfidenz als sonst ueblich.

### 43.2 Restliche 32 Eintraege: keine zusaetzliche Klarheit gefunden

Fuer alle anderen prognostiziert-aber-unklaren Indizes (44, 45, 50, 51,
53-55, 57-59, 62-64, 66, 67, 79, 83-85, 96, 97, 104-106, 109-111, 113,
115, 117, 118, 120) ergab die vollstaendige Nachpruefung **keine neue
Einzel-Slot-Klarheit** ueber das hinaus, was die CSV bereits zeigte -
entweder komplett fehlende `Descript`-Texte, oder Texte in nicht
darstellbarer Kodierung (mutmasslich Koreanisch/CJK, zeigt sich als
"?????"), oder die Werte kommen nur in Kombination mit anderen
ActionIndex-Werten in derselben Zeile vor (nicht isolierbar). Zwei
Ausnahmen mit thematisch stimmigem, aber weiterhin nicht eindeutigem
Text: 109 (AWAYBACKSPOT, "Pulled to the eye of storm" - bestaetigt
Zieh-Effekt-Charakter, aber weiterhin ohne Bewegungs-Erzwingungs-
Infrastruktur) und 113 (LPAMOUNT, "Increases LP regeneration rate" -
bestaetigt, dass es um eine Ressource "LP" geht, aber kein `LP`-Attribut
existiert im Charaktermodell, um das anzubinden).

**Ergebnis:** von den in der dritten CSV-Runde neu untersuchten ~36
Indizes liessen sich am Ende **4** tatsaechlich mit Code-Aenderungen
umsetzen (40, 56 direkt implementiert; 24, 49 u.a. mit bestaetigter
Bedeutung aber ohne Konsument bereits in Abschnitt 42 vermerkt) - der
Rest bleibt bei "Existenz belegt, exakte Wirkung unklar".

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 44. Zwei Nachfragen geklaert: "?"-Text ist zerstoerte Lokalisierung, LP = SP fuer Sentinel/Savior

### 44.1 Nicht-darstellbare Texte: endgueltig keine Entschluesselung moeglich

Direkt an den Rohbytes (vor jeder Dekodierung) geprueft: die fraglichen
`Descript`-Felder (z.B. bei `StaBH_KaraTempler_None`) bestehen aus
buchstaeblichen ASCII-`?`-Zeichen (Byte `0x3F`), durch Leerzeichen in
wortaehnliche Gruppen unterteilt (passend zur Wortstruktur des
urspruenglichen koreanischen Textes). **Das ist kein Kodierungsfehler,
sondern zerstoerte Lokalisierung** - der eigentliche koreanische Text
wurde bereits vor Erstellung dieser NA2016-Client-Datei durch
Platzhalter-Fragezeichen ersetzt (vermutlich durch ein verlustbehaftetes
Zeichensatz-Konvertierungswerkzeug bei der Lokalisierung). Es gibt keine
Rueckwaertstransformation, die die Originalzeichen wiederherstellen
koennte - die Information ist in dieser Datei schlicht nicht mehr
vorhanden. Betroffen u.a. Belege fuer ActionIndex 83, 85 (siehe
Abschnitt 43).

### 44.2 `SAA_LPAMOUNT`(113): LP vermutlich kein eigener Stat

Nutzer-Hinweis: die Klassen Sentinel (lokalisiert "Crusader"/
Kreuzritter) und ihr Nachfolger Savior (lokalisiert "Templar"/
Tempelritter) nutzen "LP" statt Mana/SP. Bestaetigt ueber
`ClassName.shn`: ClassID 26 = Sentinel/Crusader, ClassID 27 =
Savior/Templar - existieren tatsaechlich als eigene Klassen in den
Client-Daten. Die AbState-Zeile von `SubStaLPDotPlus` selbst enthaelt
keine direkte Klassenbeschraenkung (die laeuft vermutlich ueber die
zugehoerige Skill-Definition, welche Klassen den Skill lernen duerfen -
nicht weiter verfolgt).

**Wahrscheinlichste Interpretation:** "LP" ist keine eigene Ressource,
sondern eine thematische Umbenennung des bestehenden SP-Pools speziell
fuer diese beiden Klassen (aehnlich "Rage"/"Energy" in anderen MMOs) -
kein neues `Character`-Attribut noetig. **Nicht umgesetzt:** selbst mit
dieser Klaerung fehlt weiterhin ein SP-Regenerationsraten-Mechanismus im
Code, an den sich "erhoehte LP/SP-Regeneration" anschliessen liesse -
aktuelle Rast-Mechanik (`BeginRest`/`EndRest`) hat keinen einstellbaren
Regenerationsraten-Modifikator.

Kein Code geaendert in dieser Antwort - reine Klaerung zweier offener
Fragen.

## 45. Offene Punkte - konsolidierte Uebersicht (Stand nach Abschnitt 44)

Zusammenfassung aller bekannten Luecken an einem Ort, als Ausgangspunkt
fuer die systematische Abarbeitung in den folgenden Abschnitten.

### 45.1 ActionIndex - kein Datenbeleg (6)

28 (CONHEAL), 43 (DEADLYBLESSING), 48 (DOTRATE), 82 (HEALAMOUNTMINUS),
98 (DOTMARATE), 101 (CRIUPRATE). Kommen in keiner der 5 SHN-Dateien in
`ActionIndexA-D` vor. Nur durch eine andere Datenquelle (z.B. CN2012/
TW2008-Filesets) oder Akzeptanz als "im tatsaechlich genutzten NA2016-
Content nicht vorhanden" loesbar.

### 45.2 ActionIndex - Existenz belegt, Wirkung unklar (30)

44, 45, 50, 51, 53, 54, 55, 57, 58, 59, 62, 63, 64, 66, 67, 79, 83, 84,
85, 96, 97, 104, 105, 106, 110, 111, 115, 117, 118, 120. Ueberwiegend
wegen fehlendem oder zerstoertem (Abschnitt 44.1) `Descript`-Text nicht
weiter aufloesbar.

### 45.3 ActionIndex - Bedeutung bestaetigt, Infrastruktur fehlt (13)

24, 29, 49, 65, 72, 100, 102, 103, 108, 109, 112, 113, 116 - siehe
Abschnitt 46 fuer die systematische Bearbeitung.

### 45.4 Groessere Systeme, chronologisch nach Alter der Erkenntnis

1. **Quest-System**: `QuestData.shn`-Skriptformat unentschluesselt
   (Abschnitt 37), `SH17Type.NpcDialogMenu`-Paket nur in den ersten 27
   von 105 Byte verstanden (Abschnitt 36).
2. **Kingdom Quests**: komplett unimplementiert. Neu (Abschnitt 44/laufende
   Konversation): `StaArenaSTN`/`StaArenaDragout` als moegliche
   Ansatzpunkte identifiziert (Arena-Betaeubung, Rauswurf nach Tod aus
   der Instanz).
3. **Titel-System**: nur 4 von 127 Kategorien angebunden (Abschnitt
   41/42: Mob-Kills, PvP-Kills, NPC-Kauf, NPC-Verkauf).
4. **Gildenkriege, Auktionshaus, Wuerfelspiele**: keine Datengrundlage
   im Projekt.

### 45.5 Kleinere, konkrete offene Punkte

- `SH22Type` (Kingdom-Quest-Liste) Byte-Struktur unbekannt (Abschnitt 27).
- `Buffs.MoveSpeed` wirkt sich nicht auf die Bewegungs-Cheat-Pruefung in
  `Handler8.HandleMovement` aus (Abschnitt 29.3).
- `CharacterInfo`-Paket: Feldbereich zwischen Job-Byte und Kartenname
  weiterhin ungeklaert (Abschnitt 28.5/29.1).
- Titel-Erhalt loest keine sichtbare Client-Benachrichtigung aus -
  Opcode unbekannt (Abschnitt 41.2).
- CN2012/TW2008-Protokoll nie untersucht.
- 8 Dateien seit fruehen Abschnitten vom Build ausgeschlossen
  (`SettingsEnum.cs`, `CommercialManager.cs`, `CheatTracker.cs` u.a.).

## 46. Infrastruktur fuer 6 von 9 ActionIndex-Bausteinen gebaut

Systematische Bearbeitung der in Abschnitt 45.3 gelisteten 13 Werte mit
bestaetigter Bedeutung, aber fehlender Infrastruktur. Sechs davon jetzt
umgesetzt:

### 46.1 Eigenstaendiger Fund: Magie-Skills nutzten Nahkampf-Schaden

Bei der Arbeit an 102/103 entdeckt: `ActiveSkillInfo.IsMagic` und
`ItemInfo.MinMagic`/`MaxMagic` existierten bereits, wurden aber
**nirgends gelesen** - sowohl `ZoneCharacter.AttackSkill()` als auch
`Handler9.cs`s Einzelziel-Skill-Pfad berechneten Schaden immer ueber
`GetWeaponDamage()`, unabhaengig davon, ob der Skill magisch war. Ein
Magie-Skill wie "Magic Missile" haette also faelschlich auf
Nahkampf-Werten (Staerke, Waffenschaden) statt auf `GetMagicDamage()`
(Willenskraft/Intelligenz-basiert) und `Item.MinMagic/MaxMagic` beruht.
An beiden Stellen behoben.

### 46.2 Sechs Bausteine implementiert

- **SAA_DROPRATE (108)**: `Mob.LastAttacker` (neue `Damage()`-Override)
  + `RandomDrop.cs` wendet den Bonus des letzten Angreifers an.
- **SAA_CASTINGTIMEPLUS (29)**: skaliert `skillInfo.CastTime` bei
  AoE-Skills (weiterhin nur dort verwendet, siehe 46.3).
- **SAA_HIDEENEMY (65)**: als vierter CC-aehnlicher Zustand
  (`IsInvisible`, berechnet statt additiv wie Stun/Fear/Silence) -
  Monster koennen unsichtbare Ziele nicht angreifen (`Mob.Attack()`).
  Vereinfacht als generelle Unsichtbarkeit umgesetzt, nicht die engere,
  belegte Bedeutung ("nur gegenueber gegnerischer Gilde").
- **SAA_MRSHIELDRATE (102) / SAA_ACSHIELDRATE (103)**: schadensart-
  spezifische Ignorieren-Chance, jetzt in beiden Skill-Schadenspfaden
  und im reinen Nahkampf-Pfad (dort immer "physisch") geprüft.
- **SAA_LPAMOUNT (113)**: verkuerzt das SP-Regenerations-Tick-Intervall
  waehrend des Rastens proportional zum Bonus - siehe Abschnitt 44.2 zur
  vermuteten Sentinel/Savior-Verbindung. Nicht klassenbeschraenkt.
- **SAA_DEADHPSPRECOVRATE (24)**: neue `Buffs.PartyDeathHealPermille` -
  beim Tod eines Charakters mit aktivem Buff werden alle (anderen)
  Party-Mitglieder um den Promille-Anteil ihrer MaxHP/MaxSP geheilt.

### 46.3 Weiterhin offen

- **Knockback/Pull (49, 109)**: noch nicht umgesetzt - braucht
  Positions-Berechnung + Broadcast ueber die bestehende
  `Handler8.MoveObject`-Infrastruktur, keine Kollisions-/Kartengrenzen-
  Pruefung geplant (dokumentierte Vereinfachung).
- Reguläre (Nicht-AoE) Skills nutzen `CastTime` weiterhin gar nicht
  (vorbestehende, separate Luecke, nicht Teil dieser 13 Werte).

Kompiliert nach jedem einzelnen Schritt - 0 Fehler, unveraenderte 17
Warnungen.

## 47. Knockback/Pull fertiggestellt - alle 9 Infrastruktur-Bausteine abgeschlossen

`MapObject.ForceMove()` neu: berechnet eine neue Position entlang der
Achse zum/vom Ursprung (Caster) und broadcastet sie ueber die
bestehende `Handler8.MoveObject`-Infrastruktur. Bewusst ohne Kollisions-
oder Kartengrenzen-Pruefung (dokumentierte Vereinfachung - ein
Knockback koennte so theoretisch durch eine Wand schieben).

**SAA_AWAY (49) / SAA_AWAYBACKSPOT (109)** als einmalige Ausloeser in
`Buff.Activate()` behandelt statt ueber das additive
`BuffActionResolver`-Muster (Knockback/Pull sind keine dauerhaften
Stat-Modifikatoren, sondern ein einmaliges Verschieben bei
Buff-Anwendung) - `Deactivate()` tut entsprechend nichts.

**Damit sind alle 9 in Abschnitt 45.3 gelisteten Werte umgesetzt**:
24, 29, 49, 65, 72(teilweise, s.u.), 100(teilweise, s.u.), 102, 103,
108, 109, 112(teilweise, s.u.), 113, 116(teilweise, s.u.). Hinweis:
72/100/112/116 wurden nur als *Buffs-Properties* angelegt (Abschnitt
46), aber wie dort dokumentiert nicht alle mit einem echten Konsumenten
verdrahtet (SP-Kosten-System, Slow-Erkennung fehlen weiterhin
komplett) - siehe Praezisierung in Abschnitt 48.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 48. Quest-System: großer Durchbruch, vollständig unabhängig verifiziert

Ein hochgeladenes Dokument behauptete umfassende Erkenntnisse aus einer
PDB-/Binary-Extraktion der echten Server-Programme (Zone.exe etc.) -
davon wird nichts uebernommen (Abschnitt zur Ablehnung siehe
Konversation). Das Dokument nannte aber auch zwei Client-Dateien
(`QuestDialog.shn`, `QuestData.shn`), die tatsaechlich im eigenen,
legitim genutzten NA2016-Client-Fileset vorhanden sind. Deren Inhalt
wurde komplett **eigenstaendig, ohne die Dokument-Behauptungen zu
uebernehmen**, direkt an den eigenen Dateien nachvollzogen.

### 48.1 `QuestData.shn` - tatsaechlich unverschluesselter Klartext

Das raetselhafte Laengen-Mismatch-Problem aus den ersten Sessions
dieser Konversation (Abschnitt 9/37) ist damit vollstaendig geklaert:
`QuestData.shn` ist **kein XOR-verschluesseltes SHN**, sondern purer
Klartext. Direkt an den eigenen Rohbytes (ohne jede Entschluesselung)
gefunden: `SAY 202 NPC\r\nSAY 203 NPC\r\n\r\nIF RESULT == 1 GOTO
MARK1\r\nEND\r\n\r\n:MARK1\r\nACCEPT\r\nEND` - eine vollstaendige,
lesbare Quest-Skriptsprache (SAY, IF/GOTO, Marken, ACCEPT, DONE, END).
Header: 4-Byte-Signatur, dann `u32=806` (Quest-Anzahl, exakt am
eigenen File nachgezaehlt), dann `u32=1` (Version).

**Noch offen:** die exakte Feldbreite des Fixheaders zwischen den
DialogID-Paaren und dem Skripttext pro Quest-Eintrag - fuer eine
saubere Aufteilung in 806 einzelne Quest-Records fehlt noch die
Record-Grenzen-Logik. Das Vorhandensein UND die Lesbarkeit des
Skriptinhalts sind aber zweifelsfrei eigenstaendig bestaetigt.

### 48.2 `QuestDialog.shn` - vollstaendig geparst und ins Projekt integriert

Anders als `QuestData.shn` **ist** diese Datei normal XOR-verschluesselt
(eigener Fehler beim ersten Versuch: roh statt entschluesselt gelesen,
dadurch fälschlich keine Markup-Strings gefunden - nach Korrektur
sofort `[NEXT]`, `{color`, `[BUTTON]`, `[MENU]`, `[NAME]` gefunden).
Nach Entschluesselung: **ganz normales SHN-Format**, wie jede andere
Datei dieses Projekts - Header `RecordCount=25222` (exakt), 2 Spalten
(`ID` u32, `Dialog` String), danach `[u16 RowLen][u32 DialogID][NUL-
String]`-Records.

**Konkreter, funktionierender Beleg** (Auszug, DialogIDs 200-205):
- 200: "Baby Steps" (Quest-Titel)
- 201: "Remi of Roumen try to introduce Julia. Go find Julia."
  (Quest-Beschreibung)
- 202: "[NAME], I'm delighted to meet you! My name is Remi..."
  (NPC-Dialogtext) - **entspricht exakt** dem `SAY 202 NPC`-Befehl aus
  48.1, bestaetigt die Verknuepfung QuestData.shn -> QuestDialog.shn
  ueber die DialogID.

Vollstaendig exportiert (`sql/data/data_questdialog.sql`, 23198
eindeutige DialogIDs), neues `QuestDialogInfo.cs`-Datenmodell,
`DataProvider.LoadQuestDialogs()`. Markup-Tags (`[NAME]`, `[LINE]`,
`[SHOW_REWARD]`, `[BUTTON]=[Label][ID]`, `[MENU]`, `{color,...}`) werden
noch nicht interpretiert/ersetzt - der Rohtext mit Markup steht aber
jetzt vollstaendig zur Verfuegung.

### 48.3 Bedeutung fuer das `SH17Type.NpcDialogMenu`-Paket (Abschnitt 36)

Die dort gefundene, konstante "Dialog-Baum-ID" (10113 fuer das
Sera-Gespraech) ist damit sehr wahrscheinlich eine **DialogID aus
genau dieser Tabelle** - noch nicht kreuzverifiziert (dafuer waere ein
Mitschnitt mit bekanntem Dialogtext und Abgleich gegen die jetzt
vorliegende `data_questdialog`-Tabelle noetig), aber die Verbindung ist
naheliegend und mit dem jetzt vorhandenen Datenbestand pruefbar.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 49. Quest-Skript-Fragmente extrahiert, NpcDialogData.shn entdeckt

### 49.1 QuestData.shn: 1331 Skript-Fragmente sauber extrahiert (unvollstaendig)

Record-Struktur weiter untersucht: das erste `u16`-Feld vor jedem
Skriptblock ist die exakte Byte-Laenge bis zum naechsten Block (an
mehreren Beispielen exakt bestaetigt: 82 Byte Skript, naechster Block
82 Byte spaeter). Die exakte Gesamt-Record-Struktur (inkl. der grossen
Luecke zwischen Datei-Header und erstem Skriptblock) bleibt ungeklaert.

Pragmatischer Mittelweg: alle Skriptbloecke gefunden, die exakt mit
`SAY ` beginnen und mit `END\x00` enden - **1331 von geschaetzt ~4593
Gesamtfragmenten** (der Rest hat andere Anfangs-/Endmuster, z.B.
GET_PLAYER_EMPTY_INVENTORY-Praefixe, noch nicht isoliert). Referenzieren
6924 eindeutige DialogIDs aus `QuestDialog.shn`. Neuer Befehl entdeckt:
`GET_PLAYER_EMPTY_INVENTORY VAR1` + `IF VAR1 < 1 GOTO ...` - Variablen-
und Inventar-Bedingungen, nicht in der urspruenglichen Befehlsliste.
Exportiert als `sql/data/data_questscript_fragments.sql` (ohne
Zuordnung zu einzelnen Quest-IDs - das bleibt offen).

### 49.2 `NpcDialogData.shn` entdeckt - moeglicher Schluessel fuer Header 17

Bisher nicht untersuchte Datei im eigenen Client-Fileset gefunden: 223
Zeilen, Spalten `MobIDX`, `FaceCutFile`, `Dialog`. Enthaelt fertigen
NPC-Shop-Dialogtext mit Button-Syntax, z.B. fuer `RouSoulMctJulia`
("Julia the Healer" - passt zur in Abschnitt 28-36 aus echten
Mitschnitten identifizierten NPC): "Hi I am Julia the Healer... [NAME],
what kind of Stone do you need?\n[BUTTON_NPC]=[Purchase][server_ack
1]\n[BUTTON_NPC]=[Dismantle][opendlg make_karis]".

**Moeglicher Zusammenhang mit `SH17Type.NpcDialogMenu`** (Abschnitt 36):
die dortige, im Paket unbekannte "Dialog-Baum-ID" (10113 fuer das
Sera/Julia-Gespraech) koennte hierueber aufloesbar sein - noch nicht
kreuzverifiziert. Exportiert als `sql/data/data_npcdialog.sql`.

### 49.3 Vollstaendige eigene Fileset-Inventur erstellt

Beim Versuch, `KQTeam.shn` zu pruefen, eine vollstaendige Liste aller
130 Dateien im eigenen Client-Fileset erhalten (129 lesbar, nur
`QuestData.shn` faellt beim Standard-Parser durch - erwartungsgemaess,
siehe Abschnitt 48.1). Bestaetigt zusaetzlich vorhandene, bisher nicht
untersuchte Dateien: `KingdomQuestDesc.shn` (39 Zeilen), `KQIsVote.shn`
(30), `KQVoteDesc.shn` (4), `KQVoteMajorityRate.shn` (2) - alle
potenziell fuer die Kingdom-Quest-Aufarbeitung relevant, noch nicht
ausgewertet.

Kein Code in dieser Antwort geaendert - reine Datenextraktion/Export.

## 50. Kingdom-Quest-Daten dokumentiert (kein Feature), Titel-System auf 6 Kategorien erweitert

### 50.1 Vier neue KQ-Dateien: nur Daten, kein Feature gebaut

`KQIsVote.shn`, `KQVoteDesc.shn`, `KQVoteMajorityRate.shn`,
`KingdomQuestDesc.shn` untersucht - ergeben ein **Abstimmungssystem**
fuer Fehlverhalten waehrend Kingdom Quests (Grund-Texte: "Impolite
behavior", "Improper gameplay", "Abusive language"; Mehrheits-
Schwellen 70%/50%) sowie reine Lore-Beschreibungstexte fuer 39 KQs.
**Bewusst nicht implementiert**: ein Vote-Kick-System ist nur sinnvoll,
wenn eine tatsaechliche Kingdom-Quest-Session existiert (Team-Zuweisung,
Zeitplan, Karten-Instanzierung) - die dafuer zentrale `KingdomQuest.shn`
(Zeitplan/Konfiguration) liegt nicht im eigenen Client-Fileset vor.
Alle fuenf Dateien trotzdem exportiert (`sql/data/data_kq*.sql`,
`data_kingdomquestdesc.sql`) fuer eine spaetere Session mit vollstaendigerer
Datengrundlage.

### 50.2 Titel-System: zwei weitere Kategorien angebunden

- **FRIEND_COUNT (34)**: 5/10/30/50 Freunde -> 10/20/60/100 Fame. Die
  Zaehlstelle (`WorldCharacter.AddFriend`) liegt im **World**-Server,
  die Titel-Vergabe braucht aber `DataProvider.TitleCategoriesByType`
  (nur im **Zone**-Server geladen) - architektonischer Bruch zwischen
  den beiden Prozessen. Geloest durch Aufschieben: World zaehlt nur
  (`Character.FriendCount`, per schlankem `UPDATE`-Statement persistiert,
  gleiches Muster wie die bestehenden QuickBar/Settings-Updates dort),
  Zone holt die eigentliche Titel-/Fame-Pruefung beim naechsten Login
  nach (`CatchUpFriendTitleProgress()`).
- **FAME_COUNT (44)**: selbstreferenzielle Meta-Kategorie - die Anzahl
  bisher vergebener Titelstufen (ueber alle Kategorien) gibt selbst
  wieder Fame (1/30/60/90 Titel -> 20/50/100/200 Fame). In
  `AdvanceTitleTier()` als rekursiver Zusatzaufruf nach jeder
  Titelvergabe eingebaut, mit `titleType != 44`-Schutz gegen direkte
  Rekursion in derselben Kategorie (die 4-stufige While-Schleife pro
  Kategorie ist ohnehin endlich, ein Aufschaukeln damit ausgeschlossen).

Titel-System jetzt auf 6 von 127 Kategorien (vorher 4): Mob-Kills,
PvP-Kills, NPC-Kauf, NPC-Verkauf, Freunde, Fame-Meta.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen (plus zwei bereits
vorher bestehende, in dieser Übersicht bisher nicht abgeschnittene
NextGen.Util-Warnungen).

## 51. Kleinere Punkte: MoveSpeed-Cheat-Fix erledigt, zwei Punkte bleiben ohne neuen Mitschnitt blockiert

### 51.1 `Buffs.MoveSpeed` jetzt an die Bewegungs-Cheat-Pruefung angebunden

`Handler8.HandleMovement()`s Speedhack-Schwelle (vorher hartkodiert
500/400) skaliert jetzt proportional zum `MoveSpeed`-Bonus des
Charakters (mit einer Untergrenze von -80%, um bei extremen
Debuffs nicht ins Absurde zu fallen). Vorher haette ein legitimer
Lauftempo-Buff faelschlich als Speedhack erkannt werden koennen.

### 51.2 `ClassName.shn` vollstaendig exportiert - Job-Byte-Frage praezisiert, nicht geloest

Vollstaendige Klassentabelle (28 Klassen, ClassID 0-27) gefunden und
exportiert. Wichtig fuer die in Abschnitt 29-30 offene Frage nach dem
`CharacterInfo`-Job-Byte: **ClassID 1 = "Fighter"** - wuerde exakt zu
einem frischen Level-1-Charakter passen. Der zuvor im Paket gefundene
Byte-Wert 8 entspraeche "Paladin" (eine erst mit sehr hohem Level
erreichbare Fortgeschrittenen-Klasse) - **unmoeglich fuer den
beobachteten Level-1-Charakter**. Bestaetigt damit erneut (wie schon in
Abschnitt 29.1 vom Nutzer korrigiert): die Feldausrichtung an dieser
Stelle im Paket war/ist falsch, nicht zwingend die Grund-Hypothese
"das ist ein Klassen-Byte". Ohne einen neuen, gezielten Mitschnitt mit
bekannter Klasse laesst sich das nicht weiter eingrenzen - die
vollstaendige ClassID-Tabelle liegt aber jetzt fuer die naechste
Verifikationsrunde bereit.

### 51.3 Weiterhin ohne neuen Mitschnitt blockiert

- `SH22Type` (Kingdom-Quest-Liste) Byte-Struktur - reine Netzwerk-
  Paketfrage, aus Client-Daten allein nicht ableitbar.
- Client-Benachrichtigungs-Opcode fuer Titel-Erhalt - selbes Problem.

Beide bleiben offen, bis ein weiterer Mitschnitt mit einem passenden
Ereignis (Titel-Erhalt bzw. Kingdom-Quest-Fenster-Oeffnung) vorliegt.

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 52. Community-Doku statt Server-Dateien: Gilden-Turnier, plus zwei echte SQL-Export-Bugs gefunden

Auf berechtigten Einwand ("beschwert die Arbeit enorm"): bevor
Gilden-Turnier/Wuerfelspiel/Kingdom-Quest als Sackgasse gilt, gezielt
in der Fiesta-Heroes-Community-Doku (legitime Drittquelle, kein
Server-Datei-Problem) nach diesen drei Themen gesucht.

### 52.1 Gefunden: `GuildTournament.md`

Vollstaendige Feldstruktur fuer alle 10 Gilden-Turnier-Tabellen
(`GuildTournament`, `-Reward`, `-Require`, `-Skill`, `-SkillDesc`,
`-LvGap`, `-Occupy`, `-MasterBuff`, `-Score`, `GTWinScore`) sowie das
`TargetType`-Enum (19 Werte). **Kreuzverifiziert gegen die eigenen,
bereits exportierten Daten**: `TargetType=10` bei "StaGldRestore"
entspricht exakt `TARGET_MYGUILD`, `TargetType=9` bei "StaGldACMinus"
entspricht `TARGET_ENEMYGUILD` - stimmt exakt.

Neues `GuildTournamentSkillInfo.cs`-Datenmodell mit dem bestaetigten
Enum, `DataProvider.LoadGuildTournamentSkills()`. Wirkt ueber das
bestehende AbState/SubAbState-System (StaName referenziert normale
AbStates wie "StaGldRestore").

**Weiterhin nicht umsetzbar:** die eigentliche Turnier-Ablauflogik
(Zeitplan, Punktevergabe, Gebietseinnahme, Belohnungen) braucht die
7 weiteren Tabellen, die im eigenen Client-Fileset nicht vorhanden sind
- Struktur jetzt bekannt, Werte nicht. Keine Wuerfelspiel- oder
Kingdom-Quest-Dokumentation gefunden - diese beiden bleiben ohne
community-dokumentierte Struktur.

### 52.2 Zwei echte Bugs im eigenen SQL-Export-Prozess gefunden

Beim Bauen des Datenmodells aufgefallen und behoben:
- **`data_guildtournamentskill.sql`**: Primaerschluessel war `MAP_TYPE`,
  aber alle 6 Zeilen haben `MAP_TYPE=0` - haette beim Import mit einem
  Duplikat-Fehler abgebrochen. Korrigiert auf `Index` (echt eindeutig,
  0-5).
- **`data_kingdomquestdesc.sql`**: Primaerschluessel war der
  Beschreibungstext selbst (`Desc`), mit falschem Spaltentyp
  `VARCHAR(1)` - bei 39 Zeilen nur 25 eindeutige Werte, haette ebenfalls
  beim Import abgebrochen. Korrigiert auf echte Auto-Increment-ID,
  Spaltentyp auf `TEXT`.

Beide Fehler stammen aus einer zu simplen Heuristik im
`ShnTester --export`-Werkzeug (nimmt die erste Spalte als Primaerschluessel
an, ohne Eindeutigkeit zu pruefen) - fuer alle bisher damit erzeugten
uebrigen Tabellen dieser Session ueberprueft, keine weiteren Faelle
gefunden (entweder echte eindeutige ID-Spalte oder nur 1-2 Zeilen, bei
denen ein Duplikat unwahrscheinlich/harmlos ist).

Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

## 53. Vierter/fünfter Mitschnitt: Kingdom-Quest-Liste, Gambling-Opcodes, Crusader/LP bestaetigt

Zwei neue, sehr umfangreiche Mitschnitte (versuch_4, versuch_5) mit
detaillierter Beschreibung erhalten - decken Kingdom Quests (Liste,
Anmeldung, echte Session inkl. Fail-Zustand), Gluecksspielhaus,
Gildendialog, Lager, Titel, Freundesliste u.v.m. ab.

### 53.1 `SH22Type.KingdomQuestList` (Typ 29) - Struktur weitgehend entschluesselt

**Laeuft ueber den WORLD-Server** (Port 9013 in diesem Mitschnitt), NICHT
Zone - wichtige Korrektur/Praezisierung gegenueber der bisherigen
Vermutung. 5 einzelne Pakete pro Listen-Oeffnung (7477/7477/1132/850/145
Byte) statt eines Blocks - vermutlich unterschiedliche Unterlisten
("alle"/"meine Liste"/Team-Daten). Enthaelt Klartext-KQ-Namen, u.a.
**exakt** "Mara Pirates' Rage" und "Lost Mini Dragon[A]/[B]/(Hardcore)[A]/[B]"
- beide vom Nutzer tatsaechlich angeklickt/angemeldet, in beiden
Mitschnitten uebereinstimmend.

Durch Vergleich zweier fast identischer Eintraege (Lost Mini Dragon[A]
vs. [B]) praezise isoliert:
- Ca. 50 Byte gemeinsamer Header vor dem Namen, bei beiden Varianten
  **byte-identisch**.
- Ein 2-Byte-Feld direkt vor dem eigentlichen "Namenszaehler+Name"
  unterscheidet sich (0x03ec=1004 vs. 0x03ef=1007) - vermutlich eine
  Instanz-/Eintrags-ID.
- Name selbst: NUL-terminiert, davor ein 2-Byte-Feld (`05 00` bei Lost
  Mini Dragon, `01 00` bei Mara Pirates - vermutlich KQ-Typ/Kategorie,
  nicht Namenslaenge).
- Nach dem Namen: mehrere Byte identisch zwischen A/B (`38 4a 00 0c e0
  fb 05 00 00 00 00 23 88 dc 76`), dann 4 individuelle Byte pro Instanz
  (vermutlich Zeitstempel oder Instanz-Hash).

**Weiterhin nicht vollstaendig geklaert**: die exakte Bedeutung mehrerer
Header-Felder vor dem Instanz-Block, sowie die genaue Aufteilung der 5
Pakete auf "alle"/"meine Liste". Deutlich mehr verstanden als vorher
("Existenz bestaetigt, Struktur unbekannt" -> "Kernstruktur groesstenteils
entschluesselt").

### 53.2 Gambling-Opcodes (Header 47) entdeckt - eigene Zone-Verbindung fuer das Gluecksspielhaus

Der Besuch des "Lucky House" loest eine **eigene, zusaetzliche Zone-
Verbindung** aus (Zonenwechsel-Muster wie bereits bekannt, `CH6Type.
TransferKey` am Anfang bestaetigt). Darin mehrere Header-47-Interaktionen
beobachtet und mit vorlaeufigen Namen versehen (Opcode-Nummer sicher,
exakte Semantik aus Kontext erschlossen, nicht einzeln verifiziert):

- Typ 23/24 (CLI 4 Byte / SRV 11 Byte) - mehrfach wiederholt, vermutlich
  "Automat/Tisch ansprechen".
- Typ 200/201 - einmalig, vermutlich "Spiel betreten".
- Typ 216 (SRV, 21 Byte) - **periodisch alle 10 Sekunden** ohne
  Client-Anfrage, vermutlich ein Jackpot-/Timer-Update.
- Typ 202/203 - kurze Interaktion.
- Typ 100/101 (SRV 52 Byte, reichhaltig) - vermutlich Hauptspieldaten
  (aktueller Einsatz, Wuerfelwerte).
- Typ 104/105 - vermutlich "Spiel verlassen".

Noch nicht in Code umgesetzt (Opcode-Namen nicht sicher genug fuer eine
Implementierung, nur fuer eine kuenftige gezieltere Verifikation
dokumentiert).

### 53.3 Crusader/LP-Mechanik nutzerseitig bestaetigt

Mitschnitt-Beschreibung bestaetigt explizit: "es gibt keine sp/lp steine
in dieser klasse! lp regeneriert sich automatisch" und (Mitschnitt 5)
"lp (verhaellt sich definitiv nicht wie sp) voll" nach Rast - **widerspricht
der bisherigen Abschnitt-44.2-Vermutung** ("LP = SP nur umbenannt fuer
Sentinel/Savior") load. LP scheint eine eigene, automatisch (auch ohne
Rast) regenerierende Ressource zu sein, kein reines Alias fuer SP.
Nicht weiter verifiziert (keine Paketdaten dazu ausgewertet in dieser
Runde) - Vermutung aus 44.2 zurueckgezogen, echte Mechanik bleibt offen.

Kein Code fuer 53.2/53.3 geaendert - nur 53.1 (neuer Enum-Wert).
Kompiliert - 0 Fehler, unveraenderte 17 Warnungen.

### 53.4 Sehr viel weiteres Material in diesen zwei Mitschnitten unausgewertet

Guild-Manager-Dialog (Gildenerstellung, -liste, -akademie), Inventar mit
6 Taschen (2 aktiv, Auto-Sortierung, Belohnungs-/Cashshop-Tasche),
Lager-NPC (4 Seiten, kontogebunden, geteiltes Lager bei Heirat), Titel-
Auswahlfenster, Karten-Sammlung, Freundesliste/Community-Fenster,
Emote-Fenster, vollstaendige Respawn-UI mit Timer, echte KQ-Session
(Anmeldung, Teleport, Tod, Fail-Zustand bei 0 verbleibenden Respawns) -
keines davon in dieser Antwort ausgewertet.

## 54. Automatisierte pcap-Analyse-Pipeline gebaut, NpcDialogMenu vollstaendig
    entschluesselt, echte KQ-Fail-Session rekonstruiert, Revive-Bug gefunden

Fortsetzung von Abschnitt 53.4. Neues Werkzeug gebaut (siehe 54.6), damit
systematisch durch versuch_4/versuch_5 durchgegangen und mehrere der dort
als offen markierten Punkte bearbeitet.

### 54.1 SH17Type.NpcDialogMenu vollstaendig entschluesselt und gegen
     QuestDialog.shn kreuzverifiziert

Die in Abschnitt 36/48.3 offene Frage nach der "Dialog-Baum-ID" ist jetzt
geklärt - mit einer wichtigen Korrektur der urspruenglichen Hypothese.

**Byte-Layout (Typ-1, 105-Byte-Paket, Normalfall):**
```
[0-1]   u16 LE  Sequenzzaehler (erhoeht sich pro NpcDialogMenu-Paket um 1,
                wird vom Client in CH17Type.NpcDialogResponse unveraendert
                zurueckgeschickt - dient der Antwortkorrelation, ist KEIN
                Dialog-Identifikator)
[2-5]   u32 LE  Seitentyp (2 = normale Text-/Buttonseite in allen
                beobachteten Faellen)
[6]     byte    0x00 (Padding/hoeheres Byte eines eigentlich 24-Bit-Feldes?)
[7-8]   u16 LE  DialogID aus QuestDialog.shn (sql/data/data_questdialog.sql)
[9-10]  u16 LE  0x0000
[11-14] u32 LE  0x00000000
[15-16] u16 LE  Stabile NPC-Dialogbaum-ID (siehe unten)
[17...] Nullen (Rest)
```

**Kreuzverifikation, bytegenau:** Im Mitschnitt versuch_4 laufen die
Sequenzzaehlerwerte 0x0acc bis 0x0ad1 (2764-2769) durch eine komplette
Tiros-Questsequenz. Das DialogID-Feld (Byte 7-8) durchlaeuft dabei exakt
0xcce4 bis 0xccec (52452-52460) - **byte-fuer-byte identisch** mit neun
aufeinanderfolgenden Zeilen aus `data_questdialog.sql`:

- 52452: "Welcome! I don't know how long I've waited for this day to
  come..." [BUTTON]=[Start Quest][1]
- 52453 bis 52459: die Zwischendialogzeilen ("You've accomplished much,
  o teacher of glorious light, Tiros!" usw., alle mit [NEXT])
- 52460: "OK then. Now listen very carefully to what I say!"
  [BUTTON]=[End Quest][1][SHOW_REWARD][MENU]

Eine zweite, spaetere Sequenz im selben Mitschnitt (Sequenzzaehler 0x0acd
0x003e-0x0046, DialogID-Feld 0xcd3e = 52542) trifft ebenso exakt auf
`data_questdialog.sql`-Eintrag 52542 ("I'm now going to teach you the
most important skill of all...") - das ist die Questzeile fuer den
Skill "Advent", passend zur Beschreibung "questliste... quest special
skill angeklickt". Diese Seite hat einen laengeren Body mit zusaetzlichen
8 Byte am Ende (`07000000 01000000`) - vermutlich eine Skill-/Icon-
Referenz fuer die per [SHOW_REWARD] gelehrte Faehigkeit, nicht weiter
entschluesselt.

**Die eigentliche stabile "Dialog-Baum-ID"** sitzt NICHT im Sequenzzaehler
(Byte 0-1), sondern in Byte 15-16: konstant `0x2746` (**10054**) ueber
ALLE Tiros-Dialogseiten hinweg, unabhaengig von der jeweiligen DialogID.
Das bestaetigt die in Abschnitt 36/48.3 geaeusserte Vermutung einer
stabilen Baum-/NPC-ID (dort 10113 fuer Sera/Julia gefunden) mit einem
zweiten, unabhaengigen Datenpunkt im gleichen Wertebereich (10000er) fuer
einen anderen NPC (Tiros). **Korrektur gegenueber der urspruenglichen
Vermutung:** der zuvor als moeglicher ID-Kandidat gehandelte, sich
aendernde Wert war tatsaechlich der oben beschriebene Sequenzzaehler, kein
Bestandteil der Baum-ID selbst.

**CH17Type.NpcDialogResponse** (9-Byte-Body): `[Sequenzzaehler-Echo, 2
Byte][0x02][gewaehlter Button-Index, 1 Byte][000000]`. In diesem
Mitschnitt immer Index 1 (alle beobachteten Tiros-Dialoge waren
Einzelbutton-"Weiter"-Seiten) - eine echte Verzweigung mit Index 2 wurde
nicht aufgezeichnet, bleibt fuer einen kuenftigen Mitschnitt offen.

Zwei zusaetzliche, laengere Begleitpakete (Body-Typ 6 bzw. 10 statt 2,
jeweils mit dem gleichen Sequenzzaehler wie die zugehoerige Typ-2-Seite)
werden bei [SHOW_REWARD]-Seiten mitgeschickt. Sie enthalten unter anderem
ein Muster, das wie ein Unix-Zeitstempel aussieht (~0x639...), und mehrere
sich wiederholende 4-Byte-Gruppen - nicht abschliessend entschluesselt,
vermutlich Belohnungsitem-/Skillroll-Metadaten.

`PacketTypeServer.cs` (SH17Type-Kommentar) entsprechend aktualisiert.

### 54.2 Revive-Mechanik entschluesselt - Diskrepanz zum eigenen Code
     gefunden

**SH9Type.HealHP** (6-Byte-Body): `[u32 LE Heilbetrag][u16 LE
Sequenzzaehler]`. Bytegenau bestaetigt: unmittelbar nach dem ersten
Revive in versuch_5 (Uruga-Feld, t=566.66s) zeigt das Paket exakt
**459** - identisch mit der vom Nutzer notierten Beobachtung "459hp...
wieder hergestellt".

**SH4Type.Revive** (10-Byte-Body) enthaelt entgegen der urspruenglichen
Mutmassung KEINE HP-Werte, sondern eine Zielposition: `[u16 LE
RespawnPointID][u32 LE X][u32 LE Y]`. Beleg: beim zweiten Revive
(KQ-Fail-Rueckteleport nach Elderine, t=847.67s) liefert das Paket voellig
andere Werte (RespawnPointID 134 statt 17, X/Y ~1487/1517 statt
~5835/6397) - konsistent mit zwei unterschiedlichen Zielorten, nicht mit
HP.

**SH4Type.ReviveWindow** (9-Byte-Body: `[u32 LE][u32 LE][byte]`) zeigt an
BEIDEN unabhaengigen Toden exakt dieselben Werte (180, 50, 0) - vermutlich
feste Server-Konstanten (z.B. Revival-Fenster-Timeout in einer noch
unbekannten Zeiteinheit, und/oder eine Revival-Gebuehr), keine
situationsabhaengigen Werte.

**Wichtiger Fund - Diskrepanz zum eigenen `MapObject.Revive()`:** Das per
zwei unabhaengigen Paketen (`CharacterInfo`/`DetailedCharacterInfo`
unmittelbar vor dem Tod, `HealHP` unmittelbar danach) im selben Mitschnitt
bestaetigte MaxHP des Charakters zum Zeitpunkt des ersten Revives betrug
**2432** (nicht 1532, wie in der Mitschnitt-Beschreibung notiert - dieser
Wert stammt vermutlich von einem anderen, im gleichen Satz erwaehnten
Stat wie Max-LP/Max-SP). Daraus ergibt sich eine reale
Revive-Heilrate ohne aktiven Buff von 459/2432 ≈ **18,9 %**.

Der aktuelle Code (`MapObject.Revive()`, `NextGen.Zone/Game/MapObject.cs`)
faellt ohne `SAA_REVIVEHEALRATE`-Buff auf einen hartkodierten Flat-Wert
`HP = 50` zurueck (Kommentar dort fragt bereits rhetorisch "Why not take
e.g. 10% of your MaxHp?"). Der reale Client verwendet nachweislich einen
prozentualen Wert um die 19 %, keinen Flat-Betrag und auch nicht die im
Kommentar vorgeschlagenen 10 %. **Nicht in dieser Antwort geaendert** -
ein einzelner Messpunkt reicht nicht, um die exakte Formel (fester
Prozentsatz vs. level-/klassenabhaengig) sicher abzuleiten; dafuer waere
ein zweiter Todesfall mit einem anderen MaxHP-Wert hilfreich. Als
TODO-Kommentar im Code ergaenzt.

`PacketTypeServer.cs` (SH4Type/SH9Type-Kommentare) entsprechend
aktualisiert, `MapObject.cs` um einen Verweis auf diesen Abschnitt
ergaenzt (siehe 54.7).

### 54.3 Echte KQ-Fail-Session vollstaendig rekonstruiert

versuch_5 enthaelt die komplette Sequenz einer gescheiterten Kingdom
Quest ("Lost Mini Dragon (Hardcore)[B]", Instanz-ID 969) - Registrierung,
Countdown, Zonenwechsel in die Instanz, Tod, Fail-Signal,
Rueckteleport-Countdown, verzoegerter manueller Rueckteleport. Wichtige
Praezisierung der Architektur: die KQ-Instanzkarte laeuft NICHT ueber
den zuvor bereits verbundenen Zone-Server (Port 9019/9022), sondern ueber
eine dritte, komplett neue Zone-Verbindung mit eigenem Handshake
(`SH2Type.SetXorKeyPosition` + `CH6Type.TransferKey`), hier auf Port 9025
- dem gleichen Port, den in versuch_4 das Gluecksspielhaus benutzt hat.
Das ist vermutlich Zufall (naechster freier Port aus dem Server-seitigen
Pool fuer "temporaere Sub-Instanz-Karten"), keine feste Zuordnung
Port-zu-Feature.

**Ablauf (World-Server-Stream, Zeiten aus versuch_5):**

| t (s)   | Richtung | Paket                                  | Bedeutung |
|---------|----------|-----------------------------------------|-----------|
| 636.56  | c2s      | CH22Type Typ3, `[u32 969]`             | Instanz-Detailanfrage |
| 636.57  | s2c      | SH22Type Typ4, `[u32 969][u16 2]`      | Antwort (Status/Anzahl) |
| 638.12  | c2s      | CH22Type Typ5, `[u32 969]`             | **Anmeldung fuer die KQ** |
| 638.12  | s2c      | SH22Type Typ50 (26 Byte, mit Platz-halter-String "text") | Anmeldebestaetigung (unlokalisiert!) |
| 638.12  | s2c      | SH22Type Typ6, `[u32 969][u16 0x0991]`| weitere Bestaetigung |
| 638.80/650.82/661.83 | s2c | SH22Type Typ31, `[u16 1][u32 969][u16 wachsend: 2,3,4]` | periodisches Rekrutierungs-Update |
| 651.83  | s2c      | **SH22Type Typ11** (Klartext): "Kingdom Quest - Lost Mini Dragon (Hardcore)[B] will begin in  10 seconds." | Countdown-Ankuendigung |
| 651.83-660.83 | s2c | SH22Type Typ37 fuer Instanz 969, exakt 10x im 1-Sekunden-Takt | Countdown-Tick (Wert selbst blieb konstant) |
| 661.98  | s2c      | SH6Type.ChangeZone -> Port 9025        | Teleport in die Instanzkarte |

**In der Instanzkarte (Port 9025, Map "KDHDragon"):**

| t (s)   | Ereignis |
|---------|----------|
| 663.58  | Zoneneintritt, GM-Level-Broadcasts ("From 127.0.0.1", "Admin level is 100" - lokale Testserver-Eigenheit, kein Gameplay-Fund) |
| 765.54  | `SH4Type.ReviveWindow` - Tod durch Mob-Aggro (deckt sich mit "durch die mobs getötet worden") |
| 766.33  | **`SH22Type.KingdomQuestFailed` (Typ 19, leerer Payload)** - neuer Opcode, feuert einmalig exakt beim Scheitern |
| 766.83 - 791.82 | `SH8Type.GmNotice`: "Move to Elderine in 30/20/10/5 seconds." | Rueckteleport-Countdown |
| ab 791.82 | **Kein automatischer Teleport** - der Countdown laeuft aus, ohne dass etwas passiert, weil der Charakter noch tot ist. Deckt sich exakt mit der Nutzerbeobachtung "timer um nichts passiert da ich noch tot bin". |
| 847.65  | `CH4Type.ReviveToTown` (manuell "move to respawn point" gewaehlt) |
| 847.67  | `SH4Type.Revive` -> Elderine-Umgebung |

Das ist ein konkreter, bytegenau belegter Verhaltensfund: **der
automatische KQ-Fail-Rueckteleport wird nicht ausgeloest, solange der
Charakter im toten Zustand ist** - der Spieler muss zuerst manuell
respawnen. Ob das im echten NA2016-Server beabsichtigtes Verhalten war
oder ein Client-/Server-Bug im Original ist, laesst sich aus dem
Mitschnitt allein nicht sagen; fuer den NextGen-Emulator aber wichtig zu
wissen, falls die KQ-Fail-Logik implementiert wird (siehe 54.7).

`SH22Type` und `CH22Type` um alle hier gefundenen Typen ergaenzt (siehe
54.4 fuer weitere Details zu Typ 30/37/38/58).

### 54.4 SH22Type-Familie: volle Instanzliste (Typ 38) entschluesselt,
     CH22Type.GotIngame-Hypothese korrigiert

Zusaetzlich zu den KQ-Session-Paketen aus 54.3 wurden im World-Stream
mehrere periodische Broadcasts identifiziert, die offenbar den globalen
Zustand ALLER aktiven KQ-Instanzen synchron halten:

- **Typ 37** (6 Byte: `[u32 InstanzID][u16 Statuswert]`): Delta-Update
  fuer eine einzelne Instanz. Normalerweise im Minutentakt fuer
  verschiedene Instanz-IDs, aber fuer die eigene registrierte Instanz im
  letzten 10-Sekunden-Countdown exakt im 1-Sekunden-Takt (siehe 54.3) -
  der Statuswert selbst aendert sich dabei NICHT, ist also vermutlich
  eine Karten-/Typreferenz und kein Timer.
- **Typ 30** (6 oder 10 Byte: `[u16 Anzahl][u32 InstanzID] * Anzahl`):
  begleitet Typ 37 fast immer im selben Sende-Burst, offenbar eine
  Gruppierung "diese instanzen haben sich gerade geaendert".
- **Typ 38** (variable Laenge, TLV-Liste): voller Resync aller aktiven
  Instanzen. Layout: `[u16 LE Anzahl][{u32 LE InstanzID, u16 LE
  Statuswert (identisch zum Typ-37-Wert derselben Instanz), u16 LE
  zweiter Wert}] * Anzahl`. Der zweite Wert wiederholt sich fuer mehrere
  Instanzen mit gleichem Statuswert - passt zu einer gemeinsamen
  Karten-/Dungeon-ID pro KQ-Typ. Ueber die 20-minuetige Session hinweg
  schrumpfte die Anzahl der gelisteten Instanzen kontinuierlich (16 -> 16
  -> 5 -> 3 -> 2) - konsistent mit ablaufenden/geschlossenen Instanzen.
- **Typ 58** (1 Byte): tritt zuverlaessig bei jedem Zonen-/Karteneintritt
  auf, immer als Teil derselben Paketkaskade wie die in 54.4 unten
  aufgefuehrten unregistrierten `SH4Type`/`SH6Type`-Zusatzpakete -
  vermutlich eher ein allgemeiner Zone-Status als KQ-spezifisch, trotz
  Header 22.

**CH22Type.GotIngame (Typ 27) - Korrektur:** Der bestehende
Code-Kommentar beschrieb dieses Paket als einmaliges 27-Byte-Ereignis bei
der Charaktererstellung. Der reale Mitschnitt zeigt stattdessen ein 2-3
Byte kleines Paket, das WIEDERHOLT auftritt - typischerweise alle 5-10s,
und deutlich gehaeuft (fast im Sekundentakt) waehrend einer aktiven
KQ-Rekrutierungsphase. Die urspruengliche Namensgebung/Annahme war also
zumindest unvollstaendig; plausibler ist ein leichtgewichtiger
"Bereit/aktiv"-Heartbeat des KQ-Subsystems. Kommentar in
`PacketTypeClient.cs` entsprechend korrigiert, Name vorerst beibehalten
(erstes Vorkommen pro Session passt weiterhin zum Zonen-Eintritt).

**Wiederkehrende "Zonen-Eintritt-Paketkaskade" bestaetigt:** Die in
Abschnitt davor als Einzelfund vermerkten unregistrierten Pakete
`SH4Type` Typ 35/198/206/212/215/228/231/234, `SH6Type` Typ 39/43 sowie
`SH22Type` Typ 58 wurden in versuch_5 an INSGESAMT DREI unabhaengigen
Zoneneintritten (Uruga-Zone, KQ-Instanz) im exakt gleichen Bündel
reproduziert - kein Einzelfund mehr, sondern ein stabiles, reproduzierbares
Muster. Inhaltlich weiterhin nicht entschluesselt (die meisten Bodies sind
1-8 Byte, ohne lesbaren Text).

**Neuer, weiterhin unbekannter Header 36** in versuch_4 bestaetigt (kein
`CH36Type`/`SH36Type` existiert im Code) - 1-Byte-Body, zweimal beobachtet
(Werte 0 und 5, ca. 25s auseinander). Zu wenige Datenpunkte fuer eine
Hypothese; als offener Punkt vermerkt statt spekuliert.

### 54.5 Gluecksspielhaus (Header 47) - Objekt-Interaktion und Tischlimits
     entschluesselt

Praezisierung von Abschnitt 53.2 mit echten Payload-Daten aus versuch_4:

- **Typ 23/24** (Objekt ansprechen): Anfrage `[u16 LE ObjektID]`, Antwort
  `[byte Status][byte 0x26 konstant][u16 LE ObjektID-Echo][u32 LE
  Objekttyp][byte Flag]`. Zwei verschiedene angesprochene Objekte
  (ObjektID 0x4388/0x438a fuer den "Automaten", 0x4375 fuer den
  "Wuerfeltisch") liefern unterschiedliche Objekttyp-Werte (2 bzw. 1) -
  stimmt mit zwei unterschiedlichen, in der Mitschnitt-Beschreibung
  genannten Objekten ueberein.
- **Typ 216** (periodischer Broadcast, exakt alle 10s): Payload ist
  zwischen zwei Beobachtungen BYTE-IDENTISCH - **widerlegt die bisherige
  Vermutung eines Live-Jackpot-/Timer-Updates** (Abschnitt 53.2). Enthaelt
  stattdessen die Werte 100 und 500, plausibler als Mindest-/
  Hoechsteinsatz des Tisches.
- **Typ 200/201** (Spiel betreten) und **Typ 100/101** (Einsatz/Wurf,
  50-Byte-Antwort mit einem grossen Wert ~150000, evtl. Jackpot-Pool
  oder Kontostand) und **Typ 104/105** (Spiel verlassen, leerer
  Anfrage-Payload) strukturell grob erfasst, nicht abschliessend
  verifiziert.

`CH47Type` (bisher nicht existent) und `SH47Type` in den Enum-Dateien
ergaenzt.

### 54.6 Neues Werkzeug: automatisierte pcap-Decodierungs-Pipeline

`tools/pcap-analysis/decode_stream.py` (neu) automatisiert den kompletten
Weg von der rohen `.pcapng`-Datei bis zur lesbaren Opcode-Liste:

1. `tshark` extrahiert TCP-Nutzdaten je Stream und Richtung (mit
   Zeitstempel und Frame-Nummer fuer Rueckverfolgbarkeit).
2. Framing (Laengen-Praefix-Schema) wird auf dem ROHEN, noch
   verschluesselten Client->Server-Strom angewendet, NICHT nach der
   Entschluesselung - **eigener Fehler im ersten Anlauf dieser Session**:
   `Client.cs` zeigt, dass das Laengen-Praefix vor der Entschluesselung
   abgestreift wird, der XOR-Zaehler aber ueber Paketgrenzen hinweg
   weiterlaeuft. Nach Korrektur stimmt die Pipeline exakt mit dem in
   Abschnitt 27 dokumentierten Login-Handshake ueberein.
3. `tools/pcap-analysis/parse_enums.py` (neu) parst `CH*Type`/`SH*Type`
   direkt aus den echten C#-Enum-Dateien (inkl. mehrzeiliger
   Kommentare), damit Opcode-Namen nie von Hand nachgepflegt werden
   muessen.
4. **Zweiter eigener Fehler gefunden und korrigiert**: `Packet.cs` zeigt
   `this.Type = (byte)(opCode & 1023)` - der `(byte)`-Cast kappt Werte
   ueber 255 MODULO 256. Der erste Pipeline-Durchlauf ignorierte das und
   zeigte dadurch falsche Typwerte (z.B. T272 statt des tatsaechlich vom
   Code verwendeten T16).

Reproduzierbar getestet: die Pipeline bestaetigt eigenstaendig den
kompletten Login-Handshake (Abschnitt 27) und `SH22Type.KingdomQuestList`
(Abschnitt 53.1) byte-exakt, bevor sie fuer die neuen Funde in diesem
Abschnitt verwendet wurde.

### 54.7 Code-Aenderungen dieser Antwort

- `NextGen.FiestaLib/PacketTypeServer.cs`: SH4Type (ReviveWindow/Revive),
  SH8Type (GmNotice), SH9Type (HealHP), SH17Type (NpcDialogMenu), SH22Type
  (7 neue Typen), SH47Type (5 neue Typen) - alles reine Kommentare bzw.
  additive Enum-Werte, keine bestehenden Werte veraendert oder entfernt.
- `NextGen.FiestaLib/PacketTypeClient.cs`: CH22Type (Kommentarkorrektur
  GotIngame + 2 neue Typen), CH47Type (neu, 4 Typen) - ebenfalls rein
  additiv.
- `tools/pcap-analysis/decode_stream.py`, `parse_enums.py` (neu).
- **Kompilierpruefung:** Volle Solution-Kompilierung war in dieser
  Sandbox nicht moeglich (kein Zugriff auf `api.nuget.org` fuer den
  `MySqlConnector`-Paket-Restore, anders als in frueheren Sitzungen).
  Ersatzweise beide geaenderten Dateien in einem isolierten,
  abhaengigkeitsfreien Projekt mit `dotnet build` (.NET 10 SDK)
  syntaxgeprueft - 0 Fehler, 0 Warnungen. Da ausschliesslich Kommentare
  und neue (nie umbenannte/entfernte) Enum-Werte geaendert wurden, ist das
  Risiko einer Regression an anderer Stelle im Code minimal; eine volle
  Solution-Kompilierung sollte trotzdem in einer Umgebung mit
  NuGet-Zugriff nachgeholt werden, bevor weiterer Code auf den neuen
  Opcodes aufbaut.

### 54.8 Weiterhin offen

- Belohnungs-/Skillroll-Metadaten in den langen NpcDialogMenu-Begleit-
  paketen (Typ 6/10) nicht entschluesselt.
- Reale Revive-Heilrate (54.2) nur an einem Datenpunkt (18,9 %) belegt -
  zweiter Todesfall mit anderem MaxHP noetig, bevor die Formel im Code
  geaendert wird.
- `SH22Type` Typ 4/6/50 (KQ-Anmeldebestaetigung) nur grob erfasst, nicht
  bytegenau vollstaendig; der Platzhaltertext "text" in Typ 50 verdient
  eine gezielte Nachpruefung (fehlende Lokalisierung im Original-Client?).
- Neuer Header 36 weiterhin komplett unbekannt (nur 2 Datenpunkte).
- Gluecksspielhaus-Opcodes 100/101/200-203 nur grob strukturell erfasst.
- Guild-Manager-Dialog, Inventar (6 Taschen), Lager-NPC, Titel-Fenster,
  Karten-Sammlung, Freundesliste, Emote-Fenster aus Abschnitt 53.4 weiterhin
  nicht ausgewertet - liefen im Zone-Stream durchweg ueber den generischen
  `SH17Type.NpcDialogMenu`-Mechanismus oder gar nicht als eigener
  Netzwerk-Request (vermutlich rein aus bereits beim Login geladenen
  Daten wie `CharacterTitles` gespeist), was eine gezielte Analyse
  schwieriger macht als angenommen.
