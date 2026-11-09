namespace GTNSimulator.Gtn;

/// <summary>
///     Jedna správa rozhrania GTN v podobe rozobratej na polia.
///     Prevod na drôt a späť zabezpečuje <see cref="GtnEncoder" />.
/// </summary>
public sealed record GtnMessage
{
    /// <summary>
    ///     Typ správy (0802 až 0806).
    /// </summary>
    public GtnMessageType? Type { get; set; }

    /// <summary>
    ///     Pole stanice tak, ako ide na drôt - šesť znakov. Zakódovanie identifikátora stanice do tohto poľa
    ///     rieši <see cref="StationCodec" />.
    /// </summary>
    public string StationField { get; set; } = "";

    /// <summary>
    ///     Číslo vlaku. INISS pole trimuje, takže sa dopĺňa medzerami.
    /// </summary>
    public string TrainNumber { get; set; } = "";

    /// <summary>
    ///     Čas udalosti. Na drôt ide bez roku, vo formáte <c>MMddHHmmss</c>.
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    ///     Druh vlakovej cesty. Uplatní sa iba pri <see cref="GtnMessageType.Route" />.
    /// </summary>
    public GtnRouteKind RouteKind { get; set; } = GtnRouteKind.Entry;

    /// <summary>
    ///     Koľaj. INISS pole trimuje, takže sa dopĺňa medzerami.
    /// </summary>
    public string Track { get; set; } = "";

    /// <summary>
    ///     Traťová koľaj - koľaj trate, na ktorú vlak zo stanice odchádza. Uplatní sa iba pri
    ///     <see cref="GtnMessageType.Departure" /> a <see cref="GtnMessageType.Route" />. Vlaku v stanici zo správy
    ///     ju INISS nedosadí - dostanú ju nasledujúce zastávky trasy. Odchod s traťovou koľajou navyše nastaví
    ///     udalosť <c>Odj</c> a vlak v stanici ukončí (prepne ho do posledného stavu kategórie).
    ///     <see langword="null" /> znamená, že sa pole neposiela; INISS chýbajúce koncové pole nepovažuje za chybu.
    /// </summary>
    public string? LineTrack { get; set; }

    /// <summary>
    ///     Pravidelný čas vlaku - plánovaný príchod pri <see cref="GtnMessageType.Arrival" />, plánovaný odchod pri
    ///     <see cref="GtnMessageType.Departure" /> a <see cref="GtnMessageType.Route" /> v stanici zo správy. S ním
    ///     INISS páruje vlak podľa tohto času, nie podľa času udalosti, a správu o vlaku, ktorý v stanici nie je,
    ///     uplatní na nasledujúce stanice trasy. ILTIS ho neposiela. <see langword="null" /> znamená, že sa pole
    ///     neposiela.
    /// </summary>
    public DateTime? RegularTime { get; set; }

    /// <summary>
    ///     Poradové číslo dôvodu meškania - uplatní sa iba pri <see cref="GtnMessageType.DelayReason" />.
    ///     Číslované od jednotky, <c>00</c> dôvod zmaže. Viď <see cref="GtnEncoder.FormatDelayReason" />.
    /// </summary>
    public string DelayReason { get; set; } = "00";

    /// <summary>
    ///     Druh hlásenia o prestupe - uplatní sa iba pri <see cref="GtnMessageType.Transfer" />.
    /// </summary>
    public GtnTransferKind TransferKind { get; set; } = GtnTransferKind.Normal;

    /// <inheritdoc />
    public override string ToString() =>
        $"{Type?.Key} {StationField} {TrainNumber?.Trim()} {Time:dd.MM. HH:mm:ss} {Track?.Trim()}";
}
