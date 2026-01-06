using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer API/Endpoint-Analyse und Implementierung.
/// </summary>
public class ApiAnalysisStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "api-analysis";
    public override string DisplayName => "API/Endpoints Analyse";
    public override LayerScope AffectedLayers => LayerScope.Api;
    public override IReadOnlyList<string> Dependencies => ["data-analysis"];

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        var prompt = PromptTemplates.GetApiAnalysisPrompt(context);
        return await RunClaudePromptAsync(prompt, DisplayName, ct);
    }
}
