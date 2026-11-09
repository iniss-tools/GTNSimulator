using GVDEditor.Domain.Entities;
using GTNSimulator.Properties;
using GTNSimulator.Tools;

namespace GTNSimulator.Simulation;

/// <summary>
///     Jedna naplánovaná udalosť simulácie - to, čo sa v danom čase odošle INISSu.
/// </summary>
public sealed class SimEvent
{
    internal SimEvent(TrainRun run, SimEventKind kind, DateTime plannedTime, DateTime time, Track track,
        int delayReason = 0, bool isCorrection = false)
    {
        Run = run;
        Kind = kind;
        PlannedTime = plannedTime;
        Time = time;
        Track = track;
        DelayReason = delayReason;
        IsCorrection = isCorrection;
    }

    /// <summary>
    ///     Jazda vlaku, ku ktorej udalosť patrí.
    /// </summary>
    public TrainRun Run { get; }

    /// <summary>
    ///     Druh udalosti.
    /// </summary>
    public SimEventKind Kind { get; }

    /// <summary>
    ///     Čas udalosti podľa grafikonu.
    /// </summary>
    public DateTime PlannedTime { get; }

    /// <summary>
    ///     Čas, ktorý pôjde v správe. Rozdiel oproti grafikonu je to, čo si INISS pri správach 080-2 a 080-3
    ///     odvodí ako meškanie.
    /// </summary>
    public DateTime Time { get; }

    /// <summary>
    ///     Koľaj, ktorá pôjde v správe.
    /// </summary>
    public Track Track { get; }

    /// <summary>
    ///     Poradové číslo dôvodu meškania pri <see cref="SimEventKind.DelayReason" />. Nula dôvod v INISSe
    ///     zmaže.
    /// </summary>
    public int DelayReason { get; }

    /// <summary>
    ///     Dodatočná správa po zmene plánu - nová vlaková cesta po zmene koľaje alebo nový dôvod meškania,
    ///     keď už pôvodná správa odišla.
    /// </summary>
    public bool IsCorrection { get; }

    /// <summary>
    ///     Stav udalosti.
    /// </summary>
    public SimEventState State { get; internal set; }

    /// <summary>
    ///     Poradie, v ktorom udalosť prestala čakať. Podľa neho sa hľadá naposledy odoslaná správa daného
    ///     druhu, lebo čas v správe nemusí s poradím odoslania súhlasiť.
    /// </summary>
    internal long Sequence { get; set; }

    /// <summary>
    ///     Či už udalosť nečaká - bola vydaná alebo preskočená.
    /// </summary>
    public bool IsDone => State != SimEventState.Pending;

    /// <summary>
    ///     Posun oproti grafikonu.
    /// </summary>
    public TimeSpan Delay => Time - PlannedTime;

    /// <summary>
    ///     Kópia čakajúcej udalosti s iným časom.
    /// </summary>
    /// <param name="time">Nový čas.</param>
    /// <returns>Nová udalosť.</returns>
    internal SimEvent At(DateTime time) => new(Run, Kind, PlannedTime, time, Track, DelayReason, IsCorrection);

    /// <inheritdoc />
    public override string ToString() =>
        Texts.F(Resources.Event_Format, Time, Texts.Of(Kind), Run.Train.Number, Track?.Key) +
        (Delay == TimeSpan.Zero || Kind == SimEventKind.DelayReason ? "" : Texts.F(Resources.Event_Delay, Delay.TotalMinutes)) +
        (Kind == SimEventKind.DelayReason ? Texts.F(Resources.Event_Reason, DelayReason) : "");
}
