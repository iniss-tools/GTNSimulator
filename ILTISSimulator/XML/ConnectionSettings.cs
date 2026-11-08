using System.ComponentModel;
using System.Xml.Serialization;

namespace ILTISSimulator.XML;

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
