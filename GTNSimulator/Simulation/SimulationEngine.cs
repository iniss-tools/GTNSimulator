using System.Diagnostics;
using GTNSimulator.Data;
using GTNSimulator.Properties;
using GTNSimulator.Tools;

namespace GTNSimulator.Simulation;

/// <summary>
///     Jadro simulátora: drží virtuálne hodiny, naplánovaný deň a vydáva udalosti v okamihu, keď na ne
///     príde čas. Samotné odoslanie správy nechá na volajúceho, ktorý si na <see cref="EventDue" /> pripojí
///     napríklad <see cref="Net.GtnClient" />.
/// </summary>
public sealed class SimulationEngine : IDisposable
{
    private const int TickMilliseconds = 200;

    private readonly object _sync = new();
    private readonly GvdData _gvd;
    private readonly Stopwatch _clock = new();

    private System.Threading.Timer? _timer;
    private DateTime _virtualStart;
    private int _ticking;
    private bool _disposed;

    /// <summary>
    ///     Vytvorí jadro simulátora nad grafikonom.
    /// </summary>
    /// <param name="gvd">Načítaný grafikon.</param>
    /// <param name="options">Nastavenia simulácie.</param>
    public SimulationEngine(GvdData gvd, SimulationOptions options)
    {
        _gvd = gvd ?? throw new ArgumentNullException(nameof(gvd));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();

        Messages = new GtnMessageBuilder(gvd.StationId, Options.SendLineTrack ? Options.LineTrack : null);
    }

    /// <summary>
    ///     Nastavenia simulácie.
    /// </summary>
    public SimulationOptions Options { get; }

    /// <summary>
    ///     Staviteľ správ pre stanicu grafikonu.
    /// </summary>
    public GtnMessageBuilder Messages { get; }

    /// <summary>
    ///     Naplánované jazdy vlakov, alebo <see langword="null" />, kým sa deň nenaplánoval.
    /// </summary>
    public IReadOnlyList<TrainRun>? Runs { get; private set; }

    /// <summary>
    ///     Časová os naplánovaného dňa.
    /// </summary>
    public SimulationTimeline? Timeline { get; private set; }

    /// <summary>
    ///     Deň, ktorý je naplánovaný.
    /// </summary>
    public DateTime Day { get; private set; }

    /// <summary>
    ///     Vlaky, ktoré plánovanie vynechalo, lebo v ten deň ide viac záznamov s rovnakým číslom - viď
    ///     <see cref="DayPlanner.SkippedDuplicates" />.
    /// </summary>
    public IReadOnlyList<GVDEditor.Domain.Entities.Train> SkippedDuplicates { get; private set; } = [];

    /// <summary>
    ///     Autobusy, ktoré plánovanie vynechalo, lebo ich INISS so správami GTN nepáruje - viď
    ///     <see cref="DayPlanner.SkippedBuses" />.
    /// </summary>
    public IReadOnlyList<GVDEditor.Domain.Entities.Train> SkippedBuses { get; private set; } = [];

    /// <summary>
    ///     Od kedy sa deň simuluje - skoršie udalosti sa pri plánovaní preskočili.
    /// </summary>
    public DateTime PlannedFrom { get; private set; }

    /// <summary>
    ///     Či simulácia práve beží.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _timer is not null;
        }
    }

    /// <summary>
    ///     Aktuálny virtuálny čas.
    /// </summary>
    public DateTime VirtualTime
    {
        get
        {
            lock (_sync)
                return CurrentTime();
        }
    }

    /// <summary>
    ///     Udalosť, ktorej čas nastal, aj s hotovou správou. Volá sa z vlákna časovača.
    /// </summary>
    public event EventHandler<SimEventDueEventArgs>? EventDue;

    /// <summary>
    ///     Priebežné hlásenia jadra.
    /// </summary>
    public event EventHandler<string>? Log;

    /// <summary>
    ///     Jazdy, ktoré sa zmenili úpravou plánu. Volá sa z vlákna, ktoré úpravu urobilo.
    /// </summary>
    public event EventHandler<PlanChangedEventArgs>? PlanChanged;

    /// <summary>
    ///     Naplánuje deň a nastaví virtuálne hodiny.
    /// </summary>
    /// <param name="day">Deň, ktorý sa má simulovať.</param>
    /// <param name="startAt">Čas, od ktorého sa má začať. Predvolene začiatok dňa; udalosti spred tohto času
    ///     sa preskočia, aby sa INISS nezaplavil dávno prešlými správami.</param>
    /// <param name="prepare">Zmeny jázd ešte pred preskočením udalostí - napríklad ručné zmeny zo scenára dňa,
    ///     ktoré tak platia, ako keby ich vylosoval plánovač.</param>
    public void PlanDay(DateTime day, DateTime? startAt = null, Action<IReadOnlyList<TrainRun>, PlanEditContext>? prepare = null)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            StopTimer();

            Day = day.Date;

            var planner = new DayPlanner(_gvd, Options);
            Runs = planner.Plan(Day);
            prepare?.Invoke(Runs, new PlanEditContext(Options, Day));
            Timeline = new SimulationTimeline(Runs);

            _virtualStart = startAt ?? Day;
            PlannedFrom = _virtualStart;
            _clock.Reset();

            var skipped = Timeline.SeekTo(_virtualStart);

            OnLog(Texts.F(Resources.Engine_Planned, Day, Runs.Count, Timeline.Total, skipped, Options.Seed));

            SkippedDuplicates = planner.SkippedDuplicates;
            SkippedBuses = planner.SkippedBuses;

            if (planner.SkippedDuplicates.Count > 0)
                OnLog(Texts.F(Resources.Engine_SkippedDuplicates,
                    string.Join(", ", planner.SkippedDuplicates.Select(train => train.Number).Distinct())));

            if (planner.SkippedBuses.Count > 0)
                OnLog(Texts.F(Resources.Engine_SkippedBuses,
                    string.Join(", ", planner.SkippedBuses.Select(train => train.Number).Distinct())));
        }
    }

    /// <summary>
    ///     Spustí alebo obnoví beh simulácie.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            ThrowIfDisposed();

            if (Timeline is null)
                throw new InvalidOperationException(Resources.Error_PlanFirst);

            if (_timer is not null)
                return;

            _clock.Start();
            _timer = new System.Threading.Timer(Tick, null, 0, TickMilliseconds);

            OnLog(Texts.F(Resources.Engine_Running, CurrentTime(), Options.TimeFactor));
        }
    }

    /// <summary>
    ///     Pozastaví beh. Virtuálny čas sa zastaví tam, kde je.
    /// </summary>
    public void Pause()
    {
        lock (_sync)
        {
            if (_timer is null)
                return;

            StopTimer();
            OnLog(Texts.F(Resources.Engine_Paused, CurrentTime()));
        }
    }

    /// <summary>
    ///     Zastaví beh a vráti simuláciu na čas, od ktorého bol deň naplánovaný - udalosti pred ním sa znova
    ///     preskočia. Úpravy plánu ostanú, dodatočné správy po nich sa zahodia - jazdy sa zostavia nanovo
    ///     z aktuálnych hodnôt.
    /// </summary>
    public void Stop()
    {
        lock (_sync)
        {
            StopTimer();

            _clock.Reset();
            _virtualStart = PlannedFrom;

            if (Runs is not null && Timeline is not null)
            {
                foreach (var run in Runs)
                {
                    run.ResetEvents(RunEvents.Build(run, Options));
                    RunEvents.UpdateOccupancy(run, Options);
                }

                Timeline.Reload();
                Timeline.SeekTo(PlannedFrom);
            }

            OnLog(Resources.Engine_Stopped);
        }

        if (Runs is not null)
            OnPlanChanged(Runs);
    }

    /// <summary>
    ///     Zmení jazdu vlaku. Zmena sa urobí pod zámkom jadra s aktuálnym virtuálnym časom a časová os
    ///     sa hneď zosúladí, takže sa dá meniť aj počas behu.
    /// </summary>
    /// <param name="run">Jazda vlaku z <see cref="Runs" />.</param>
    /// <param name="edit">Zmena, napríklad <see cref="PlanEditing.SetTrack" />.</param>
    /// <returns>Dôvod, prečo sa zmena nedala urobiť, alebo <see cref="PlanEditError.None" />.</returns>
    public PlanEditError Edit(TrainRun run, Func<TrainRun, PlanEditContext, PlanEditError> edit)
    {
        ArgumentNullException.ThrowIfNull(run);

        var failed = Edit([run], edit);

        return failed.TryGetValue(run, out var error) ? error : PlanEditError.None;
    }

    /// <summary>
    ///     Zmení viac jázd naraz - napríklad hromadné meškanie.
    /// </summary>
    /// <param name="runs">Jazdy vlakov z <see cref="Runs" />.</param>
    /// <param name="edit">Zmena jednej jazdy.</param>
    /// <returns>Jazdy, ktoré sa zmeniť nedali, s dôvodom.</returns>
    public IReadOnlyDictionary<TrainRun, PlanEditError> Edit(IEnumerable<TrainRun> runs,
        Func<TrainRun, PlanEditContext, PlanEditError> edit)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(edit);

        var failed = new Dictionary<TrainRun, PlanEditError>();
        var changed = new List<TrainRun>();

        lock (_sync)
        {
            ThrowIfDisposed();

            if (Timeline is null)
                throw new InvalidOperationException(Resources.Error_PlanFirst);

            var context = new PlanEditContext(Options, CurrentTime());

            foreach (var run in runs.Distinct())
            {
                var error = edit(run, context);
                if (error != PlanEditError.None)
                {
                    failed[run] = error;
                    continue;
                }

                Timeline.Sync(run);
                changed.Add(run);
            }
        }

        if (changed.Count > 0)
            OnPlanChanged(changed);

        return failed;
    }

    /// <summary>
    ///     Zmení zrýchlenie času tak, aby virtuálny čas neposkočil - ďalej plynie od svojej aktuálnej hodnoty.
    /// </summary>
    /// <param name="factor">Nové zrýchlenie.</param>
    public void SetTimeFactor(double factor)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), Resources.Error_TimeFactor);

        lock (_sync)
        {
            _virtualStart = CurrentTime();

            var running = _clock.IsRunning;
            _clock.Reset();
            if (running)
                _clock.Start();

            Options.TimeFactor = factor;
        }
    }

    /// <summary>
    ///     Najbližšia čakajúca udalosť dňa.
    /// </summary>
    /// <returns>Udalosť, alebo <see langword="null" />, ak už nič nečaká.</returns>
    public SimEvent? NextEvent()
    {
        lock (_sync)
            return Timeline?.Next;
    }

    /// <summary>
    ///     Koľko udalostí dňa už prebehlo a koľko ich je spolu.
    /// </summary>
    /// <returns>Počet vydaných a preskočených udalostí a počet všetkých udalostí.</returns>
    public (int Done, int Total) Progress()
    {
        lock (_sync)
            return Timeline is null ? (0, 0) : (Timeline.Done, Timeline.Total);
    }

    /// <summary>
    ///     Presunie virtuálny čas na zadaný okamih. Udalosti, ktoré tým preskočí, sa neodošlú.
    /// </summary>
    /// <param name="time">Nový virtuálny čas.</param>
    public void JumpTo(DateTime time)
    {
        lock (_sync)
        {
            ThrowIfDisposed();

            if (Timeline is null)
                throw new InvalidOperationException(Resources.Error_PlanFirst);

            var running = _timer is not null;
            StopTimer();

            _virtualStart = time;
            _clock.Reset();

            var skipped = Timeline.SeekTo(time);
            OnLog(Texts.F(Resources.Engine_Jumped, time, skipped));

            if (running)
            {
                _clock.Start();
                _timer = new System.Threading.Timer(Tick, null, 0, TickMilliseconds);
            }
        }
    }

    /// <summary>
    ///     Vydá udalosti, ktorých čas už nastal. Volá sa z časovača, ale dá sa zavolať aj ručne - vtedy
    ///     simulácia beží krokovo bez vlákien.
    /// </summary>
    /// <param name="time">Čas, po ktorý sa majú udalosti vydať.</param>
    /// <returns>Počet vydaných udalostí.</returns>
    public int Pump(DateTime time)
    {
        IReadOnlyList<SimEvent> due;

        lock (_sync)
        {
            if (Timeline is null)
                return 0;

            due = Timeline.DueUpTo(time);
        }

        foreach (var simEvent in due)
        {
            try
            {
                EventDue?.Invoke(this, new SimEventDueEventArgs(simEvent, Messages.Build(simEvent)));
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "Spracovanie udalosti {Event} zlyhalo", simEvent);
                OnLog(Texts.F(Resources.Engine_EventFailed, simEvent, e.Message));
            }
        }

        return due.Count;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            StopTimer();
            _disposed = true;
        }
    }

    private void Tick(object? state)
    {
        // Ak sa predchádzajúci tik ešte nedobehol, tento sa preskočí - udalosti nezmiznú, len počkajú.
        if (Interlocked.CompareExchange(ref _ticking, 1, 0) != 0)
            return;

        try
        {
            Pump(VirtualTime);
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "Tik simulacie zlyhal");
            OnLog(Texts.F(Resources.Engine_RunFailed, e.Message));
        }
        finally
        {
            Interlocked.Exchange(ref _ticking, 0);
        }
    }

    private DateTime CurrentTime() => _virtualStart + TimeSpan.FromTicks((long)(_clock.Elapsed.Ticks * Options.TimeFactor));

    private void StopTimer()
    {
        _clock.Stop();

        if (_timer is null)
            return;

        _timer.Dispose();
        _timer = null;
    }

    private void OnLog(string text) => Log?.Invoke(this, text);

    private void OnPlanChanged(IReadOnlyList<TrainRun> runs) => PlanChanged?.Invoke(this, new PlanChangedEventArgs(runs));

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
