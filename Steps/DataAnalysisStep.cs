using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer Data/Entity-Analyse und Implementierung.
/// </summary>
public class DataAnalysisStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "data-analysis";
    public override string DisplayName => "Data/Entities Analyse";
    public override LayerScope AffectedLayers => LayerScope.Data;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        var prompt = PromptTemplates.GetDataAnalysisPrompt(context);
        return await RunClaudePromptAsync(prompt, DisplayName, ct);
    }
}
