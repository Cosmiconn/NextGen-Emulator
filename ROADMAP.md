# NextGen Emulator – Modernisierungs-Roadmap

> **Projekt:** NextGen Fiesta Online Emulator  
> **Basierend auf:** Estrella Emulator (GitHub: Temperament/Estrella)  
> **Ziel-Framework:** .NET 9.0  
> **Stand:** August 2026

---

## Inhaltsverzeichnis

1. [Bereits abgeschlossen (Phase 0)](#phase-0-fundament)
2. [Phase 1: Infrastruktur & Konfiguration](#phase-1-infrastruktur--konfiguration)
3. [Phase 2: Datenbankschicht](#phase-2-datenbankschicht)
4. [Phase 3: Netzwerk-Core](#phase-3-netzwerk-core)
5. [Phase 4: Client-Kompatibilität & Krypto](#phase-4-client-kompatibilität--krypto)
6. [Phase 5: Spiel-Logik & Thread-Safety](#phase-5-spiel-logik--thread-safety)
7. [Phase 6: DevOps & Qualitätssicherung](#phase-6-devops--qualitätssicherung)

---

## Phase 0: Fundament (ABGESCHLOSSEN)

### 0.1 Repository-Setup
- [x] Repository geklont von `https://github.com/Temperament/Estrella`
- [x] Nach `/mnt/agents/output/NextGen-Emulator` kopiert
- [x] Alle Verzeichnis-/Dateinamen von `Estrella` auf `NextGen` umbenannt
- [x] Alle Code-Referenzen (Namespaces, using-Direktiven) aktualisiert
- [x] Build-Artefakte (`bin/`, `obj/`, `.suo`, `.userprefs`) bereinigt

### 0.2 .NET 9 SDK-Installation
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"
```
- [x] .NET 9.0.317 SDK installiert
- [x] `dotnet --version` bestätigt: `9.0.317`

### 0.3 Projekt-Migration auf SDK-Style
| Projekt | Altes Format | SDK-Style | Target |
|---|---|---|---|
| NextGen.Util | Legacy .csproj (ToolsVersion=4.0) | ✅ | net9.0 |
| NextGen.Database | Legacy .csproj | ✅ | net9.0 |
| NextGen.FiestaLib | Legacy .csproj | ✅ | net9.0 |
| NextGen.InterLib | Legacy .csproj | ✅ | net9.0 |
| NextGen.Login | Legacy .csproj | ✅ | net9.0 |
| NextGen.World | Legacy .csproj | ✅ | net9.0 |
| NextGen.Zone | Legacy .csproj | ✅ | net9.0 |

### 0.4 NuGet-Paket-Migration
| Alt | Neu | Version |
|---|---|---|
| `MySql.Data.dll` (lokal, 2013) | `MySqlConnector` | 2.4.0 |
| `System.Diagnostics.PerformanceCounter` (GAC) | NuGet-Paket | 9.0.0 |

### 0.5 Namespace-Migration
| Alt | Neu | Betroffene Dateien |
|---|---|---|
| `MySql.Data.MySqlClient` | `MySqlConnector` | 20+ .cs Dateien |
| `System.Data.EntityClient` | Entfernt | `ConnectionStringbuilder.cs` |
| `System.Data.Objects` | Entfernt | `Account.Designer.cs`, `World.Designer.cs` |

### 0.6 Entfernte Legacy-Dateien
| Datei/Verzeichnis | Grund |
|---|---|
| `NextGen.Util/ConnectionStringbuilder.cs` | EF6 nicht kompatibel mit .NET 9 |
| `NextGen.Util/DUpdater/` | Veralteter DB-Updater |
| `NextGen.Login/Data/Account.Designer.cs` | EF6 Entity Designer |
| `NextGen.World/Data/World.Designer.cs` | EF6 Entity Designer |
| `*/Properties/AssemblyInfo.cs` | SDK-Style generiert automatisch |
| `MySql.Data.dll` | Durch NuGet-Paket ersetzt |

### 0.7 .NET 9 Kompatibilitäts-Fixes
| Problem | Lösung | Datei |
|---|---|---|
| `Assembly.GlobalAssemblyCache` obsolete | `assembly.IsDynamic` | `Reflector.cs` |
| `SecurityPermissionAttribute` obsolete | Entfernt | `Program.cs` (3×) |
| `EdmSchemaAttribute` (EF6) nicht gefunden | Datei entfernt | `Account.Designer.cs` |
| `PerformanceCounter` nicht gefunden | NuGet-Paket hinzugefügt | `NextGen.Zone.csproj` |

### 0.8 Build-Status
```
✅ NextGen.Util       → bin/Debug/net9.0/NextGen.Util.dll
✅ NextGen.Database   → bin/Debug/net9.0/NextGen.Database.dll
✅ NextGen.InterLib   → bin/Debug/net9.0/NextGen.InterLib.dll
✅ NextGen.FiestaLib  → bin/Debug/net9.0/NextGen.FiestaLib.dll
✅ NextGen.Zone       → bin/Debug/net9.0/NextGen.Zone.dll
✅ NextGen.World      → bin/Debug/net9.0/NextGen.World.dll
✅ NextGen.Login      → bin/Debug/net9.0/NextGen.Login.exe
```

**Verbleibende Warnungen (nicht blockierend):**
- `SYSLIB0006`: `Thread.Abort()` in `Zone/Worker.cs`
- `CA1416`: `PerformanceCounter` nur Windows
- `CA1416`: `Console.WindowWidth` nur Windows

---

## Phase 1: Infrastruktur & Konfiguration (Woche 1)

### 1.1 Logging-System (Serilog)
**Ziel:** Ersetzen des alten Textdatei-Loggers durch modernes, strukturiertes Logging

**Aktueller Stand:**
```csharp
// NextGen.Util/Log.cs – veraltet
public static void WriteLine(LogLevel pLogLevel, string pFormat, params object[] pArgs)
{
    locker.WaitOne();
    Console.ForegroundColor = GetColor(pLogLevel);
    Console.Write(header);
    Console.WriteLine(buffer);
    locker.ReleaseMutex();
}
```

**Geplante Umsetzung:**
```bash
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Extensions.Logging
```

**Neue Architektur:**
```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/server-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Migrationsschritte:**
- [ ] `Serilog` NuGet-Pakete zu allen Projekten hinzufügen
- [ ] `Log.cs` durch `ILogger<T>`-Injection ersetzen
- [ ] Alte `LogLevel`-Enum durch `Serilog.Events.LogEventLevel` ersetzen
- [ ] Rotierende Log-Dateien konfigurieren (täglich, 30 Tage Aufbewahrung)
- [ ] JSON-Structured Logging für zentrale Log-Aggregation vorbereiten

### 1.2 Konfigurations-Migration (appsettings.json)
**Ziel:** Ersetzen der `Config.cfg` durch moderne `appsettings.json`

**Aktueller Stand:**
```ini
# Config.cfg – veraltetes Key-Value-Format
Login.Mysql.Server=localhost
Login.Mysql.Port=3306
Login.Debug=true
Login.Port=9010
```

**Geplante Umsetzung:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NextGen.Login": "Debug"
    }
  },
  "LoginServer": {
    "Port": 9010,
    "Debug": true,
    "InterPassword": "lol",
    "InterServerPort": 10022,
    "Database": {
      "Server": "localhost",
      "Port": 3306,
      "User": "root",
      "Password": "1234567",
      "Database": "fiesta_login",
      "MinPoolSize": 10,
      "MaxPoolSize": 20
    }
  },
  "WorldServer": { ... },
  "ZoneServer": { ... }
}
```

**Migrationsschritte:**
- [ ] `Microsoft.Extensions.Configuration` NuGet-Pakete hinzufügen
- [ ] `Microsoft.Extensions.Configuration.Json`
- [ ] `Microsoft.Extensions.Configuration.Binder`
- [ ] `Microsoft.Extensions.Options`
- [ ] Konfigurations-Klassen mit `IOptions<T>` Pattern erstellen
- [ ] `Settings.cs` in jedem Projekt durch `IOptions<T>` ersetzen
- [ ] `Config.cfg` Parser durch `ConfigurationBuilder` ersetzen
- [ ] Umgebungsvariablen-Support hinzufügen (`ASPNETCORE_ENVIRONMENT`)

### 1.3 Dependency Injection
**Ziel:** Entkopplung durch DI-Container

**Geplante Umsetzung:**
```csharp
// Program.cs
var services = new ServiceCollection();
services.AddSingleton<ILoggerFactory, LoggerFactory>();
services.AddSingleton<IDatabaseManager, DatabaseManager>();
services.AddSingleton<IClientManager, ClientManager>();
services.AddSingleton<IWorldManager, WorldManager>();

var serviceProvider = services.BuildServiceProvider();
```

**Migrationsschritte:**
- [ ] `Microsoft.Extensions.DependencyInjection` hinzufügen
- [ ] Interfaces für alle Manager-Klassen definieren
- [ ] Statische Singletons (`Instance`) durch DI ersetzen
- [ ] `ServerModuleAttribute` / `InitializerMethodAttribute` durch DI-Container ersetzen

---

## Phase 2: Datenbankschicht (Woche 2)

### 2.1 SQL-Injection-Fix
**Ziel:** Alle unsicheren String-Concatenation-Queries eliminieren

**Kritische Dateien:**
| Datei | Zeile | Problem |
|---|---|---|
| `LoginHandler.cs` | 67 | `SELECT * FROM accounts WHERE Username= '" + username + "'` |
| `LoginHandler.cs` | 71 | `INSERT INTO accounts (...) VALUES ('" + username + "','" + clientPassword + "')` |
| `LoginHandler.cs` | 74 | `SELECT * FROM accounts WHERE Username= '" + username + "'` |

**Geplante Umsetzung (Dapper):**
```csharp
// UNSICHER (alt):
loginData = dbClient.ReadDataTable("SELECT * FROM accounts WHERE Username= '" + username + "'");

// SICHER (neu):
var account = await connection.QueryFirstOrDefaultAsync<Account>(
    "SELECT * FROM accounts WHERE Username = @Username",
    new { Username = username });
```

**Migrationsschritte:**
- [ ] `Dapper` NuGet-Paket hinzufügen
- [ ] Alle `ReadDataTable()`-Aufrufe auditieren
- [ ] Parameterized Queries für alle DB-Operationen
- [ ] `DatabaseClient.cs` komplett überarbeiten
- [ ] Eigenes Connection-Pooling entfernen (MySqlConnector hat bereits Pooling)

### 2.2 Passwort-Hashing
**Ziel:** Klartext-Passwörter durch sichere Hashes ersetzen

**Aktueller Stand:**
```csharp
// LoginHandler.cs:84
if (clientPassword == password)  // KLARTEXT!
```

**Geplante Umsetzung (BCrypt):**
```bash
dotnet add package BCrypt.Net-Next
```

```csharp
// Registrierung
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(clientPassword, workFactor: 12);

// Login
bool valid = BCrypt.Net.BCrypt.Verify(clientPassword, storedHash);
```

**Migrationsschritte:**
- [ ] `BCrypt.Net-Next` NuGet-Paket hinzufügen
- [ ] `accounts`-Tabelle: Spalte `password` auf VARCHAR(60) erweitern
- [ ] Auto-Account-Creation mit BCrypt-Hashing
- [ ] Migration bestehender Klartext-Passwörter (optional: Force-Reset)

### 2.3 Datenbank-Zugriff (Dapper)
**Ziel:** Rohe ADO.NET durch Dapper ersetzen

**Aktueller Stand:**
```csharp
// DatabaseClient.cs
public DataTable ReadDataTable(string sQuery)
{
    MySqlCommand command = new MySqlCommand(sQuery, mConnection);
    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
    DataTable table = new DataTable();
    adapter.Fill(table);
    return table;
}
```

**Geplante Umsetzung:**
```csharp
public class AccountRepository : IAccountRepository
{
    private readonly IDbConnection _connection;

    public async Task<Account> GetByUsernameAsync(string username)
    {
        return await _connection.QueryFirstOrDefaultAsync<Account>(
            "SELECT * FROM accounts WHERE Username = @Username",
            new { Username = username });
    }

    public async Task CreateAsync(Account account)
    {
        await _connection.ExecuteAsync(
            "INSERT INTO accounts (username, password) VALUES (@Username, @Password)",
            account);
    }
}
```

**Migrationsschritte:**
- [ ] `Dapper` NuGet-Paket zu allen Projekten hinzufügen
- [ ] Repository-Pattern für jede Entität implementieren
- [ ] Async/await für alle DB-Operationen
- [ ] `DatabaseManager` als `IDbConnectionFactory` umgestalten
- [ ] `using` Statements für Connection-Disposal

### 2.4 SQL-Skripte modernisieren
**Ziel:** Datenbank-Schema für moderne SQL-Standards anpassen

**Migrationsschritte:**
- [ ] `utf8mb4` statt `utf8` für Unicode-Support
- [ ] `DATETIME(3)` für Millisekunden-Präzision
- [ ] `BIGINT UNSIGNED` für IDs (Auto-Increment)
- [ ] Foreign Keys hinzufügen
- [ ] Indizes für häufige Queries optimieren
- [ ] `accounts`-Tabelle: `password` auf VARCHAR(60) für BCrypt

---

## Phase 3: Netzwerk-Core (Woche 3–4)

### 3.1 System.IO.Pipelines
**Ziel:** Alte Socket-Engine durch Pipelines ersetzen

**Aktueller Stand:**
```csharp
// Client.cs – veraltete Socket-API
private void BeginReceive()
{
    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
    args.Completed += EndReceive;
    args.SetBuffer(receiveBuffer, mReceiveStart, ...);
    if (!this.Socket.ReceiveAsync(args))
        EndReceive(this, args);
}
```

**Geplante Umsetzung:**
```csharp
public class PipelineClient
{
    private readonly Pipe _pipe;
    private readonly Socket _socket;

    public async Task ProcessAsync(CancellationToken ct)
    {
        var writer = _pipe.Writer;
        while (!ct.IsCancellationRequested)
        {
            var memory = writer.GetMemory(4096);
            int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, ct);
            if (bytesRead == 0) break;
            writer.Advance(bytesRead);
            await writer.FlushAsync(ct);
        }
    }
}
```

**Migrationsschritte:**
- [ ] `System.IO.Pipelines` NuGet-Paket hinzufügen
- [ ] `Client.cs` komplett neu schreiben
- [ ] `Listener.cs` auf `Socket.Listen()` + `AcceptAsync()` umstellen
- [ ] Paket-Parser auf Pipelines umstellen
- [ ] Backpressure-Handling implementieren

### 3.2 Async/Await Refactoring
**Ziel:** Blockierende Threads eliminieren

**Aktueller Stand:**
```csharp
// Blocking!
while (true)
    Console.ReadLine();
```

**Geplante Umsetzung:**
```csharp
public async Task RunAsync(CancellationToken cancellationToken)
{
    await InitializeAsync(cancellationToken);

    while (!cancellationToken.IsCancellationRequested)
    {
        var command = await Task.Run(Console.ReadLine, cancellationToken);
        await HandleCommandAsync(command, cancellationToken);
    }
}
```

**Migrationsschritte:**
- [ ] Alle `void Main()` → `async Task Main()`
- [ ] `Console.ReadLine()` → `Task.Run(Console.ReadLine)`
- [ ] `Thread.Sleep()` → `Task.Delay()`
- [ ] `lock()` → `SemaphoreSlim` oder `async` Locks
- [ ] `Timer` → `PeriodicTimer` (.NET 6+)

### 3.3 Speicheroptimierung (ArrayPool)
**Ziel:** GC-Druck durch Buffer-Pooling reduzieren

**Geplante Umsetzung:**
```csharp
private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

public void ProcessPacket(int length)
{
    byte[] buffer = _bufferPool.Rent(length);
    try
    {
        // Verarbeite Paket
    }
    finally
    {
        _bufferPool.Return(buffer);
    }
}
```

**Migrationsschritte:**
- [ ] `ArrayPool<byte>` für Paket-Buffer einführen
- [ ] `Memory<byte>` statt `byte[]` für Paket-Daten
- [ ] `Span<T>` für Parsing-Operationen
- [ ] `IBufferWriter<byte>` für Serialisierung

---

## Phase 4: Client-Kompatibilität & Krypto (Woche 5)

### 4.1 Krypto-Update
**Ziel:** XOR-Verschlüsselung für moderne Client-Versionen anpassen

**Aktueller Stand:**
```csharp
// NetCrypto.cs
public void Crypt(byte[] pData, int pIndex, int pLength)
{
    // XOR-basierte Verschlüsselung
}
```

**Migrationsschritte:**
- [ ] Client-Version-Detection implementieren
- [ ] Verschiedene Krypto-Algorithmen pro Client-Version
- [ ] `ReadOnlySpan<byte>` für Krypto-Operationen

### 4.2 Opcode-Mapping
**Ziel:** Paket-Header für aktuelle Client-Versionen

**Aktueller Stand:**
```csharp
public enum SH3Type : byte
{
    VersionAllowed = 0,
    Error = 1,
    // ...
}
```

**Migrationsschritte:**
- [ ] Opcode-Versionierung einführen
- [ ] Client-Version → Opcode-Map
- [ ] Dynamische Paket-Handler-Registrierung

---

## Phase 5: Spiel-Logik & Thread-Safety (Woche 6+)

### 5.1 Concurrent Collections
**Ziel:** Thread-sichere Datenstrukturen

**Aktueller Stand:**
```csharp
private readonly List<LoginClient> clients = new List<LoginClient>();
lock (clients) { clients.Add(client); }
```

**Geplante Umsetzung:**
```csharp
private readonly ConcurrentDictionary<int, LoginClient> _clients = new();
_clients.TryAdd(client.AccountID, client);
```

**Migrationsschritte:**
- [ ] `List<T>` → `ConcurrentBag<T>` oder `ConcurrentDictionary<K,V>`
- [ ] `Dictionary<K,V>` → `ConcurrentDictionary<K,V>`
- [ ] `lock()` → `Interlocked` oder `Concurrent`-Collections
- [ ] `Timer` → `Channel<T>` für Event-Queues

### 5.2 Modul-Testing
**Ziel:** Spiel-Systeme stabilisieren

**Module:**
- [ ] Movement
- [ ] Chat
- [ ] Inventar
- [ ] Kampf-System
- [ ] Gruppen
- [ ] Gilden
- [ ] Handel

---

## Phase 6: DevOps & Qualitätssicherung

### 6.1 Docker-Unterstützung
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NextGen.Login.dll"]
```

### 6.2 docker-compose.yml
```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: 1234567
    ports:
      - "3306:3306"

  login:
    build: .
    depends_on:
      - mysql
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

### 6.3 GitHub Actions CI/CD
```yaml
name: .NET CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### 6.4 Unit Tests
```bash
dotnet add package xunit
dotnet add package Moq
dotnet add package FluentAssertions
```

---

## Anhang: NuGet-Paket-Übersicht

| Paket | Version | Zweck |
|---|---|---|
| `MySqlConnector` | 2.4.0 | MySQL-Datenbankverbindung |
| `System.Diagnostics.PerformanceCounter` | 9.0.0 | Performance-Counter (Windows) |
| `Serilog` | 4.x | Logging-Framework |
| `Serilog.Sinks.Console` | 4.x | Konsolen-Logging |
| `Serilog.Sinks.File` | 4.x | Datei-Logging |
| `Microsoft.Extensions.Configuration` | 9.0.0 | Konfiguration |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | JSON-Config |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | DI-Container |
| `Dapper` | 2.1.x | Micro-ORM |
| `BCrypt.Net-Next` | 4.x | Passwort-Hashing |
| `System.IO.Pipelines` | 9.0.0 | High-Performance I/O |
| `xunit` | 2.x | Testing-Framework |
| `Moq` | 4.x | Mocking |

---

## Anhang: Git-Workflow

```bash
# Repository initialisieren
git init
git add .
git commit -m "Initial commit: .NET 9 migration complete"

# Branching-Strategie
git checkout -b feature/serilog-logging
git checkout -b feature/dapper-database
git checkout -b feature/pipelines-network

# Tags für Meilensteine
git tag -a v0.1.0 -m "Phase 0: .NET 9 Build"
git tag -a v0.2.0 -m "Phase 1: Infrastructure"
git tag -a v0.3.0 -m "Phase 2: Database Layer"
git tag -a v1.0.0 -m "Phase 6: Production Ready"
```

---

*Dokument erstellt: 2026-08-31*  
*Letzte Aktualisierung: Phase 0 abgeschlossen, Build erfolgreich*
