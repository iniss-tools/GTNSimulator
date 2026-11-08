namespace ILTISSimulator.Simulation;

/// <summary>
///     Údaje udalosti <see cref="SimulationEngine.PlanChanged" />.
/// </summary>
public sealed class PlanChangedEventArgs : EventArgs
{
    internal PlanChangedEventArgs(IReadOnlyList<TrainRun> runs) => Runs = runs;

    /// <summary>
    ///     Zmenené jazdy.
    /// </summary>
    public IReadOnlyList<TrainRun> Runs { get; }
}
