using Automation.Cli.Contracts.Pipeline;
using Shiny.Extensions.DependencyInjection;

namespace Automation.Cli.Infrastructure;

/// <summary>
/// Registry fuer alle Pipeline-Steps.
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd, Type = typeof(IStepRegistry))]
public class StepRegistry : IStepRegistry
{
    private readonly Dictionary<string, IExecutableStep> _steps = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IExecutableStep step)
    {
        _steps[step.StepId] = step;
    }

    public IExecutableStep? GetStep(string stepId)
    {
        return _steps.TryGetValue(stepId, out var step) ? step : null;
    }

    public IEnumerable<IExecutableStep> GetAllSteps()
    {
        return _steps.Values;
    }

    public IEnumerable<string> GetAllStepIds()
    {
        return _steps.Keys;
    }

    public IReadOnlyList<IExecutableStep> BuildExecutionOrder(IEnumerable<string> requiredStepIds)
    {
        var stepIds = requiredStepIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<IExecutableStep>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stepId in stepIds)
        {
            if (!visited.Contains(stepId))
            {
                TopologicalSort(stepId, stepIds, visited, inProgress, result);
            }
        }

        return result;
    }

    private void TopologicalSort(
        string stepId,
        HashSet<string> requiredStepIds,
        HashSet<string> visited,
        HashSet<string> inProgress,
        List<IExecutableStep> result)
    {
        if (inProgress.Contains(stepId))
        {
            throw new InvalidOperationException($"Zyklische Abhaengigkeit bei Step: {stepId}");
        }

        if (visited.Contains(stepId))
            return;

        var step = GetStep(stepId);
        if (step is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] Step '{stepId}' nicht gefunden, wird uebersprungen.");
            Console.ResetColor();
            visited.Add(stepId);
            return;
        }

        inProgress.Add(stepId);

        // Zuerst alle Dependencies verarbeiten
        foreach (var dependency in step.Dependencies)
        {
            if (requiredStepIds.Contains(dependency) || _steps.ContainsKey(dependency))
            {
                TopologicalSort(dependency, requiredStepIds, visited, inProgress, result);
            }
        }

        inProgress.Remove(stepId);
        visited.Add(stepId);
        result.Add(step);
    }

    /// <summary>
    /// Gibt eine formatierte Liste aller Steps aus.
    /// </summary>
    public void PrintAvailableSteps()
    {
        Console.WriteLine("Verfuegbare Steps:");
        Console.WriteLine();
        foreach (var step in _steps.Values.OrderBy(s => s.StepId))
        {
            Console.WriteLine($"  {step.StepId,-20} - {step.DisplayName}");
            if (step.Dependencies.Count > 0)
            {
                Console.WriteLine($"    Dependencies: {string.Join(", ", step.Dependencies)}");
            }
        }
    }
}
