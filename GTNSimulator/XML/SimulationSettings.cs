using GTNSimulator.Simulation;
using System.ComponentModel;
using System.Xml.Serialization;

namespace GTNSimulator.XML;

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
