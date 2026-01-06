using Automation.Cli.Contracts.Classification;
using Shiny.Mediator;

namespace Automation.Cli.Contracts.Requests;

/// <summary>
/// Request zur Klassifizierung eines Tickets.
/// </summary>
public record ClassifyTicketRequest(StepContext Context) : IRequest<TicketClassification>;
