using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Orchestrierungs-Handler: Fuehrt alle Steps aus
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class RunAllStepsHandler(IMediator mediator) : IRequestHandler<RunAllStepsRequest, StepResult>
{
    public async Task<StepResult> Handle(RunAllStepsRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Ticket Analyse gestartet ===");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Ticket: {request.Context.TicketDescription}");
        Console.ResetColor();

        var stepContext = request.Context;
        var errors = new List<string>();

        // Step 1-5: Analyse
        var analysisSteps = new (string Name, Func<Task<(IMediatorContext, StepResult)>> Execute)[]
        {
            ("Data Analysis", () => mediator.Request(new DataAnalysisRequest(stepContext), ct)),
            ("API Analysis", () => mediator.Request(new ApiAnalysisRequest(stepContext), ct)),
            ("Frontend Analysis", () => mediator.Request(new FrontendAnalysisRequest(stepContext), ct)),
            ("Project Structure", () => mediator.Request(new ProjectStructureRequest(stepContext), ct)),
            ("Skill Mapping", () => mediator.Request(new SkillMappingRequest(stepContext), ct)),
        };

        var stepNumber = 1;

        foreach (var step in analysisSteps)
        {
            var (_, result) = await step.Execute();
            if (!result.Success)
            {
                errors.Add($"Step {stepNumber} ({step.Name}): {result.Error}");
            }
            stepNumber++;
        }

        // Step 6: Tasks laden
        var (_, loadResult) = await mediator.Request(new LoadTasksRequest(stepContext), ct);
        if (!loadResult.Success)
        {
            errors.Add($"Step {stepNumber} (Load Tasks): {loadResult.Error}");
        }

        // Step 7: Tasks implementieren (falls vorhanden)
        if (loadResult.Tasks.Count > 0)
        {
            var (_, implementResult) = await mediator.Request(new ImplementTasksRequest(stepContext, loadResult.Tasks), ct);
            if (!implementResult.Success)
            {
                errors.Add($"Step {stepNumber + 1} (Implement Tasks): {implementResult.Error}");
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== Ticket Analyse abgeschlossen ===");
        Console.ResetColor();

        return errors.Count > 0
            ? StepResult.Failed("All Steps", string.Join("; ", errors))
            : StepResult.Ok("All Steps");
    }
}
