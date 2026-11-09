using GTNSimulator.Simulation;

namespace GTNSimulator.Diagram;

/// <summary>
///     Pruh vlaku v koľajovom diagrame - jazda a riadok v rámci koľaje.
/// </summary>
/// <param name="Run">Jazda vlaku.</param>
/// <param name="Row">Riadok v koľaji - viac riadkov má koľaj len pri súbehu vlakov.</param>
public sealed record DiagramBar(TrainRun Run, int Row);
