using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer Frontend/Uno-Analyse und Implementierung.
/// </summary>
public class FrontendAnalysisStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "frontend-analysis";
    public override string DisplayName => "Frontend/Uno Analyse";
    public override LayerScope AffectedLayers => LayerScope.Frontend;
    public override IReadOnlyList<string> Dependencies => ["api-analysis"];

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        var prompt = PromptTemplates.GetFrontendAnalysisPrompt(context);
        return await RunClaudePromptAsync(prompt, DisplayName, ct);
    }
}
