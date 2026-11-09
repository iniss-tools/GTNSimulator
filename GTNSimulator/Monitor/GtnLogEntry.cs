namespace GTNSimulator.Monitor;

/// <summary>
///     Jeden riadok logu rozhrania INISSu (<c>GTN.LOG</c>): kedy INISS správu prijal, jej text a ako ju spracoval.
/// </summary>
/// <param name="Logged">Čas zápisu podľa hodín INISSu.</param>
/// <param name="Message">Text správy tak, ako prišla (bez ukončenia riadka).</param>
/// <param name="Result">Výsledok spracovania, napríklad <c>OK, delay 3 min., delay 3 min., track 2</c>.</param>
public sealed record GtnLogEntry(DateTime Logged, string Message, string Result)
{
    /// <summary>
    ///     INISS správu prijal a priradil vlaku.
    /// </summary>
    public bool Ok => Result.StartsWith("OK", StringComparison.Ordinal);

    /// <summary>
    ///     Výsledok bez opakovaných častí - INISS meškanie píše dvakrát (<c>OK, delay 3 min., delay 3 min., track 2</c>).
    /// </summary>
    public string Summary => string.Join(", ", Result.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.Ordinal));

    /// <summary>
    ///     Rozoberie riadok logu v tvare <c>dd.MM.yyyy HH:mm:ss: "správa" - výsledok</c>. INISS2 má pred pomlčkou
    ///     namiesto medzery tabulátor.
    /// </summary>
    /// <param name="line">Riadok logu.</param>
    /// <param name="entry">Rozobraný záznam.</param>
    /// <returns><see langword="true" />, ak riadok zodpovedá tvaru záznamu o správe.</returns>
    public static bool TryParse(string line, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GtnLogEntry? entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(line))
            return false;

        const string stampFormat = "dd.MM.yyyy HH:mm:ss";
        var quote = line.IndexOf(": \"", StringComparison.Ordinal);
        if (quote != stampFormat.Length ||
            !DateTime.TryParseExact(line[..quote], stampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var logged))
            return false;

        // text spravy moze obsahovat aj uvodzovky - vysledok zacina az za poslednym oddelovacom
        var end = Math.Max(line.LastIndexOf("\" - ", StringComparison.Ordinal), line.LastIndexOf("\"\t - ", StringComparison.Ordinal));
        if (end < quote + 3)
            return false;

        var result = line[(end + 1)..].TrimStart();
        entry = new GtnLogEntry(logged, line[(quote + 3)..end], result[1..].Trim());
        return true;
    }
}
