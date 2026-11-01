using GVDEditor.Domain.Entities;
using ILTISSimulator.Data;
using ILTISSimulator.Properties;
using ILTISSimulator.Simulation;
using ILTISSimulator.Tools;
using ILTISSimulator.XML;
using ToolsCore.Icons;
using ToolsCore.Tools;

namespace ILTISSimulator;

internal sealed partial class FMain
{
    private readonly Dictionary<TrainRun, DataGridViewRow> _rowByRun = new();
    private ISet<TrainRun> _conflicts = new HashSet<TrainRun>();
    private Font? _boldCellFont;
    private bool _refreshing;

    #region Stĺpce

    private void InitGrid()
    {
        // bunka bez obrazka nema kreslit cerveny kriz
        cState.DefaultCellStyle.NullValue = null;
        cRouting.DefaultCellStyle.NullValue = null;
        cState.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        cRouting.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        foreach (var column in new[] { cArrival, cArrivalDelay, cDeparture, cDepartureDelay })
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        // poradie, sirku a viditelnost stlpcov si tabulka pamata sama (tahanie hlavicky, ponuka na hlavicke)
        GridColumns.Attach(dgvPlan, "Plan", () => Config, SaveConfig);
        UpdateGridFonts();
    }

    /// <summary>
    ///     Pozadie tabuľky pod riadkami ako bunky - téma nastavuje len bunky a v svetlom režime by ostala sivá plocha.
    /// </summary>
    private void UpdateGridBackground() => dgvPlan.BackgroundColor = dgvPlan.DefaultCellStyle.BackColor;

    private void UpdateGridFonts()
    {
        UpdateGridBackground();
        _boldCellFont?.Dispose();
        _boldCellFont = new Font(dgvPlan.DefaultCellStyle.Font ?? dgvPlan.Font, FontStyle.Bold);
    }

    #endregion

    #region Riadky

    /// <summary>
    ///     Naplní tabuľku jazdami naplánovaného dňa. Vlaky vynechané pre dvojité číslo a autobusy sú medzi nimi
    ///     ako sivé riadky, aby bolo vidno, prečo sa o nich nič neposiela.
    /// </summary>
    private void FillGrid()
    {
        _refreshing = true;
        dgvPlan.SuspendLayout();
        try
        {
            dgvPlan.Rows.Clear();
            _rowByRun.Clear();
            FillGridChoices();
            UpdateConflicts();

            if (_engine?.Runs is not { } runs)
                return;

            var day = _engine.Day;
            var entries = runs
                .Select(run => (Time: run.PlannedArrival ?? run.PlannedDeparture ?? day, Run: (TrainRun?)run, Train: run.Train, Bus: false))
                .Concat(_engine.SkippedDuplicates.Select(train => Skipped(train, false)))
                .Concat(_engine.SkippedBuses.Select(train => Skipped(train, true)))
                .OrderBy(entry => entry.Time)
                .ThenBy(entry => entry.Train.ID);

            foreach (var entry in entries)
            {
                var row = dgvPlan.Rows[dgvPlan.Rows.Add()];
                if (entry.Run is null)
                {
                    FillSkippedRow(row, entry.Train, day, entry.Bus);
                    continue;
                }

                row.Tag = entry.Run;
                _rowByRun[entry.Run] = row;
                FillRow(row, entry.Run);
            }

            (DateTime Time, TrainRun? Run, Train Train, bool Bus) Skipped(Train train, bool bus) =>
                (GvdData.ArrivalOn(train, day) ?? GvdData.DepartureOn(train, day) ?? day, null, train, bus);
        }
        finally
        {
            dgvPlan.ResumeLayout();
            _refreshing = false;
            ResetDiagram();
        }

        ApplyFilter();
    }

    /// <summary>
    ///     Možnosti koľají a dôvodov meškania v tabuľke - koľaje, ktoré sa dajú ohlásiť, a dôvody zo zvukovej banky.
    /// </summary>
    private void FillGridChoices()
    {
        cTrack.Items.Clear();
        foreach (var key in UsableTracks().Select(track => track.Key)
                     .Concat(_engine?.Runs?.Select(run => run.Track?.Key ?? "") ?? [])
                     .Where(key => key.Length > 0)
                     .Distinct(StringComparer.Ordinal))
            cTrack.Items.Add(key);

        cReason.Items.Clear();
        cReason.Items.Add("");
        foreach (var number in UsableReasons().Select(reason => reason.Number)
                     .Concat(_engine?.Runs?.Select(run => run.DelayReason) ?? [])
                     .Where(number => number != 0)
                     .Distinct()
                     .Order())
            cReason.Items.Add(ReasonText(number));
    }

    /// <summary>
    ///     Riadok vlaku, o ktorom sa nič neposiela - dvojité číslo alebo autobus.
    /// </summary>
    private void FillSkippedRow(DataGridViewRow row, Train train, DateTime day, bool bus)
    {
        row.Cells[cState.Index].Value = IconImages.For(dgvPlan, "status-info");
        row.Cells[cState.Index].ToolTipText = bus ? Resources.Grid_BusTip : Resources.Grid_DuplicateTip;
        row.Cells[cTrain.Index].Value = train.Number;
        row.Cells[cType.Index].Value = train.Type?.Key ?? "";
        row.Cells[cRouting.Index].Value = RoutingImage(train.Routing);
        row.Cells[cArrival.Index].Value = Time(GvdData.ArrivalOn(train, day));
        row.Cells[cDeparture.Index].Value = Time(GvdData.DepartureOn(train, day));
        row.Cells[cTrack.Index].Value = null;
        row.Cells[cNext.Index].Value = bus ? Resources.Grid_Bus : Resources.Grid_Duplicate;
        row.Cells[cName.Index].Value = train.Name;
        row.ReadOnly = true;
        row.DefaultCellStyle.ForeColor = Palette.Dim(dgvPlan.DefaultCellStyle.ForeColor, dgvPlan.DefaultCellStyle.BackColor);
    }

    private void FillRow(DataGridViewRow row, TrainRun run)
    {
        var finished = run.IsFinished;
        var conflicts = _conflicts.Contains(run);
        var drawn = run.Drawn;

        SetCell(row, cState, StateImage(run, conflicts), StateTip(run, conflicts));
        SetCell(row, cTrain, run.Train.Number);
        SetCell(row, cType, run.Train.Type?.Key ?? "");
        SetCell(row, cRouting, RoutingImage(run.Train.Routing), run.Train.Routing?.Name);
        SetCell(row, cArrival, Time(run.PlannedArrival));
        SetCell(row, cArrivalDelay, run.PlannedArrival.HasValue ? DelayFormat.Format(run.ArrivalDelay) : "");
        SetCell(row, cDeparture, Time(run.PlannedDeparture));
        SetCell(row, cDepartureDelay, run.PlannedDeparture.HasValue ? DelayFormat.Format(run.DepartureDelay) : "");
        SetCell(row, cTrack, run.Track?.Key is { Length: > 0 } key ? key : null,
            conflicts ? ConflictTip(run) : run.Track?.FullName);
        SetCell(row, cTrackSource, Texts.Of(run.TrackSource));
        SetCell(row, cReason, ReasonText(run.DelayReason));
        SetCell(row, cNext, NextText(run));
        SetCell(row, cResponse, ResponseText(run), ResponseTip(run));
        SetCell(row, cName, run.Train.Name);

        // co sa este da menit
        row.Cells[cArrivalDelay.Index].ReadOnly = !run.PlannedArrival.HasValue || finished || run.IsDone(SimEventKind.Arrival);
        row.Cells[cDepartureDelay.Index].ReadOnly = !run.PlannedDeparture.HasValue || finished;
        row.Cells[cTrack.Index].ReadOnly = finished;
        row.Cells[cReason.Index].ReadOnly = finished;

        // rucne zmenene hodnoty tucne
        Bold(row.Cells[cArrivalDelay.Index], run.ArrivalDelay != drawn.ArrivalDelay);
        Bold(row.Cells[cDepartureDelay.Index], run.DepartureDelay != drawn.DepartureDelay);
        Bold(row.Cells[cTrack.Index], !ReferenceEquals(run.Track, drawn.Track));
        Bold(row.Cells[cReason.Index], run.DelayReason != drawn.DelayReason);

        var back = dgvPlan.DefaultCellStyle.BackColor;
        var fore = dgvPlan.DefaultCellStyle.ForeColor;
        row.DefaultCellStyle.ForeColor = finished || run.Suppressed ? Palette.Dim(fore, back) : Color.Empty;
        row.Cells[cTrack.Index].Style.ForeColor = conflicts ? Palette.Of(PaletteColor.Red, back) : Color.Empty;
        row.Cells[cResponse.Index].Style.ForeColor = _responses.TryGetValue(run, out var response) && !response.Ok
            ? Palette.Of(PaletteColor.Red, back)
            : Color.Empty;

        foreach (DataGridViewCell cell in row.Cells)
            cell.ErrorText = "";
    }

    private static void SetCell(DataGridViewRow row, DataGridViewColumn column, object? value, string? tip = null)
    {
        var cell = row.Cells[column.Index];
        if (!Equals(cell.Value, value))
            cell.Value = value;

        cell.ToolTipText = tip ?? "";
    }

    private void Bold(DataGridViewCell cell, bool bold) => cell.Style.Font = bold ? _boldCellFont : null;

    /// <summary>
    ///     Obnoví riadok jazdy po jej zmene alebo po odoslaní udalosti.
    /// </summary>
    private void RefreshRow(TrainRun run)
    {
        if (!_rowByRun.TryGetValue(run, out var row) || row.DataGridView is null)
            return;

        _refreshing = true;
        try
        {
            FillRow(row, run);
        }
        finally
        {
            _refreshing = false;
        }

        ApplyFilter(row);
        trackDiagram.Invalidate();
    }

    private void RefreshAllRows()
    {
        foreach (var run in _rowByRun.Keys)
            RefreshRow(run);

        RefreshDiagram();
    }

    private void UpdateConflicts() =>
        _conflicts = _engine?.Runs is { } runs ? PlanConflicts.All(runs) : new HashSet<TrainRun>();

    #endregion

    #region Texty a ikony buniek

    private static string Time(DateTime? time) => time?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? "";

    private Bitmap? StateImage(TrainRun run, bool conflicts)
    {
        var name = conflicts ? "status-warning"
            : run.Suppressed ? "send+delete"
            : run.IsFinished ? "status-ok"
            : run.Events.Any(simEvent => simEvent.IsDone) ? "train"
            : null;

        return name is null ? null : IconImages.For(dgvPlan, name);
    }

    private string StateTip(TrainRun run, bool conflicts) =>
        conflicts ? ConflictTip(run)
        : run.Suppressed ? Resources.Grid_StateSuppressed
        : run.IsFinished ? Resources.Grid_StateFinished
        : run.Events.Any(simEvent => simEvent.IsDone) ? Resources.Grid_StateRunning
        : Resources.Grid_StateWaiting;

    private string ConflictTip(TrainRun run) =>
        Texts.F(Resources.Grid_ConflictTip, run.Track.Key, string.Join(", ",
            PlanConflicts.With(run, _engine!.Runs!).Select(other =>
                Texts.F(Resources.Grid_ConflictWith, other.Train.Number, other.OccupiedFrom, other.OccupiedTo))));

    private Bitmap? RoutingImage(Routing? routing)
    {
        var name = routing?.CharSymbol switch
        {
            "K" => "arrival",
            "P" => "pass-through",
            "V" => "departure",
            _ => null
        };

        return name is null ? null : IconImages.For(dgvPlan, name);
    }

    private static string NextText(TrainRun run)
    {
        if (run.Suppressed)
            return Resources.Grid_NextSuppressed;

        if (run.NextEvent is not { } next)
            return run.IsFinished ? Resources.Grid_NextDone : "";

        var text = Texts.F(Resources.Grid_NextFormat, next.Time, Texts.Of(next.Kind));
        return next.IsCorrection ? text + Resources.Grid_NextCorrection : text;
    }

    #endregion

    #region Filter

    private void tstbSearch_TextChanged(object? sender, EventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        foreach (DataGridViewRow row in dgvPlan.Rows)
            ApplyFilter(row);
    }

    private void ApplyFilter(DataGridViewRow row)
    {
        var search = tstbSearch.Text.Trim();
        var number = row.Cells[cTrain.Index].Value as string ?? "";
        var name = row.Cells[cName.Index].Value as string ?? "";

        var matches = search.Length == 0 ||
                      number.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                      name.Contains(search, StringComparison.OrdinalIgnoreCase);
        var hidden = Config.View.HideFinished && row.Tag is TrainRun { IsFinished: true };
        var visible = matches && !hidden;

        if (row.Visible == visible)
            return;

        // riadok s aktualnou bunkou sa skryt neda
        if (!visible && dgvPlan.CurrentCell?.RowIndex == row.Index)
            dgvPlan.CurrentCell = null;

        row.Visible = visible;
    }

    /// <summary>
    ///     Posunie tabuľku na vlak, o ktorom práve odišla správa - výber ostane, ako ho má používateľ.
    /// </summary>
    private void FollowRun(TrainRun run)
    {
        if (!Config.View.FollowCurrent || !_rowByRun.TryGetValue(run, out var row) || !row.Visible || row.Displayed)
            return;

        var index = Math.Max(0, row.Index - 3);
        while (index < row.Index && !dgvPlan.Rows[index].Visible)
            index++;

        dgvPlan.FirstDisplayedScrollingRowIndex = index;
    }

    #endregion

    #region Výber a úpravy v tabuľke

    /// <summary>
    ///     Vybrané jazdy (riadky vynechaných dvojitých čísel sa nepočítajú).
    /// </summary>
    private List<TrainRun> SelectedRuns()
    {
        var runs = dgvPlan.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Tag)
            .OfType<TrainRun>()
            .ToList();

        if (runs.Count == 0 && dgvPlan.CurrentRow?.Tag is TrainRun current)
            runs.Add(current);

        return runs.OrderBy(run => _rowByRun.TryGetValue(run, out var row) ? row.Index : 0).ToList();
    }

    private void dgvPlan_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateCommandStates();
        FillManualFromSelection();
        SyncDiagramSelection();
    }

    private void dgvPlan_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        // pravy klik mimo vyberu vyberie riadok pod mysou, aby sa kontextova ponuka tykala neho
        if (e.Button != MouseButtons.Right || e.RowIndex < 0 || dgvPlan.Rows[e.RowIndex].Selected)
            return;

        dgvPlan.ClearSelection();
        dgvPlan.CurrentCell = dgvPlan.Rows[e.RowIndex].Cells[Math.Max(e.ColumnIndex, 0)];
        dgvPlan.Rows[e.RowIndex].Selected = true;
    }

    private void cmsPlan_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        UpdateCommandStates();
        e.Cancel = SelectedRuns().Count == 0;
    }

    private void dgvPlan_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        // vyber zo zoznamu (kolaj, dovod) plati hned, nie az po opusteni bunky
        if (dgvPlan.IsCurrentCellDirty && dgvPlan.CurrentCell?.OwningColumn is DataGridViewComboBoxColumn)
            dgvPlan.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void dgvPlan_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        // hodnota mimo zoznamu (napr. koľaj, ktorú grafikon nepozná) sa len zobrazí, nie je to chyba
        e.ThrowException = false;
    }

    private void dgvPlan_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_refreshing || _engine is null || e.RowIndex < 0 || dgvPlan.Rows[e.RowIndex].Tag is not TrainRun run)
            return;

        var cell = dgvPlan.Rows[e.RowIndex].Cells[e.ColumnIndex];
        var text = cell.Value as string;
        string? error = null;

        if (e.ColumnIndex == cArrivalDelay.Index || e.ColumnIndex == cDepartureDelay.Index)
        {
            if (!DelayFormat.TryParse(text, out var delay))
                error = Resources.Grid_DelayFormat;
            else
                error = Message(e.ColumnIndex == cArrivalDelay.Index
                    ? _engine.Edit(run, (r, context) => PlanEditing.SetArrivalDelay(r, delay, context))
                    : _engine.Edit(run, (r, context) => PlanEditing.SetDepartureDelay(r, delay, context)));
        }
        else if (e.ColumnIndex == cTrack.Index)
        {
            var track = UsableTracks().FirstOrDefault(candidate => candidate.Key == text);
            error = track is null
                ? Texts.Of(PlanEditError.TrackUnusable)
                : Message(_engine.Edit(run, (r, context) => PlanEditing.SetTrack(r, track, context)));
        }
        else if (e.ColumnIndex == cReason.Index)
        {
            var reason = ReasonNumber(text);
            error = Message(_engine.Edit(run, (r, context) => PlanEditing.SetDelayReason(r, reason, context)));
        }
        else
        {
            return;
        }

        // bunka sa obnovi az po skonceni upravy - zapis do upravovanej bunky v tejto obsluhe WinForms nedovoli
        var column = e.ColumnIndex;
        BeginInvoke(() =>
        {
            RefreshRow(run);

            if (error is null || !_rowByRun.TryGetValue(run, out var row))
                return;

            row.Cells[column].ErrorText = error;
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_EditFailed, run.Train.Number, error));
        });
    }

    private static string? Message(PlanEditError error) => error == PlanEditError.None ? null : Texts.Of(error);

    private int ReasonNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var number = text.Length >= 2 && int.TryParse(text[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        return _delayReasons.Any(reason => reason.Number == number) || number != 0 ? number : 0;
    }

    #endregion
}
