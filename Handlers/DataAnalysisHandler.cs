using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 1: Data/Entities Analyse
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class DataAnalysisHandler(IProcessRunner processRunner) : IRequestHandler<DataAnalysisRequest, StepResult>
{
    public async Task<StepResult> Handle(DataAnalysisRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 1: Data/Entities Analyse ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetDataAnalysisPrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed("Data Analysis", result.Error);
        }

        return StepResult.Ok("Data Analysis");
    }
}
