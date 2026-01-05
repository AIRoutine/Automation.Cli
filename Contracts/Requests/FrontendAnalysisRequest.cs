using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Step 3: Frontend/Uno Analyse
/// </summary>
public record FrontendAnalysisRequest(StepContext Context) : IRequest<StepResult>;
