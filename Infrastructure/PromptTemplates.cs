using Automation.Cli.Contracts;

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
