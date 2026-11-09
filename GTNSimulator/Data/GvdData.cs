using GVDEditor.Domain.Entities;
using ToolsCore.Iniss.Expressions;

namespace GTNSimulator.Data;

/// <summary>
///     Grafikon načítaný v rozsahu, ktorý potrebuje simulátor GTN - bez zvukovej banky, tabúľ a hlásení.
/// </summary>
public sealed class GvdData
{
    /// <summary>
    ///     Index zabudovaného druhu <c>Bus</c> v tabuľke druhov INISSu.
    /// </summary>
    private static readonly int BusTypeIndex = ExprTrainTypes.IndexOf("Bus");

    internal GvdData(GVDInfo info, List<TrainType> trainTypes, List<Track> tracks, List<Train> trains,
        Dictionary<int, BitArray> trainDays, Iniss2Source? iniss2 = null)
    {
        Info = info;
        TrainTypes = trainTypes;
        Tracks = tracks;
        Trains = trains;
        TrainDays = trainDays;
        Iniss2 = iniss2;
    }

    /// <summary>
    ///     Informácie o grafikone.
    /// </summary>
    public GVDInfo Info { get; }

    /// <summary>
    ///     Kategórie vlakov.
    /// </summary>
    public List<TrainType> TrainTypes { get; }

    /// <summary>
    ///     Koľaje v stanici vrátane <see cref="Track.None" /> na prvom mieste.
    /// </summary>
    public List<Track> Tracks { get; }

    /// <summary>
    ///     Vlaky grafikonu, zoradené podľa identifikátora.
    /// </summary>
    public List<Train> Trains { get; }

    /// <summary>
    ///     Dátumové obmedzenie vlaku ako bitové pole dní, kľúčom je <see cref="Train.ID" />. Bit s indexom
    ///     <c>i</c> platí pre deň <see cref="Train.ZaciatokPlatnosti" /> plus <c>i</c> dní.
    /// </summary>
    public Dictionary<int, BitArray> TrainDays { get; }

    /// <summary>
    ///     Údaje servera INISS2, alebo <see langword="null" /> pri grafikone INISS 3.39.
    /// </summary>
    public Iniss2Source? Iniss2 { get; }

    /// <summary>
    ///     Grafikon patrí serveru INISS2: správy musia niesť pravidelný čas a dôvody meškania ani prestupy server neprijíma.
    /// </summary>
    public bool IsIniss2 => Iniss2 is not null;

    /// <summary>
    ///     Identifikátor stanice, pre ktorú je grafikon urobený.
    /// </summary>
    public int StationId => int.Parse(Info.ThisStation.ID, CultureInfo.InvariantCulture);

    /// <summary>
    ///     Zistí, či vlak ide v zadaný deň.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <param name="day">Deň.</param>
    /// <returns><see langword="true" />, ak vlak v ten deň ide.</returns>
    public bool RunsOn(Train train, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(train);

        if (!TrainDays.TryGetValue(train.ID, out var days))
            return false;

        var index = (int)(day.Date - train.ZaciatokPlatnosti.ToDateTime(TimeOnly.MinValue)).TotalDays;
        return index >= 0 && index < days.Length && days[index];
    }

    /// <summary>
    ///     Vráti vlaky, ktoré idú v zadaný deň.
    /// </summary>
    /// <param name="day">Deň.</param>
    /// <returns>Vlaky idúce v ten deň.</returns>
    public IEnumerable<Train> TrainsOn(DateTime day) => Trains.Where(train => RunsOn(train, day));

    /// <summary>
    ///     Vráti koľaj vlaku podľa grafikonu. V INISS2 závisí od dňa.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <param name="day">Deň, keď vlak ide stanicou.</param>
    /// <returns>Koľaj, alebo <see langword="null" />, ak ju vlak v ten deň nemá.</returns>
    public Track? TrackOn(Train train, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(train);

        return Iniss2 is null ? train.Track : Iniss2.TrackOn(train, day);
    }

    /// <summary>
    ///     Vráti číslo vlaku pri príchode. V INISS2 môže vlak v stanici zmeniť číslo.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns>Číslo vlaku.</returns>
    public string ArrivalNumber(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return Iniss2?.ArrivalNumber(train) ?? train.Number;
    }

    /// <summary>
    ///     Vráti číslo vlaku pri odchode.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns>Číslo vlaku.</returns>
    public string DepartureNumber(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return Iniss2?.DepartureNumber(train) ?? train.Number;
    }

    /// <summary>
    ///     Vráti čas príchodu vlaku v zadaný deň.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <param name="day">Deň.</param>
    /// <returns>Čas príchodu, alebo <see langword="null" /> pri východzom vlaku.</returns>
    public static DateTime? ArrivalOn(Train train, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(train);

        return train.Arrival.HasValue ? day.Date + train.Arrival.Value.ToTimeSpan() : (DateTime?)null;
    }

    /// <summary>
    ///     Vráti čas odchodu vlaku v zadaný deň. Ak vlak stojí cez polnoc, odchod pripadne na nasledujúci deň.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <param name="day">Deň.</param>
    /// <returns>Čas odchodu, alebo <see langword="null" /> pri končiacom vlaku.</returns>
    public static DateTime? DepartureOn(Train train, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(train);

        if (!train.Departure.HasValue)
            return null;

        var departure = day.Date + train.Departure.Value.ToTimeSpan();
        var arrival = ArrivalOn(train, day);

        return arrival.HasValue && departure < arrival.Value ? departure.AddDays(1) : departure;
    }

    /// <summary>
    ///     Zistí, či je vlak autobus - teda či je jeho zabudovaný druh <c>Bus</c>. Rozhoduje kategória druhu,
    ///     nie kľúč: druh, ktorý v <c>TrTypes.txt</c> mapuje iný kľúč (napríklad <c>NAD</c>) na <c>Bus</c>, je tiež
    ///     autobus. INISS autobusy so správami GTN nepáruje.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns><see langword="true" />, ak je vlak autobus.</returns>
    public static bool IsBus(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return train.Type is { } type && ExprTrainTypes.IndexOf(type.CategoryTrain) == BusTypeIndex;
    }

    /// <summary>
    ///     Zistí, či má vlak v grafikone priradenú skutočnú koľaj. Vlak bez nej (predvolená alebo nedefinovaná
    ///     koľaj) dostane koľaj až od simulátora.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns><see langword="true" />, ak má vlak priradenú skutočnú koľaj.</returns>
    public static bool HasRealTrack(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return IsRealTrack(train.Track);
    }

    /// <summary>
    ///     Zistí, či je koľaj skutočná - nie je predvolená ani nedefinovaná.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <returns><see langword="true" />, ak je koľaj skutočná.</returns>
    public static bool IsRealTrack(Track? track) => track is not null && track != Track.None && !string.IsNullOrWhiteSpace(track.TrackName);
}
