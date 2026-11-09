using GTNSimulator.Properties;
using GTNSimulator.Tools;
using ToolsCore.Iniss.Entities;
using ToolsCore.Iniss.Tools;

namespace GTNSimulator.Data;

/// <summary>
///     Načítanie dôvodov meškania zo zvukovej banky stanice.
/// </summary>
/// <remarks>
///     Zoznam dôvodov nie je v grafikone - je to skupina <c>dz</c> zvukovej banky a správa 080-5 do nej
///     posiela poradové číslo. Poradie zo <c>FyzZvuk.dat</c> zodpovedá číslovaniu, ktoré používa INISS;
///     overené naživo proti banke z Bardejova, kde 01 až 04 vrátili Nehoda, Počasie, Prevádzkové dôvody
///     a Technické príčiny.
/// </remarks>
public static class DelayReasonReader
{
    /// <summary>
    ///     Názov skupiny zvukovej banky, v ktorej sú dôvody meškania.
    /// </summary>
    public const string SoundGroup = "dz";

    /// <summary>
    ///     Názov priečinka so zvukovou bankou vedľa priečinka DATA.
    /// </summary>
    public const string RawBankDirName = "RAWBANK";

    /// <summary>
    ///     Zistí, či sa dôvod s daným poradovým číslom dá cez správu 080-5 vybrať. Posledná položka skupiny
    ///     je pre INISS nedosiahnuteľná - testuje <c>číslo &lt; počet</c> a potom siaha na <c>číslo − 1</c>.
    /// </summary>
    /// <param name="number">Poradové číslo dôvodu, číslované od jednotky.</param>
    /// <param name="count">Počet položiek v skupine.</param>
    /// <returns><see langword="true" />, ak INISS dôvod nájde.</returns>
    public static bool IsUsable(int number, int count) => number >= 1 && number < count;

    /// <summary>
    ///     Odvodí cestu k zvukovej banke z priečinka s grafikonom. Banka je súrodencom priečinka DATA, teda
    ///     o dve úrovne vyššie než grafikon.
    /// </summary>
    /// <param name="gvdDir">Priečinok s grafikonom, napríklad <c>...\DATA\Košice.2025</c>.</param>
    /// <returns>Cesta k banke, alebo <see langword="null" />, ak sa nenašla.</returns>
    public static string? ResolveRawBankDir(string? gvdDir)
    {
        if (string.IsNullOrWhiteSpace(gvdDir))
            return null;

        var dataDir = Path.GetDirectoryName(Path.GetFullPath(gvdDir));
        var inissDir = dataDir is null ? null : Path.GetDirectoryName(dataDir);
        if (inissDir is null)
            return null;

        var rawBank = PathUtils.CombinePath(inissDir, RawBankDirName);

        return Directory.Exists(rawBank) ? rawBank : null;
    }

    /// <summary>
    ///     Načíta dôvody meškania zo zvukovej banky.
    /// </summary>
    /// <param name="rawBankDir">Priečinok so zvukovou bankou.</param>
    /// <returns>Dôvody v poradí, v akom ich čísluje INISS. Prázdny zoznam, ak banka skupinu nemá.</returns>
    /// <exception cref="DirectoryNotFoundException">Ak priečinok s bankou neexistuje.</exception>
    public static IReadOnlyList<DelayReason> Read(string rawBankDir)
    {
        if (string.IsNullOrWhiteSpace(rawBankDir))
            throw new ArgumentNullException(nameof(rawBankDir));
        if (!Directory.Exists(rawBankDir))
            throw new DirectoryNotFoundException(Texts.F(Resources.Error_RawBankMissing, rawBankDir));

        var languages = RawBankParser.ReadFyzBankFile(rawBankDir, out _);
        if (languages.Count == 0)
            return new List<DelayReason>();

        // V niektorých bankách nemá základný jazyk nastavený príznak, vtedy sa berie prvý.
        var language = FyzLanguage.GetBasicLanguage(languages) ?? languages[0];

        var sounds = RawBankParser.ReadFyzZvukFile(rawBankDir, language)
            .Where(sound => sound.Group is not null && sound.Group.Name == SoundGroup)
            .ToList();

        var reasons = new List<DelayReason>(sounds.Count);
        for (var i = 0; i < sounds.Count; i++)
            reasons.Add(new DelayReason(i + 1, sounds[i].Name, sounds[i].Text, IsUsable(i + 1, sounds.Count)));

        return reasons;
    }

    /// <summary>
    ///     Načíta dôvody meškania k danému grafikonu. Ak sa banka nenájde, vráti prázdny zoznam.
    /// </summary>
    /// <param name="gvdDir">Priečinok s grafikonom.</param>
    /// <returns>Dôvody meškania.</returns>
    public static IReadOnlyList<DelayReason> ReadForGvd(string gvdDir)
    {
        var rawBank = ResolveRawBankDir(gvdDir);

        return rawBank is null ? new List<DelayReason>() : Read(rawBank);
    }
}
