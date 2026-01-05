using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 5: Skills Zuordnung
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class SkillMappingHandler(IProcessRunner processRunner) : IRequestHandler<SkillMappingRequest, StepResult>
{
    public async Task<StepResult> Handle(SkillMappingRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 5: Skills Zuordnung ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetSkillMappingPrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fehler: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed("Skill Mapping", result.Error);
        }

        return StepResult.Ok("Skill Mapping");
    }
}
