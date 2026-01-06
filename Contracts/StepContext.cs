using Automation.Cli.Contracts.Classification;

namespace Automation.Cli.Contracts;

/// <summary>
/// Gemeinsamer Kontext fuer alle Steps.
/// </summary>
public sealed class StepContext
{
    public StepContext(string ticketDescription)
    {
        TicketDescription = ticketDescription;
        ExtractGitHubInfo();
    }

    /// <summary>
    /// Die urspruengliche Ticket-Beschreibung oder URL.
    /// </summary>
    public string TicketDescription { get; }

    /// <summary>
    /// GitHub Repository (owner/repo), falls aus URL extrahiert.
    /// </summary>
    public string? GitHubRepo { get; private set; }

    /// <summary>
    /// GitHub Issue-Nummer, falls aus URL extrahiert.
    /// </summary>
    public int? GitHubIssueNumber { get; private set; }

    /// <summary>
    /// Die Klassifizierung des Tickets (nach Classifier-Step).
    /// </summary>
    public TicketClassification? Classification { get; set; }

    /// <summary>
    /// Aktueller Step-Index waehrend der Ausfuehrung.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Steps die uebersprungen wurden.
    /// </summary>
    public List<string> SkippedSteps { get; } = [];

    /// <summary>
    /// Geladene Tasks vom Ticket.
    /// </summary>
    public List<string> Tasks { get; } = [];

    /// <summary>
    /// Zusaetzlicher Kontext der waehrend der Ausfuehrung gesammelt wird.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = [];

    /// <summary>
    /// Prueft ob ein GitHub Issue erkannt wurde.
    /// </summary>
    public bool IsGitHubIssue => GitHubRepo is not null && GitHubIssueNumber.HasValue;

    /// <summary>
    /// Prueft ob bereits klassifiziert wurde.
    /// </summary>
    public bool IsClassified => Classification is not null;

    private void ExtractGitHubInfo()
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            TicketDescription,
            @"github\.com/([^/]+/[^/]+)/issues/(\d+)");

        if (match.Success)
        {
            GitHubRepo = match.Groups[1].Value;
            GitHubIssueNumber = int.Parse(match.Groups[2].Value);
        }
    }

    /// <summary>
    /// Generiert den Shared Context fuer Prompts.
    /// </summary>
    public string GetSharedContext() => $"""
        Ticket: {TicketDescription}

        WICHTIG: Du bist im nicht-interaktiven Modus. Du MUSST die verfuegbaren Tools (Write, Edit, Bash, Glob, Grep, Read) aktiv nutzen um Dateien zu erstellen und zu bearbeiten.

        - Verwende Write um neue Dateien zu erstellen
        - Verwende Edit um bestehende Dateien zu aendern
        - Verwende Bash um dotnet CLI Befehle auszufuehren (dotnet new, dotnet build, etc.)
        - Lies CLAUDE.md fuer Projektrichtlinien

        Du hast Zugriff auf die GitHub CLI (gh). Falls das Ticket eine GitHub Issue URL ist, lade den vollstaendigen Inhalt mit:
          gh issue view <issue-number> --repo <owner/repo>
          gh issue view <issue-number> --repo <owner/repo> --comments

        Nutze diese Tools um alle Details, Beschreibungen und SubTasks des Tickets zu laden bevor du mit der Analyse beginnst.

        AKTION ERFORDERLICH: Fuehre die angeforderten Aenderungen durch - antworte nicht nur mit einer Beschreibung, sondern erstelle/bearbeite die Dateien direkt!
        """;

    /// <summary>
    /// Generiert Kontext mit Klassifizierungs-Info.
    /// </summary>
    public string GetClassifiedContext()
    {
        if (Classification is null)
            return GetSharedContext();

        return $"""
            {GetSharedContext()}

            === KLASSIFIZIERUNG ===
            Typ: {Classification.Type}
            Scope: {Classification.Scope}
            Komplexitaet: {Classification.Complexity}
            Zusammenfassung: {Classification.Summary}

            Geplante Steps: {string.Join(" -> ", Classification.GetOrderedStepIds())}
            """;
    }
}
