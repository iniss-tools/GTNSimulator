using GVDEditor.Domain.Entities;
using ToolsCore.Iniss2.Grafikon;

namespace ILTISSimulator.Data;

/// <summary>
///     Údaje grafikonu INISS2, ktoré sa do entít GVDEditora nezmestia: číslo vlaku pri príchode a odchode, koľaj
///     podľa dňa a nastavenia servera.
/// </summary>
public sealed class Iniss2Source
{
    private readonly StationTimetable _timetable;
    private readonly IReadOnlyList<Track> _tracks;
    private readonly IReadOnlyDictionary<int, Iniss2TrainCall> _calls;

    internal Iniss2Source(StationTimetable timetable, IReadOnlyList<Track> tracks, IReadOnlyDictionary<int, Iniss2TrainCall> calls,
        int gtnPort, Func<DateOnly, string> gtnLogFile)
    {
        _timetable = timetable;
        _tracks = tracks;
        _calls = calls;
        GtnPort = gtnPort > 0 ? gtnPort : null;
        GtnLogFile = gtnLogFile;
    }

    /// <summary>
    ///     Port, na ktorom server počúva správy GTN, alebo <see langword="null" />, ak ich má vypnuté.
    /// </summary>
    public int? GtnPort { get; }

    /// <summary>
    ///     Vráti log správ GTN zo zadaného dňa.
    /// </summary>
    public Func<DateOnly, string> GtnLogFile { get; }

    /// <summary>
    ///     Vráti číslo vlaku pri príchode.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns>Číslo vlaku.</returns>
    public string ArrivalNumber(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return _calls[train.ID].Call.ArrivalNumber;
    }

    /// <summary>
    ///     Vráti číslo vlaku pri odchode. V cieľovej stanici je to číslo pri príchode.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <returns>Číslo vlaku.</returns>
    public string DepartureNumber(Train train)
    {
        ArgumentNullException.ThrowIfNull(train);

        return _calls[train.ID].Call.Number;
    }

    /// <summary>
    ///     Nájde koľaj vlaku v zadaný deň rovnako ako server.
    /// </summary>
    /// <param name="train">Vlak.</param>
    /// <param name="day">Deň, keď vlak ide stanicou.</param>
    /// <returns>Koľaj, alebo <see langword="null" />, ak ju vlak v ten deň nemá alebo stanica taký kľúč nepozná.</returns>
    public Track? TrackOn(Train train, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(train);

        var entry = _calls[train.ID];
        var run = DateOnly.FromDateTime(day).AddDays(-entry.DayOffset);

        if (_timetable.PositionFor(entry.Call, run) is not { } position)
            return null;

        var key = train.Arrival.HasValue && position.ArrivalTrack.Length > 0 ? position.ArrivalTrack : position.DepartureTrack;

        return Track.GetFromID(_tracks, key);
    }
}

/// <summary>
///     Zastavenie z grafikonu INISS2 k vlaku simulátora.
/// </summary>
/// <param name="Call">Zastavenie.</param>
/// <param name="DayOffset">O koľko dní po dni jazdy vlaku ide vlak stanicou.</param>
internal sealed record Iniss2TrainCall(StationCall Call, int DayOffset);
