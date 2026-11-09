namespace GTNSimulator.Data;

/// <summary>
///     Jeden dôvod meškania zo zvukovej banky stanice.
/// </summary>
public sealed class DelayReason
{
    internal DelayReason(int number, string key, string text, bool usable)
    {
        Number = number;
        Key = key;
        Text = text;
        Usable = usable;
    }

    /// <summary>
    ///     Poradové číslo, ktoré ide do správy 080-5. Číslované od jednotky.
    /// </summary>
    public int Number { get; }

    /// <summary>
    ///     Kľúč zvuku, teda krátke označenie dôvodu.
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     Text, ktorý INISS ohlási.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Či sa dôvod dá cez správu 080-5 vôbec vybrať. Posledná položka skupiny sa vybrať nedá - INISS
    ///     testuje <c>číslo &lt; počet</c> a potom siaha na položku <c>číslo − 1</c>.
    /// </summary>
    public bool Usable { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Number:00} {Key}";
}
