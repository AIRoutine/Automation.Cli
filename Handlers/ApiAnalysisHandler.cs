using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 2: RestService/API Analyse
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class ApiAnalysisHandler(IProcessRunner processRunner) : IRequestHandler<ApiAnalysisRequest, StepResult>
{
    public async Task<StepResult> Handle(ApiAnalysisRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 2: RestService/API Analyse ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetApiAnalysisPrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed("API Analysis", result.Error);
        }

        return StepResult.Ok("API Analysis");
    }
}
