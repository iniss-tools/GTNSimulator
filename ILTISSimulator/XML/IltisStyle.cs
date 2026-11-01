using ToolsCore.XML;

namespace ILTISSimulator.XML;

/// <summary>
/// Farebna schema ILTISSimulatora (len ovladacie prvky).
/// </summary>
public class IltisStyle : Style
{
    /// <inheritdoc />
    protected override Style CreateEmpty() => new IltisStyle();
}
