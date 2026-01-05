using Microsoft.Extensions.DependencyInjection;

namespace Automation.Cli;

/// <summary>
/// DI-Konstanten fuer CLI-Services (analog zu ApiService/UnoService).
/// </summary>
public static class CliService
{
    public const ServiceLifetime Lifetime = ServiceLifetime.Scoped;
    public const bool TryAdd = true;
}
