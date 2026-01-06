namespace Automation.Cli.Contracts.Classification;

/// <summary>
/// Art des Tickets/der Anforderung.
/// </summary>
public enum TicketType
{
    /// <summary>
    /// Komplett neue Funktionalitaet.
    /// </summary>
    NewFeature,

    /// <summary>
    /// Erweiterung einer bestehenden Funktionalitaet.
    /// </summary>
    Enhancement,

    /// <summary>
    /// Fehlerbehebung.
    /// </summary>
    BugFix,

    /// <summary>
    /// Code-Umstrukturierung ohne Funktionsaenderung.
    /// </summary>
    Refactoring,

    /// <summary>
    /// Nur Dokumentationsaenderungen.
    /// </summary>
    Documentation,

    /// <summary>
    /// Konfigurationsaenderungen (Settings, Environment, etc.).
    /// </summary>
    Configuration,

    /// <summary>
    /// Datenbank-Migrationen oder Datenstruktur-Aenderungen.
    /// </summary>
    DataMigration
}
