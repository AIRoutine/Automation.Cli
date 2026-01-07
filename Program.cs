using System.CommandLine;
using Automation.Cli.Configuration;
using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator;

// Host mit DI erstellen
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCliServices();

var host = builder.Build();

// Step-Registry initialisieren
host.Services.InitializeStepRegistry();

var mediator = host.Services.GetRequiredService<IMediator>();
var registry = host.Services.GetRequiredService<IStepRegistry>();
var executor = host.Services.GetRequiredService<IPipelineExecutor>();

// Root Command
var rootCommand = new RootCommand("Intelligentes Ticket-Analyse CLI Tool");

// Ticket Argument (wiederverwendbar)
var ticketArg = new Argument<string>("ticket", "Ticket URL oder Beschreibung");
var dryRunOption = new Option<bool>("--dry-run", "Zeigt den Plan ohne Ausfuehrung");

// === smart Command (neu, Hauptbefehl) ===
var smartCommand = new Command("smart", "Intelligente Ticket-Verarbeitung: Klassifiziert und fuehrt nur relevante Steps aus");
smartCommand.AddArgument(ticketArg);
smartCommand.AddOption(dryRunOption);
smartCommand.SetHandler(async (ticket, dryRun) =>
{
    var context = new StepContext(ticket);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("===============================================");
    Console.WriteLine("   SMART TICKET PROCESSING");
    Console.WriteLine("===============================================");
    Console.ResetColor();
    Console.WriteLine($"Ticket: {ticket}");
    Console.WriteLine();

    // Step 1: Klassifizierung
    var (_, classification) = await mediator.Request(new ClassifyTicketRequest(context));

    if (dryRun)
    {
        // Nur Plan anzeigen
        executor.DryRun(classification);
        Environment.ExitCode = 0;
        return;
    }

    // Step 2: Pipeline ausfuehren
    var result = await executor.ExecuteAsync(classification, context);
    Environment.ExitCode = result.Success ? 0 : 1;

    // Zusammenfassung
    PrintSummary(result);

}, ticketArg, dryRunOption);

// === classify Command (nur Klassifizierung) ===
var classifyCommand = new Command("classify", "Klassifiziert ein Ticket ohne Ausfuehrung");
classifyCommand.AddArgument(ticketArg);
classifyCommand.SetHandler(async (ticket) =>
{
    var context = new StepContext(ticket);
    var (_, classification) = await mediator.Request(new ClassifyTicketRequest(context));

    // Dry-Run zeigt den Plan
    executor.DryRun(classification);
    Environment.ExitCode = 0;

}, ticketArg);

// === fast Command (schnelle Implementierung ohne Klassifizierung) ===
var fastCommand = new Command("fast", "Schnelle Implementierung: Fuehrt alles in einem Claude-Aufruf aus, dann visuelle Validierung");
var fastTicketArg = new Argument<string>("ticket", "Ticket URL oder Beschreibung");
fastCommand.AddArgument(fastTicketArg);
fastCommand.SetHandler(async (ticket) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("===============================================");
    Console.WriteLine("   FAST IMPLEMENTATION MODE");
    Console.WriteLine("===============================================");
    Console.ResetColor();
    Console.WriteLine($"Ticket: {ticket}");
    Console.WriteLine();

    // Erstelle Context mit Frontend-Scope (da Fast-Mode hauptsaechlich fuer Frontend)
    var context = new StepContext(ticket);
    context.Tasks.Add(ticket);
    context.Classification = new TicketClassification
    {
        Type = TicketType.Enhancement,
        Scope = LayerScope.Frontend,
        Complexity = Complexity.Simple,
        Summary = ticket,
        Tasks = [ticket],
        Steps = []
    };

    // FastImplementStep direkt ausfuehren
    var fastStep = registry.GetStep("fast-implement");
    if (fastStep is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("FastImplementStep nicht gefunden!");
        Console.ResetColor();
        Environment.ExitCode = 1;
        return;
    }

    var result = await fastStep.ExecuteAsync(context);
    Environment.ExitCode = result.Success ? 0 : 1;

    Console.WriteLine();
    if (result.Success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("   FAST IMPLEMENTATION ERFOLGREICH");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"   FAST IMPLEMENTATION FEHLGESCHLAGEN: {result.Error}");
    }
    Console.ResetColor();
    Console.WriteLine("===============================================");

}, fastTicketArg);

// === validate Command (nur visuelle Validierung) ===
var validateCommand = new Command("validate", "Startet die App und validiert visuell (ohne Implementierung)");
var validateDescArg = new Argument<string>("description", () => "Pruefe ob die App korrekt aussieht", "Was geprueft werden soll");
validateCommand.AddArgument(validateDescArg);
validateCommand.SetHandler(async (description) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("===============================================");
    Console.WriteLine("   VISUAL VALIDATION MODE");
    Console.WriteLine("===============================================");
    Console.ResetColor();
    Console.WriteLine($"Pruefe: {description}");
    Console.WriteLine();

    var context = new StepContext(description);

    // VisualValidateStep direkt ausfuehren
    var validateStep = registry.GetStep("visual-validate");
    if (validateStep is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("VisualValidateStep nicht gefunden!");
        Console.ResetColor();
        Environment.ExitCode = 1;
        return;
    }

    var result = await validateStep.ExecuteAsync(context);
    Environment.ExitCode = result.Success ? 0 : 1;

    Console.WriteLine();
    if (result.Success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("   VALIDIERUNG ERFOLGREICH");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"   VALIDIERUNG FEHLGESCHLAGEN: {result.Error}");
    }
    Console.ResetColor();
    Console.WriteLine("===============================================");

}, validateDescArg);

// === list-steps Command ===
var listStepsCommand = new Command("list-steps", "Liste aller verfuegbaren Steps");
listStepsCommand.SetHandler(() =>
{
    if (registry is StepRegistry stepRegistry)
    {
        stepRegistry.PrintAvailableSteps();
    }
    else
    {
        foreach (var stepId in registry.GetAllStepIds())
        {
            var step = registry.GetStep(stepId);
            Console.WriteLine($"  {stepId,-20} - {step?.DisplayName}");
        }
    }
});

// === step Command (einzelner Step) ===
var stepCommand = new Command("step", "Fuehrt einen einzelnen Step aus");
var stepNameArg = new Argument<string>("name", "Step-ID (siehe list-steps)");
stepCommand.AddArgument(stepNameArg);
stepCommand.AddArgument(ticketArg);
stepCommand.SetHandler(async (stepName, ticket) =>
{
    var step = registry.GetStep(stepName);
    if (step is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Step '{stepName}' nicht gefunden.");
        Console.WriteLine("Verfuegbare Steps:");
        foreach (var id in registry.GetAllStepIds())
        {
            Console.WriteLine($"  - {id}");
        }
        Console.ResetColor();
        Environment.ExitCode = 1;
        return;
    }

    var context = new StepContext(ticket);
    var result = await step.ExecuteAsync(context);
    Environment.ExitCode = result.Success ? 0 : 1;

}, stepNameArg, ticketArg);

// Commands registrieren
rootCommand.AddCommand(smartCommand);
rootCommand.AddCommand(fastCommand);
rootCommand.AddCommand(validateCommand);
rootCommand.AddCommand(classifyCommand);
rootCommand.AddCommand(stepCommand);
rootCommand.AddCommand(listStepsCommand);

// CLI ausfuehren
return await rootCommand.InvokeAsync(args);

// === Helper ===
static void PrintSummary(PipelineResult result)
{
    Console.WriteLine();
    Console.WriteLine("===============================================");

    if (result.Success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("   ERFOLGREICH ABGESCHLOSSEN");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("   FEHLGESCHLAGEN");
    }

    Console.ResetColor();
    Console.WriteLine("===============================================");
    Console.WriteLine();
    Console.WriteLine($"Ticket-Typ:     {result.Classification.Type}");
    Console.WriteLine($"Komplexitaet:   {result.Classification.Complexity}");
    Console.WriteLine($"Scope:          {result.Classification.Scope}");
    Console.WriteLine();
    Console.WriteLine($"Ausgefuehrte Steps: {result.StepResults.Count}");

    foreach (var stepResult in result.StepResults)
    {
        var icon = stepResult.Success ? "[OK]" : "[FAIL]";
        var color = stepResult.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = color;
        Console.Write($"  {icon} ");
        Console.ResetColor();
        Console.WriteLine(stepResult.StepName);
    }

    if (result.SkippedSteps.Count > 0)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Uebersprungen: {string.Join(", ", result.SkippedSteps)}");
        Console.ResetColor();
    }

    if (!string.IsNullOrEmpty(result.Error))
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Fehler: {result.Error}");
        Console.ResetColor();
    }

    Console.WriteLine();
}
