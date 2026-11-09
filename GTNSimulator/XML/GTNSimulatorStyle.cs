using ToolsCore.XML;

namespace GTNSimulator.XML;

/// <summary>
/// Farebna schema GTNSimulatora (len ovladacie prvky).
/// </summary>
public class GTNSimulatorStyle : Style
{
    /// <inheritdoc />
    protected override Style CreateEmpty() => new GTNSimulatorStyle();
}
