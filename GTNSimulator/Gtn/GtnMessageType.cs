using ToolsCore.Iniss.Tools;

namespace GTNSimulator.Gtn;

/// <summary>
///     Typ správy rozhrania GTN. INISS pozná typy 0802 až 0806 a kódy 1802 až 1804 ako inú podobu
///     0802 až 0804; ostatné zaloguje ako <c>unknown message type</c>.
/// </summary>
public sealed class GtnMessageType : Enumeration<GtnMessageType>
{
    /// <summary>
    ///     080-2 Príchod vlaku (vznik vlaku).
    /// </summary>
    public static readonly GtnMessageType Arrival = new("0802", "Príchod vlaku");

    /// <summary>
    ///     080-3 Odchod vlaku. Spolu s 080-4 nesie traťovú koľaj (pre nasledujúce zastávky trasy).
    /// </summary>
    public static readonly GtnMessageType Departure = new("0803", "Odchod vlaku");

    /// <summary>
    ///     080-4 Vlaková cesta. Ako jediná správa nesie príznak druhu vlakovej cesty, spolu s 080-3 aj traťovú koľaj.
    /// </summary>
    public static readonly GtnMessageType Route = new("0804", "Vlaková cesta");

    /// <summary>
    ///     080-5 Dôvod meškania. Pozná ju až INISS 3.39.
    /// </summary>
    public static readonly GtnMessageType DelayReason = new("0805", "Dôvod meškania");

    /// <summary>
    ///     080-6 Hlásenie o prestupe. Pozná ju až INISS 3.39.
    /// </summary>
    public static readonly GtnMessageType Transfer = new("0806", "Hlásenie o prestupe");

    private GtnMessageType(string key, string name) : base(key, name)
    {
    }

    /// <summary>
    ///     Vráti typ správy podľa štvorznakového kódu na začiatku správy. Kódy 1802 až 1804 sú len inou podobou
    ///     0802 až 0804.
    /// </summary>
    /// <param name="code">Kód typu správy (napr. <c>0802</c>).</param>
    /// <returns>Typ správy alebo <see langword="null" />, ak kód nezodpovedá žiadnemu známemu typu.</returns>
    public static GtnMessageType? FromCode(string code) => code switch
    {
        "1802" => Arrival,
        "1803" => Departure,
        "1804" => Route,
        _ => GetValues().FirstOrDefault(type => type.Key == code)
    };

    /// <inheritdoc />
    public override string ToString() => $"{Key} - {Name}";
}
