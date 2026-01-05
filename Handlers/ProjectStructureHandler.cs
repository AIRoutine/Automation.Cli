using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 4: Projektstruktur/CSProj Analyse
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class ProjectStructureHandler(IProcessRunner processRunner) : IRequestHandler<ProjectStructureRequest, StepResult>
{
    public async Task<StepResult> Handle(ProjectStructureRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 4: Projektstruktur/CSProj Analyse ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetProjectStructurePrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed("Project Structure Analysis", result.Error);
        }

        return StepResult.Ok("Project Structure Analysis");
    }
}
