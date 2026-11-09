using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace GTNSimulator.XML;

/// <summary>
///     Koľaje vylúčené z prideľovania pre každú stanicu zvlášť - označenia koľají sa medzi grafikonmi líšia.
///     V <c>config.xml</c> je každá stanica prvok <c>&lt;Station id="5613600"&gt;BUS,VYLUKA&lt;/Station&gt;</c>.
/// </summary>
public sealed class ExcludedTracks : IXmlSerializable
{
    private const string ElementStation = "Station";
    private const string AttrId = "id";

    private readonly Dictionary<int, HashSet<string>> _stations = new();

    /// <summary>
    ///     Vylúčené koľaje stanice.
    /// </summary>
    /// <param name="stationId">Identifikátor stanice z grafikonu.</param>
    /// <returns>Kľúče koľají - nová množina, zmena sa neprejaví bez <see cref="Set" />.</returns>
    public ISet<string> Get(int stationId) =>
        new HashSet<string>(_stations.TryGetValue(stationId, out var keys) ? keys : [], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Nastaví vylúčené koľaje stanice.
    /// </summary>
    /// <param name="stationId">Identifikátor stanice z grafikonu.</param>
    /// <param name="keys">Kľúče koľají.</param>
    public void Set(int stationId, IEnumerable<string> keys)
    {
        var set = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);

        if (set.Count == 0)
            _stations.Remove(stationId);
        else
            _stations[stationId] = set;
    }

    /// <summary>
    ///     Nezávislá kópia.
    /// </summary>
    /// <returns>Kópia.</returns>
    public ExcludedTracks Clone()
    {
        var copy = new ExcludedTracks();
        foreach (var (id, keys) in _stations)
            copy._stations[id] = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);

        return copy;
    }

    XmlSchema? IXmlSerializable.GetSchema() => null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        _stations.Clear();
        var empty = reader.IsEmptyElement;
        reader.ReadStartElement();
        if (empty)
            return;

        reader.MoveToContent();
        while (reader.NodeType != XmlNodeType.EndElement && !reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == ElementStation &&
                int.TryParse(reader.GetAttribute(AttrId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                Set(id, reader.ReadElementContentAsString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                reader.Skip();
            }
            else
            {
                reader.Read();
            }

            reader.MoveToContent();
        }

        reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        foreach (var (id, keys) in _stations.OrderBy(pair => pair.Key))
        {
            writer.WriteStartElement(ElementStation);
            writer.WriteAttributeString(AttrId, id.ToString(CultureInfo.InvariantCulture));
            writer.WriteString(string.Join(",", keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase)));
            writer.WriteEndElement();
        }
    }
}
