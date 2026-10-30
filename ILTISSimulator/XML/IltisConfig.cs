using ILTISSimulator.Diagram;
using ILTISSimulator.Simulation;
using System.ComponentModel;
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
        DesktopCols = original.DesktopCols.Clone();
        Connection = original.Connection with { };
        Simulation = original.Simulation with { };
        ExcludedTracks = original.ExcludedTracks.Clone();
        View = original.View with { };
    }

    /// <summary>
    ///     Stĺpce tabuľky plánu.
    /// </summary>
    [XmlElement("DesktopCols")]
    public DesktopColumns DesktopCols { get; set; } = new();


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

/// <summary>
///     Spojenie s INISSom.
/// </summary>
public record ConnectionSettings
{
    /// <summary>
    ///     Adresa počítača s INISSom.
    /// </summary>
    [XmlElement("Host"), DefaultValue("127.0.0.1")]
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    ///     Port, na ktorom INISS počúva (<c>GTN\RecvPort</c> v registri INISSu).
    /// </summary>
    [XmlElement("Port"), DefaultValue(4000)]
    public int Port { get; set; } = 4000;

    /// <summary>
    ///     Sledovať log rozhrania INISSu a ku každej správe ukázať, ako ju INISS prijal.
    /// </summary>
    [XmlElement("WatchGtnLog"), DefaultValue(true)]
    public bool WatchGtnLog { get; set; } = true;

    /// <summary>
    ///     Cesta k logu rozhrania INISSu. Prázdna = nájsť podľa nastavení INISSu v registri.
    /// </summary>
    [XmlElement("GtnLog"), DefaultValue("")]
    public string GtnLog { get; set; } = "";
}

/// <summary>
///     Model náhody a beh simulácie. Pravdepodobnosti sú v percentách.
/// </summary>
public record SimulationSettings
{
    /// <summary>
    ///     Seed generátora náhody - rovnaký seed a deň dajú rovnaký plán.
    /// </summary>
    [XmlElement("Seed"), DefaultValue(1)]
    public int Seed { get; set; } = 1;

    /// <summary>
    ///     Zrýchlenie virtuálneho času.
    /// </summary>
    [XmlElement("Speed"), DefaultValue(60)]
    public int Speed { get; set; } = 60;

    /// <summary>
    ///     Pravdepodobnosť príchodu načas.
    /// </summary>
    [XmlElement("OnTime"), DefaultValue(75)]
    public int OnTimePercent { get; set; } = 75;

    /// <summary>
    ///     Stredné meškanie meškajúcich vlakov v minútach.
    /// </summary>
    [XmlElement("MeanDelay"), DefaultValue(8)]
    public int MeanDelayMinutes { get; set; } = 8;

    /// <summary>
    ///     Pravdepodobnosť, že vlak dostane inú koľaj než v grafikone.
    /// </summary>
    [XmlElement("TrackChange"), DefaultValue(7)]
    public int TrackChangePercent { get; set; } = 7;

    /// <summary>
    ///     Pravdepodobnosť, že meškajúci vlak dostane dôvod meškania.
    /// </summary>
    [XmlElement("DelayReason"), DefaultValue(60)]
    public int DelayReasonPercent { get; set; } = 60;

    /// <summary>
    ///     Koľko minút pred príchodom sa stavia vjazdová vlaková cesta.
    /// </summary>
    [XmlElement("EntryRouteLead"), DefaultValue(4)]
    public int EntryRouteLeadMinutes { get; set; } = 4;

    /// <summary>
    ///     Koľko minút pred odchodom sa stavia odchodová vlaková cesta.
    /// </summary>
    [XmlElement("ExitRouteLead"), DefaultValue(2)]
    public int ExitRouteLeadMinutes { get; set; } = 2;

    /// <summary>
    ///     Posielať v odchode traťovú koľaj, aby INISS nastavil udalosť <c>Odj</c> a vlak po odchode zmazal.
    /// </summary>
    [XmlElement("SendLineTrack"), DefaultValue(true)]
    public bool SendLineTrack { get; set; } = true;

    /// <summary>
    ///     Traťová koľaj odchodu.
    /// </summary>
    [XmlElement("LineTrack"), DefaultValue(SimulationOptions.DefaultLineTrack)]
    public string LineTrack { get; set; } = SimulationOptions.DefaultLineTrack;
}

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

    /// <summary>
    ///     Zobraziť panel ručnej správy.
    /// </summary>
    [XmlElement("ManualMessage"), DefaultValue(false)]
    public bool ManualMessage { get; set; }

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
