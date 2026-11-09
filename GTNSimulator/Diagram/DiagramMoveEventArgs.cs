using GVDEditor.Domain.Entities;
using GTNSimulator.Simulation;

namespace GTNSimulator.Diagram;

/// <summary>
///     Presun vlaka v koľajovom diagrame.
/// </summary>
/// <param name="run">Jazda vlaku.</param>
/// <param name="track">Nová koľaj, alebo <see langword="null" />, ak sa koľaj nemení.</param>
/// <param name="minutes">O koľko minút sa vlak posunul, záporné = skôr.</param>
internal sealed class DiagramMoveEventArgs(TrainRun run, Track? track, int minutes) : EventArgs
{
    /// <summary>
    ///     Jazda vlaku.
    /// </summary>
    public TrainRun Run { get; } = run;

    /// <summary>
    ///     Nová koľaj, alebo <see langword="null" />, ak sa koľaj nemení.
    /// </summary>
    public Track? Track { get; } = track;

    /// <summary>
    ///     O koľko minút sa vlak posunul.
    /// </summary>
    public int Minutes { get; } = minutes;
}
