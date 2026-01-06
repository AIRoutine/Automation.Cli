using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer die eigentliche Implementierung aller Tasks.
/// </summary>
public class ImplementStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "implement";
    public override string DisplayName => "Implementierung";
    public override LayerScope AffectedLayers => LayerScope.All;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        if (context.Tasks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Keine Tasks zum Implementieren gefunden.");
            Console.ResetColor();
            return StepResult.Ok(DisplayName);
        }

        Console.WriteLine($"  Implementiere {context.Tasks.Count} Task(s)...");

        // Alle Tasks in einem Prompt implementieren (effizienter)
        var prompt = PromptTemplates.GetImplementAllTasksPrompt(context);
        var result = await ProcessRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed(DisplayName, result.Error);
        }

        // Seeding fuer Data-Tasks
        var dataTasks = context.Tasks.Where(PromptTemplates.IsDataTask).ToList();
        if (dataTasks.Count > 0)
        {
            Console.WriteLine($"  Erstelle Seeder fuer {dataTasks.Count} Data-Task(s)...");
            var seedingPrompt = PromptTemplates.GetSeedingPrompt(context);
            await ProcessRunner.RunClaudeAsync(seedingPrompt, ct: ct);
        }

        return StepResult.Ok(DisplayName, context.Tasks);
    }
}
