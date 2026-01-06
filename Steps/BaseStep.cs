using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Basisklasse fuer alle Pipeline-Steps.
/// </summary>
public abstract class BaseStep(IProcessRunner processRunner) : IExecutableStep
{
    protected IProcessRunner ProcessRunner => processRunner;

    public abstract string StepId { get; }
    public abstract string DisplayName { get; }
    public abstract LayerScope AffectedLayers { get; }
    public virtual IReadOnlyList<string> Dependencies => [];

    public abstract Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default);

    /// <summary>
    /// Fuehrt einen Claude-Prompt aus und gibt das Ergebnis zurueck.
    /// </summary>
    protected async Task<StepResult> RunClaudePromptAsync(
        string prompt,
        string stepName,
        CancellationToken ct)
    {
        var result = await ProcessRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed(stepName, result.Error);
        }

        return StepResult.Ok(stepName);
    }
}
