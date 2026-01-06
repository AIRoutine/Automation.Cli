using Automation.Cli.Contracts.Classification;

namespace Automation.Cli.Contracts.Pipeline;

/// <summary>
/// Interface fuer einen ausfuehrbaren Pipeline-Step.
/// </summary>
public interface IExecutableStep
{
    /// <summary>
    /// Eindeutige ID des Steps (z.B. "data-analysis").
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Anzeigename fuer Logging.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Welche Schichten dieser Step betrifft.
    /// </summary>
    LayerScope AffectedLayers { get; }

    /// <summary>
    /// Step-IDs die vor diesem Step ausgefuehrt werden muessen.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Fuehrt den Step aus.
    /// </summary>
    Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default);
}
