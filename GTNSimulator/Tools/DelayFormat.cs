namespace GTNSimulator.Tools;

/// <summary>
///     Meškanie v tabuľke plánu - celé minúty so znamienkom, pri sekundách aj tie (<c>+5:30</c>).
/// </summary>
public static class DelayFormat
{
    /// <summary>
    ///     Text meškania do bunky; nula je prázdna.
    /// </summary>
    /// <param name="delay">Meškanie, záporné = náskok.</param>
    /// <returns>Text, napríklad <c>+5</c>, <c>-3</c> alebo <c>+5:30</c>.</returns>
    public static string Format(TimeSpan delay)
    {
        if (delay == TimeSpan.Zero)
            return "";

        var sign = delay < TimeSpan.Zero ? "-" : "+";
        var abs = delay.Duration();
        var minutes = (int)abs.TotalMinutes;

        return abs.Seconds == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{sign}{minutes}")
            : string.Create(CultureInfo.InvariantCulture, $"{sign}{minutes}:{abs.Seconds:00}");
    }

    /// <summary>
    ///     Prečíta meškanie zadané v bunke: <c>5</c>, <c>+5</c>, <c>-3</c>, <c>5:30</c>; prázdne = načas.
    /// </summary>
    /// <param name="text">Text z bunky.</param>
    /// <param name="delay">Meškanie.</param>
    /// <returns><see langword="true" />, ak je text platné meškanie.</returns>
    public static bool TryParse(string? text, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        text = text?.Trim() ?? "";
        if (text.Length == 0)
            return true;

        var negative = text[0] == '-';
        if (text[0] is '+' or '-')
            text = text[1..].Trim();

        var parts = text.Split(':');
        if (parts.Length > 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
            return false;

        var seconds = 0;
        if (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds > 59))
            return false;

        delay = new TimeSpan(0, minutes, seconds);
        if (negative)
            delay = -delay;

        return true;
    }
}
