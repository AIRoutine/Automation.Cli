using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Step fuer Claude-Skill-Zuordnung.
/// </summary>
public class SkillMappingStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "skill-mapping";
    public override string DisplayName => "Skills Zuordnung";
    public override LayerScope AffectedLayers => LayerScope.All;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        var prompt = PromptTemplates.GetSkillMappingPrompt(context);
        return await RunClaudePromptAsync(prompt, DisplayName, ct);
    }
}
