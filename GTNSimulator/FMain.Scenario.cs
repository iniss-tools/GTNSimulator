using System.Text.Json;
using GTNSimulator.Data;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;
using GTNSimulator.Tools;
using Serilog;

namespace GTNSimulator;

internal sealed partial class FMain
{
    private void SaveScenario()
    {
        if (_gvd is null || _engine?.Runs is null)
            return;

        sfdScenario.Filter = Resources.Main_ScenarioFilter;
        sfdScenario.FileName = Texts.F(Resources.Main_ScenarioFileName, _gvd.Info.ThisStation.Name, _engine.Day);
        if (sfdScenario.ShowDialog(this) != DialogResult.OK)
            return;

        var scenario = DayScenario.Capture(_gvd.StationId, _engine.Day, _engine.PlannedFrom, _engine.Options, _engine.Runs);

        try
        {
            ScenarioFile.Write(sfdScenario.FileName, scenario);
            AppendLog(LogKind.Info, Texts.F(Resources.Log_ScenarioSaved, sfdScenario.FileName, scenario.Changes.Count));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error(e, "Scenar {File} sa nepodarilo ulozit", sfdScenario.FileName);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_ScenarioFailed, e.Message));
        }
    }

    private void OpenScenario()
    {
        if (_gvd is null)
            return;

        ofdScenario.Filter = Resources.Main_ScenarioFilter;
        if (ofdScenario.ShowDialog(this) != DialogResult.OK || !ConfirmDiscardChanges())
            return;

        OpenScenario(ofdScenario.FileName);
    }

    /// <summary>
    ///     Otvorí scenár dňa zo súboru - naplánuje jeho deň a použije ručné zmeny.
    /// </summary>
    /// <param name="path">Súbor scenára.</param>
    private void OpenScenario(string path)
    {
        if (_gvd is null)
            return;

        DayScenario scenario;
        try
        {
            scenario = ScenarioFile.Read(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            Log.Error(e, "Scenar {File} sa nepodarilo nacitat", path);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_ScenarioFailed, e.Message));
            return;
        }

        if (scenario.StationId != _gvd.StationId)
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_ScenarioOtherStation, scenario.StationId, _gvd.StationId));

        var day = scenario.Day.ToDateTime(TimeOnly.MinValue);
        var from = day + scenario.From.ToTimeSpan();
        var options = scenario.ToOptions(UsableReasons().Select(reason => reason.Number), SelectedSpeed);
        options.SendLineTrack = Config.Simulation.SendLineTrack;
        options.LineTrack = Config.Simulation.LineTrack;

        var failed = new List<(RunChange Change, PlanEditError Error)>();
        if (!Plan(day, from, options, (runs, context) => failed.AddRange(scenario.Apply(runs, _gvd.Tracks, context))))
            return;

        dtpDay.Value = day;
        dtpFrom.Value = from;

        foreach (var (change, error) in failed)
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_EditFailed, change.Train,
                error == PlanEditError.NotPending ? Resources.Log_ScenarioTrainMissing : Texts.Of(error)));

        AppendLog(LogKind.Info, Texts.F(Resources.Log_ScenarioOpened, path, scenario.Changes.Count - failed.Count));
    }
}
