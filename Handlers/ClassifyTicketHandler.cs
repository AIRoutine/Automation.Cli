using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Classification;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer die Ticket-Klassifizierung.
/// Analysiert das Ticket und erstellt einen Ausfuehrungsplan.
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public partial class ClassifyTicketHandler(IProcessRunner processRunner) : IRequestHandler<ClassifyTicketRequest, TicketClassification>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<TicketClassification> Handle(ClassifyTicketRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("=== TICKET KLASSIFIZIERUNG ===");
        Console.ResetColor();

        var prompt = PromptTemplates.GetClassifierPrompt(request.Context);
        var result = await processRunner.RunClaudeAsync(prompt, ct: ct);

        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Klassifizierung fehlgeschlagen: {result.Error}");
            Console.WriteLine("Verwende Fallback (alle Steps)...");
            Console.ResetColor();
            return TicketClassification.CreateFallback("Fallback: Klassifizierung fehlgeschlagen");
        }

        try
        {
            var classification = ParseClassification(result.Output);

            // In Context speichern
            request.Context.Classification = classification;

            // Tasks in Context uebernehmen
            if (classification.Tasks.Count > 0)
            {
                request.Context.Tasks.Clear();
                request.Context.Tasks.AddRange(classification.Tasks);
            }

            PrintClassification(classification);
            return classification;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"JSON-Parsing fehlgeschlagen: {ex.Message}");
            Console.WriteLine("Verwende Fallback (alle Steps)...");
            Console.ResetColor();
            return TicketClassification.CreateFallback($"Fallback: {ex.Message}");
        }
    }

    private static TicketClassification ParseClassification(string output)
    {
        // JSON aus der Ausgabe extrahieren (kann in ```json ... ``` eingebettet sein)
        var jsonMatch = JsonBlockRegex().Match(output);
        var json = jsonMatch.Success ? jsonMatch.Groups[1].Value : output;

        // Versuche alternatives Pattern ohne Backticks
        if (!jsonMatch.Success)
        {
            var startIndex = output.IndexOf('{');
            var endIndex = output.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                json = output[startIndex..(endIndex + 1)];
            }
        }

        var dto = JsonSerializer.Deserialize<ClassificationDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Leeres JSON-Ergebnis");

        return new TicketClassification
        {
            Type = ParseTicketType(dto.Type),
            Scope = ParseLayerScope(dto.Scope),
            Complexity = ParseComplexity(dto.Complexity),
            Steps = dto.Steps.Select(s => new ExecutionStep
            {
                StepId = s.StepId,
                Order = s.Order,
                IsRequired = s.Required,
                Reason = s.Reason
            }).ToList(),
            Tasks = dto.Tasks ?? [],
            Summary = dto.Summary ?? "Keine Zusammenfassung"
        };
    }

    private static TicketType ParseTicketType(string? type) => type?.ToLowerInvariant() switch
    {
        "newfeature" or "new_feature" or "feature" => TicketType.NewFeature,
        "enhancement" => TicketType.Enhancement,
        "bugfix" or "bug_fix" or "bug" or "fix" => TicketType.BugFix,
        "refactoring" or "refactor" => TicketType.Refactoring,
        "documentation" or "docs" => TicketType.Documentation,
        "configuration" or "config" => TicketType.Configuration,
        "datamigration" or "data_migration" or "migration" => TicketType.DataMigration,
        _ => TicketType.NewFeature
    };

    private static LayerScope ParseLayerScope(List<string>? scopes)
    {
        if (scopes is null || scopes.Count == 0)
            return LayerScope.All;

        var result = LayerScope.None;
        foreach (var scope in scopes)
        {
            result |= scope.ToLowerInvariant() switch
            {
                "data" => LayerScope.Data,
                "api" => LayerScope.Api,
                "frontend" or "ui" => LayerScope.Frontend,
                "shared" or "contracts" => LayerScope.Shared,
                "infrastructure" or "infra" => LayerScope.Infrastructure,
                _ => LayerScope.None
            };
        }
        return result == LayerScope.None ? LayerScope.All : result;
    }

    private static Complexity ParseComplexity(string? complexity) => complexity?.ToLowerInvariant() switch
    {
        "trivial" => Complexity.Trivial,
        "simple" => Complexity.Simple,
        "medium" => Complexity.Medium,
        "complex" => Complexity.Complex,
        "epic" => Complexity.Epic,
        _ => Complexity.Medium
    };

    private static void PrintClassification(TicketClassification classification)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Klassifizierung erfolgreich!");
        Console.ResetColor();

        Console.WriteLine($"  Typ:         {classification.Type}");
        Console.WriteLine($"  Scope:       {classification.Scope}");
        Console.WriteLine($"  Komplexitaet: {classification.Complexity}");
        Console.WriteLine($"  Summary:     {classification.Summary}");
        Console.WriteLine();
        Console.WriteLine("  Geplante Steps:");
        foreach (var step in classification.Steps.OrderBy(s => s.Order))
        {
            Console.WriteLine($"    {step.Order}. {step.StepId}{(step.IsRequired ? "" : " (optional)")}");
            if (!string.IsNullOrEmpty(step.Reason))
                Console.WriteLine($"       -> {step.Reason}");
        }

        if (classification.Tasks.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Extrahierte Tasks:");
            foreach (var task in classification.Tasks)
            {
                Console.WriteLine($"    - {task}");
            }
        }
        Console.WriteLine();
    }

    [GeneratedRegex(@"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonBlockRegex();

    // DTO fuer JSON-Parsing
    private sealed class ClassificationDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("scope")]
        public List<string>? Scope { get; set; }

        [JsonPropertyName("complexity")]
        public string? Complexity { get; set; }

        [JsonPropertyName("steps")]
        public List<StepDto> Steps { get; set; } = [];

        [JsonPropertyName("tasks")]
        public List<string>? Tasks { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
    }

    private sealed class StepDto
    {
        [JsonPropertyName("stepId")]
        public string StepId { get; set; } = "";

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
