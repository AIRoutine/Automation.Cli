using Automation.Cli.Contracts.Pipeline;
using Automation.Cli.Infrastructure;
using Automation.Cli.Steps;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Configuration;

/// <summary>
/// DI-Setup fuer die CLI-Services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCliServices(this IServiceCollection services)
    {
        // Auto-registriert alle [Service] Attribute
        // Inkludiert: ProcessRunner, StepRegistry, PipelineExecutor, ClassifyTicketHandler
        services.AddShinyServiceRegistry();

        // Mediator registrieren
        services.AddShinyMediator();

        // Steps explizit registrieren (da mehrere IExecutableStep Implementierungen)
        services.AddScoped<IExecutableStep, DataAnalysisStep>();
        services.AddScoped<IExecutableStep, ApiAnalysisStep>();
        services.AddScoped<IExecutableStep, FrontendAnalysisStep>();
        services.AddScoped<IExecutableStep, ProjectStructureStep>();
        services.AddScoped<IExecutableStep, SkillMappingStep>();
        services.AddScoped<IExecutableStep, ImplementStep>();
        services.AddScoped<IExecutableStep, FastImplementStep>();

        return services;
    }

    /// <summary>
    /// Initialisiert die Step-Registry mit allen registrierten Steps.
    /// Muss nach dem Build des Hosts aufgerufen werden.
    /// </summary>
    public static void InitializeStepRegistry(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IStepRegistry>();
        var steps = serviceProvider.GetServices<IExecutableStep>().ToList();

        foreach (var step in steps)
        {
            registry.Register(step);
        }

        // Debug-Output
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[Registry] {steps.Count} Steps registriert: {string.Join(", ", steps.Select(s => s.StepId))}");
        Console.ResetColor();
    }
}
