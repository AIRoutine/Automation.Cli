namespace Automation.Cli.Contracts;

/// <summary>
/// Ergebnis eines Step-Handlers.
/// </summary>
public record StepResult
{
    private StepResult(string stepName, bool success, string? error = null, List<string>? tasks = null)
    {
        StepName = stepName;
        Success = success;
        Error = error;
        Tasks = tasks ?? [];
    }

    public string StepName { get; }
    public bool Success { get; }
    public string? Error { get; }
    public List<string> Tasks { get; }

    public static StepResult Ok(string stepName) => new(stepName, true);

    public static StepResult Ok(string stepName, List<string> tasks) => new(stepName, true, tasks: tasks);

    public static StepResult Failed(string stepName, string? error = null) => new(stepName, false, error);
}
