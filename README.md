# Automation.Cli

CLI-Tool zur automatisierten Ticket-Analyse und Implementierung mit Claude AI.

## Verwendung

```bash
# Alle Steps ausfuehren (Analyse + Implementierung)
analyze-ticket run "https://github.com/owner/repo/issues/123"

# Nur Analyse ausfuehren (Steps 1-5, ohne Implementierung)
analyze-ticket analyze "https://github.com/owner/repo/issues/123"

# Einzelnen Step ausfuehren
analyze-ticket step <name> "https://github.com/owner/repo/issues/123"

# Verfuegbare Steps anzeigen
analyze-ticket list-steps
```

## Verfuegbare Steps

| Step | Name | Beschreibung |
|------|------|--------------|
| 1 | `data` | Data/Entities Analyse - Prueft ob Entities erstellt/geaendert/geloescht werden muessen |
| 2 | `api` | RestService/API Analyse - Prueft ob API-Endpoints angepasst werden muessen |
| 3 | `frontend` | Frontend/Uno Analyse - Prueft ob Pages/ViewModels angepasst werden muessen |
| 4 | `structure` | Projektstruktur Analyse - Ordnet Tasks den betroffenen csprojs zu |
| 5 | `skills` | Skills Zuordnung - Ordnet passende Claude Skills den Tasks zu |
| 6 | `load-tasks` | Tasks laden - Laedt Tasks aus GitHub Issue (Checklisten oder Sub-Issues) |
| 7 | `implement` | Implementierung - Implementiert alle geladenen Tasks mit Claude |

## Architektur

Das Tool verwendet das **Shiny Mediator Pattern**:

- Jeder Step ist ein `IRequest<StepResult>` mit eigenem `IRequestHandler`
- Automatische Service-Registrierung via `[Service]` Attribut
- Orchestrierung ueber `IMediator.Request()`

### Projektstruktur

```
Automation.Cli/
├── Program.cs                          # Entry point mit System.CommandLine
├── CliService.cs                       # DI-Konstanten
├── Contracts/
│   ├── StepContext.cs                  # Shared Context (Ticket-Info)
│   ├── StepResult.cs                   # Ergebnis-Record
│   └── Requests/                       # Request-Records fuer jeden Step
├── Handlers/                           # Handler-Implementierungen
├── Infrastructure/
│   ├── IProcessRunner.cs               # Interface fuer CLI-Aufrufe
│   ├── ProcessRunner.cs                # claude/gh CLI Wrapper
│   └── PromptTemplates.cs              # Prompt-Templates
└── Configuration/
    └── ServiceCollectionExtensions.cs  # DI-Setup
```

## Voraussetzungen

- .NET 10
- [Claude CLI](https://claude.ai/code) installiert und konfiguriert
- [GitHub CLI](https://cli.github.com/) installiert und authentifiziert (fuer GitHub Issues)

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run -- run "https://github.com/owner/repo/issues/123"

# Oder als Tool installieren
dotnet pack
dotnet tool install --global --add-source ./nupkg Automation.Cli
```

## Beispiele

### Komplette Ticket-Analyse mit Implementierung

```bash
analyze-ticket run "https://github.com/myorg/myrepo/issues/42"
```

Fuehrt alle 7 Steps aus:
1. Analysiert Data/Entity-Aenderungen
2. Analysiert API-Aenderungen
3. Analysiert Frontend-Aenderungen
4. Ordnet Tasks den Projekten zu
5. Ordnet Skills zu
6. Laedt Tasks aus dem GitHub Issue
7. Implementiert jeden Task mit Claude

### Nur Analyse ohne Implementierung

```bash
analyze-ticket analyze "https://github.com/myorg/myrepo/issues/42"
```

Fuehrt nur Steps 1-5 aus und erstellt SubTasks im GitHub Issue.

### Einzelnen Step ausfuehren

```bash
# Nur Data-Analyse
analyze-ticket step data "https://github.com/myorg/myrepo/issues/42"

# Nur Frontend-Analyse
analyze-ticket step frontend "https://github.com/myorg/myrepo/issues/42"
```

## Erweiterung

### Neuen Step hinzufuegen

1. Request in `Contracts/Requests/` erstellen:
```csharp
public record MyNewRequest(StepContext Context) : IRequest<StepResult>;
```

2. Handler in `Handlers/` erstellen:
```csharp
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class MyNewHandler(IProcessRunner processRunner)
    : IRequestHandler<MyNewRequest, StepResult>
{
    public async Task<StepResult> Handle(
        MyNewRequest request,
        IMediatorContext context,
        CancellationToken ct)
    {
        // Implementierung
        return StepResult.Ok("My New Step");
    }
}
```

3. In `Program.cs` zum step-Command hinzufuegen
