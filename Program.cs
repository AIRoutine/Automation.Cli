using System.CommandLine;
using Automation.Cli.Configuration;
using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shiny.Mediator;

// Host mit DI erstellen
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCliServices();

var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

// Root Command
var rootCommand = new RootCommand("Ticket-Analyse CLI Tool");

// Ticket Argument (wiederverwendbar)
var ticketArg = new Argument<string>("ticket", "Ticket URL oder Beschreibung");

// === run Command ===
var runCommand = new Command("run", "Alle Steps ausfuehren (Analyse + Implementierung)");
runCommand.AddArgument(ticketArg);
runCommand.SetHandler(async (ticket) =>
{
    var context = new StepContext(ticket);
    var (_, result) = await mediator.Request(new RunAllStepsRequest(context));
    Environment.ExitCode = result.Success ? 0 : 1;
}, ticketArg);

// === analyze Command ===
var analyzeCommand = new Command("analyze", "Nur Analyse ausfuehren (Steps 1-5, ohne Implementierung)");
analyzeCommand.AddArgument(ticketArg);
analyzeCommand.SetHandler(async (ticket) =>
{
    var context = new StepContext(ticket);
    var (_, result) = await mediator.Request(new AnalyzeOnlyRequest(context));
    Environment.ExitCode = result.Success ? 0 : 1;
}, ticketArg);

// === step Command ===
var stepCommand = new Command("step", "Einzelnen Step ausfuehren");
var stepNameArg = new Argument<string>("name", "Step-Name: data, api, frontend, structure, skills, load-tasks, implement");
stepCommand.AddArgument(stepNameArg);
stepCommand.AddArgument(ticketArg);
stepCommand.SetHandler(async (stepName, ticket) =>
{
    var context = new StepContext(ticket);

    var (_, result) = stepName.ToLowerInvariant() switch
    {
        "data" => await mediator.Request(new DataAnalysisRequest(context)),
        "api" => await mediator.Request(new ApiAnalysisRequest(context)),
        "frontend" => await mediator.Request(new FrontendAnalysisRequest(context)),
        "structure" => await mediator.Request(new ProjectStructureRequest(context)),
        "skills" => await mediator.Request(new SkillMappingRequest(context)),
        "load-tasks" => await mediator.Request(new LoadTasksRequest(context)),
        "implement" => await mediator.Request(new ImplementTasksRequest(context, context.Tasks)),
        _ => throw new ArgumentException($"Unbekannter Step: {stepName}. Verfuegbar: data, api, frontend, structure, skills, load-tasks, implement")
    };

    Environment.ExitCode = result.Success ? 0 : 1;
}, stepNameArg, ticketArg);

// === list-steps Command ===
var listStepsCommand = new Command("list-steps", "Liste aller verfuegbaren Steps");
listStepsCommand.SetHandler(() =>
{
    Console.WriteLine("Verfuegbare Steps:");
    Console.WriteLine();
    Console.WriteLine("  data        - Data/Entities Analyse");
    Console.WriteLine("  api         - RestService/API Analyse");
    Console.WriteLine("  frontend    - Frontend/Uno Analyse");
    Console.WriteLine("  structure   - Projektstruktur/CSProj Analyse");
    Console.WriteLine("  skills      - Skills Zuordnung");
    Console.WriteLine("  load-tasks  - Tasks vom Ticket laden");
    Console.WriteLine("  implement   - Tasks implementieren");
    Console.WriteLine();
    Console.WriteLine("Verwendung:");
    Console.WriteLine("  analyze-ticket step <name> <ticket>");
});

// Commands registrieren
rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(analyzeCommand);
rootCommand.AddCommand(stepCommand);
rootCommand.AddCommand(listStepsCommand);

// CLI ausfuehren
return await rootCommand.InvokeAsync(args);
