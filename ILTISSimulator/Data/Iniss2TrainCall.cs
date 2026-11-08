using ToolsCore.Iniss2.Grafikon;

namespace ILTISSimulator.Data;

/// <summary>
///     Zastavenie z grafikonu INISS2 k vlaku simulátora.
/// </summary>
/// <param name="Call">Zastavenie.</param>
/// <param name="DayOffset">O koľko dní po dni jazdy vlaku ide vlak stanicou.</param>
internal sealed record Iniss2TrainCall(StationCall Call, int DayOffset);
