using System.Text.Json.Serialization;
using GVDEditor.Domain.Entities;

namespace GTNSimulator.Simulation;

/// <summary>
///     Ručná zmena jednej jazdy - len hodnoty, ktoré sa líšia od vylosovaných.
/// </summary>
public sealed record RunChange
{
    /// <summary>
    ///     Číslo vlaku. Simulátor posiela na jedno číslo len jednu jazdu, takže jazdu jednoznačne určuje.
    /// </summary>
    [JsonPropertyName("train")]
    public string Train { get; init; } = "";

    /// <summary>
    ///     Meškanie na príchode v sekundách.
    /// </summary>
    [JsonPropertyName("arrivalDelay")]
    public int? ArrivalDelaySeconds { get; init; }

    /// <summary>
    ///     Meškanie na odchode v sekundách.
    /// </summary>
    [JsonPropertyName("departureDelay")]
    public int? DepartureDelaySeconds { get; init; }

    /// <summary>
    ///     Kľúč koľaje.
    /// </summary>
    [JsonPropertyName("track")]
    public string? Track { get; init; }

    /// <summary>
    ///     Dôvod meškania, nula = bez dôvodu.
    /// </summary>
    [JsonPropertyName("delayReason")]
    public int? DelayReason { get; init; }

    /// <summary>
    ///     O vlaku sa neposielajú správy.
    /// </summary>
    [JsonPropertyName("suppressed")]
    public bool Suppressed { get; init; }

    /// <summary>
    ///     Zmena jazdy oproti tomu, čo vylosoval plánovač.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <returns>Zmena.</returns>
    public static RunChange Of(TrainRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var drawn = run.Drawn;

        return new RunChange
        {
            Train = run.Train.Number,
            ArrivalDelaySeconds = run.ArrivalDelay == drawn.ArrivalDelay ? null : (int)run.ArrivalDelay.TotalSeconds,
            DepartureDelaySeconds = run.DepartureDelay == drawn.DepartureDelay ? null : (int)run.DepartureDelay.TotalSeconds,
            Track = ReferenceEquals(run.Track, drawn.Track) ? null : run.Track.Key,
            DelayReason = run.DelayReason == drawn.DelayReason ? null : run.DelayReason,
            Suppressed = run.Suppressed
        };
    }

    /// <summary>
    ///     Prenesie zmenu na jazdu. Koľaj a dôvod idú pred meškaniami, meškanie na odchode až po príchode, aby sa
    ///     nevrátilo posunom príchodu.
    /// </summary>
    internal PlanEditError ApplyTo(TrainRun run, IReadOnlyList<Track> tracks, PlanEditContext context)
    {
        var error = PlanEditError.None;

        if (Track is not null)
        {
            var track = tracks.FirstOrDefault(candidate => candidate.Key == Track);
            error = Worst(error, track is null ? PlanEditError.TrackUnusable : PlanEditing.SetTrack(run, track, context));
        }

        if (DelayReason is { } reason)
            error = Worst(error, PlanEditing.SetDelayReason(run, reason, context));

        if (ArrivalDelaySeconds is { } arrival)
            error = Worst(error, PlanEditing.SetArrivalDelay(run, TimeSpan.FromSeconds(arrival), context));

        if (DepartureDelaySeconds is { } departure)
            error = Worst(error, PlanEditing.SetDepartureDelay(run, TimeSpan.FromSeconds(departure), context));

        if (Suppressed)
            error = Worst(error, PlanEditing.SetSuppressed(run, true, context));

        return error;
    }

    private static PlanEditError Worst(PlanEditError current, PlanEditError next) => current == PlanEditError.None ? next : current;
}
