namespace ILTISSimulator.Data;

/// <summary>
///     Stanica INISS2 na výber.
/// </summary>
/// <param name="Group">Názov grafikonu.</param>
/// <param name="Name">Názov priečinka stanice.</param>
/// <param name="Directory">Priečinok stanice.</param>
public sealed record Iniss2Station(string Group, string Name, string Directory)
{
    /// <inheritdoc />
    public override string ToString() => Group + " / " + Name;
}
