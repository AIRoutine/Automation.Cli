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

        // PHASE 2: Visuelle Validierung (deaktiviert - uno-app MCP-Integration noch nicht stabil)
        // Die uno.devserver MCP-Anbindung funktioniert aktuell nicht zuverlaessig.
        // Die App muss manuell gestartet und die Aenderungen visuell geprueft werden.
        var scope = context.Classification?.Scope ?? LayerScope.All;
        if (scope.HasFlag(LayerScope.Frontend))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== HINWEIS: Manuelle Validierung erforderlich ===");
            Console.WriteLine("  Die automatische visuelle Validierung ist deaktiviert.");
            Console.WriteLine("  Bitte starte die App manuell um die Aenderungen zu pruefen:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    cd src/uno");
            Console.WriteLine("    dotnet run -f net10.0-desktop --project src/HufnaglCV.App/HufnaglCV.App.csproj");
            Console.ResetColor();
        }

        return StepResult.Ok(DisplayName, context.Tasks);
    }
}
