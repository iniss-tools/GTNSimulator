using GVDEditor.Domain.Entities;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;

namespace GTNSimulator.Tools;

/// <summary>
///     Spoločné pomôcky na prácu s prekladmi. Texty okien sú v zdrojoch pri okne (<c>FMain.resx</c>,
///     <c>FTracks.resx</c>), tu sú len tie, ktoré patria vrstve pod rozhraním.
/// </summary>
internal static class Texts
{
    /// <summary>
    ///     Názov druhu udalosti simulácie.
    /// </summary>
    /// <param name="kind">Druh udalosti.</param>
    /// <returns>Názov udalosti.</returns>
    public static string Of(SimEventKind kind) => kind switch
    {
        SimEventKind.EntryRoute => Resources.EventKind_EntryRoute,
        SimEventKind.DelayReason => Resources.EventKind_DelayReason,
        SimEventKind.Arrival => Resources.EventKind_Arrival,
        SimEventKind.ExitRoute => Resources.EventKind_ExitRoute,
        SimEventKind.Departure => Resources.EventKind_Departure,
        _ => kind.ToString()
    };

    /// <summary>
    ///     Názov pôvodu koľaje. Patrí sem, a nie k oknu, lebo ho okrem tabuľky používa aj
    ///     <see cref="TrainRun.ToString" /> v zázname behu.
    /// </summary>
    /// <param name="source">Pôvod koľaje.</param>
    /// <returns>Názov pôvodu koľaje.</returns>
    public static string Of(TrackSource source) => source switch
    {
        TrackSource.Timetable => Resources.Source_Timetable,
        TrackSource.Changed => Resources.Source_Changed,
        TrackSource.Assigned => Resources.Source_Assigned,
        TrackSource.Conflict => Resources.Source_Conflict,
        TrackSource.Manual => Resources.Source_Manual,
        _ => ""
    };

    /// <summary>
    ///     Koľaj v zozname na výber - kľúč, názov a nástupište.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <returns>Text položky.</returns>
    public static string Of(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        var platform = track.Platform is null || string.IsNullOrWhiteSpace(track.Platform.Key) || track.Platform.Key == "N"
            ? Resources.Tracks_NoPlatform
            : F(Resources.Tracks_WithPlatform, track.Platform.Key);

        return F(Resources.Tracks_ItemFormat, track.Key, track.FullName, platform);
    }

    /// <summary>
    ///     Vysvetlenie, prečo sa zmena jazdy nedala urobiť.
    /// </summary>
    /// <param name="error">Dôvod.</param>
    /// <returns>Text pre obsluhu.</returns>
    public static string Of(PlanEditError error) => error switch
    {
        PlanEditError.Finished => Resources.EditError_Finished,
        PlanEditError.ArrivalSent => Resources.EditError_ArrivalSent,
        PlanEditError.NoArrival => Resources.EditError_NoArrival,
        PlanEditError.NoDeparture => Resources.EditError_NoDeparture,
        PlanEditError.DelayOutOfRange => F(Resources.EditError_DelayOutOfRange, PlanEditing.MinDelay.TotalMinutes,
            SimulationOptions.DelayCeiling.TotalMinutes),
        PlanEditError.DepartureTooEarly => Resources.EditError_DepartureTooEarly,
        PlanEditError.TrackUnusable => Resources.EditError_TrackUnusable,
        PlanEditError.ReasonOutOfRange => Resources.EditError_ReasonOutOfRange,
        PlanEditError.NotPending => Resources.EditError_NotPending,
        PlanEditError.StandingOnTrack => Resources.EditError_StandingOnTrack,
        PlanEditError.NoFreeTrack => Resources.EditError_NoFreeTrack,
        _ => ""
    };

    /// <summary>
    ///     Zloží text s počtom v správnom tvare podľa slovenského a českého skloňovania (1, 2 až 4, ostatné).
    /// </summary>
    /// <param name="count">Počet.</param>
    /// <param name="one">Tvar pre jednotku.</param>
    /// <param name="few">Tvar pre dva až štyri.</param>
    /// <param name="many">Tvar pre ostatné.</param>
    /// <returns>Text s doplneným počtom.</returns>
    public static string Count(int count, string one, string few, string many) => count switch
    {
        1 => one,
        >= 2 and <= 4 => string.Format(CultureInfo.CurrentCulture, few, count),
        _ => string.Format(CultureInfo.CurrentCulture, many, count)
    };

    /// <summary>
    ///     Skratka pre <see cref="string.Format(IFormatProvider,string,object[])" /> s aktuálnou kultúrou.
    /// </summary>
    /// <param name="format">Formátovací reťazec zo zdrojov.</param>
    /// <param name="args">Argumenty.</param>
    /// <returns>Zložený text.</returns>
    public static string F(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}
