using System.Diagnostics;
using Shiny.Extensions.DependencyInjection;

namespace Automation.Cli.Infrastructure;

/// <summary>
/// Implementierung fuer das Ausfuehren von CLI-Prozessen.
/// </summary>
[Service(CliService.Lifetime, TryAdd = CliService.TryAdd, Type = typeof(IProcessRunner))]
public class ProcessRunner : IProcessRunner
{
    // Default working directory for Lebenslauf project
    // Navigate from subm/cli to root: subm/cli -> subm -> Lebenslauf
    private static readonly string DefaultWorkingDirectory = GetLebenslaufRootDirectory();

    private static string GetLebenslaufRootDirectory()
    {
        // Try to find the Lebenslauf root by looking for CLAUDE.md
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);

        // Walk up until we find CLAUDE.md or hit the root
        while (currentDir != null)
        {
            var claudeMdPath = Path.Combine(currentDir.FullName, "CLAUDE.md");
            if (File.Exists(claudeMdPath))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }

        // Fallback: assume we're in subm/cli/bin/Debug/net10.0
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    public async Task<ProcessResult> RunClaudeAsync(string prompt, string? workingDirectory = null, CancellationToken ct = default)
    {
        var effectiveWorkingDir = workingDirectory ?? DefaultWorkingDirectory;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[Claude] Working Directory: {effectiveWorkingDir}");
        Console.ResetColor();

        // Write prompt to a temp file to avoid escaping issues
        var promptFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(promptFile, prompt, ct);

        try
        {
            // Use PowerShell to run claude with proper argument handling
            // --dangerously-skip-permissions: auto-approves ALL tool uses in headless mode (no explicit --allowedTools needed)
            var psCommand = $"Get-Content -Raw '{promptFile}' | claude -p - --dangerously-skip-permissions";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{psCommand}\"",
                WorkingDirectory = effectiveWorkingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[Claude] Command: claude -p <prompt> --dangerously-skip-permissions");
            Console.ResetColor();

            return await RunProcessAsync(startInfo, ct);
        }
        finally
        {
            // Clean up temp file
            try { File.Delete(promptFile); } catch { /* ignore */ }
        }
    }

    public async Task<ProcessResult> RunGitHubAsync(string arguments, CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await RunProcessAsync(startInfo, ct);
    }

    private static async Task<ProcessResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken ct)
    {
        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            return new ProcessResult(
                Success: process.ExitCode == 0,
                Output: output,
                Error: string.IsNullOrWhiteSpace(error) ? null : error);
        }
        catch (Exception ex)
        {
            return new ProcessResult(Success: false, Output: string.Empty, Error: ex.Message);
        }
    }

    private static string EscapeArgument(string arg)
    {
        // Escape double quotes and backslashes for command line
        return arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
