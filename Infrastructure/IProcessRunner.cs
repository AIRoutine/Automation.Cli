namespace Automation.Cli.Infrastructure;

/// <summary>
/// Interface fuer das Ausfuehren von CLI-Prozessen.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Fuehrt Claude CLI mit einem Prompt aus.
    /// </summary>
    /// <param name="prompt">Der Prompt fuer Claude</param>
    /// <param name="workingDirectory">Optionales Working Directory (default: Lebenslauf Hauptprojekt)</param>
    /// <param name="ct">Cancellation Token</param>
    Task<ProcessResult> RunClaudeAsync(string prompt, string? workingDirectory = null, CancellationToken ct = default);

    /// <summary>
    /// Fuehrt GitHub CLI aus.
    /// </summary>
    Task<ProcessResult> RunGitHubAsync(string arguments, CancellationToken ct = default);
}

/// <summary>
/// Ergebnis eines Prozess-Aufrufs.
/// </summary>
public record ProcessResult(bool Success, string Output, string? Error = null);
