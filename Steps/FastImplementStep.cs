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

        // PHASE 2: Visuelle Validierung fuer Frontend-Aenderungen
        var scope = context.Classification?.Scope ?? LayerScope.All;
        if (scope.HasFlag(LayerScope.Frontend))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== VISUELLE VALIDIERUNG ===");
            Console.ResetColor();

            // App starten (im Hintergrund mit Hot Reload)
            // Verwendet das run-uno-net10-desktop.ps1 Script das in jedem Projekt vorhanden sein muss
            Console.WriteLine("  Starte App im Hintergrund...");
            var startAppResult = await ProcessRunner.RunBashAsync(
                @"& '.\run-uno-net10-desktop.ps1' -Background",
                ct: ct);

            if (!startAppResult.Success)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warnung: App konnte nicht gestartet werden: {startAppResult.Error}");
                Console.WriteLine("  Bitte App manuell starten und pruefen.");
                Console.ResetColor();
            }
            else
            {
                // Polling: Warte auf App-Start mit mehreren Versuchen
                var appReady = await WaitForAppWithPollingAsync(ct);

                if (!appReady)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  App nicht bereit nach mehreren Versuchen. Ueberspringe visuelle Validierung.");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("  Validiere Aenderungen visuell...");
                    Console.WriteLine();

                    var validationPrompt = PromptTemplates.GetVisualValidationPrompt(context);
                    var validationResult = await ProcessRunner.RunClaudeAsync(validationPrompt, ct: ct);

                    if (!validationResult.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Warnung bei Validierung: {validationResult.Error}");
                        Console.WriteLine("  Die Implementierung wurde trotzdem abgeschlossen.");
                        Console.ResetColor();
                    }
                    else
                    {
                        // Parse JSON-Antwort und zeige Ergebnis
                        var validationOutput = ParseValidationResult(validationResult.Output);
                        DisplayValidationResult(validationOutput);
                    }
                }
            }
        }

        return StepResult.Ok(DisplayName, context.Tasks);
    }

    /// <summary>
    /// Wartet mit Polling auf den App-Start statt einer festen Wartezeit.
    /// </summary>
    private async Task<bool> WaitForAppWithPollingAsync(CancellationToken ct)
    {
        const int maxAttempts = 6;
        const int delayMs = 3000;

        for (var i = 0; i < maxAttempts; i++)
        {
            Console.WriteLine($"  Pruefe App-Status (Versuch {i + 1}/{maxAttempts})...");

            // Kurzer Claude-Aufruf nur fuer Status-Check
            var checkPrompt = """
                Pruefe ob die Uno App laeuft:
                1. Rufe uno_app_get_runtime_info auf
                2. Antworte NUR mit einem Wort: "RUNNING" wenn die App verbunden ist, "WAITING" wenn nicht
                """;

            var result = await ProcessRunner.RunClaudeAsync(checkPrompt, ct: ct);

            if (result.Success && result.Output?.Contains("RUNNING", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  App ist bereit!");
                Console.ResetColor();
                return true;
            }

            if (i < maxAttempts - 1)
            {
                await Task.Delay(delayMs, ct);
            }
        }

        return false;
    }

    /// <summary>
    /// Datenstruktur fuer das Validierungsergebnis.
    /// </summary>
    private record ValidationResult(
        string Status,
        bool AppRunning,
        bool ScreenshotTaken,
        bool? ChangesVisible,
        string[] Issues,
        string Summary);

    /// <summary>
    /// Parst die JSON-Antwort vom Validierungsprompt.
    /// </summary>
    private static ValidationResult ParseValidationResult(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new ValidationResult("failed", false, false, null, ["Keine Ausgabe erhalten"], "Validierung fehlgeschlagen");
        }

        try
        {
            // Extrahiere JSON aus der Antwort (zwischen ``` oder direkt)
            var json = output;
            var jsonStart = output.IndexOf('{');
            var jsonEnd = output.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                json = output[jsonStart..(jsonEnd + 1)];
            }

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ValidationResult(
                Status: root.TryGetProperty("status", out var s) ? s.GetString() ?? "unknown" : "unknown",
                AppRunning: root.TryGetProperty("appRunning", out var ar) && ar.GetBoolean(),
                ScreenshotTaken: root.TryGetProperty("screenshotTaken", out var st) && st.GetBoolean(),
                ChangesVisible: root.TryGetProperty("changesVisible", out var cv) && cv.ValueKind != System.Text.Json.JsonValueKind.Null ? cv.GetBoolean() : null,
                Issues: root.TryGetProperty("issues", out var issues) ? issues.EnumerateArray().Select(i => i.GetString() ?? "").ToArray() : [],
                Summary: root.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "" : ""
            );
        }
        catch
        {
            return new ValidationResult("failed", false, false, null, ["JSON-Parsing fehlgeschlagen"], output);
        }
    }

    /// <summary>
    /// Zeigt das Validierungsergebnis formatiert an.
    /// </summary>
    private static void DisplayValidationResult(ValidationResult result)
    {
        Console.WriteLine();

        var statusColor = result.Status switch
        {
            "success" => ConsoleColor.Green,
            "skipped" => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };

        Console.ForegroundColor = statusColor;
        Console.WriteLine($"  Status: {result.Status.ToUpperInvariant()}");
        Console.ResetColor();

        Console.WriteLine($"  App verbunden: {(result.AppRunning ? "Ja" : "Nein")}");
        Console.WriteLine($"  Screenshot: {(result.ScreenshotTaken ? "Ja" : "Nein")}");

        if (result.ChangesVisible.HasValue)
        {
            var changeColor = result.ChangesVisible.Value ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.ForegroundColor = changeColor;
            Console.WriteLine($"  Aenderungen sichtbar: {(result.ChangesVisible.Value ? "Ja" : "Nein")}");
            Console.ResetColor();
        }

        if (result.Issues.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Probleme:");
            foreach (var issue in result.Issues)
            {
                Console.WriteLine($"    - {issue}");
            }
            Console.ResetColor();
        }

        Console.WriteLine($"  Zusammenfassung: {result.Summary}");

        if (result.Status == "success")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Visuelle Validierung abgeschlossen!");
            Console.ResetColor();
        }
    }
}
