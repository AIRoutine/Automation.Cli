using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Step 5: Skills Zuordnung
/// </summary>
public record SkillMappingRequest(StepContext Context) : IRequest<StepResult>;
