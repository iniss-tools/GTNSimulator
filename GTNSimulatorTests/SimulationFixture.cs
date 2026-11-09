using System.Reflection;
using GTNSimulator.Data;

namespace GTNSimulatorTests;

/// <summary>
///     Spoločný testovací grafikon z priečinka <c>TestData</c>.
/// </summary>
internal static class SimulationFixture
{
    private static GvdData? _gvd;

    /// <summary>
    ///     Prvý deň platnosti testovacieho grafikonu.
    /// </summary>
    public static readonly DateTime FirstDay = new(2024, 1, 1);

    /// <summary>
    ///     Priečinok s testovacím grafikonom.
    /// </summary>
    public static string TestDataDir =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "TestData");

    /// <summary>
    ///     Načítaný testovací grafikon.
    /// </summary>
    public static GvdData Gvd => _gvd ??= GvdLightReader.Read(TestDataDir);
}
