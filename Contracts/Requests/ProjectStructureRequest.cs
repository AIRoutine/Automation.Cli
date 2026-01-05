using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Step 4: Projektstruktur/CSProj Analyse
/// </summary>
public record ProjectStructureRequest(StepContext Context) : IRequest<StepResult>;
