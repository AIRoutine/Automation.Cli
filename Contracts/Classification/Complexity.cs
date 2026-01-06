namespace Automation.Cli.Contracts.Classification;

/// <summary>
/// Geschaetzte Komplexitaet des Tickets.
/// </summary>
public enum Complexity
{
    /// <summary>
    /// Trivial: Wenige Zeilen, eine Datei.
    /// </summary>
    Trivial,

    /// <summary>
    /// Einfach: Wenige Dateien, klare Aenderungen.
    /// </summary>
    Simple,

    /// <summary>
    /// Mittel: Mehrere Dateien, Feature-Umfang.
    /// </summary>
    Medium,

    /// <summary>
    /// Komplex: Viele Dateien, Cross-Cutting Concerns.
    /// </summary>
    Complex,

    /// <summary>
    /// Epic: Sehr gross, sollte aufgeteilt werden.
    /// </summary>
    Epic
}
