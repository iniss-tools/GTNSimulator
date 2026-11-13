using GTNSimulator.Diagram;
using System.ComponentModel;
using System.Xml.Serialization;

namespace GTNSimulator.XML;

/// <summary>
///     Zobrazenie hlavného okna - zapamätá sa medzi spusteniami.
/// </summary>
public record ViewSettings
{
    /// <summary>
    ///     Posielať udalosti do INISSu. Vypnuté = len zápis do záznamu.
    /// </summary>
    [XmlElement("SendToIniss"), DefaultValue(true)]
    public bool SendToIniss { get; set; } = true;

    // panel rucnej spravy a zaznam su v ConfigBase.DockLayout (predtym ManualMessage - starsi config.xml ho obsahuje,
    // XmlSerializer nezname prvky vynecha)

    /// <summary>
    ///     Skryť vlaky, ktorých jazda v stanici už skončila.
    /// </summary>
    [XmlElement("HideFinished"), DefaultValue(false)]
    public bool HideFinished { get; set; }

    /// <summary>
    ///     Posúvať tabuľku na vlak, o ktorom práve odišla správa.
    /// </summary>
    [XmlElement("FollowCurrent"), DefaultValue(true)]
    public bool FollowCurrent { get; set; } = true;

    /// <summary>
    ///     Okno navrchu ostatných.
    /// </summary>
    [XmlElement("TopMost"), DefaultValue(false)]
    public bool TopMost { get; set; }

    /// <summary>
    ///     Zobraziť koľajový diagram pod tabuľkou plánu.
    /// </summary>
    [XmlElement("TrackDiagram"), DefaultValue(false)]
    public bool TrackDiagram { get; set; }

    /// <summary>
    ///     Mierka koľajového diagramu - šírka hodiny v bodoch pri 96 DPI.
    /// </summary>
    [XmlElement("DiagramHourWidth"), DefaultValue(DiagramScale.DefaultHourWidth)]
    public int DiagramHourWidth { get; set; } = DiagramScale.DefaultHourWidth;

    /// <summary>
    ///     Podiel výšky tabuľky plánu v percentách, keď je pod ňou koľajový diagram.
    /// </summary>
    [XmlElement("DiagramSplit"), DefaultValue(55)]
    public int DiagramSplit { get; set; } = 55;
}
