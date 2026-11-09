using GVDEditor.Domain.Entities;

namespace GTNSimulator.Simulation;

/// <summary>
///     Výluka koľaje v intervale - vlaky, ktoré v ňom koľaj držia, sa presunú na voľnú koľaj. Je to hromadná ručná
///     zmena koľaje cez <see cref="PlanEditing.SetTrack" />, takže INISS dostane novú koľaj v ďalších správach,
///     prípadne novú vlakovú cestu.
/// </summary>
public static class TrackClosure
{
    /// <summary>
    ///     Jazdy, ktorých sa výluka týka - posielajú sa o nich správy, ešte neskončili a koľaj držia v intervale.
    /// </summary>
    /// <param name="runs">Jazdy dňa.</param>
    /// <param name="closed">Vylúčená koľaj.</param>
    /// <param name="from">Začiatok výluky.</param>
    /// <param name="to">Koniec výluky.</param>
    /// <returns>Jazdy zoradené podľa začiatku obsadenia koľaje.</returns>
    public static IReadOnlyList<TrainRun> Affected(IEnumerable<TrainRun> runs, Track closed, DateTime from, DateTime to)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(closed);

        return runs
            .Where(run => !run.Suppressed && !run.IsFinished && run.Track?.Key == closed.Key &&
                          run.OccupiedFrom < to && from < run.OccupiedTo)
            .OrderBy(run => run.OccupiedFrom)
            .ToList();
    }

    /// <summary>
    ///     Koľaje, na ktoré sa smie vlak z vylúčenej koľaje presunúť - prednostne pri tom istom nástupišti, inak
    ///     v poradí grafikonu. Vynechá koľaje, ktoré sa nedajú ohlásiť, a koľaje vylúčené z plánovania.
    /// </summary>
    /// <param name="tracks">Koľaje stanice.</param>
    /// <param name="closed">Vylúčená koľaj.</param>
    /// <param name="excluded">Kľúče koľají vylúčených z plánovania.</param>
    /// <returns>Koľaje v poradí, v akom sa skúšajú.</returns>
    public static IReadOnlyList<Track> Candidates(IEnumerable<Track> tracks, Track closed, IEnumerable<string> excluded)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(closed);

        var skip = new HashSet<string>(excluded ?? [], StringComparer.OrdinalIgnoreCase) { closed.Key };
        var platform = closed.Platform?.Key;

        return tracks
            .Where(track => TrackAllocator.CanUse(track) && !skip.Contains(track.Key))
            .OrderBy(track => string.IsNullOrEmpty(platform) || track.Platform?.Key != platform)
            .ToList();
    }

    /// <summary>
    ///     Presunie jazdu z vylúčenej koľaje na prvú koľaj, ktorá je počas jej obsadenia voľná.
    /// </summary>
    /// <param name="run">Jazda na vylúčenej koľaji.</param>
    /// <param name="runs">Všetky jazdy dňa - podľa nich sa zisťuje, či je koľaj voľná.</param>
    /// <param name="candidates">Koľaje z <see cref="Candidates" />.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa jazda presunúť nedala, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError MoveOff(TrainRun run, IReadOnlyList<TrainRun> runs, IReadOnlyList<Track> candidates,
        PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(candidates);

        if (run.IsFinished)
            return PlanEditError.Finished;

        // vlak, ktory uz na kolaji stoji, sa nepresuva - vyluka sa zacina az po jeho odchode
        if (run.PlannedArrival.HasValue && run.IsDone(SimEventKind.Arrival))
            return PlanEditError.StandingOnTrack;

        var target = candidates.FirstOrDefault(track => runs.All(other =>
            ReferenceEquals(other, run) || other.Suppressed || other.Track?.Key != track.Key ||
            other.OccupiedTo <= run.OccupiedFrom || run.OccupiedTo <= other.OccupiedFrom));

        return target is null ? PlanEditError.NoFreeTrack : PlanEditing.SetTrack(run, target, context);
    }
}
