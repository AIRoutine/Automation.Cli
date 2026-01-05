using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 7: Tasks implementieren
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public class ImplementTasksHandler(IProcessRunner processRunner) : IRequestHandler<ImplementTasksRequest, StepResult>
{
    public async Task<StepResult> Handle(ImplementTasksRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 7: Tasks implementieren ===");
        Console.ResetColor();

        var stepContext = request.Context;
        var tasks = request.Tasks;

        if (tasks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Keine Tasks gefunden - ueberspringe Implementierung");
            Console.ResetColor();
            return StepResult.Ok("Implement Tasks");
        }

        var taskNumber = 1;
        var errors = new List<string>();

        foreach (var task in tasks)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== Task {taskNumber}/{tasks.Count} implementieren ===");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Task: {task}");
            Console.ResetColor();

            var implementPrompt = PromptTemplates.GetImplementTaskPrompt(stepContext, task);
            var result = await processRunner.RunClaudeAsync(implementPrompt, ct: ct);

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fehler bei Task {taskNumber}");
                Console.ResetColor();
                errors.Add($"Task {taskNumber}: {result.Error}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Task {taskNumber} abgeschlossen");
                Console.ResetColor();

                // Nach Data/Entity Tasks Seeding erstellen
                if (PromptTemplates.IsDataTask(task))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("=== Seeding fuer Data Task erstellen ===");
                    Console.ResetColor();

                    var seedingPrompt = PromptTemplates.GetSeedingPrompt(stepContext);
                    var seedingResult = await processRunner.RunClaudeAsync(seedingPrompt, ct: ct);

                    if (!seedingResult.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Fehler bei Seeding");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Seeding erstellt");
                        Console.ResetColor();
                    }
                }
            }

            taskNumber++;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== Alle Tasks implementiert ===");
        Console.ResetColor();

        return errors.Count > 0
            ? StepResult.Failed("Implement Tasks", string.Join("; ", errors))
            : StepResult.Ok("Implement Tasks");
    }
}
