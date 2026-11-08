using System.Xml.Serialization;
using ToolsCore.XML;

namespace ILTISSimulator.XML;

/// <summary>
///     Nastavenia programu v <c>config.xml</c>.
/// </summary>
[XmlRoot("CONFIG")]
public record IltisConfig : ConfigBase
{
    /// <summary>
    ///     Predvolené nastavenia.
    /// </summary>
    public IltisConfig()
    {
    }

    /// <summary>
    ///     Kópia nastavení (okno Nastavenia programu) - vnorené časti sa nesmú zdieľať s pôvodnými.
    /// </summary>
    protected IltisConfig(IltisConfig original) : base(original)
    {
        Connection = original.Connection with { };
        Simulation = original.Simulation with { };
        ExcludedTracks = original.ExcludedTracks.Clone();
        View = original.View with { };
    }



    /// <summary>
    ///     Spojenie s INISSom.
    /// </summary>
    [XmlElement("Connection")]
    public ConnectionSettings Connection { get; set; } = new();

    /// <summary>
    ///     Model náhody a beh simulácie.
    /// </summary>
    [XmlElement("Simulation")]
    public SimulationSettings Simulation { get; set; } = new();

    /// <summary>
    ///     Koľaje vylúčené z prideľovania, pre každú stanicu zvlášť.
    /// </summary>
    [XmlElement("ExcludedTracks")]
    public ExcludedTracks ExcludedTracks { get; set; } = new();

    /// <summary>
    ///     Zobrazenie hlavného okna.
    /// </summary>
    [XmlElement("View")]
    public ViewSettings View { get; set; } = new();

    /// <inheritdoc />
    public override string LinkAppSettingsGuide => "https://iniss-tools.fvat.sk/docs/iltissimulator/nastavenia-programu/";
}
