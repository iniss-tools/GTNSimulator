using GVDEditor.Domain.Entities;
using ILTISSimulator.Data;
using ILTISSimulator.Properties;

namespace ILTISSimulator.Simulation;

/// <summary>
///     Zostaví plán jedného dňa: pre každý vlak grafikonu vylosuje meškanie a koľaj a vyrobí udalosti,
///     ktoré sa počas dňa odošlú INISSu.
/// </summary>
public sealed class DayPlanner
{
    private readonly GvdData _gvd;
    private readonly SimulationOptions _options;

    /// <summary>
    ///     Vytvorí plánovač nad grafikonom.
    /// </summary>
    /// <param name="gvd">Načítaný grafikon.</param>
    /// <param name="options">Nastavenia simulácie.</param>
    public DayPlanner(GvdData gvd, SimulationOptions options)
    {
        _gvd = gvd ?? throw new ArgumentNullException(nameof(gvd));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _options.Validate();
    }

    /// <summary>
    ///     Vlaky, ktoré posledné plánovanie vynechalo, lebo v ten deň ide viac záznamov s rovnakým číslom.
    /// </summary>
    /// <remarks>
    ///     INISS páruje správy s vlakom iba podľa čísla a stanice, takže z niekoľkých záznamov s rovnakým
    ///     číslom vezme ten svoj prvý. Overené naživo na vlaku 617 v grafikone Košíc, ktorý je v grafikone
    ///     dvakrát - ako končiaci a ako prechádzajúci pod pokračovaním 964. Správa poslaná podľa toho druhého
    ///     dostala od INISSu meškanie 43 minút, lebo si ju porovnal s časom toho prvého. Simulátor preto
    ///     posiela iba jednu súvislú sadu udalostí na číslo vlaku - záznam s najnižším identifikátorom,
    ///     rovnako ako si ho vyberá INISS. Server INISS2 páruje aj podľa pravidelného času, takže vlaky s rovnakým
    ///     číslom v inom čase sú rôzne jazdy.
    /// </remarks>
    public IReadOnlyList<Train> SkippedDuplicates { get; private set; } = new List<Train>();

    /// <summary>
    ///     Autobusy, ktoré posledné plánovanie vynechalo - viď <see cref="GvdData.IsBus" />.
    /// </summary>
    /// <remarks>
    ///     INISS pri párovaní správy s vlakom preskakuje vlaky druhu <c>Bus</c>, takže by každú správu o nich
    ///     odmietol s <c>train … not found</c>. Vlak s rovnakým číslom, ktorý autobusom nie je, sa ďalej páruje.
    /// </remarks>
    public IReadOnlyList<Train> SkippedBuses { get; private set; } = new List<Train>();

    /// <summary>
    ///     Naplánuje jeden deň. Rovnaký deň a rovnaký <see cref="SimulationOptions.Seed" /> dajú vždy rovnaký
    ///     výsledok.
    /// </summary>
    /// <param name="day">Deň, ktorý sa má naplánovať.</param>
    /// <returns>Jazdy vlakov zoradené podľa prvej udalosti.</returns>
    public IReadOnlyList<TrainRun> Plan(DateTime day)
    {
        var random = new Random(_options.Seed);
        var delays = new DelayModel(_options, random);
        var allocator = new TrackAllocator(_gvd.Tracks, _options.ExcludedTrackKeys);

        if (allocator.Tracks.Count == 0)
            throw new InvalidOperationException(
                Resources.Error_NoTrackLeft);

        var trains = _gvd.TrainsOn(day).OrderBy(train => train.ID).ToList();

        SkippedBuses = trains.Where(GvdData.IsBus).ToList();

        var byNumber = trains
            .Where(train => !GvdData.IsBus(train))
            .GroupBy(train => _gvd.IsIniss2 ? (train.Number, train.Arrival, train.Departure) : (train.Number, null, null))
            .ToList();

        SkippedDuplicates = byNumber.SelectMany(group => group.Skip(1)).ToList();

        var runs = byNumber
            .Select(group => group.First())
            .Select(train => new TrainRun(train, GvdData.ArrivalOn(train, day), GvdData.DepartureOn(train, day))
            {
                ArrivalNumber = _gvd.ArrivalNumber(train),
                DepartureNumber = _gvd.DepartureNumber(train),
                Iniss2 = _gvd.IsIniss2
            })
            .Where(run => run.PlannedArrival.HasValue || run.PlannedDeparture.HasValue)
            .OrderBy(run => run.PlannedArrival ?? run.PlannedDeparture)
            .ThenBy(run => run.Train.ID)
            .ToList();

        foreach (var run in runs)
        {
            AssignDelays(run, delays);
            AssignDelayReason(run, random);
            AssignTrack(run, _gvd.TrackOn(run.Train, day), allocator, random);

            run.Drawn = run.Current;
            run.ResetEvents(RunEvents.Build(run, _options));
        }

        return runs;
    }

    private static void AssignDelays(TrainRun run, DelayModel delays)
    {
        if (run.PlannedArrival.HasValue)
        {
            run.ArrivalDelay = delays.NextArrivalDelay();

            if (run.PlannedDeparture.HasValue)
            {
                var stay = run.PlannedDeparture.Value - run.PlannedArrival.Value;
                run.DepartureDelay = delays.NextDepartureDelay(run.ArrivalDelay, stay);
            }

            return;
        }

        // Východzí vlak nemá príchod, meškanie sa mu losuje priamo na odchode.
        run.DepartureDelay = delays.NextArrivalDelay();
    }

    private void AssignDelayReason(TrainRun run, Random random)
    {
        var delay = run.PlannedArrival.HasValue ? run.ArrivalDelay : run.DepartureDelay;

        if (_options.DelayReasons.Count == 0 || delay < _options.MinDelayForReason)
            return;

        if (random.NextDouble() >= _options.DelayReasonProbability)
            return;

        run.DelayReason = _options.DelayReasons[random.Next(_options.DelayReasons.Count)];
    }

    private void AssignTrack(TrainRun run, Track? timetable, TrackAllocator allocator, Random random)
    {
        RunEvents.UpdateOccupancy(run, _options);

        var planned = GvdData.IsRealTrack(timetable) && TrackAllocator.CanUse(timetable)
            ? timetable
            : null;

        if (planned is not null && !allocator.IsManaged(planned))
        {
            // Koľaj z grafikonu je vylúčená z prideľovania. Grafikon neprepisujeme, len ju neobsadzujeme.
            run.Track = planned;
            run.TrackSource = TrackSource.Timetable;
            return;
        }

        if (planned is null)
        {
            var assigned = allocator.ReserveAny(run.OccupiedFrom, run.OccupiedTo, random);
            run.Track = assigned ?? timetable ?? Track.None;
            run.TrackSource = assigned is null ? TrackSource.Conflict : TrackSource.Assigned;
            return;
        }

        if (random.NextDouble() < _options.TrackChangeProbability)
        {
            var changed = allocator.ReserveAny(run.OccupiedFrom, run.OccupiedTo, random, planned, planned);
            if (changed is not null)
            {
                run.Track = changed;
                run.TrackSource = TrackSource.Changed;
                return;
            }
        }

        if (allocator.TryReserve(planned, run.OccupiedFrom, run.OccupiedTo))
        {
            run.Track = planned;
            run.TrackSource = TrackSource.Timetable;
            return;
        }

        // Koľaj z grafikonu je obsadená - vlak musí ísť inam.
        var replacement = allocator.ReserveAny(run.OccupiedFrom, run.OccupiedTo, random, planned, planned);

        run.Track = replacement ?? planned;
        run.TrackSource = replacement is null ? TrackSource.Conflict : TrackSource.Changed;
    }
}
