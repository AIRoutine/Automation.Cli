using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 3: Frontend/Uno Analyse
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class FrontendAnalysisHandler(IProcessRunner processRunner) : IRequestHandler<FrontendAnalysisRequest, StepResult>
{
    public async Task<StepResult> Handle(FrontendAnalysisRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 3: Frontend/Uno Analyse ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetFrontendAnalysisPrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed("Frontend Analysis", result.Error);
        }

        return StepResult.Ok("Frontend Analysis");
    }
}
