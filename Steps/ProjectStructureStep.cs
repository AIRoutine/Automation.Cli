using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer Projektstruktur-Analyse (neue csproj, etc.).
/// </summary>
public class ProjectStructureStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "project-structure";
    public override string DisplayName => "Projektstruktur Analyse";
    public override LayerScope AffectedLayers => LayerScope.Infrastructure;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        var prompt = PromptTemplates.GetProjectStructurePrompt(context);
        return await RunClaudePromptAsync(prompt, DisplayName, ct);
    }
}
