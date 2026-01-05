using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Step 2: RestService/API Analyse
/// </summary>
public record ApiAnalysisRequest(StepContext Context) : IRequest<StepResult>;
