using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler: Nur Analyse (Steps 1-5), ohne Implementierung
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class AnalyzeOnlyHandler(IMediator mediator) : IRequestHandler<AnalyzeOnlyRequest, StepResult>
{
    public async Task<StepResult> Handle(AnalyzeOnlyRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Ticket Analyse (nur Analyse) gestartet ===");
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

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== Ticket Analyse abgeschlossen ===");
        Console.ResetColor();

        return errors.Count > 0
            ? StepResult.Failed("Analyze Only", string.Join("; ", errors))
            : StepResult.Ok("Analyze Only");
    }
}
