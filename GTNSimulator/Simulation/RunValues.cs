using GVDEditor.Domain.Entities;

namespace GTNSimulator.Simulation;

/// <summary>
///     Hodnoty jazdy, ktoré sa dajú meniť - meškania, koľaj a dôvod meškania.
/// </summary>
/// <param name="ArrivalDelay">Meškanie na príchode.</param>
/// <param name="DepartureDelay">Meškanie na odchode.</param>
/// <param name="Track">Koľaj. Koľaje grafikonu sa porovnávajú referenciou.</param>
/// <param name="TrackSource">Odkiaľ sa koľaj vzala.</param>
/// <param name="DelayReason">Dôvod meškania, nula = bez dôvodu.</param>
public sealed record RunValues(TimeSpan ArrivalDelay, TimeSpan DepartureDelay, Track Track, TrackSource TrackSource, int DelayReason);
