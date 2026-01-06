namespace Automation.Cli.Contracts.Pipeline;

/// <summary>
/// Registry fuer alle verfuegbaren Pipeline-Steps.
/// </summary>
public interface IStepRegistry
{
    /// <summary>
    /// Registriert einen Step.
    /// </summary>
    void Register(IExecutableStep step);

    /// <summary>
    /// Holt einen Step anhand seiner ID.
    /// </summary>
    IExecutableStep? GetStep(string stepId);

    /// <summary>
    /// Gibt alle registrierten Steps zurueck.
    /// </summary>
    IEnumerable<IExecutableStep> GetAllSteps();

    /// <summary>
    /// Gibt alle Step-IDs zurueck.
    /// </summary>
    IEnumerable<string> GetAllStepIds();

    /// <summary>
    /// Baut die Ausfuehrungsreihenfolge basierend auf Dependencies.
    /// </summary>
    IReadOnlyList<IExecutableStep> BuildExecutionOrder(IEnumerable<string> requiredStepIds);
}
