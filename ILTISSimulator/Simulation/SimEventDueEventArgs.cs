using ILTISSimulator.Gtn;

namespace ILTISSimulator.Simulation;

/// <summary>
///     Údaje udalosti <see cref="SimulationEngine.EventDue" />.
/// </summary>
public sealed class SimEventDueEventArgs : EventArgs
{
    internal SimEventDueEventArgs(SimEvent simEvent, GtnMessage message)
    {
        SimEvent = simEvent;
        Message = message;
    }

    /// <summary>
    ///     Udalosť simulácie.
    /// </summary>
    public SimEvent SimEvent { get; }

    /// <summary>
    ///     Hotová správa pripravená na odoslanie.
    /// </summary>
    public GtnMessage Message { get; }
}
