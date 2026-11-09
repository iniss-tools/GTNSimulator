using GTNSimulator.Diagram;
using GTNSimulator.Simulation;

namespace GTNSimulator;

/// <summary>
///     Koľajový diagram pod tabuľkou plánu - zobrazenie, výber zdieľaný s tabuľkou a zmeny ťahaním.
/// </summary>
internal sealed partial class FMain
{
    private bool _syncingSelection;
    private bool _applyingSplit;

    private void InitDiagram()
    {
        trackDiagram.HourWidth = LogicalToDeviceUnits(Config.View.DiagramHourWidth);
        ApplyDiagramVisible();
    }

    private void SetTrackDiagramVisible(bool value)
    {
        Config.View.TrackDiagram = value;
        SaveConfig();
        ApplyDiagramVisible();
        UpdateCommandStates();

        if (value && _engine?.Timeline is not null)
            trackDiagram.ShowTime(_engine.VirtualTime);
    }

    private void ApplyDiagramVisible()
    {
        splitPlan.Panel2Collapsed = !Config.View.TrackDiagram;
        if (!Config.View.TrackDiagram)
            return;

        var max = splitPlan.Height - splitPlan.Panel2MinSize - splitPlan.SplitterWidth;
        if (max <= splitPlan.Panel1MinSize)
            return;

        _applyingSplit = true;
        splitPlan.SplitterDistance = Math.Clamp(splitPlan.Height * Config.View.DiagramSplit / 100, splitPlan.Panel1MinSize, max);
        _applyingSplit = false;
    }

    /// <summary>
    ///     Nový plán alebo grafikon - diagram sa zostaví znova a posunie na aktuálny čas.
    /// </summary>
    private void ResetDiagram()
    {
        var start = _engine?.VirtualTime ?? DateTime.Today;
        trackDiagram.SetPlan(_engine?.Day ?? DateTime.Today, _engine?.Runs, _gvd?.Tracks ?? [], _excludedTracks.ToList(), start);
        trackDiagram.SetConflicts(_conflicts);
        UpdateDiagramClock();
    }

    /// <summary>
    ///     Zmena plánu - pruhy sa rozložia znova, poloha diagramu ostane.
    /// </summary>
    private void RefreshDiagram()
    {
        trackDiagram.SetConflicts(_conflicts);
        trackDiagram.RefreshPlan();
    }

    private void UpdateDiagramClock()
    {
        if (_engine?.Timeline is null)
        {
            trackDiagram.SetNow(null);
            return;
        }

        var now = _engine.VirtualTime;
        trackDiagram.SetNow(now);
        if (Config.View.FollowCurrent && _engine.IsRunning)
            trackDiagram.ShowTime(now);
    }

    /// <summary>
    ///     Výber v tabuľke sa prenesie do diagramu.
    /// </summary>
    private void SyncDiagramSelection()
    {
        if (!_syncingSelection)
            trackDiagram.SetSelection(SelectedRuns());
    }

    private void trackDiagram_SelectionChanged(object? sender, EventArgs e)
    {
        _syncingSelection = true;
        try
        {
            var rows = trackDiagram.SelectedRuns
                .Select(run => _rowByRun.GetValueOrDefault(run))
                .OfType<DataGridViewRow>()
                .Where(row => row.Visible)
                .OrderBy(row => row.Index)
                .ToList();

            // aktualna bunka posunie tabulku na vlak - vyber sa nastavi az po nej, inak by ho zrusila
            if (rows.Count > 0 && rows[0].Cells.Cast<DataGridViewCell>().FirstOrDefault(cell => cell.Visible) is { } cell)
                dgvPlan.CurrentCell = cell;

            dgvPlan.ClearSelection();
            foreach (var row in rows)
                row.Selected = true;
        }
        finally
        {
            _syncingSelection = false;
        }

        UpdateCommandStates();
        FillManualFromSelection();
    }

    /// <summary>
    ///     Vlak potiahnutý v diagrame - zmena koľaje a meškania rovnako ako v tabuľke.
    /// </summary>
    private void trackDiagram_RunMoved(object? sender, DiagramMoveEventArgs e)
    {
        if (_engine is null)
            return;

        var failed = new Dictionary<TrainRun, PlanEditError>();
        if (e.Track is { } track)
        {
            var error = _engine.Edit(e.Run, (run, context) => PlanEditing.SetTrack(run, track, context));
            if (error != PlanEditError.None)
                failed[e.Run] = error;
        }

        if (e.Minutes != 0 && failed.Count == 0)
        {
            var error = _engine.Edit(e.Run, (run, context) => PlanEditing.AddDelay(run, TimeSpan.FromMinutes(e.Minutes), context));
            if (error != PlanEditError.None)
                failed[e.Run] = error;
        }

        ReportFailures(failed);
    }

    private void trackDiagram_HourWidthChanged(object? sender, EventArgs e) =>
        Config.View.DiagramHourWidth = trackDiagram.HourWidth * 96 / Math.Max(1, DeviceDpi);

    private void splitPlan_SplitterMoved(object? sender, SplitterEventArgs e)
    {
        if (!_applyingSplit && Config.View.TrackDiagram && splitPlan.Height > 0)
            Config.View.DiagramSplit = splitPlan.SplitterDistance * 100 / splitPlan.Height;
    }
}
