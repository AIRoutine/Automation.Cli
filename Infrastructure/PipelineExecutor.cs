using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Shiny.Extensions.DependencyInjection;

namespace Automation.Cli.Infrastructure;

/// <summary>
/// Fuehrt die Pipeline basierend auf der Klassifizierung aus.
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd, Type = typeof(IPipelineExecutor))]
public class PipelineExecutor(IStepRegistry registry) : IPipelineExecutor
{
    public async Task<PipelineResult> ExecuteAsync(
        TicketClassification classification,
        StepContext context,
        CancellationToken ct = default)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== PIPELINE AUSFUEHRUNG ===");
        Console.ResetColor();

        var stepIds = classification.GetOrderedStepIds().ToList();
        var orderedSteps = registry.BuildExecutionOrder(stepIds);

        Console.WriteLine($"Ausfuehrungsreihenfolge: {string.Join(" -> ", orderedSteps.Select(s => s.StepId))}");
        Console.WriteLine();

        var results = new List<StepResult>();
        var skippedSteps = new List<string>();
        var allRegisteredSteps = registry.GetAllStepIds().ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Welche Steps werden uebersprungen?
        foreach (var registeredStep in allRegisteredSteps)
        {
            if (!stepIds.Contains(registeredStep, StringComparer.OrdinalIgnoreCase))
            {
                skippedSteps.Add(registeredStep);
            }
        }

        if (skippedSteps.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Uebersprungene Steps: {string.Join(", ", skippedSteps)}");
            Console.ResetColor();
            Console.WriteLine();
        }

        context.SkippedSteps.AddRange(skippedSteps);

        // Steps ausfuehren
        for (var i = 0; i < orderedSteps.Count; i++)
        {
            var step = orderedSteps[i];
            context.CurrentStepIndex = i + 1;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{i + 1}/{orderedSteps.Count}] {step.DisplayName}");
            Console.ResetColor();

            try
            {
                var result = await step.ExecuteAsync(context, ct);
                results.Add(result);

                if (!result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Step fehlgeschlagen: {result.Error}");
                    Console.ResetColor();

                    return PipelineResult.Failed(
                        classification,
                        results,
                        $"Step '{step.StepId}' fehlgeschlagen: {result.Error}");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  -> Erfolgreich");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception in Step '{step.StepId}': {ex.Message}");
                Console.ResetColor();

                results.Add(StepResult.Failed(step.StepId, ex.Message));
                return PipelineResult.Failed(classification, results, ex.Message);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== PIPELINE ERFOLGREICH ABGESCHLOSSEN ===");
        Console.ResetColor();

        return PipelineResult.Ok(classification, results, skippedSteps);
    }

    public PipelineResult DryRun(TicketClassification classification)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== DRY-RUN MODUS ===");
        Console.ResetColor();

        var stepIds = classification.GetOrderedStepIds().ToList();
        var orderedSteps = registry.BuildExecutionOrder(stepIds);

        Console.WriteLine();
        Console.WriteLine("Klassifizierung:");
        Console.WriteLine($"  Typ:         {classification.Type}");
        Console.WriteLine($"  Scope:       {classification.Scope}");
        Console.WriteLine($"  Komplexitaet: {classification.Complexity}");
        Console.WriteLine($"  Summary:     {classification.Summary}");

        Console.WriteLine();
        Console.WriteLine("Geplante Steps:");
        for (var i = 0; i < orderedSteps.Count; i++)
        {
            var step = orderedSteps[i];
            Console.WriteLine($"  {i + 1}. {step.DisplayName} ({step.StepId})");
            Console.WriteLine($"     Betroffene Schichten: {step.AffectedLayers}");
            if (step.Dependencies.Count > 0)
            {
                Console.WriteLine($"     Dependencies: {string.Join(", ", step.Dependencies)}");
            }
        }

        var skippedSteps = registry.GetAllStepIds()
            .Where(id => !stepIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (skippedSteps.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Uebersprungene Steps: {string.Join(", ", skippedSteps)}");
            Console.ResetColor();
        }

        if (classification.Tasks.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Extrahierte Tasks:");
            foreach (var task in classification.Tasks)
            {
                Console.WriteLine($"  - {task}");
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("(Keine Aenderungen vorgenommen - Dry-Run Modus)");
        Console.ResetColor();

        return PipelineResult.Ok(classification, [], skippedSteps);
    }
}
