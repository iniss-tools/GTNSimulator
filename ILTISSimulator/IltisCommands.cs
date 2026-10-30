using ILTISSimulator.Properties;
using ToolsCore.Commands;

namespace ILTISSimulator;

/// <summary>
///     Príkazy hlavného okna s predvolenými klávesovými skratkami. Identifikátor je názov prvku skratky v config.xml -
///     nesmie sa meniť, inak by sa stratili skratky nastavené používateľom.
/// </summary>
internal static class IltisCommands
{
    public static CommandInfo OpenGvd => new("OpenGvd", Resources.Cmd_OpenGvd, Shortcut.CtrlO);
    public static CommandInfo ReloadGvd => new("ReloadGvd", Resources.Cmd_ReloadGvd, Shortcut.CtrlR);
    public static CommandInfo OpenScenario => new("OpenScenario", Resources.Cmd_OpenScenario, Shortcut.CtrlShiftO);
    public static CommandInfo SaveScenario => new("SaveScenario", Resources.Cmd_SaveScenario, Shortcut.CtrlS);
    public static CommandInfo Tracks => new("Tracks", Resources.Cmd_Tracks, Shortcut.None);
    public static CommandInfo Connect => new("Connect", Resources.Cmd_Connect, Shortcut.CtrlK);
    public static CommandInfo Plan => new("Plan", Resources.Cmd_Plan, Shortcut.CtrlP);
    public static CommandInfo Start => new("Start", Resources.Cmd_Start, Shortcut.F5);
    public static CommandInfo Pause => new("Pause", Resources.Cmd_Pause, Shortcut.F6);
    public static CommandInfo Stop => new("Stop", Resources.Cmd_Stop, Shortcut.ShiftF5);
    public static CommandInfo Jump => new("Jump", Resources.Cmd_Jump, Shortcut.CtrlJ);
    public static CommandInfo Now => new("Now", Resources.Cmd_Now, Shortcut.CtrlT);
    public static CommandInfo SendToIniss => new("SendToIniss", Resources.Cmd_SendToIniss, Shortcut.None);
    public static CommandInfo SendNext => new("SendNext", Resources.Cmd_SendNext, Shortcut.F9);
    public static CommandInfo SendEntryRoute => new("SendEntryRoute", Resources.Cmd_SendEntryRoute, Shortcut.None);
    public static CommandInfo SendReason => new("SendReason", Resources.Cmd_SendReason, Shortcut.None);
    public static CommandInfo SendArrival => new("SendArrival", Resources.Cmd_SendArrival, Shortcut.None);
    public static CommandInfo SendExitRoute => new("SendExitRoute", Resources.Cmd_SendExitRoute, Shortcut.None);
    public static CommandInfo SendDeparture => new("SendDeparture", Resources.Cmd_SendDeparture, Shortcut.None);
    public static CommandInfo Transfer => new("Transfer", Resources.Cmd_Transfer, Shortcut.F8);
    public static CommandInfo ExpressTransfer => new("ExpressTransfer", Resources.Cmd_ExpressTransfer, Shortcut.ShiftF8);
    public static CommandInfo AddDelay => new("AddDelay", Resources.Cmd_AddDelay, Shortcut.CtrlD);
    public static CommandInfo TrackClosure => new("TrackClosure", Resources.Cmd_TrackClosure, Shortcut.CtrlL);
    public static CommandInfo Suppress => new("Suppress", Resources.Cmd_Suppress, Shortcut.CtrlDel);
    public static CommandInfo Revert => new("Revert", Resources.Cmd_Revert, Shortcut.CtrlShiftZ);
    public static CommandInfo ManualMessage => new("ManualMessage", Resources.Cmd_ManualMessage, Shortcut.CtrlM);
    public static CommandInfo TrackDiagram => new("TrackDiagram", Resources.Cmd_TrackDiagram, Shortcut.CtrlG);
    public static CommandInfo HideFinished => new("HideFinished", Resources.Cmd_HideFinished, Shortcut.CtrlH);
    public static CommandInfo Follow => new("Follow", Resources.Cmd_Follow, Shortcut.None);
    public static CommandInfo Find => new("Find", Resources.Cmd_Find, Shortcut.CtrlF);
    public static CommandInfo TopMost => new("TopMost", Resources.Cmd_TopMost, Shortcut.None);
    public static CommandInfo AppSettings => new("AppSettings", Resources.Cmd_AppSettings, Shortcut.CtrlShiftN);
    public static CommandInfo Docs => new("Docs", Resources.Cmd_Docs, Shortcut.F1);

    // text = identifikator: v strome skratiek sa ukaze text polozky ponuky
    public static CommandInfo InfoApp => new(nameof(InfoApp), nameof(InfoApp), Shortcut.None);
    public static CommandInfo CheckUpdates => new(nameof(CheckUpdates), nameof(CheckUpdates), Shortcut.None);
    public static CommandInfo ShowLogs => new(nameof(ShowLogs), nameof(ShowLogs), Shortcut.None);
    public static CommandInfo Exit => new(nameof(Exit), nameof(Exit), Shortcut.None);

    /// <summary>
    ///     Všetky príkazy hlavného okna pre nastavenia skratiek (strom ich zoradí podľa ponuky okna).
    /// </summary>
    public static IReadOnlyList<CommandInfo> All =>
    [
        OpenGvd, ReloadGvd, OpenScenario, SaveScenario, Tracks, Connect, Plan, Start, Pause, Stop, Jump, Now, SendToIniss,
        SendNext, SendEntryRoute, SendReason, SendArrival, SendExitRoute, SendDeparture, Transfer, ExpressTransfer,
        AddDelay, TrackClosure, Suppress, Revert, ManualMessage, TrackDiagram, HideFinished, Follow, Find, TopMost, AppSettings, Docs,
        InfoApp, CheckUpdates, ShowLogs, Exit
    ];
}
