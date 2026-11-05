using ILTISSimulator.Monitor;
using ILTISSimulator.Properties;
using ILTISSimulator.Simulation;
using ILTISSimulator.Tools;

namespace ILTISSimulator;

internal sealed partial class FMain
{
    // odoslane spravy, na ktorych vysledok sa caka v logu INISSu - podla textu spravy
    private readonly Dictionary<string, Queue<TrainRun>> _expected = new(StringComparer.Ordinal);
    private readonly Dictionary<TrainRun, GtnLogEntry> _responses = new();
    private GtnLogTail? _gtnLog;

    /// <summary>
    ///     Začne sledovať log rozhrania INISSu podľa nastavení - zadaný súbor, inak nájdený pri grafikone.
    /// </summary>
    private void RestartGtnLog()
    {
        StopGtnLog();

        if (!Config.Connection.WatchGtnLog)
        {
            tsslGtnLog.Visible = false;
            return;
        }

        tsslGtnLog.Visible = true;

        _gtnLog = CreateGtnLog();
        if (_gtnLog is null)
        {
            SetGtnLogStatus("status-warning", Resources.Main_GtnLogNotFound, Resources.Main_GtnLogNotFoundTip);
            return;
        }

        if (_gtnLog.Exists)
            SetGtnLogStatus("status-ok", Resources.Main_GtnLogWatching, _gtnLog.Path);
        else
            SetGtnLogStatus("status-warning", Resources.Main_GtnLogMissing, _gtnLog.Path);

        AppendLog(LogKind.Info, Texts.F(Resources.Log_GtnLogWatching, _gtnLog.Path));
    }

    private GtnLogTail? CreateGtnLog()
    {
        var path = Config.Connection.GtnLog.Trim();
        if (path.Length > 0)
            return new GtnLogTail(path);

        if (_gvd?.Iniss2 is { } iniss2)
            return new GtnLogTail(() => iniss2.GtnLogFile(DateOnly.FromDateTime(DateTime.Now)));

        path = _gvdDir is null ? "" : GtnLogTail.Find(_gvdDir) ?? "";

        return path.Length == 0 ? null : new GtnLogTail(path);
    }

    private void StopGtnLog() => _gtnLog = null;

    private void SetGtnLogStatus(string icon, string text, string tip)
    {
        tsslGtnLog.IconName = icon;
        tsslGtnLog.Text = text;
        tsslGtnLog.ToolTipText = tip;
    }

    /// <summary>
    ///     Prečíta nové riadky logu a priradí ich k odoslaným správam.
    /// </summary>
    private void PollGtnLog()
    {
        if (_gtnLog is null)
            return;

        foreach (var entry in _gtnLog.Poll())
        {
            if (_expected.TryGetValue(entry.Message, out var waiting) && waiting.Count > 0)
            {
                var run = waiting.Dequeue();
                _responses[run] = entry;
                RefreshRow(run);

                if (!entry.Ok)
                    AppendLog(LogKind.Warning, Texts.F(Resources.Log_InissRejected, run.Train.Number, entry.Result, entry.Message));

                continue;
            }

            // sprava odinakial (rucna, iny odosielatel) - zaujimava len ked ju INISS odmietol
            if (!entry.Ok)
                AppendLog(LogKind.Warning, Texts.F(Resources.Log_InissRejectedOther, entry.Result, entry.Message));
        }
    }

    /// <summary>
    ///     Zapamätá si odoslanú správu jazdy, aby sa k nej našiel výsledok v logu INISSu.
    /// </summary>
    private void ExpectResponse(TrainRun run, string wire)
    {
        if (_gtnLog is null)
            return;

        if (!_expected.TryGetValue(wire, out var waiting))
            _expected[wire] = waiting = new Queue<TrainRun>();

        waiting.Enqueue(run);
    }

    private void ClearResponses()
    {
        _expected.Clear();
        _responses.Clear();
    }

    private string ResponseText(TrainRun run) => _responses.TryGetValue(run, out var entry) ? entry.Summary : "";

    private string? ResponseTip(TrainRun run) =>
        _responses.TryGetValue(run, out var entry)
            ? Texts.F(Resources.Grid_ResponseTip, entry.Logged, entry.Message, entry.Result)
            : null;
}
