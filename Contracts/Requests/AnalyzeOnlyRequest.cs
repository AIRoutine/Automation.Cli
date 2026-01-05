using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Nur Analyse (Steps 1-5), ohne Implementierung
/// </summary>
public record AnalyzeOnlyRequest(StepContext Context) : IRequest<StepResult>;
