using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Step 1: Data/Entities Analyse
/// </summary>
public record DataAnalysisRequest(StepContext Context) : IRequest<StepResult>;
