using GVDEditor.Domain.Entities;
using ILTISSimulator.Properties;
using ILTISSimulator.Tools;

namespace ILTISSimulator.Simulation;

/// <summary>
///     Jazda jedného vlaku stanicou v jeden deň tak, ako ju simulátor vylosoval a ako ju prípadne zmenil
///     používateľ.
/// </summary>
public sealed class TrainRun
{
    private readonly List<SimEvent> _events = [];

    internal TrainRun(Train train, DateTime? plannedArrival, DateTime? plannedDeparture)
    {
        Train = train;
        PlannedArrival = plannedArrival;
        PlannedDeparture = plannedDeparture;
        ArrivalNumber = train.Number;
        DepartureNumber = train.Number;
    }

    /// <summary>
    ///     Vlak z grafikonu.
    /// </summary>
    public Train Train { get; }

    /// <summary>
    ///     Číslo vlaku v správach o príchode. V INISS2 sa môže líšiť od čísla pri odchode.
    /// </summary>
    public string ArrivalNumber { get; internal init; }

    /// <summary>
    ///     Číslo vlaku v správach o odchode.
    /// </summary>
    public string DepartureNumber { get; internal init; }

    /// <summary>
    ///     Jazda patrí serveru INISS2: správy nesú pravidelný čas a dôvod meškania sa neposiela.
    /// </summary>
    public bool Iniss2 { get; internal init; }

    /// <summary>
    ///     Príchod podľa grafikonu, alebo <see langword="null" /> pri východzom vlaku.
    /// </summary>
    public DateTime? PlannedArrival { get; }

    /// <summary>
    ///     Odchod podľa grafikonu, alebo <see langword="null" /> pri končiacom vlaku.
    /// </summary>
    public DateTime? PlannedDeparture { get; }

    /// <summary>
    ///     Meškanie na príchode.
    /// </summary>
    public TimeSpan ArrivalDelay { get; internal set; }

    /// <summary>
    ///     Meškanie na odchode. Môže byť menšie než meškanie na príchode, ak vlak skrátil pobyt.
    /// </summary>
    public TimeSpan DepartureDelay { get; internal set; }

    /// <summary>
    ///     Skutočný príchod, teda čas, ktorý pôjde v správe 080-2.
    /// </summary>
    public DateTime? ActualArrival => PlannedArrival + ArrivalDelay;

    /// <summary>
    ///     Skutočný odchod, teda čas, ktorý pôjde v správe 080-3.
    /// </summary>
    public DateTime? ActualDeparture => PlannedDeparture + DepartureDelay;

    /// <summary>
    ///     Koľaj, na ktorú simulátor vlak postavil.
    /// </summary>
    public Track Track { get; internal set; } = null!;

    /// <summary>
    ///     Odkiaľ sa koľaj vzala.
    /// </summary>
    public TrackSource TrackSource { get; internal set; }

    /// <summary>
    ///     Dôvod meškania - poradové číslo v skupine <c>dz</c> zvukovej banky. Nula znamená, že sa dôvod
    ///     neposiela.
    /// </summary>
    public int DelayReason { get; internal set; }

    /// <summary>
    ///     O vlaku sa neposielajú správy. Rozhranie ILTIS-INISS nemá správu o zrušení vlaku, takže takto sa
    ///     dá vyskúšať, čo INISS urobí, keď mu o vlaku nič nepríde.
    /// </summary>
    public bool Suppressed { get; internal set; }

    /// <summary>
    ///     Hodnoty, ktoré vylosoval plánovač. Používateľ sa k nim môže vrátiť.
    /// </summary>
    public RunValues Drawn { get; internal set; } = null!;

    /// <summary>
    ///     Aktuálne hodnoty jazdy.
    /// </summary>
    public RunValues Current => new(ArrivalDelay, DepartureDelay, Track, TrackSource, DelayReason);

    /// <summary>
    ///     Či používateľ jazdu zmenil oproti tomu, čo vylosoval plánovač.
    /// </summary>
    public bool IsChanged => Suppressed || Current != Drawn;

    /// <summary>
    ///     Od kedy vlak drží koľaj - od postavenia vjazdovej cesty.
    /// </summary>
    public DateTime OccupiedFrom { get; internal set; }

    /// <summary>
    ///     Dokedy vlak drží koľaj - po odchode plus rezerva.
    /// </summary>
    public DateTime OccupiedTo { get; internal set; }

    /// <summary>
    ///     Všetky udalosti jazdy - už vydané, preskočené aj čakajúce - zoradené podľa času.
    /// </summary>
    public IReadOnlyList<SimEvent> Events => _events;

    /// <summary>
    ///     Posledná udalosť jazdy v stanici - odchod, pri končiacom vlaku príchod.
    /// </summary>
    public SimEventKind FinalKind => PlannedDeparture.HasValue ? SimEventKind.Departure : SimEventKind.Arrival;

    /// <summary>
    ///     Či už jazda v stanici skončila - jej posledná udalosť bola vydaná alebo preskočená. Takú jazdu
    ///     už nemá zmysel meniť.
    /// </summary>
    public bool IsFinished => IsDone(FinalKind);

    /// <summary>
    ///     Najbližšia čakajúca udalosť.
    /// </summary>
    public SimEvent? NextEvent => _events.FirstOrDefault(simEvent => !simEvent.IsDone);

    /// <summary>
    ///     Zistí, či už udalosť daného druhu prebehla - bola vydaná alebo preskočená.
    /// </summary>
    /// <param name="kind">Druh udalosti.</param>
    /// <returns><see langword="true" />, ak už udalosť daného druhu nečaká.</returns>
    public bool IsDone(SimEventKind kind) => _events.Any(simEvent => simEvent.Kind == kind && simEvent.IsDone);

    /// <summary>
    ///     Naposledy vydaná alebo preskočená udalosť daného druhu.
    /// </summary>
    /// <param name="kind">Druh udalosti.</param>
    /// <returns>Udalosť, alebo <see langword="null" />, ak žiadna taká ešte neprebehla.</returns>
    public SimEvent? LastDone(SimEventKind kind) => _events
        .Where(simEvent => simEvent.Kind == kind && simEvent.IsDone)
        .MaxBy(simEvent => simEvent.Sequence);

    /// <summary>
    ///     Nahradí všetky udalosti jazdy.
    /// </summary>
    internal void ResetEvents(IEnumerable<SimEvent> events)
    {
        _events.Clear();
        _events.AddRange(events);
        Sort();
    }

    /// <summary>
    ///     Nahradí čakajúce udalosti jazdy, vydané a preskočené ostanú.
    /// </summary>
    internal void ReplacePending(IEnumerable<SimEvent> events)
    {
        _events.RemoveAll(simEvent => !simEvent.IsDone);
        _events.AddRange(events);
        Sort();
    }

    private void Sort()
    {
        var sorted = _events.OrderBy(simEvent => simEvent.Time).ThenBy(simEvent => simEvent.Kind).ToList();

        _events.Clear();
        _events.AddRange(sorted);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Texts.F(Resources.Run_Format, Train.Type?.Key, Train.Number, Train.Routing?.CharSymbol, Track?.Key,
            Texts.Of(TrackSource)) + (Suppressed ? Resources.Run_Suppressed : "");
}
