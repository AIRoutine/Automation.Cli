using Automation.Cli.Contracts.Classification;

namespace Automation.Cli.Contracts.Pipeline;

/// <summary>
/// Ergebnis einer Pipeline-Ausfuehrung.
/// </summary>
public sealed record PipelineResult
{
    /// <summary>
    /// Waren alle Steps erfolgreich?
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Ergebnisse aller ausgefuehrten Steps.
    /// </summary>
    public required IReadOnlyList<StepResult> StepResults { get; init; }

    /// <summary>
    /// Die verwendete Klassifizierung.
    /// </summary>
    public required TicketClassification Classification { get; init; }

    /// <summary>
    /// Uebersprungene Steps (nicht in der Klassifizierung).
    /// </summary>
    public IReadOnlyList<string> SkippedSteps { get; init; } = [];

    /// <summary>
    /// Gesamtfehler falls Pipeline abgebrochen.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Erstellt ein erfolgreiches Ergebnis.
    /// </summary>
    public static PipelineResult Ok(TicketClassification classification, IReadOnlyList<StepResult> results, IReadOnlyList<string>? skipped = null) => new()
    {
        Success = true,
        Classification = classification,
        StepResults = results,
        SkippedSteps = skipped ?? []
    };

    /// <summary>
    /// Erstellt ein fehlgeschlagenes Ergebnis.
    /// </summary>
    public static PipelineResult Failed(TicketClassification classification, IReadOnlyList<StepResult> results, string error) => new()
    {
        Success = false,
        Classification = classification,
        StepResults = results,
        Error = error
    };
}

/// <summary>
/// Executor fuer die dynamische Pipeline.
/// </summary>
public interface IPipelineExecutor
{
    /// <summary>
    /// Fuehrt die Pipeline basierend auf der Klassifizierung aus.
    /// </summary>
    Task<PipelineResult> ExecuteAsync(
        TicketClassification classification,
        StepContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Fuehrt einen Dry-Run durch (zeigt Plan ohne Ausfuehrung).
    /// </summary>
    PipelineResult DryRun(TicketClassification classification);
}
