# NextGen-Emulator — Client-Versions-Kompatibilität

> Sammelt alles, was zu NA2016, CN2012, TW2008 und dem 2026er-Client
> (NA2026) besprochen wurde: Ausgangslage, Verifikationsvorgehen,
> Werkzeuge, Architektur-Konsequenzen. Ergänzt `SETUP.md` und
> `DOCUMENTATION.md` (Abschnitt 11, Versions-Infrastruktur).

---

## 1. Zielbild

Geplant ist Unterstützung für die drei von Fiesta Heroes gepflegten
Filesets — **NA2016**, **CN2012**, **TW2008** — mit dem 2026er-Client
(**NA2026**, mutmaßlich weiterhin Nordamerika/Englisch als
Weltsprache) als Fernziel, auf dem aufgebaut werden kann, sobald die
drei bestehenden Filesets laufen.

| Client | Client-Dateien | Server-Dateien | Aktiv untersucht | Vermutetes Opcode-Format |
|---|---|---|---|---|
| NA2016 | ✅ vorhanden | ✅ vorhanden (geleakt) | ✅ ja, Basis für NextGen-Emulator | Byte-Header + Byte-Type (aktuelles `PacketHandlerAttribute`-Format) |
| CN2012 | ✅ vorhanden | ✅ vorhanden | ❌ noch nicht | Unklar — evtl. älteres `ushort`-Opcode-Format, siehe Abschnitt 4 |
| TW2008 | ✅ vorhanden | ✅ vorhanden | ❌ noch nicht | Unklar, TW2008 ist älter als CN2012 — noch wahrscheinlicher altes Format |
| NA2026 | ✅ vorhanden (nur Client) | ❌ keine eigenen Server-Dateien | ❌ noch nicht | Unbekannt, vermutlich Weiterentwicklung des NA2016-Formats |

---

## 2. Verifikationsvorgehen NA2016 / CN2012 / TW2008 (eigener Client + eigener Server)

Für alle drei gilt dasselbe, unproblematische Vorgehen, weil beides —
Client und Server — bereits vorliegt: eigene Infrastruktur, eigener
Client, eigener Mitschnitt.

1. **Login-Server der jeweiligen Generation lokal starten** (für
   NA2016: NextGen-Emulator selbst; für CN2012/TW2008 vorerst die
   jeweiligen Original-Server-Binaries, da der Emulator sie noch nicht
   abbildet).
2. **Client gegen `127.0.0.1` verbinden lassen** — bei CN2012/TW2008
   zuerst prüfen, ob die Serveradresse als Klartext-Hostname/IP in
   Config-Dateien oder der exe steht (siehe Abschnitt 3.1 — dasselbe
   Vorgehen wie für NA2026 beschrieben, meist aber bei diesen älteren,
   eigenständigen Clients ohne Glyph-Launcher deutlich einfacher zu
   finden).
3. **Mitschneiden** mit Wireshark oder FiestaShark (siehe Abschnitt 5).
4. **Erste Bytes des allerersten Pakets prüfen** — das entscheidet
   sofort, welches Framing-Format vorliegt (Abschnitt 4).

---

## 3. Verifikationsvorgehen NA2026 (nur Client vorhanden, kein eigener Server)

### 3.1 Schneller String-Check zuerst

**PowerShell (als Administrator, im Spiel-/Glyph-Installationsordner):**
```powershell
Select-String -Path ".\*.exe" -Pattern "gamigo|trionworlds|glyph|fiesta.*\.com|\.com$" -Encoding ascii
Select-String -Path ".\*.exe" -Pattern "gamigo|trionworlds|glyph|fiesta.*\.com|\.com$" -Encoding unicode
```
`-Encoding unicode` nicht vergessen — viele Strings in moderneren
Clients sind UTF-16.

```powershell
Get-ChildItem -Path "$env:ProgramData\Glyph", "$env:APPDATA\Glyph", "$env:LOCALAPPDATA\Glyph" -Recurse -ErrorAction SilentlyContinue -Include *.cfg,*.ini,*.json,*.xml |
  Select-String -Pattern "server|host|ip|address"
```

Ergebnis mit festem Hostname/IP → weiter mit Hosts-Datei (Abschnitt
3.4a). Kein Ergebnis → weiter mit Prozessanalyse.

### 3.2 Process Monitor (Sysinternals)

1. [Process Monitor](https://learn.microsoft.com/sysinternals/downloads/procmon)
   von Microsofts offizieller Sysinternals-Seite laden, keine
   Installation nötig.
2. **Als Administrator starten.**
3. Filter setzen, *bevor* aufgezeichnet wird:
   - `Filter` → `Filter...`
   - `Operation` `is` `TCP Connect` → `Include`
   - `Operation` `is` `Process Create` → `Include`
4. Aufzeichnung starten (`Ctrl+E`).
5. Glyph starten, Spiel ganz normal launchen, bis Erfolg oder Fehler.
6. Aufzeichnung stoppen (`Ctrl+E`).

**Auswertung:**
- Nach `Process Create` filtern → zeigt die komplette Prozesskette
  (Glyph.exe → evtl. Updater → eigentliche Spiel-exe). Name der
  **letzten** exe vor dem eigentlichen Spiel notieren.
- Nach `TCP Connect` filtern, nach `Process Name` gruppieren → für
  jeden Prozess die Ziel-IP:Port. Wichtig: welche Adresse ruft die
  **Spiel-exe selbst** auf (nicht Glyph) — vermutlich der
  Login-Server-Port.

### 3.3 Wireshark parallel

1. Wireshark installieren (Npcap-Treiber wird mitgebracht).
2. Aktives Internet-Interface wählen.
3. Anzeigefilter für ausgehende Verbindungsversuche:
   ```
   tcp.flags.syn == 1 && tcp.flags.ack == 0
   ```
4. Aufzeichnen, Glyph + Spiel starten.
5. Bei game-relevanter IP (meist kein bekannter CDN/Cloud-Bereich):
   Filter auf `ip.addr == <gefundene IP>` erweitern, um den ganzen
   Austausch zu sehen.

### 3.4 Interpretation der Ergebnisse

| Beobachtung | Bedeutung | Nächster Schritt |
|---|---|---|
| Nur Glyph.exe verbindet sich, danach nichts | Spiel-exe startet erst nach erfolgreicher Glyph-Auth | Umleitung *vor* diesem Punkt bringt nichts — DLL-Hook nötig (3.4c) |
| Separate Spiel-exe verbindet sich selbst, fester Hostname/IP | Guter Kandidat für Hosts-Datei | 3.4a |
| Spiel-exe verbindet sich auf immer gleichen Port, IP variiert | `netsh interface portproxy` reicht, IP muss nicht bekannt sein | 3.4b |
| IP wechselt zwischen Neustarts | Dynamische Serverzuweisung durch Glyph | DLL-Hook nötig (3.4c) |

#### 3.4a Hosts-Datei
Falls fester Hostname gefunden: `C:\Windows\System32\drivers\etc\hosts`
um eine Zeile `127.0.0.1 <hostname>` ergänzen (Admin-Rechte nötig).

#### 3.4b `netsh interface portproxy`
Windows-Bordmittel, leitet jede ausgehende Verbindung auf einem
bestimmten Port um, unabhängig von der Zieladressermittlung:
```
netsh interface portproxy add v4tov4 listenport=<PORT> listenaddress=0.0.0.0 connectport=<PORT> connectaddress=127.0.0.1
```
Ziel-IP muss dafür nicht bekannt sein, nur der Port.

#### 3.4c DLL-Hook auf `connect()`/`WSAConnect()`
Robustester Weg, technisch fast identisch zum bestehenden
`srvhook-2016`-Ansatz: DLL per Injection in den **Spiel-Prozess**
(nicht Glyph selbst — Glyph ist nur der Launcher, patcht/authentifiziert
und startet eine separate Spiel-exe) laden, die Winsock-`connect()`
abfängt und umschreibt, bevor der Aufruf das Betriebssystem erreicht.
Funktioniert unabhängig davon, wie die Adresse ermittelt wurde.
Basiert auf Microsoft Detours (MIT) — bereits Abhängigkeit im
Hook-DLL-Track, hier nur erweitert statt neu aufgesetzt.

### 3.5 Fallback: Mitschnitt gegen den echten Live-Server

Falls der 2026er-Client zwingend an Gamigos echter Infrastruktur hängt
und keine der Umleitungen greift: dieselbe Aufzeichnung stattdessen
beim **normalen Spielen mit dem eigenen Account gegen den echten
Server** durchführen — eigener Client, eigene Verbindung, normaler
Mitschnitt, nichts anderes als für NA2016 ohnehin vorgesehen. Liefert
die Ground Truth unabhängig davon, ob die Server-Umleitung technisch
gelingt.

---

## 4. Framing-Format-Erkennung (wichtig für CN2012/TW2008)

Bei `TakenBerry/Fiesta_Utils` (Community-Tools rund um
Zepheus/DragonFiesta) existiert ein `FiestaOpcodeConverter`, der laut
Beschreibung "ältere Opcodes (`ushort`) in das neuere Byte-Byte-Format"
konvertiert. Das deutet auf **mindestens zwei grundsätzlich
verschiedene Paket-Header-Formate** in der Geschichte von Fiesta
Online hin:

- **Neu** (aktuell in `NextGen.FiestaLib.Networking.PacketHandlerAttribute`
  abgebildet): getrenntes Header-Byte + Type-Byte.
- **Alt**: ein zusammenhängender 16-Bit-Opcode.

TW2008 und CN2012 liegen zeitlich deutlich vor NA2016 — es ist
plausibel, dass sie noch das alte Format sprechen, nicht nur andere
Opcode-*Werte* im selben neuen Format haben.

**Praktische Konsequenz:** Die in dieser Session gebaute
versionsbewusste Handler-Registry (`DOCUMENTATION.md`, Abschnitt 11)
setzt voraus, dass Header/Type bereits korrekt aus dem Byte-Strom
extrahiert wurden — sie löst nur, welcher Handler für eine gegebene
(Header, Type, Version)-Kombination zuständig ist. Falls CN2012/TW2008
grundsätzlich anders framen, wird zusätzlich eine **Framing-Erkennung
vor** der Header/Type-Extraktion nötig (in `Client.cs`, dort wo aktuell
`mReceivingPacketLength`/`headerLength` verarbeitet werden).

**Wie man das aus einem Mitschnitt erkennt:** die ersten paar Bytes
nach der (unverschlüsselten) Crypto-Handshake-Antwort ansehen. Ergibt
die Interpretation als zwei getrennte Bytes (Header, Type) sinnvolle,
kleine, stabile Werte über mehrere Pakete hinweg — neues Format. Ergibt
erst die Interpretation als zusammenhängender 16-Bit-Wert sinnvolle,
bekannte Opcode-artige Werte — altes Format.

---

## 5. Werkzeuge

| Werkzeug | Zweck | Quelle |
|---|---|---|
| **Wireshark** | Allgemeiner Paket-Mitschnitt | wireshark.org |
| **FiestaShark** | Dedizierter Fiesta-Online-Packet-Sniffer, für Zepheus/DragonFiesta gebaut | `TakenBerry/Fiesta_Utils` (GitHub) |
| **FiestaOpcodeConverter** | Konvertiert altes `ushort`- in neues Byte-Byte-Opcode-Format | `TakenBerry/Fiesta_Utils` (GitHub) |
| **Process Monitor** | Prozesskette + TCP-Connect-Ziele | Microsoft Sysinternals |
| **`netsh interface portproxy`** | Verbindungsumleitung ohne Client-Patch | Windows-Bordmittel |
| **Microsoft Detours** | DLL-Hooking (`connect()`/`WSAConnect()`) | bereits im Hook-DLL-Track (`srvhook-2016`) verwendet |

**Noch zu klären, bevor NA2016-Opcodes komplett neu gesnifft werden:**
Ob `doc.fiestaheroes.com` bzw. der Fiesta-Heroes-Discord tatsächlich
eine Opcode-/Protokoll-Referenz für NA2016 bereithält (Recherche in
dieser Session ergab: das öffentliche Doku-Repo besteht laut
GitHub-Sprachstatistik zu 81 % aus Lua, deutet eher auf
Content-/Quest-Scripting-Doku hin als auf Netzwerkprotokoll — aber
nicht abschließend verifizierbar per Suche/Fetch, direkte Nachfrage im
Discord lohnt sich).

---

## 6. Wichtige Abgrenzung

Alles oben Beschriebene bezieht sich auf **eigene Infrastruktur**:
eigener Server, eigener Client, eigener Mitschnitt zur
Protokollverifikation — das ist unproblematische technische Forschung.

Einen Server **öffentlich** zu betreiben, gegen den fremde Nutzer mit
dem (originalen oder modifizierten) Client eines weiterhin aktiv
gepflegten, kommerziellen Spiels spielen, ist davon zu unterscheiden
und berührt Nutzungsbedingungen und Urheberrecht unabhängig von der
gewählten technischen Umleitungsmethode. Das gilt besonders für NA2026
als erkennbar aktiv gepflegten Client. Details siehe Gesprächsverlauf.

---

## 7. Priorisierung

1. **NA2016 zuerst fertig verifizieren** — meiste Grundlage vorhanden
   (Client *und* Server, NextGen-Emulator baut bereits darauf auf).
2. **CN2012/TW2008 danach**, sobald per Mitschnitt (Abschnitt 2 + 4)
   klar ist, ob das alte oder neue Framing-Format vorliegt — das
   entscheidet, ob die bestehende Architektur reicht oder eine
   Framing-Abstraktionsschicht vorgeschaltet werden muss.
3. **NA2026 als Fernziel** — anderer Aufwand, da keine geleakten
   Server-Assets vorliegen; Opcodes müssten komplett neu erschlossen
   werden, dazu zuerst technisch klären (Abschnitt 3), ob/wie sich
   eigene Testinfrastruktur überhaupt zwischenschalten lässt.
