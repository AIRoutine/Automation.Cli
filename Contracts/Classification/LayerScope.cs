namespace Automation.Cli.Contracts.Classification;

/// <summary>
/// Betroffene Architektur-Schichten (Flags fuer Kombinationen).
/// </summary>
[Flags]
public enum LayerScope
{
    /// <summary>
    /// Keine Schicht betroffen.
    /// </summary>
    None = 0,

    /// <summary>
    /// Data-Schicht: Entities, DbContext, Migrations, Seeding.
    /// </summary>
    Data = 1,

    /// <summary>
    /// API-Schicht: Endpoints, Handlers, Services.
    /// </summary>
    Api = 2,

    /// <summary>
    /// Frontend-Schicht: Pages, ViewModels, XAML.
    /// </summary>
    Frontend = 4,

    /// <summary>
    /// Shared-Schicht: Contracts, DTOs, Interfaces.
    /// </summary>
    Shared = 8,

    /// <summary>
    /// Infrastruktur: Build, CI/CD, Config.
    /// </summary>
    Infrastructure = 16,

    /// <summary>
    /// Alle Schichten.
    /// </summary>
    All = Data | Api | Frontend | Shared | Infrastructure
}
