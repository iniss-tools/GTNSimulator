namespace GTNSimulator.Simulation;

/// <summary>
///     Skladba udalostí jazdy z jej aktuálnych hodnôt. Spoločná pre plánovač aj úpravy plánu, aby
///     naplánovaná a zmenená jazda mali rovnaké udalosti.
/// </summary>
internal static class RunEvents
{
    /// <summary>
    ///     Zostaví všetky udalosti jazdy podľa jej aktuálnych hodnôt, všetky čakajúce. Jazda, o ktorej sa
    ///     správy neposielajú, nemá žiadnu.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="options">Nastavenia simulácie.</param>
    /// <returns>Udalosti jazdy.</returns>
    public static List<SimEvent> Build(TrainRun run, SimulationOptions options)
    {
        var events = new List<SimEvent>();

        if (run.Suppressed)
            return events;

        if (run.PlannedArrival.HasValue)
        {
            var planned = run.PlannedArrival.Value - options.EntryRouteLead;
            var route = run.ActualArrival!.Value - options.EntryRouteLead;

            events.Add(new SimEvent(run, SimEventKind.EntryRoute, planned, route, run.Track));

            // Dôvod ide spolu s vlakovou cestou, aby ho INISS mal skôr, než odbaví hlásenie o meškaní.
            if (SendsReason(run))
                events.Add(new SimEvent(run, SimEventKind.DelayReason, planned, route, run.Track, run.DelayReason));

            events.Add(new SimEvent(run, SimEventKind.Arrival, run.PlannedArrival.Value, run.ActualArrival.Value, run.Track));
        }

        if (!run.PlannedDeparture.HasValue)
            return events;

        var plannedExit = run.PlannedDeparture.Value - options.ExitRouteLead;
        var exit = run.ActualDeparture!.Value - options.ExitRouteLead;

        if (SendsReason(run) && !run.PlannedArrival.HasValue)
            events.Add(new SimEvent(run, SimEventKind.DelayReason, plannedExit, exit, run.Track, run.DelayReason));

        events.Add(new SimEvent(run, SimEventKind.ExitRoute, plannedExit, exit, run.Track));
        events.Add(new SimEvent(run, SimEventKind.Departure, run.PlannedDeparture.Value, run.ActualDeparture.Value, run.Track));

        return events;
    }

    private static bool SendsReason(TrainRun run) => run.DelayReason != 0 && !run.Iniss2;

    /// <summary>
    ///     Prepočíta, kedy vlak drží koľaj - od postavenia vjazdovej cesty po odchod plus rezerva.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="options">Nastavenia simulácie.</param>
    public static void UpdateOccupancy(TrainRun run, SimulationOptions options)
    {
        var start = (run.ActualArrival ?? run.ActualDeparture)!.Value;
        var end = (run.ActualDeparture ?? run.ActualArrival)!.Value;

        run.OccupiedFrom = start - options.EntryRouteLead;
        run.OccupiedTo = end + options.TrackReleaseMargin;
    }
}
