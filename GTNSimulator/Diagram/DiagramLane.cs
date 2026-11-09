using GVDEditor.Domain.Entities;

namespace GTNSimulator.Diagram;

/// <summary>
///     Koľaj v koľajovom diagrame s pruhmi vlakov, ktoré ju držia.
/// </summary>
/// <param name="Track">Koľaj.</param>
/// <param name="Excluded">Koľaj je vynechaná z plánovania - simulátor na ňu sám vlaky nestavia.</param>
/// <param name="Rows">Počet riadkov, aspoň jeden.</param>
/// <param name="Bars">Pruhy vlakov zoradené podľa začiatku obsadenia.</param>
public sealed record DiagramLane(Track Track, bool Excluded, int Rows, IReadOnlyList<DiagramBar> Bars);
