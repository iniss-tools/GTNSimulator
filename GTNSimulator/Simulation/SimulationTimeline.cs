namespace GTNSimulator.Simulation;

/// <summary>
///     Udalosti celého dňa. Čakajúce udalosti drží zoradené podľa času, vydané a preskočené si pamätajú
///     jazdy. Neobsahuje žiadne vlákna ani hodiny - posun času jej dodáva volajúci.
/// </summary>
public sealed class SimulationTimeline
{
    private readonly List<SimEvent> _pending = [];
    private long _sequence;

    /// <summary>
    ///     Zostaví časovú os z jázd vlakov.
    /// </summary>
    /// <param name="runs">Jazdy vlakov.</param>
    public SimulationTimeline(IReadOnlyList<TrainRun> runs)
    {
        Runs = runs ?? throw new ArgumentNullException(nameof(runs));

        Reload();
    }

    /// <summary>
    ///     Jazdy vlakov dňa.
    /// </summary>
    public IReadOnlyList<TrainRun> Runs { get; }

    /// <summary>
    ///     Všetky udalosti dňa - vydané, preskočené aj čakajúce - zoradené podľa času.
    /// </summary>
    public IReadOnlyList<SimEvent> Events => Runs
        .SelectMany(run => run.Events)
        .OrderBy(simEvent => simEvent.Time)
        .ThenBy(simEvent => simEvent.Kind)
        .ToList();

    /// <summary>
    ///     Počet všetkých udalostí dňa.
    /// </summary>
    public int Total => Runs.Sum(run => run.Events.Count);

    /// <summary>
    ///     Počet udalostí, ktoré už nečakajú - boli vydané alebo preskočené.
    /// </summary>
    public int Done => Total - _pending.Count;

    /// <summary>
    ///     Či sú všetky udalosti dňa za nami.
    /// </summary>
    public bool IsFinished => _pending.Count == 0;

    /// <summary>
    ///     Čas najbližšej čakajúcej udalosti.
    /// </summary>
    public DateTime? NextTime => Next?.Time;

    /// <summary>
    ///     Najbližšia čakajúca udalosť.
    /// </summary>
    public SimEvent? Next => IsFinished ? null : _pending[0];

    /// <summary>
    ///     Vydá všetky čakajúce udalosti, ktorých čas už nastal.
    /// </summary>
    /// <param name="time">Aktuálny virtuálny čas.</param>
    /// <returns>Udalosti na odoslanie, zoradené podľa času.</returns>
    public IReadOnlyList<SimEvent> DueUpTo(DateTime time) => Take(time, SimEventState.Emitted);

    /// <summary>
    ///     Preskočí čakajúce udalosti po zadaný čas bez toho, aby sa vydali. Používa sa pri štarte simulácie
    ///     uprostred dňa a pri skoku, aby sa INISS nezaplavil udalosťami, ktoré už dávno prešli.
    /// </summary>
    /// <param name="time">Čas, na ktorý sa má os nastaviť.</param>
    /// <returns>Počet preskočených udalostí.</returns>
    public int SeekTo(DateTime time) => Take(time, SimEventState.Skipped).Count;

    /// <summary>
    ///     Zosúladí os s čakajúcimi udalosťami jazdy po jej zmene.
    /// </summary>
    /// <param name="run">Zmenená jazda.</param>
    public void Sync(TrainRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        _pending.RemoveAll(simEvent => ReferenceEquals(simEvent.Run, run));

        foreach (var simEvent in run.Events.Where(simEvent => !simEvent.IsDone))
            Insert(simEvent);
    }

    /// <summary>
    ///     Načíta čakajúce udalosti zo všetkých jázd nanovo - po plánovaní alebo po vrátení dňa na začiatok.
    /// </summary>
    public void Reload()
    {
        _pending.Clear();
        _pending.AddRange(Runs
            .SelectMany(run => run.Events)
            .Where(simEvent => !simEvent.IsDone)
            .OrderBy(simEvent => simEvent.Time)
            .ThenBy(simEvent => simEvent.Kind));
    }

    private List<SimEvent> Take(DateTime time, SimEventState state)
    {
        var count = 0;
        while (count < _pending.Count && _pending[count].Time <= time)
            count++;

        var taken = _pending.GetRange(0, count);
        _pending.RemoveRange(0, count);

        foreach (var simEvent in taken)
        {
            simEvent.State = state;
            simEvent.Sequence = ++_sequence;
        }

        return taken;
    }

    /// <summary>
    ///     Vloží udalosť za všetky čakajúce s rovnakým alebo skorším časom a druhom, aby sa poradie
    ///     rovnocenných udalostí nemenilo.
    /// </summary>
    private void Insert(SimEvent simEvent)
    {
        var index = _pending.FindIndex(other =>
            other.Time > simEvent.Time || other.Time == simEvent.Time && other.Kind > simEvent.Kind);

        _pending.Insert(index < 0 ? _pending.Count : index, simEvent);
    }
}
