namespace GTNSimulator.Simulation;

/// <summary>
///     Súbehy na koľajach - dva vlaky ohlásené na tú istú koľaj v čase, keď ju oba držia. Plánovač súbehom
///     predchádza, ale ručná zmena koľaje alebo meškania ich môže vytvoriť; zmenu nezakazujú, len na ne
///     upozorňujú.
/// </summary>
public static class PlanConflicts
{
    /// <summary>
    ///     Vráti jazdy, s ktorými sa jazda stretne na svojej koľaji.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="runs">Všetky jazdy dňa.</param>
    /// <returns>Jazdy v súbehu zoradené podľa času obsadenia.</returns>
    public static IReadOnlyList<TrainRun> With(TrainRun run, IEnumerable<TrainRun> runs)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(runs);

        if (!Counts(run))
            return [];

        return runs
            .Where(other => !ReferenceEquals(other, run) && Counts(other) && Overlap(run, other))
            .OrderBy(other => other.OccupiedFrom)
            .ToList();
    }

    /// <summary>
    ///     Vráti všetky jazdy, ktoré sú v súbehu s inou jazdou.
    /// </summary>
    /// <param name="runs">Všetky jazdy dňa.</param>
    /// <returns>Jazdy v súbehu.</returns>
    public static ISet<TrainRun> All(IEnumerable<TrainRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        var conflicts = new HashSet<TrainRun>();

        foreach (var track in runs.Where(Counts).GroupBy(run => run.Track.Key))
        {
            var ordered = track.OrderBy(run => run.OccupiedFrom).ToList();

            for (var i = 0; i < ordered.Count; i++)
            for (var j = i + 1; j < ordered.Count && ordered[j].OccupiedFrom < ordered[i].OccupiedTo; j++)
            {
                conflicts.Add(ordered[i]);
                conflicts.Add(ordered[j]);
            }
        }

        return conflicts;
    }

    /// <summary>
    ///     Do súbehu sa počíta len vlak, o ktorom sa posielajú správy, na koľaji, ktorá sa dá ohlásiť.
    /// </summary>
    private static bool Counts(TrainRun run) => !run.Suppressed && TrackAllocator.CanUse(run.Track);

    private static bool Overlap(TrainRun a, TrainRun b) =>
        a.Track.Key == b.Track.Key && a.OccupiedFrom < b.OccupiedTo && b.OccupiedFrom < a.OccupiedTo;
}
