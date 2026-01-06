using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Fast-Mode Step: Fuehrt die gesamte Implementierung in einem einzigen Claude-Aufruf durch.
/// Dies ist viel schneller als die separate Ausfuehrung mehrerer Analysis-Steps.
/// Bei Frontend-Aenderungen wird anschliessend ein separater Validierungs-Aufruf gemacht.
/// </summary>
public class FastImplementStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "fast-implement";
    public override string DisplayName => "Schnelle Implementierung";
    public override LayerScope AffectedLayers => LayerScope.All;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        if (context.Tasks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Keine Tasks zum Implementieren gefunden.");
            Console.ResetColor();
            return StepResult.Ok(DisplayName);
        }

        Console.WriteLine($"  Implementiere {context.Tasks.Count} Task(s) in einem Durchgang...");
        Console.WriteLine();

        // Zeige die Tasks an
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (var i = 0; i < context.Tasks.Count; i++)
        {
            Console.WriteLine($"    {i + 1}. {context.Tasks[i]}");
        }
        Console.ResetColor();
        Console.WriteLine();

        // PHASE 1: Implementierung mit Uno Docs MCP
        var prompt = PromptTemplates.GetFastImplementPrompt(context);
        var result = await ProcessRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Fehler bei Implementierung: {result.Error}");
            Console.ResetColor();
            return StepResult.Failed(DisplayName, result.Error);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  Implementierung abgeschlossen!");
        Console.ResetColor();

        // PHASE 2: Visuelle Validierung bei Frontend-Aenderungen
        var scope = context.Classification?.Scope ?? LayerScope.All;
        if (scope.HasFlag(LayerScope.Frontend))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== SCHRITT 3: Visuelle Validierung ===");
            Console.ResetColor();
            Console.WriteLine("  Starte App und erstelle Screenshot...");

            // Zuerst App im Hintergrund starten
            var startAppResult = await ProcessRunner.RunBashAsync(
                "Start-Process powershell -ArgumentList \"-NoExit\", \"-File\", \".\\run-uno-net10-desktop.ps1\"",
                ct: ct);

            if (!startAppResult.Success)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warnung: App konnte nicht gestartet werden: {startAppResult.Error}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("  App wird gestartet, warte 25 Sekunden...");

                // Warten bis App gestartet
                await Task.Delay(25000, ct);

                // Validierung mit separatem Claude-Aufruf (nur uno-app MCP)
                var validationPrompt = PromptTemplates.GetVisualValidationPrompt(context);
                var validationResult = await ProcessRunner.RunClaudeAsync(validationPrompt, ct: ct);

                if (!validationResult.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Warnung bei Validierung: {validationResult.Error}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  Visuelle Validierung abgeschlossen!");
                    Console.ResetColor();

                    // Output der Validierung anzeigen (gekuerzt)
                    if (!string.IsNullOrWhiteSpace(validationResult.Output))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        var lines = validationResult.Output.Split('\n').Take(10);
                        foreach (var line in lines)
                        {
                            Console.WriteLine($"    {line}");
                        }
                        if (validationResult.Output.Split('\n').Length > 10)
                        {
                            Console.WriteLine("    ...");
                        }
                        Console.ResetColor();
                    }
                }
            }
        }

        return StepResult.Ok(DisplayName, context.Tasks);
    }
}
