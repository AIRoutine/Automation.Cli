using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Orchestrierungs-Request: Alle Steps ausfuehren
/// </summary>
public record RunAllStepsRequest(StepContext Context) : IRequest<StepResult>;
