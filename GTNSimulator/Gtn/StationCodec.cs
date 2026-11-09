using GTNSimulator.Properties;
using ToolsCore.Iniss.Tools;

namespace GTNSimulator.Gtn;

/// <summary>
///     Zakódovanie identifikátora stanice do šesťznakového poľa správy GTN.
/// </summary>
/// <remarks>
///     <para>
///         INISS z poľa číta iba prvých päť znakov a prečíta ich ako celé číslo - medzery pred ním a za ním
///         smú byť, iný znak nie. Šiesty znak poľa nemá na výsledok vplyv. Prečítané číslo porovná s číslom
///         stanice vlaku: zhoda je pri rovnosti, alebo pri rovnosti modulo <see cref="Modulus" />, keď je jedno
///         z čísel menšie ako <see cref="Modulus" />.
///     </para>
///     <para>
///         Päť znakov dá najviac 99 999, takže rozhoduje vždy <strong>posledných päť číslic identifikátora
///         stanice</strong> z grafikonu, teda <c>ID mod 100 000</c>. To ide do poľa pri odosielaní.
///     </para>
/// </remarks>
public static class StationCodec
{
    /// <summary>
    ///     Počet znakov poľa stanice, ktoré INISS naozaj číta.
    /// </summary>
    public const int SignificantDigits = 5;

    /// <summary>
    ///     Rozsah, na ktorý sa identifikátor stanice cestou oreže.
    /// </summary>
    public const int Modulus = 100000;

    /// <summary>
    ///     Výplňový znak na šiestej pozícii poľa. INISS ho ignoruje.
    /// </summary>
    public const char Filler = '0';

    /// <summary>
    ///     Zakóduje identifikátor stanice do šesťznakového poľa správy.
    /// </summary>
    /// <param name="stationId">Identifikátor stanice z grafikonu (napr. 5613600).</param>
    /// <returns>Šesťznakové pole stanice (napr. <c>136000</c>).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Ak je identifikátor záporný.</exception>
    public static string Encode(int stationId)
    {
        if (stationId < 0)
            throw new ArgumentOutOfRangeException(nameof(stationId), Resources.Error_StationId);

        return (stationId % Modulus).PadZeros(SignificantDigits) + Filler;
    }

    /// <summary>
    ///     Vráti hodnotu, podľa ktorej INISS stanicu v správach rozlišuje - posledných päť číslic identifikátora.
    ///     Dve stanice s rovnakým výsledkom nie je možné v správach GTN rozlíšiť.
    /// </summary>
    /// <param name="stationId">Identifikátor stanice z grafikonu.</param>
    /// <returns>Číslo, ktoré pre túto stanicu INISS zo správy prečíta.</returns>
    public static int NormalizeAsIniss(int stationId) => stationId % Modulus;

    /// <summary>
    ///     Prečíta pole stanice presne tak, ako to robí INISS: vezme prvých päť znakov a prečíta ich ako celé
    ///     číslo, medzery pred ním a za ním pripustí.
    /// </summary>
    /// <param name="field">Pole stanice zo správy.</param>
    /// <param name="number">Prečítané číslo.</param>
    /// <returns>
    ///     <see langword="true" />, ak sa pole podarilo prečítať; inak <see langword="false" />, čo INISS
    ///     zaloguje ako <c>unknown station number format</c>.
    /// </returns>
    public static bool TryDecodeAsIniss(string field, out int number)
    {
        number = 0;

        if (string.IsNullOrEmpty(field))
            return false;

        var significant = field.Length > SignificantDigits ? field.Substring(0, SignificantDigits) : field;
        return int.TryParse(significant, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture, out number);
    }

    /// <summary>
    ///     Porovná číslo zo správy s identifikátorom stanice tak, ako to robí INISS: rovnosť, alebo rovnosť modulo
    ///     <see cref="Modulus" />, keď je jedno z čísel menšie ako <see cref="Modulus" />.
    /// </summary>
    /// <param name="number">Číslo prečítané z poľa stanice.</param>
    /// <param name="stationId">Identifikátor stanice z grafikonu.</param>
    /// <returns><see langword="true" />, ak INISS číslo priradí tejto stanici.</returns>
    public static bool SameStation(int number, int stationId) =>
        number == stationId || ((number < Modulus || stationId < Modulus) && number % Modulus == stationId % Modulus);

    /// <summary>
    ///     Zistí, či pole stanice zo správy adresuje danú stanicu.
    /// </summary>
    /// <param name="field">Pole stanice zo správy.</param>
    /// <param name="stationId">Identifikátor stanice z grafikonu.</param>
    /// <returns><see langword="true" />, ak INISS správu priradí tejto stanici.</returns>
    public static bool Matches(string field, int stationId) =>
        TryDecodeAsIniss(field, out var number) && SameStation(number, stationId);
}
