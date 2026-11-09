using GTNSimulator.Simulation;

namespace GTNSimulator.Diagram;

/// <summary>
///     Časová os koľajového diagramu - prevod času na vodorovnú polohu a späť.
/// </summary>
public sealed class DiagramScale
{
    /// <summary>
    ///     Najmenšia mierka - celý deň sa zmestí do 720 bodov.
    /// </summary>
    public const int MinHourWidth = 30;

    /// <summary>
    ///     Najväčšia mierka - minúta má 30 bodov.
    /// </summary>
    public const int MaxHourWidth = 1800;

    /// <summary>
    ///     Predvolená mierka.
    /// </summary>
    public const int DefaultHourWidth = 240;

    /// <summary>
    ///     Vytvorí časovú os.
    /// </summary>
    /// <param name="start">Začiatok osi.</param>
    /// <param name="end">Koniec osi.</param>
    /// <param name="hourWidth">Šírka hodiny v bodoch.</param>
    public DiagramScale(DateTime start, DateTime end, int hourWidth)
    {
        Start = start;
        End = end < start ? start : end;
        HourWidth = Math.Clamp(hourWidth, MinHourWidth, MaxHourWidth);
    }

    /// <summary>
    ///     Začiatok osi.
    /// </summary>
    public DateTime Start { get; }

    /// <summary>
    ///     Koniec osi.
    /// </summary>
    public DateTime End { get; }

    /// <summary>
    ///     Šírka hodiny v bodoch.
    /// </summary>
    public int HourWidth { get; }

    /// <summary>
    ///     Šírka celej osi v bodoch.
    /// </summary>
    public int Width => (int)Math.Ceiling(X(End));

    /// <summary>
    ///     Vodorovná poloha času od začiatku osi.
    /// </summary>
    /// <param name="time">Čas.</param>
    /// <returns>Poloha v bodoch.</returns>
    public double X(DateTime time) => (time - Start).TotalHours * HourWidth;

    /// <summary>
    ///     Čas na vodorovnej polohe.
    /// </summary>
    /// <param name="x">Poloha v bodoch od začiatku osi.</param>
    /// <returns>Čas.</returns>
    public DateTime TimeAt(double x) => Start + TimeSpan.FromHours(x / HourWidth);

    /// <summary>
    ///     Celé minúty, o ktoré sa posunie vlak potiahnutý o daný počet bodov.
    /// </summary>
    /// <param name="dx">Posun v bodoch.</param>
    /// <returns>Minúty, záporné = skôr.</returns>
    public int Minutes(double dx) => (int)Math.Round(dx * 60 / HourWidth, MidpointRounding.AwayFromZero);

    /// <summary>
    ///     Rovnaká os s inou mierkou.
    /// </summary>
    /// <param name="hourWidth">Šírka hodiny v bodoch.</param>
    /// <returns>Nová os.</returns>
    public DiagramScale WithHourWidth(int hourWidth) => new(Start, End, hourWidth);

    /// <summary>
    ///     Rozsah osi pre plán dňa - od polnoci (alebo skôr, ak vlak drží koľaj už pred ňou) po koniec obsadenia
    ///     posledného vlaku, na celé hodiny, aspoň celý deň.
    /// </summary>
    /// <param name="day">Deň plánu.</param>
    /// <param name="runs">Jazdy dňa.</param>
    /// <returns>Začiatok a koniec osi.</returns>
    public static (DateTime Start, DateTime End) Range(DateTime day, IEnumerable<TrainRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        var start = day.Date;
        var end = start.AddDays(1);

        foreach (var run in runs)
        {
            if (run.OccupiedFrom < start)
                start = run.OccupiedFrom;
            if (run.OccupiedTo > end)
                end = run.OccupiedTo;
        }

        var floor = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, start.Kind);
        var ceiling = new DateTime(end.Year, end.Month, end.Day, end.Hour, 0, 0, end.Kind);
        if (ceiling < end)
            ceiling = ceiling.AddHours(1);

        return (floor, ceiling);
    }
}
