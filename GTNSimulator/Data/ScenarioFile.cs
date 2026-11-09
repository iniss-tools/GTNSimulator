using System.Text.Json;
using System.Text.Json.Serialization;
using GTNSimulator.Simulation;

namespace GTNSimulator.Data;

/// <summary>
///     Súbor scenára dňa (<c>*.gtn</c>) - JSON s kľúčmi podľa <see cref="DayScenario" />.
/// </summary>
public static class ScenarioFile
{
    /// <summary>
    ///     Prípona súboru scenára.
    /// </summary>
    public const string Extension = ".gtn";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    ///     Zapíše scenár do súboru.
    /// </summary>
    /// <param name="path">Cesta k súboru.</param>
    /// <param name="scenario">Scenár.</param>
    public static void Write(string path, DayScenario scenario) =>
        File.WriteAllText(path, JsonSerializer.Serialize(scenario, Options), new UTF8Encoding(false));

    /// <summary>
    ///     Prečíta scenár zo súboru.
    /// </summary>
    /// <param name="path">Cesta k súboru.</param>
    /// <returns>Scenár.</returns>
    /// <exception cref="JsonException">Ak súbor nie je scenár.</exception>
    public static DayScenario Read(string path) =>
        JsonSerializer.Deserialize<DayScenario>(File.ReadAllText(path), Options) ?? throw new JsonException(path);
}
