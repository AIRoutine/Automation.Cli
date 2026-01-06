using System.Text.Json.Serialization;

namespace Automation.Cli.Contracts.Classification;

/// <summary>
/// Ergebnis der Ticket-Klassifizierung durch Claude.
/// </summary>
public sealed record TicketClassification
{
    /// <summary>
    /// Art des Tickets (Feature, Bug, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public required TicketType Type { get; init; }

    /// <summary>
    /// Betroffene Architektur-Schichten.
    /// </summary>
    [JsonPropertyName("scope")]
    public required LayerScope Scope { get; init; }

    /// <summary>
    /// Geschaetzte Komplexitaet.
    /// </summary>
    [JsonPropertyName("complexity")]
    public required Complexity Complexity { get; init; }

    /// <summary>
    /// Geplante Ausfuehrungs-Schritte in der richtigen Reihenfolge.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<ExecutionStep> Steps { get; init; }

    /// <summary>
    /// Kurze Zusammenfassung der Klassifizierung.
    /// </summary>
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>
    /// Extrahierte Tasks/Subtasks aus dem Ticket.
    /// </summary>
    [JsonPropertyName("tasks")]
    public IReadOnlyList<string> Tasks { get; init; } = [];

    /// <summary>
    /// Erstellt eine Fallback-Klassifizierung wenn Parsing fehlschlaegt.
    /// </summary>
    public static TicketClassification CreateFallback(string summary) => new()
    {
        Type = TicketType.NewFeature,
        Scope = LayerScope.All,
        Complexity = Complexity.Medium,
        Steps =
        [
            new ExecutionStep { StepId = "data-analysis", Order = 1, Reason = "Fallback: Alle Steps" },
            new ExecutionStep { StepId = "api-analysis", Order = 2, Reason = "Fallback: Alle Steps" },
            new ExecutionStep { StepId = "frontend-analysis", Order = 3, Reason = "Fallback: Alle Steps" },
            new ExecutionStep { StepId = "project-structure", Order = 4, Reason = "Fallback: Alle Steps" },
            new ExecutionStep { StepId = "skill-mapping", Order = 5, Reason = "Fallback: Alle Steps" },
            new ExecutionStep { StepId = "implement", Order = 6, Reason = "Fallback: Alle Steps" }
        ],
        Summary = summary
    };

    /// <summary>
    /// Prueft ob eine bestimmte Schicht betroffen ist.
    /// </summary>
    public bool AffectsLayer(LayerScope layer) => (Scope & layer) == layer;

    /// <summary>
    /// Gibt alle Step-IDs in Ausfuehrungsreihenfolge zurueck.
    /// </summary>
    public IEnumerable<string> GetOrderedStepIds() =>
        Steps.OrderBy(s => s.Order).Select(s => s.StepId);
}
