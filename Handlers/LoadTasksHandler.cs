using System.Text.RegularExpressions;
using Automation.Cli.Contracts;
using Automation.Cli.Contracts.Requests;
using Automation.Cli.Infrastructure;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Automation.Cli.Handlers;

/// <summary>
/// Handler fuer Step 6: Tasks vom Ticket laden
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd)]
public partial class LoadTasksHandler(IProcessRunner processRunner) : IRequestHandler<LoadTasksRequest, StepResult>
{
    public async Task<StepResult> Handle(LoadTasksRequest request, IMediatorContext context, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Step 6: Tasks laden ===");
        Console.ResetColor();

        var stepContext = request.Context;

        if (!stepContext.IsGitHubIssue)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Keine GitHub Issue URL erkannt - ueberspringe Task-Laden");
            Console.ResetColor();
            return StepResult.Ok("Load Tasks", []);
        }

        Console.WriteLine($"Lade Issue #{stepContext.GitHubIssueNumber} von {stepContext.GitHubRepo}...");

        // Issue-Body laden
        var issueResult = await processRunner.RunGitHubAsync(
            $"issue view {stepContext.GitHubIssueNumber} --repo {stepContext.GitHubRepo} --json body --jq \".body\"",
            ct);

        var tasks = new List<string>();

        if (issueResult.Success && !string.IsNullOrWhiteSpace(issueResult.Output))
        {
            // Markdown Task-Listen extrahieren: - [ ] Task oder - [x] Task
            var taskMatches = TaskListRegex().Matches(issueResult.Output);
            foreach (Match match in taskMatches)
            {
                tasks.Add(match.Groups[1].Value.Trim());
            }
        }

        // Falls keine Tasks im Body, Sub-Issues laden
        if (tasks.Count == 0)
        {
            var subIssuesResult = await processRunner.RunGitHubAsync(
                $"issue list --repo {stepContext.GitHubRepo} --search \"linked:{stepContext.GitHubIssueNumber}\" --json title,createdAt --jq \"sort_by(.createdAt) | .[].title\"",
                ct);

            if (subIssuesResult.Success && !string.IsNullOrWhiteSpace(subIssuesResult.Output))
            {
                tasks.AddRange(subIssuesResult.Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()));
            }
        }

        // Tasks in den Context speichern
        stepContext.Tasks.AddRange(tasks);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Gefundene Tasks: {tasks.Count}");
        Console.ResetColor();

        foreach (var task in tasks)
        {
            Console.WriteLine($"  - {task}");
        }

        return StepResult.Ok("Load Tasks", tasks);
    }

    [GeneratedRegex(@"- \[[ x]\] (.+)")]
    private static partial Regex TaskListRegex();
}
