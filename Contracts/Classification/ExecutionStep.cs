namespace Automation.Cli.Contracts.Classification;

/// <summary>
/// Ein geplanter Ausfuehrungs-Schritt.
/// </summary>
public sealed record ExecutionStep
{
    /// <summary>
    /// Eindeutige ID des Steps (z.B. "data-analysis", "api-analysis").
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// Ausfuehrungsreihenfolge (1 = erster Step).
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Ist dieser Step zwingend erforderlich?
    /// </summary>
    public bool IsRequired { get; init; } = true;

    /// <summary>
    /// Begruendung warum dieser Step benoetigt wird.
    /// </summary>
    public string? Reason { get; init; }
}
