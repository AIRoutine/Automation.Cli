using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;

namespace Automation.Cli.Steps;

/// <summary>
/// Standalone Step fuer visuelle Validierung - startet die App und macht einen Screenshot.
/// Kann unabhaengig von der Implementierung ausgefuehrt werden.
/// </summary>
public class VisualValidateStep(IProcessRunner processRunner) : BaseStep(processRunner)
{
    public override string StepId => "visual-validate";
    public override string DisplayName => "Visuelle Validierung";
    public override LayerScope AffectedLayers => LayerScope.Frontend;

    public override async Task<StepResult> ExecuteAsync(StepContext context, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== VISUELLE VALIDIERUNG ===");
        Console.ResetColor();

        // App starten
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
            return StepResult.Failed(DisplayName, "App konnte nicht gestartet werden");
        }

        // Polling: Warte auf App-Start
        var appReady = await WaitForAppWithPollingAsync(ct);

        if (!appReady)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  App nicht bereit nach mehreren Versuchen.");
            Console.ResetColor();
            return StepResult.Failed(DisplayName, "App nicht bereit");
        }

        // Visuelle Validierung durchfuehren
        Console.WriteLine("  Validiere visuell...");
        Console.WriteLine();

        var validationPrompt = PromptTemplates.GetStandaloneVisualValidationPrompt(context);
        var validationResult = await ProcessRunner.RunClaudeAsync(validationPrompt, ct: ct);

        if (!validationResult.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Fehler bei Validierung: {validationResult.Error}");
            Console.ResetColor();
            return StepResult.Failed(DisplayName, validationResult.Error);
        }

        // Parse JSON-Antwort und zeige Ergebnis
        var result = ParseValidationResult(validationResult.Output);
        DisplayValidationResult(result);

        return result.Status == "success"
            ? StepResult.Ok(DisplayName)
            : StepResult.Failed(DisplayName, result.Summary);
    }

    private async Task<bool> WaitForAppWithPollingAsync(CancellationToken ct)
    {
        const int maxAttempts = 6;
        const int delayMs = 3000;

        for (var i = 0; i < maxAttempts; i++)
        {
            Console.WriteLine($"  Pruefe App-Status (Versuch {i + 1}/{maxAttempts})...");

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

    private record ValidationResult(
        string Status,
        bool AppRunning,
        bool ScreenshotTaken,
        bool? ChangesVisible,
        string[] Issues,
        string Summary);

    private static ValidationResult ParseValidationResult(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new ValidationResult("failed", false, false, null, ["Keine Ausgabe erhalten"], "Validierung fehlgeschlagen");
        }

        try
        {
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
            Console.WriteLine($"  UI korrekt: {(result.ChangesVisible.Value ? "Ja" : "Nein")}");
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
    }
}
