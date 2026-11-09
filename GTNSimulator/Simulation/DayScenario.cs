using System.Text.Json.Serialization;
using GVDEditor.Domain.Entities;

namespace GTNSimulator.Simulation;

/// <summary>
///     Scenár dňa - všetko, čo treba na zopakovanie rovnakého dňa: stanica, deň, nastavenia modelu náhody
///     (seed) a ručné zmeny jázd. Vylosované hodnoty sa neukladajú - rovnaký seed ich vylosuje znova.
/// </summary>
public sealed record DayScenario
{
    /// <summary>
    ///     Identifikátor stanice z grafikonu - scenár patrí k jednej stanici.
    /// </summary>
    [JsonPropertyName("station")]
    public int StationId { get; init; }

    /// <summary>
    ///     Simulovaný deň.
    /// </summary>
    [JsonPropertyName("day")]
    public DateOnly Day { get; init; }

    /// <summary>
    ///     Od kedy sa deň simuluje.
    /// </summary>
    [JsonPropertyName("from")]
    public TimeOnly From { get; init; }

    /// <summary>
    ///     Seed generátora náhody.
    /// </summary>
    [JsonPropertyName("seed")]
    public int Seed { get; init; }

    /// <summary>
    ///     Pravdepodobnosť príchodu načas v percentách.
    /// </summary>
    [JsonPropertyName("onTimePercent")]
    public int OnTimePercent { get; init; }

    /// <summary>
    ///     Stredné meškanie v minútach.
    /// </summary>
    [JsonPropertyName("meanDelayMinutes")]
    public double MeanDelayMinutes { get; init; }

    /// <summary>
    ///     Pravdepodobnosť zmeny koľaje v percentách.
    /// </summary>
    [JsonPropertyName("trackChangePercent")]
    public int TrackChangePercent { get; init; }

    /// <summary>
    ///     Pravdepodobnosť dôvodu meškania v percentách.
    /// </summary>
    [JsonPropertyName("delayReasonPercent")]
    public int DelayReasonPercent { get; init; }

    /// <summary>
    ///     Predstih vjazdovej vlakovej cesty v minútach.
    /// </summary>
    [JsonPropertyName("entryRouteLeadMinutes")]
    public double EntryRouteLeadMinutes { get; init; }

    /// <summary>
    ///     Predstih odchodovej vlakovej cesty v minútach.
    /// </summary>
    [JsonPropertyName("exitRouteLeadMinutes")]
    public double ExitRouteLeadMinutes { get; init; }

    /// <summary>
    ///     Koľaje vylúčené z prideľovania.
    /// </summary>
    [JsonPropertyName("excludedTracks")]
    public IReadOnlyList<string> ExcludedTracks { get; init; } = [];

    /// <summary>
    ///     Ručné zmeny jázd.
    /// </summary>
    [JsonPropertyName("changes")]
    public IReadOnlyList<RunChange> Changes { get; init; } = [];

    /// <summary>
    ///     Zachytí scenár z naplánovaného dňa.
    /// </summary>
    /// <param name="stationId">Identifikátor stanice.</param>
    /// <param name="day">Deň.</param>
    /// <param name="from">Od kedy sa simuluje.</param>
    /// <param name="options">Nastavenia simulácie, s ktorými sa deň naplánoval.</param>
    /// <param name="runs">Jazdy dňa.</param>
    /// <returns>Scenár.</returns>
    public static DayScenario Capture(int stationId, DateTime day, DateTime from, SimulationOptions options, IEnumerable<TrainRun> runs)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(runs);

        return new DayScenario
        {
            StationId = stationId,
            Day = DateOnly.FromDateTime(day),
            From = TimeOnly.FromDateTime(from),
            Seed = options.Seed,
            OnTimePercent = Percent(options.OnTimeProbability),
            MeanDelayMinutes = options.MeanDelayMinutes,
            TrackChangePercent = Percent(options.TrackChangeProbability),
            DelayReasonPercent = Percent(options.DelayReasonProbability),
            EntryRouteLeadMinutes = options.EntryRouteLead.TotalMinutes,
            ExitRouteLeadMinutes = options.ExitRouteLead.TotalMinutes,
            ExcludedTracks = options.ExcludedTrackKeys.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Changes = runs.Where(run => run.IsChanged).Select(RunChange.Of).ToList()
        };
    }

    /// <summary>
    ///     Nastavenia simulácie zo scenára. Dôvody meškania sa doplnia zo zvukovej banky stanice.
    /// </summary>
    /// <param name="delayReasons">Poradové čísla dôvodov meškania stanice.</param>
    /// <param name="timeFactor">Zrýchlenie času.</param>
    /// <returns>Nastavenia simulácie.</returns>
    public SimulationOptions ToOptions(IEnumerable<int> delayReasons, double timeFactor) => new()
    {
        Seed = Seed,
        TimeFactor = timeFactor,
        OnTimeProbability = OnTimePercent / 100.0,
        MeanDelayMinutes = MeanDelayMinutes,
        TrackChangeProbability = TrackChangePercent / 100.0,
        DelayReasonProbability = DelayReasonPercent / 100.0,
        EntryRouteLead = TimeSpan.FromMinutes(EntryRouteLeadMinutes),
        ExitRouteLead = TimeSpan.FromMinutes(ExitRouteLeadMinutes),
        DelayReasons = delayReasons.ToList(),
        ExcludedTrackKeys = new HashSet<string>(ExcludedTracks, StringComparer.OrdinalIgnoreCase)
    };

    /// <summary>
    ///     Prenesie ručné zmeny na naplánované jazdy.
    /// </summary>
    /// <param name="runs">Jazdy dňa naplánované podľa <see cref="ToOptions" />.</param>
    /// <param name="tracks">Koľaje grafikonu.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Zmeny, ktoré sa preniesť nedali, s dôvodom; nenájdený vlak má <see cref="PlanEditError.NotPending" />.</returns>
    public IReadOnlyList<(RunChange Change, PlanEditError Error)> Apply(IReadOnlyList<TrainRun> runs, IReadOnlyList<Track> tracks,
        PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(tracks);

        var failed = new List<(RunChange, PlanEditError)>();

        foreach (var change in Changes)
        {
            var run = runs.FirstOrDefault(candidate => candidate.Train.Number == change.Train);
            if (run is null)
            {
                failed.Add((change, PlanEditError.NotPending));
                continue;
            }

            var error = change.ApplyTo(run, tracks, context);
            if (error != PlanEditError.None)
                failed.Add((change, error));
        }

        return failed;
    }

    private static int Percent(double probability) => (int)Math.Round(probability * 100);
}
