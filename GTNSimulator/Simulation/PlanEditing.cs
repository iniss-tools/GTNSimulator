using GVDEditor.Domain.Entities;

namespace GTNSimulator.Simulation;

/// <summary>
///     Zmeny naplánovanej jazdy - aj počas behu simulácie. Mení sa len to, čo ešte nebolo ohlásené: po
///     každej zmene sa čakajúce udalosti jazdy zostavia nanovo, vydané a preskočené ostanú tak, ako boli.
/// </summary>
/// <remarks>
///     Ak už INISS dostal údaj, ktorý sa zmenil, pošle sa dodatočná správa: po zmene koľaje nová vlaková
///     cesta (ako keby ju dispečer postavil znova), po zmene dôvodu meškania nová správa 080-5, pri zrušení
///     dôvodu s číslom 00. Udalosti, ktorých nový čas už uplynul, sa vydajú pri najbližšom kroku simulácie
///     so svojím časom - INISS si z neho meškanie odvodí rovnako.
/// </remarks>
public static class PlanEditing
{
    /// <summary>
    ///     Najväčší náskok vlaku oproti grafikonu. INISS prijme správu najviac dve hodiny pred grafikonom,
    ///     vlaková cesta ide ešte o predstih skôr.
    /// </summary>
    public static readonly TimeSpan MinDelay = TimeSpan.FromMinutes(-60);

    /// <summary>
    ///     Najkratší pobyt v stanici, na ktorý môže vlak skrátiť pobyt z grafikonu (kapitola 7.2 návodu
    ///     k INISSu).
    /// </summary>
    public static readonly TimeSpan MinStay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Zmení meškanie na príchode. Meškanie na odchode sa posunie o rovnaký rozdiel, ak by tým vlak
    ///     neodišiel skôr, než dovolí najkratší pobyt.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="delay">Nové meškanie na príchode.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SetArrivalDelay(TrainRun run, TimeSpan delay, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!run.PlannedArrival.HasValue)
            return PlanEditError.NoArrival;
        if (run.IsFinished)
            return PlanEditError.Finished;
        if (run.IsDone(SimEventKind.Arrival))
            return PlanEditError.ArrivalSent;
        if (!InRange(delay))
            return PlanEditError.DelayOutOfRange;

        if (run.PlannedDeparture.HasValue)
        {
            var shifted = run.DepartureDelay + (delay - run.ArrivalDelay);
            var minimum = MinDepartureDelay(run, delay);
            run.DepartureDelay = Clamp(shifted < minimum ? minimum : shifted);
        }

        run.ArrivalDelay = delay;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Zmení meškanie na odchode.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="delay">Nové meškanie na odchode.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SetDepartureDelay(TrainRun run, TimeSpan delay, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!run.PlannedDeparture.HasValue)
            return PlanEditError.NoDeparture;
        if (run.IsFinished)
            return PlanEditError.Finished;
        if (!InRange(delay))
            return PlanEditError.DelayOutOfRange;
        if (run.PlannedArrival.HasValue && delay < MinDepartureDelay(run, run.ArrivalDelay))
            return PlanEditError.DepartureTooEarly;

        run.DepartureDelay = delay;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Postaví vlak na inú koľaj. Obsadenosť koľaje sa nekontroluje - na súbeh upozorní
    ///     <see cref="PlanConflicts" />.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="track">Nová koľaj.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SetTrack(TrainRun run, Track track, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!TrackAllocator.CanUse(track))
            return PlanEditError.TrackUnusable;
        if (run.IsFinished)
            return PlanEditError.Finished;

        // Návrat na vylosovanú koľaj vráti aj jej pôvod, aby jazda nevyzerala zmenená.
        var drawn = track.Key == run.Drawn.Track?.Key;
        run.Track = drawn ? run.Drawn.Track! : track;
        run.TrackSource = drawn ? run.Drawn.TrackSource : TrackSource.Manual;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Zmení dôvod meškania.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="reason">Poradové číslo dôvodu v skupine <c>dz</c> zvukovej banky, nula = bez dôvodu.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SetDelayReason(TrainRun run, int reason, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (reason is < 0 or > 99)
            return PlanEditError.ReasonOutOfRange;
        if (run.IsFinished)
            return PlanEditError.Finished;

        run.DelayReason = reason;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Zapne alebo vypne posielanie správ o vlaku. Po zapnutí sa pošle všetko, čo ešte nebolo
    ///     ohlásené - aj udalosti, ktorých čas medzitým uplynul, so svojím časom.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="suppressed"><see langword="true" />, ak sa o vlaku nemajú posielať správy.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SetSuppressed(TrainRun run, bool suppressed, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.IsFinished)
            return PlanEditError.Finished;

        run.Suppressed = suppressed;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Pridá vlaku meškanie - na príchode, ak ešte nebol ohlásený, inak na odchode.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="extra">Prídavok k meškaniu, môže byť aj záporný.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError AddDelay(TrainRun run, TimeSpan extra, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.IsFinished)
            return PlanEditError.Finished;

        return run.PlannedArrival.HasValue && !run.IsDone(SimEventKind.Arrival)
            ? SetArrivalDelay(run, run.ArrivalDelay + extra, context)
            : SetDepartureDelay(run, run.DepartureDelay + extra, context);
    }

    /// <summary>
    ///     Ohlási udalosť hneď. Pri príchode a odchode sa tým zmení meškanie na rozdiel medzi aktuálnym
    ///     časom a grafikonom, vlaková cesta a dôvod meškania sa len posunú na aktuálny čas.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="kind">Druh udalosti.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError SendNow(TrainRun run, SimEventKind kind, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.IsFinished)
            return PlanEditError.Finished;
        if (run.Suppressed || !run.Events.Any(simEvent => simEvent.Kind == kind && !simEvent.IsDone))
            return PlanEditError.NotPending;

        switch (kind)
        {
            case SimEventKind.Arrival:
                return SetArrivalDelay(run, context.Now - run.PlannedArrival!.Value, context);

            case SimEventKind.Departure:
                var delay = context.Now - run.PlannedDeparture!.Value;
                if (!InRange(delay))
                    return PlanEditError.DelayOutOfRange;

                // Odchod "teraz" je výslovná požiadavka, minimálny pobyt sa neuplatní. Ak vlak ešte
                // neprišiel, príde v tej istej chvíli.
                if (run.PlannedArrival.HasValue && !run.IsDone(SimEventKind.Arrival))
                {
                    var arrival = context.Now - run.PlannedArrival.Value;
                    if (!InRange(arrival))
                        return PlanEditError.DelayOutOfRange;

                    run.ArrivalDelay = arrival;
                }

                run.DepartureDelay = delay;
                Rebuild(run, context);
                return PlanEditError.None;

            default:
                // Vlaková cesta a dôvod sa posunú, nové meškanie z nich nevzniká. Dôvod ide s cestou.
                var moved = run.Events
                    .Where(simEvent => !simEvent.IsDone &&
                                       (simEvent.Kind == kind || kind != SimEventKind.DelayReason &&
                                           simEvent.Kind == SimEventKind.DelayReason && RouteOf(run) == kind))
                    .ToList();

                run.ReplacePending(run.Events
                    .Where(simEvent => !simEvent.IsDone)
                    .Select(simEvent => moved.Contains(simEvent) && simEvent.Time > context.Now ? simEvent.At(context.Now) : simEvent)
                    .ToList());
                return PlanEditError.None;
        }
    }

    /// <summary>
    ///     Vráti jazde hodnoty, ktoré vylosoval plánovač, a zapne posielanie správ. Čo už bolo ohlásené,
    ///     ostane.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="context">Kontext zmeny.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public static PlanEditError Revert(TrainRun run, PlanEditContext context)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.IsFinished)
            return PlanEditError.Finished;

        var drawn = run.Drawn;

        if (!run.IsDone(SimEventKind.Arrival))
            run.ArrivalDelay = drawn.ArrivalDelay;

        if (run.PlannedDeparture.HasValue)
        {
            var minimum = run.PlannedArrival.HasValue ? MinDepartureDelay(run, run.ArrivalDelay) : MinDelay;
            run.DepartureDelay = drawn.DepartureDelay < minimum ? minimum : drawn.DepartureDelay;
        }

        run.Track = drawn.Track;
        run.TrackSource = drawn.TrackSource;
        run.DelayReason = drawn.DelayReason;
        run.Suppressed = false;
        Rebuild(run, context);

        return PlanEditError.None;
    }

    /// <summary>
    ///     Najmenšie meškanie na odchode pri danom meškaní na príchode - vlak smie skrátiť pobyt najviac
    ///     na <see cref="MinStay" />, kratší pobyt z grafikonu ostane.
    /// </summary>
    /// <param name="run">Jazda vlaku s príchodom aj odchodom.</param>
    /// <param name="arrivalDelay">Meškanie na príchode.</param>
    /// <returns>Najmenšie meškanie na odchode.</returns>
    public static TimeSpan MinDepartureDelay(TrainRun run, TimeSpan arrivalDelay)
    {
        ArgumentNullException.ThrowIfNull(run);

        var stay = run.PlannedDeparture!.Value - run.PlannedArrival!.Value;
        var kept = stay < MinStay ? stay : MinStay;

        return arrivalDelay - (stay - kept);
    }

    /// <summary>
    ///     Zostaví čakajúce udalosti jazdy nanovo podľa jej aktuálnych hodnôt a doplní dodatočné správy.
    /// </summary>
    /// <param name="run">Jazda vlaku.</param>
    /// <param name="context">Kontext zmeny.</param>
    internal static void Rebuild(TrainRun run, PlanEditContext context)
    {
        var pending = RunEvents.Build(run, context.Options)
            .Where(simEvent => !run.IsDone(simEvent.Kind))
            .ToList();

        if (!run.Suppressed)
            pending.AddRange(Corrections(run, pending, context.Now));

        run.ReplacePending(pending);
        RunEvents.UpdateOccupancy(run, context.Options);
    }

    private static List<SimEvent> Corrections(TrainRun run, List<SimEvent> pending, DateTime now)
    {
        var corrections = new List<SimEvent>();

        // Vlaková cesta už odišla s inou koľajou, než má vlak teraz, a pohyb, ku ktorému patrí, ešte nie.
        foreach (var (route, movement) in new[]
                 {
                     (SimEventKind.EntryRoute, SimEventKind.Arrival),
                     (SimEventKind.ExitRoute, SimEventKind.Departure)
                 })
        {
            var sent = run.LastDone(route);
            var next = pending.FirstOrDefault(simEvent => simEvent.Kind == movement);

            if (sent is null || next is null || sent.Track?.Key == run.Track.Key)
                continue;

            corrections.Add(new SimEvent(run, route, sent.PlannedTime, Earlier(now, next.Time), run.Track,
                isCorrection: true));
        }

        // Dôvod meškania už odišiel a medzitým sa zmenil.
        var sentReason = run.LastDone(SimEventKind.DelayReason);
        var nextMovement = pending
            .Where(simEvent => simEvent.Kind is SimEventKind.Arrival or SimEventKind.Departure)
            .MinBy(simEvent => simEvent.Time);

        if (sentReason is not null && nextMovement is not null && sentReason.DelayReason != run.DelayReason)
            corrections.Add(new SimEvent(run, SimEventKind.DelayReason, sentReason.PlannedTime,
                Earlier(now, nextMovement.Time), run.Track, run.DelayReason, true));

        return corrections;
    }

    /// <summary>
    ///     Vlaková cesta, s ktorou ide dôvod meškania - vjazdová, pri východzom vlaku odchodová.
    /// </summary>
    private static SimEventKind RouteOf(TrainRun run) =>
        run.PlannedArrival.HasValue ? SimEventKind.EntryRoute : SimEventKind.ExitRoute;

    private static DateTime Earlier(DateTime a, DateTime b) => a < b ? a : b;

    private static bool InRange(TimeSpan delay) => delay >= MinDelay && delay <= SimulationOptions.DelayCeiling;

    private static TimeSpan Clamp(TimeSpan delay) => delay > SimulationOptions.DelayCeiling ? SimulationOptions.DelayCeiling : delay;
}
