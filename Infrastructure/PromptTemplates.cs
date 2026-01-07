using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;

namespace Automation.Cli.Infrastructure;

/// <summary>
/// Alle Prompt-Templates fuer die Analyse-Steps.
/// </summary>
public static class PromptTemplates
{
    private const string ClassifierJsonTemplate = """
        ```json
        {
          "type": "NewFeature|Enhancement|BugFix|Refactoring|Documentation|Configuration|DataMigration",
          "scope": ["Data", "Api", "Frontend", "Shared", "Infrastructure"],
          "complexity": "Trivial|Simple|Medium|Complex|Epic",
          "steps": [
            {"stepId": "step-id", "order": 1, "required": true, "reason": "Begruendung"}
          ],
          "tasks": ["Task 1 aus dem Ticket", "Task 2 aus dem Ticket"],
          "summary": "Kurze Zusammenfassung was das Ticket will"
        }
        ```
        """;

    /// <summary>
    /// Classifier-Prompt: Analysiert das Ticket und gibt strukturiertes JSON zurueck.
    /// </summary>
    public static string GetClassifierPrompt(StepContext context) => $"""
        Analysiere das folgende Ticket und klassifiziere es.

        {context.GetSharedContext()}

        AUFGABE: Analysiere das Ticket gruendlich und erstelle eine Klassifizierung.

        1. Lade zuerst den vollstaendigen Ticket-Inhalt (falls GitHub Issue)
        2. Lies alle Subtasks, Kommentare und Beschreibungen
        3. Bestimme welche Bereiche betroffen sind

        Antworte NUR mit folgendem JSON-Format (keine andere Ausgabe!):

        {ClassifierJsonTemplate}

        VERFUEGBARE STEPS (waehle nur relevante):
        - data-analysis: Fuer Entity/Datenbank-Aenderungen (neue Entities, Migrationen)
        - api-analysis: Fuer API-Endpoint-Aenderungen (neue/geaenderte Endpoints)
        - frontend-analysis: Fuer UI-Aenderungen (Pages, ViewModels, XAML)
        - project-structure: Fuer neue Projekte/Ordner (nur wenn neue csproj noetig)
        - skill-mapping: Fuer Claude-Skill-Zuweisung (optional, nur bei komplexen Tasks)
        - implement: Fuer die eigentliche Implementierung (immer als letzter Step)

        REGELN:
        - Waehle NUR Steps die wirklich benoetigt werden
        - Ein reiner Frontend-Bug braucht KEINEN data-analysis Step
        - Ein reiner API-Fix braucht KEINEN frontend-analysis Step
        - "implement" sollte immer der letzte Step sein
        - Extrahiere alle Tasks/Subtasks aus dem Ticket in "tasks"

        BEISPIELE:

        Ticket: "Button-Farbe aendern"
        -> scope: ["Frontend"], steps: [frontend-analysis, implement]

        Ticket: "Neues User-Profil Feature"
        -> scope: ["Data", "Api", "Frontend"], steps: [data-analysis, api-analysis, frontend-analysis, project-structure, implement]

        Ticket: "API-Endpoint fuer Login fixen"
        -> scope: ["Api"], steps: [api-analysis, implement]

        WICHTIG: Gib NUR das JSON zurueck, keine Erklaerungen davor oder danach!
        """;

    public static string GetDataAnalysisPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === SCHRITT: DATA ANALYSE ===

        Analysiere und implementiere die Data/Entity-Aenderungen fuer dieses Ticket.

        1. Welche Entities muessen erstellt werden?
        2. Welche Entities muessen geaendert werden?
        3. Welche Entities muessen geloescht werden?
        4. Braucht es Migrationen?

        AKTION: Implementiere die notwendigen Entity-Aenderungen JETZT.
        Verwende Write/Edit Tools um die Dateien zu erstellen/aendern.
        Erstelle auch Seeder fuer neue Entities mit realistischen Testdaten.
        """;

    public static string GetApiAnalysisPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === SCHRITT: API ANALYSE ===

        Analysiere und implementiere die API-Aenderungen fuer dieses Ticket.

        1. Welche Endpoints muessen erstellt werden?
        2. Welche Endpoints muessen geaendert werden?
        3. Welche Endpoints muessen geloescht werden?
        4. Welche Handler/Services sind betroffen?

        AKTION: Implementiere die notwendigen API-Aenderungen JETZT.
        Verwende Write/Edit Tools um die Dateien zu erstellen/aendern.
        Nutze den Skill uno-dev:api-endpoint-authoring falls sinnvoll.
        """;

    public static string GetFrontendAnalysisPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === SCHRITT: FRONTEND ANALYSE ===

        Analysiere und implementiere die Frontend-Aenderungen fuer dieses Ticket.

        1. Welche Pages/Views muessen erstellt werden?
        2. Welche Pages/Views muessen geaendert werden?
        3. Welche ViewModels sind betroffen?
        4. Welche XAML-Aenderungen sind noetig?

        AKTION: Implementiere die notwendigen Frontend-Aenderungen JETZT.
        Verwende Write/Edit Tools um die Dateien zu erstellen/aendern.
        Nutze die Skills uno-dev:viewmodel-authoring und uno-dev:xaml-authoring.
        """;

    public static string GetProjectStructurePrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === SCHRITT: PROJEKTSTRUKTUR ===

        Analysiere ob neue Projekte/csproj-Dateien benoetigt werden.

        1. Braucht es ein neues Feature-Projekt?
        2. Welche bestehenden Projekte sind betroffen?
        3. Muessen Projekt-Referenzen angepasst werden?

        AKTION: Erstelle neue Projekte falls noetig.
        Verwende 'dotnet new classlib' ueber Bash.
        Nutze den Skill uno-dev:api-library-authoring fuer neue API-Features.
        """;

    public static string GetSkillMappingPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === SCHRITT: SKILL MAPPING ===

        Ordne den Tasks die passenden Claude-Skills zu.

        Verfuegbare Skills:
        - uno-dev:api-endpoint-authoring - API Endpoints mit Shiny.Mediator
        - uno-dev:api-library-authoring - Neue API Feature Libraries
        - uno-dev:entity-authoring - Entity Framework Core Entities
        - uno-dev:mediator-authoring - Commands, Events, Requests
        - uno-dev:store-authoring - Persistenter State mit Shiny Stores
        - uno-dev:viewmodel-authoring - ViewModels fuer Uno Platform
        - uno-dev:xaml-authoring - XAML Views, Pages, UserControls

        Dokumentiere welcher Skill fuer welchen Task verwendet werden soll.
        """;

    public static string GetImplementTaskPrompt(StepContext context, string task) => $"""
        {context.GetClassifiedContext()}

        === IMPLEMENTIERUNG ===

        Implementiere folgenden Task JETZT:
        {task}

        ANLEITUNG:
        1. Lies zuerst CLAUDE.md fuer die Projektstruktur und Konventionen
        2. Verwende das Write-Tool um neue Dateien anzulegen
        3. Verwende das Edit-Tool um bestehende Dateien zu aendern
        4. Verwende Bash um dotnet CLI Befehle auszufuehren
        5. Nach Erstellung: Fuehre 'dotnet build' aus um Fehler zu pruefen

        Nutze die passenden Skills falls angegeben.

        WICHTIG: Du MUSST die Dateien tatsaechlich erstellen - nicht nur beschreiben!
        Beginne SOFORT mit der Implementierung.
        """;

    public static string GetImplementAllTasksPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === IMPLEMENTIERUNG ALLER TASKS ===

        Implementiere ALLE folgenden Tasks:

        {string.Join("\n", context.Tasks.Select((t, i) => $"{i + 1}. {t}"))}

        ANLEITUNG:
        1. Lies zuerst CLAUDE.md fuer die Projektstruktur und Konventionen
        2. Arbeite die Tasks der Reihe nach ab
        3. Verwende Write/Edit Tools fuer Dateien
        4. Erstelle Seeder fuer neue Entities
        5. Fuehre am Ende 'dotnet build' aus

        WICHTIG: Implementiere ALLE Tasks vollstaendig!
        """;

    /// <summary>
    /// Kombinierter Prompt fuer Fast-Mode: Alles in einem Aufruf.
    /// </summary>
    public static string GetFastImplementPrompt(StepContext context) => $"""
        {context.GetClassifiedContext()}

        === VOLLSTAENDIGE IMPLEMENTIERUNG ===

        Du sollst jetzt die gesamte Implementierung in einem Durchgang durchfuehren.

        TICKET-SUMMARY: {context.Classification?.Summary ?? context.TicketDescription}

        SCOPE: {context.Classification?.Scope.ToString() ?? "All"}

        TASKS:
        {string.Join("\n", context.Tasks.Select((t, i) => $"  {i + 1}. {t}"))}

        === IMPLEMENTIERUNGS-PLAN ===

        Fuehre folgende Schritte der Reihe nach aus:

        {GetImplementationSteps(context)}

        === REGELN ===

        1. Lies zuerst CLAUDE.md fuer die Projektstruktur und Konventionen
        2. Verwende die passenden Skills wenn verfuegbar
        3. Erstelle Seeder fuer neue Entities mit realistischen Testdaten
        4. Fuehre am Ende 'dotnet build' aus um Fehler zu pruefen
        5. Bei Build-Fehlern: Behebe sie sofort!

        WICHTIG:
        - Du MUSST die Dateien tatsaechlich erstellen/aendern - nicht nur beschreiben!
        - Implementiere ALLES vollstaendig in diesem Durchgang!
        - Beginne JETZT mit der Implementierung!
        """;

    private static string GetImplementationSteps(StepContext context)
    {
        var steps = new List<string>();
        var scope = context.Classification?.Scope ?? LayerScope.All;
        var stepNumber = 1;

        if (scope.HasFlag(LayerScope.Data))
        {
            steps.Add($"""
                {stepNumber}. DATA/ENTITIES:
                   - Erstelle/aendere Entity-Klassen in src/api/src/Features/
                   - Erstelle EntityConfiguration fuer EF Core
                   - Erstelle Seeder mit realistischen Testdaten
                   - Nutze Skill: uno-dev:entity-authoring
                """);
            stepNumber++;
        }

        if (scope.HasFlag(LayerScope.Api))
        {
            steps.Add($"""
                {stepNumber}. API/ENDPOINTS:
                   - Erstelle/aendere Request/Response DTOs in Contracts
                   - Erstelle/aendere Handler fuer Mediator
                   - Erstelle/aendere Endpoints
                   - Nutze Skill: uno-dev:api-endpoint-authoring
                """);
            stepNumber++;
        }

        if (scope.HasFlag(LayerScope.Frontend))
        {
            steps.Add($"""
                {stepNumber}. FRONTEND/UNO:
                   - WICHTIG: Du MUSST zuerst den Uno MCP initialisieren!
                   - Rufe ZUERST uno_platform_agent_rules_init auf
                   - Rufe DANACH uno_platform_usage_rules_init auf
                   - Bei Fragen zur Uno Platform: Nutze uno_platform_docs_search
                   - Erstelle/aendere HTTP Contracts im ApiClient
                   - Erstelle/aendere ViewModels
                   - Erstelle/aendere XAML Pages/Views
                   - Nutze Skills: uno-dev:viewmodel-authoring, uno-dev:xaml-authoring
                """);
            stepNumber++;
        }

        steps.Add($"""
            {stepNumber}. BUILD & VERIFY:
               - Fuehre 'dotnet build' aus
               - Behebe alle Build-Fehler
               - Stelle sicher dass alles kompiliert
            """);

        return string.Join("\n\n", steps);
    }

    private const string VisualValidationJsonTemplate = """
        ```json
        {
          "status": "success|skipped|failed",
          "appRunning": true,
          "screenshotTaken": true,
          "changesVisible": true,
          "issues": ["issue1", "issue2"],
          "summary": "Kurze Beschreibung des Ergebnisses"
        }
        ```
        """;

    /// <summary>
    /// Separater Prompt fuer visuelle Validierung - wird nach der Implementierung ausgefuehrt.
    /// Erwartet strukturierte JSON-Antwort fuer maschinelle Auswertung.
    /// </summary>
    public static string GetVisualValidationPrompt(StepContext context) => $"""
        === VISUELLE VALIDIERUNG ===

        Die Implementierung wurde abgeschlossen. Validiere die Aenderungen visuell.

        TICKET: {context.Classification?.Summary ?? context.TicketDescription}

        === SCHRITT 1: APP-STATUS PRUEFEN ===

        Pruefe ob uno-app MCP Tools verfuegbar sind:
        - Versuche uno_app_get_runtime_info aufzurufen
        - Falls Tool nicht verfuegbar oder Fehler: setze status="skipped"

        Falls App nicht laeuft ("No connected app"):
        - Warte 5 Sekunden mit Bash: Start-Sleep -Seconds 5
        - Versuche erneut (max 3 Versuche)
        - Falls nach 3 Versuchen keine App: setze appRunning=false

        === SCHRITT 2: SCREENSHOT ERSTELLEN ===

        Nur wenn App laeuft (appRunning=true):
        - Rufe uno_app_get_screenshot auf
        - Analysiere das Bild:
          * Was siehst du?
          * Sind die gewuenschten Aenderungen sichtbar?
          * Gibt es Layout-Probleme?
        - Setze screenshotTaken=true bei Erfolg
        - Setze changesVisible=true wenn Ticket-Aenderungen sichtbar

        === SCHRITT 3: APP BEENDEN ===

        Nur wenn App laeuft:
        - Rufe uno_app_close auf

        === ANTWORT-FORMAT ===

        WICHTIG: Antworte NUR mit folgendem JSON-Format (keine andere Ausgabe!):

        {VisualValidationJsonTemplate}

        REGELN:
        - status: "success" wenn alles OK, "skipped" wenn MCP nicht verfuegbar, "failed" bei Fehlern
        - appRunning: true wenn App erfolgreich verbunden war
        - screenshotTaken: true wenn Screenshot erstellt wurde
        - changesVisible: true wenn Ticket-Aenderungen im Screenshot sichtbar, null wenn nicht pruefbar
        - issues: Array mit gefundenen Problemen (leer wenn keine)
        - summary: Kurze menschenlesbare Zusammenfassung

        WICHTIG: Gib NUR das JSON zurueck, keine Erklaerungen davor oder danach!
        """;

    public static string GetSeedingPrompt(StepContext context) =>
        context.GetSharedContext() + """


        AKTION: Erstelle JETZT einen Seeder fuer das gerade implementierte Data/Entity Feature.

        Folge den Konventionen aus CLAUDE.md fuer Seeder.
        Erstelle 5-10 realistische Testdaten.

        WICHTIG: Du MUSST die Dateien mit Write/Edit erstellen - nicht nur beschreiben!
        """;

    /// <summary>
    /// Prueft ob ein Task Data/Entity-bezogen ist.
    /// </summary>
    public static bool IsDataTask(string task) =>
        task.Contains("Data", StringComparison.OrdinalIgnoreCase) ||
        task.Contains("Entity", StringComparison.OrdinalIgnoreCase) ||
        task.Contains("Entities", StringComparison.OrdinalIgnoreCase) ||
        task.Contains("Daten", StringComparison.OrdinalIgnoreCase);
}
