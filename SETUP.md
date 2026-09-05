# NextGen-Emulator — Installation & Einrichtung

> Diese Anleitung geht davon aus, dass du `NextGen-Emulator.zip` entpackt
> hast und in einem Terminal im entpackten Ordner stehst. Getestet
> (Compile-Verifikation, siehe `DOCUMENTATION.md` Abschnitt 0) unter Ubuntu
> 24.04 / .NET 10. Unter Windows funktionieren dieselben `dotnet`-Befehle
> identisch (PowerShell/cmd statt bash).

---

## 1. Voraussetzungen installieren

### 1.1 .NET 10 SDK

**Linux (Ubuntu/Debian):**
```bash
# Microsoft-Paketquelle einrichten (einmalig, falls noch nicht vorhanden)
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```
**Windows/macOS:** Installer von https://dotnet.microsoft.com/download/dotnet/10.0
herunterladen (SDK, nicht nur Runtime).

Prüfen:
```bash
dotnet --version
# sollte 10.0.x ausgeben
```

### 1.2 MySQL oder MariaDB

Irgendeine MySQL-kompatible Datenbank, lokal oder erreichbar. Für lokale
Entwicklung reicht z. B.:

```bash
sudo apt-get install -y mariadb-server
sudo systemctl start mariadb
sudo mysql_secure_installation   # Root-Passwort setzen, Rest kann man mit "n" beantworten
```

Oder per Docker, falls vorhanden:
```bash
docker run -d --name nextgen-mysql -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=DEIN_PASSWORT \
  mysql:8.0
```

---

## 2. Build

```bash
cd NextGen-Emulator
dotnet restore NextGen-Emulator.sln
dotnet build NextGen-Emulator.sln -c Release
```

Das lädt beim ersten Mal `MySqlConnector` und
`System.Diagnostics.PerformanceCounter` von nuget.org (Internetzugriff
nötig) und baut alle sieben Projekte. Ergebnis liegt danach je Projekt in
`NextGen.<Name>/bin/Release/net10.0/`.

**Wenn das nicht durchläuft:** Zuerst `dotnet restore` isoliert laufen
lassen und die Fehlermeldung lesen — meistens entweder fehlender
Internetzugriff auf nuget.org, oder eine Firewall/ein Proxy, der
`api.nuget.org` blockiert. Ansonsten: Abschnitt 0 von `DOCUMENTATION.md`
enthält die Details zur bisherigen Verifikation und bekannte
Einschränkungen.

---

## 3. Datenbanken anlegen

Dieses Repo bringt inzwischen Schema für **alle drei** Datenbanken mit —
Login (mit Original-Daten), World/Zone-Kern und Referenzdaten (letztere
teils mit **echten Daten aus dem NA2016-Client**, teils nur Struktur ohne
Daten). Details zur Herkunft und Vertrauenswürdigkeit jeder Tabelle:
`DOCUMENTATION.md`, Abschnitt 10. Genaue Import-Reihenfolge: `sql/README.md`.

```bash
# Login (legt seine Datenbank selbst an, siehe Kommentar unten)
mysql -u root -p < sql/login/login-base.sql

# World (Kernschema: characters, items, equips, guilds, groups, ...)
mysql -u root -p -e "CREATE DATABASE fiesta_world DEFAULT CHARACTER SET utf8mb4;"
mysql -u root -p fiesta_world < sql/world/schema.sql

# Data (Referenzdaten: erst Struktur, dann die 5 Tabellen mit echten
# Client-Daten - Reihenfolge wichtig, siehe sql/README.md)
mysql -u root -p -e "CREATE DATABASE fiesta_data DEFAULT CHARACTER SET utf8mb4;"
mysql -u root -p fiesta_data < sql/data/schema_derived.sql
mysql -u root -p fiesta_data < sql/data/data_iteminfo.sql
mysql -u root -p fiesta_data < sql/data/data_mobinfo.sql
mysql -u root -p fiesta_data < sql/data/mapinfo.sql
mysql -u root -p fiesta_data < sql/data/activeskill.sql
mysql -u root -p fiesta_data < sql/data/minihouse.sql
mysql -u root -p fiesta_data < sql/data/data_abstate.sql
mysql -u root -p fiesta_data < sql/data/data_subabstate.sql
mysql -u root -p fiesta_data < sql/data/data_abstateview.sql
mysql -u root -p fiesta_data < sql/data/data_passiveskill.sql
mysql -u root -p fiesta_data < sql/data/data_charactertitle.sql
mysql -u root -p fiesta_data < sql/data/data_questdialog.sql
mysql -u root -p fiesta_data < sql/data/data_questscript_fragments.sql
mysql -u root -p fiesta_data < sql/data/data_npcdialog.sql
mysql -u root -p fiesta_data < sql/data/data_kqteam.sql
mysql -u root -p fiesta_data < sql/data/data_kqisvote.sql
mysql -u root -p fiesta_data < sql/data/data_kqvotedesc.sql
mysql -u root -p fiesta_data < sql/data/data_kqvotemajorityrate.sql
mysql -u root -p fiesta_data < sql/data/data_kingdomquestdesc.sql
mysql -u root -p fiesta_data < sql/data/data_classname.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentrequire.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentskill.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentskilldesc.sql
mysql -u root -p fiesta_data < sql/data/data_dicedividind.sql
mysql -u root -p fiesta_data < sql/data/data_gbdicedividind.sql
mysql -u root -p fiesta_data < sql/data/data_gbhouse.sql
```

**Wichtig:** Das Login-Skript legt intern per `DROP/CREATE DATABASE
fiesta_account` + `USE fiesta_account` seine eigene Datenbank an — nicht
vorher eine eigene anlegen und dort einspielen, das Skript ignoriert das
ohnehin (siehe Troubleshooting, Abschnitt 8).

**Was noch fehlt:** `sql/world/schema.sql` und `sql/data/schema_derived.sql`
sind zwar vollständig aus dem tatsächlichen C#-Code abgeleitet (jede Spalte
stammt aus einem echten `row["..."]`-Zugriff), aber **nicht gegen einen
echten Server oder echte Spielstanddaten verifiziert** — im Unterschied zu
den fünf `.shn`-basierten Data-Tabellen, die echte Client-Daten enthalten.
Bitte vor Produktivnutzung gegenprüfen.

Der Login-Datenbankname **ist dagegen nicht frei wählbar** — er muss
`fiesta_account` heißen (oder du bearbeitest `sql/login/login-base.sql`
und ersetzt `fiesta_account` durchgängig durch deinen Wunschnamen, bevor
du es einspielst). Die mitgelieferte `Config.cfg` ist bereits auf
`Login.Mysql.Database=fiesta_account` gestellt.

Optional, für Gilden-Erstellung und Item-/Ausrüstungs-Erzeugung per Stored
Procedure: die Prozeduren aus `sql/do-not-use/SQL scripts/` (der Name ist
historisch, nicht wörtlich zu nehmen — sie werden vom Code tatsächlich
erwartet, z. B. `Guild_Create` von `GuildManager.cs`, `give_item`/
`give_equip` von `Item.cs`) zusätzlich in `fiesta_world` einspielen, nach
`sql/world/schema.sql` (die Tabellen, auf die sie sich beziehen, existieren
jetzt bereits).

---

## 4. Config.cfg einrichten

**Eine einzige Datei** an der Repo-Wurzel (`Config.cfg`) ist die Quelle
der Wahrheit — sie wird bei jedem Build automatisch neben jede der drei
Server-`.dll`s kopiert (das war vorher nicht so: im Original gab es drei
widersprüchliche Kopien mit unterschiedlichen, teils fremden Werten
inklusive einer echten externen IP-Adresse eines früheren Entwicklers;
die wurden bei der Modernisierung entfernt, siehe `DOCUMENTATION.md`).

Öffne `Config.cfg` und trage mindestens diese Werte für deine Umgebung
ein:

```ini
Login.Mysql.Server=localhost
Login.Mysql.Port=3306
Login.Mysql.User=root
Login.Mysql.Password=DEIN_PASSWORT
Login.Mysql.Database=fiesta_account

World.Mysql.Server=localhost
World.Mysql.Port=3306
World.Mysql.User=root
World.Mysql.Password=DEIN_PASSWORT
World.Mysql.Database=fiesta_world

Data.Mysql.Server=localhost
Data.Mysql.Port=3306
Data.Mysql.User=root
Data.Mysql.Password=DEIN_PASSWORT
Data.Mysql.Database=fiesta_data
```

Die übrigen Werte (Ports, `InterPassword`, `World.Name` usw.) können für
einen lokalen Testlauf unverändert bleiben. `InterPassword=lol` ist zwar
albern, aber nur das Passwort für die interne Server-zu-Server-Kommunikation
auf localhost — für einen öffentlich erreichbaren Server solltest du das
und alle DB-Passwörter trotzdem ändern.

**Nach jeder Änderung an `Config.cfg` neu bauen** (`dotnet build`), damit
die aktualisierte Version in die drei Output-Ordner kopiert wird — oder
die bereits kopierten Dateien direkt in
`NextGen.Login/bin/Release/net10.0/Config.cfg`,
`NextGen.World/bin/Release/net10.0/Config.cfg`,
`NextGen.Zone/bin/Release/net10.0/Config.cfg` editieren.

---

## 5. Server starten

Reihenfolge ist wichtig — Login zuerst, dann World, dann Zone:

```bash
# Terminal 1
dotnet NextGen.Login/bin/Release/net10.0/NextGen.Login.dll

# Terminal 2 (warten bis Login "Settings loaded successfully" o.ä. zeigt)
dotnet NextGen.World/bin/Release/net10.0/NextGen.World.dll

# Terminal 3
dotnet NextGen.Zone/bin/Release/net10.0/NextGen.Zone.dll
```

Ports laut Standard-`Config.cfg`: Login `9010`, World `9110`
(Inter-Server-Port `10022`/`11000`), Zone-Basis-Port `9210` aufwärts
(`World.ZoneCount=2` → zwei Zone-Instanzen erwartet, `Zone.IP`/
`Zone.WorldServerIP` entsprechend anpassen, falls Zone auf einer anderen
Maschine als World läuft).

**Was ich nicht verifizieren konnte:** ob die drei Prozesse tatsächlich
sauber miteinander sprechen und hochfahren — dafür fehlt in dieser
Sandbox ein laufender MySQL-Server und die Möglichkeit, Netzwerk-Sockets
zwischen mehreren lokalen Prozessen zu testen. Das ist der erste Schritt,
den du selbst verifizieren musst.

---

## 6. Bekannte Lücken, die vor einem produktiven Testlauf zu klären sind

Der Reihe nach, ungefähr nach Dringlichkeit:

1. **World-/Zone-Datenbankschema jetzt vorhanden, aber unverifiziert.**
   `sql/world/schema.sql` und `sql/data/schema_derived.sql` decken
   `characters`, `items`, `equips`, `friends`, `Guilds`, `GuildAcademy`,
   `groups`, `Skillist`, `BlockUser`, `PremiumItems`, `Rewarditems`,
   `Mobspawn`, `blockinfo`, `Vendors` und weitere ab — jede Spalte aus dem
   tatsächlichen C#-Code abgeleitet. `data_iteminfo`, `data_mobinfo`,
   `mapinfo`, `activeskill`, `minihouse` sind zusätzlich mit **echten
   Daten aus dem NA2016-Client** befüllt. Aber: **keines davon wurde
   gegen einen echten laufenden Server getestet** — es kann sein, dass
   einzelne Spaltentypen/-längen nicht exakt passen oder eine Tabelle
   fehlt, die der Code an einer Stelle erwartet, die nicht durchsucht
   wurde. Details: `DOCUMENTATION.md`, Abschnitt 10.
2. **Protokoll-/Opcode-Kompatibilität zum echten NA2016-Client ist
   nicht verifiziert.** Das war schon vor dieser Session offen und bleibt
   es. Nächster konkreter Schritt: Login-Handshake des echten Clients
   mitschneiden (Wireshark/Loopback) und gegen `NetCrypto.cs`/
   `LoginHandler.cs` abgleichen.
3. **8 Dateien sind weiterhin absichtlich vom Build ausgeschlossen**
   (der `.shn`-Dateiparser wurde reaktiviert, siehe `DOCUMENTATION.md`
   Abschnitt 9). Rest: Abschnitt 4.
4. **Der Client selbst braucht vermutlich einen Loopback-Patch**, damit er
   sich statt an den offiziellen Gamigo-Login-Server an deinen lokalen
   Server wendet — das ist Teil des separaten Hook-DLL-Tracks, nicht
   dieses Repos.

---

## 7. Kurz-Checkliste

- [ ] `dotnet --version` zeigt `10.0.x`
- [ ] MySQL/MariaDB läuft, per `mysql -u root -p` erreichbar
- [ ] `dotnet restore && dotnet build` läuft ohne Fehler durch
- [ ] Drei Datenbanken angelegt, alle Schema-Dateien eingespielt (siehe
      Abschnitt 3 / `sql/README.md`)
- [ ] `Config.cfg` mit deinen echten DB-Zugangsdaten befüllt
- [ ] Login-Server startet ohne "Error reading settings"
- [ ] World-Server startet und verbindet sich zum Login-Server
- [ ] Zone-Server startet und verbindet sich zum World-Server
- [ ] World-/Zone-Schema eingespielt, aber **unverifiziert** — bitte beim
      ersten echten Testlauf genau beobachten, ob einzelne Tabellen/Spalten
      fehlen (Abschnitt 6, Punkt 1)

---

## 8. Troubleshooting

**`NullReferenceException` in `Program.Load()` (World/Zone/Login), meist
in Zusammenhang mit `Settings.Instance.XyzMysqlServer`:**
Bedeutet: `Config.cfg` fehlt ein von diesem Server erwarteter Key.
`Settings.Load()` loggt seit dem letzten Fix die konkrete Ursache
*vor* dem Absturz (`Fehler beim Laden der ... Settings aus Config.cfg: ...`)
— schau in die Zeile direkt davor im Log. Bekannter Fall, der genau das
ausgelöst hat: `World.TicksToSleep`, `World.SleepTime`, `Zone.TicksToSleep`,
`Zone.SleepTime` fehlten in einer früheren Version der mitgelieferten
`Config.cfg` (in der aktuellen Version bereits enthalten).

**`sql/login/login-base.sql` legt eine andere Datenbank an als erwartet:**
Das Skript enthält `DROP DATABASE IF EXISTS fiesta_account` /
`CREATE DATABASE fiesta_account` fest einprogrammiert und ignoriert jeden
Datenbanknamen, den du vorher selbst angelegt hast. Löse es, indem du
`Login.Mysql.Database=fiesta_account` in `Config.cfg` einträgst (bereits
Standard in der mitgelieferten Datei) — nicht, indem du versuchst, das
Skript in eine andere Datenbank einzuspielen.

**`(Debug) Disconnected database client #1` im Log, kurz nach dem Start:**
Kein Fehler. `Program.Load()` öffnet beim Start bewusst eine
Test-Verbindung (`DatabaseManager.GetClient(); //testclient`), um die
DB-Erreichbarkeit zu prüfen. Ein separater Hintergrund-Thread
(`MonitorClientsLoop` in `DatabaseManager.cs`) schließt Verbindungen
automatisch, die 60 Sekunden lang nicht genutzt wurden — genau das siehst
du hier. Der Client wird bei Bedarf automatisch neu geöffnet.
