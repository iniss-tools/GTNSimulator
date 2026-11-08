using ILTISSimulator.Gtn;
using ILTISSimulator.Simulation;

namespace ILTISSimulator.DocScreenshots;

/// <summary>
///     Hlavné okno simulátora ovládané cez reflexiu - okno je v programe interné.
/// </summary>
internal sealed class MainWindow : IDisposable
{
    private readonly string _gvdDir;
    private readonly FakeIniss _iniss;

    public MainWindow(string gvdDir)
    {
        _gvdDir = gvdDir;

        var type = typeof(SimulationEngine).Assembly.GetType("ILTISSimulator.FMain", true)!;
        Form = (Form)Activator.CreateInstance(type, Program.Session)!;

        // FMain_Load by argumenty harnessu bral ako cestu ku grafikonu a registroval jump list
        Form.Load -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), Form, "FMain_Load");
        Form.StartPosition = FormStartPosition.Manual;
        Form.Location = new Point(40, 40);
        Form.Size = new Size(1360, 780);
        Form.Show();
        Pump.Events();

        Invoke("LoadGvd", gvdDir);
        Pump.Events();

        var install = Path.GetDirectoryName(Path.GetDirectoryName(gvdDir))!;
        var tracks = Gvd.Tracks.Select(track => track.Key);
        _iniss = new FakeIniss(Path.Combine(install, "Logy", "GTN.LOG"), Planned, tracks);
    }

    public Form Form { get; }

    private Data.GvdData Gvd => (Data.GvdData)Field("_gvd")!;

    private SimulationEngine? Engine => (SimulationEngine?)Field("_engine");

    /// <summary>
    ///     Deň v behu: pripojený INISS, naplánované od rána, niekoľko ručných zmien a udalosti do 9:15 odoslané.
    /// </summary>
    public void PrepareRunningDay(DateTime day)
    {
        Program.Session.Config.Connection.Host = "127.0.0.1";
        Program.Session.Config.Connection.Port = _iniss.Port;
        Invoke("ToggleConnection");

        ((ExControls.ExDateTimePicker)Field("dtpDay")!).Value = day;
        ((ExControls.ExDateTimePicker)Field("dtpFrom")!).Value = day.AddHours(5);
        Invoke("Plan", day, day.AddHours(5), null, null);
        var engine = Engine!;
        var runs = engine.Runs!;

        // rucne zmeny - meskanie s dovodom, ina kolaj, vlak bez sprav
        var withArrival = runs.Where(run => run.PlannedArrival.HasValue && run.PlannedArrival > day.AddHours(9)).ToList();
        if (withArrival.Count > 0)
        {
            engine.Edit(withArrival[0], (run, context) => PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(12), context));
            engine.Edit(withArrival[0], (run, context) => PlanEditing.SetDelayReason(run, 3, context));
        }

        var later = runs.Where(run => (run.PlannedArrival ?? run.PlannedDeparture) > day.AddHours(10)).ToList();
        if (later.Count > 1)
        {
            var other = Gvd.Tracks.Where(TrackAllocator.CanUse).First(track => track.Key != later[0].Track.Key);
            engine.Edit(later[0], (run, context) => PlanEditing.SetTrack(run, other, context));
            engine.Edit(later[1], (run, context) => PlanEditing.SetSuppressed(run, true, context));
        }

        var until = day.AddHours(9).AddMinutes(15);
        var sent = engine.Pump(until);
        engine.JumpTo(until);

        Pump.Until(() => _iniss.Lines >= sent, 10000);
        Pump.Events(30);
        Invoke("RefreshAllRows");
        Invoke("UpdateStatus");
        Pump.Events();
    }

    /// <summary>
    ///     Panel ručnej správy s vybraným vlakom.
    /// </summary>
    public void ShowManualMessage(bool visible)
    {
        Invoke("SetManualVisible", visible);
        if (!visible)
            return;

        var grid = (DataGridView)Field("dgvPlan")!;
        var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is TrainRun { IsFinished: false, Suppressed: false });
        if (row is not null)
        {
            grid.ClearSelection();
            grid.CurrentCell = row.Cells[1];
            row.Selected = true;
        }

        Pump.Events();
    }

    /// <summary>
    ///     Koľajový diagram pod tabuľkou s mierkou, pri ktorej je vidno väčšinu dňa, a vybraným vlakom.
    /// </summary>
    public void ShowTrackDiagram(bool visible, DateTime day)
    {
        Invoke("SetTrackDiagramVisible", visible);
        if (!visible)
            return;

        if (Engine?.Runs is null)
            PrepareRunningDay(day);

        var diagram = (Control)Field("trackDiagram")!;
        diagram.GetType().GetProperty("HourWidth")!.SetValue(diagram, diagram.LogicalToDeviceUnits(150));
        Invoke("ResetDiagram");
        ShowManualMessage(false);

        var grid = (DataGridView)Field("dgvPlan")!;
        var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is TrainRun { IsFinished: false, Suppressed: false });
        if (row is not null)
        {
            grid.CurrentCell = row.Cells[1];
            row.Selected = true;
        }

        Pump.Events();
    }

    public static Form CreateSettings(string page)
    {
        // bez prikazov hlavneho okna - strom skratiek podla katalogu (Activator nepozna volitelne parametre)
        var form = CreateDialog("ILTISSimulator.FAppSettings", Program.Session, null!);
        form.GetType().GetMethod("PreselectMenuItem", Program.Any)!.Invoke(form, [page]);
        return form;
    }

    private static readonly string[] ExcludedTrack = ["4"];

    public Form CreateTracks() => CreateDialog("ILTISSimulator.FTracks", Gvd.Tracks, ExcludedTrack);

    /// <summary>
    ///     Výluka koľaje, ktorú od 9:15 do 11:15 drží najviac vlakov.
    /// </summary>
    public Form CreateTrackClosure(DateTime day)
    {
        if (Engine?.Runs is null)
            PrepareRunningDay(day);

        var runs = Engine!.Runs!;
        var from = day.AddHours(9).AddMinutes(15);
        var closed = Gvd.Tracks.Where(TrackAllocator.CanUse)
            .MaxBy(track => TrackClosure.Affected(runs, track, from, from.AddHours(2)).Count)!;

        return CreateDialog("ILTISSimulator.FTrackClosure", Gvd.Tracks, runs, from, closed);
    }

    public static Form CreateDialog(string type, params object[] args) =>
        (Form)Activator.CreateInstance(typeof(SimulationEngine).Assembly.GetType(type, true)!, args)!;

    /// <summary>
    ///     Kontrola úprav v tabuľke bez klikania - zápis do buniek ako po úprave používateľom a porovnanie s jazdou.
    /// </summary>
    public void CheckEditing(List<string> log)
    {
        var grid = (DataGridView)Field("dgvPlan")!;
        var row = grid.Rows.Cast<DataGridViewRow>().First(r => r.Tag is TrainRun { IsFinished: false, Suppressed: false, PlannedArrival: not null } run
                                                             && !run.IsDone(SimEventKind.Arrival));
        var run = (TrainRun)row.Tag!;
        var other = Gvd.Tracks.Where(TrackAllocator.CanUse).First(track => track.Key != run.Track.Key);

        void Check(string what, bool ok) => log.Add($"kontrola {what}: {(ok ? "OK" : "CHYBA")}");

        row.Cells["cArrivalDelay"].Value = "+7";
        Pump.Events();
        Check("meškanie príchodu", run.ArrivalDelay == TimeSpan.FromMinutes(7));

        row.Cells["cTrack"].Value = other.Key;
        Pump.Events();
        Check("koľaj", run.Track.Key == other.Key && run.TrackSource == TrackSource.Manual);

        var reason = grid.Columns["cReason"] is DataGridViewComboBoxColumn combo ? combo.Items[2] as string : null;
        row.Cells["cReason"].Value = reason;
        Pump.Events();
        Check("dôvod", run.DelayReason == 2);

        row.Cells["cArrivalDelay"].Value = "x";
        Pump.Events();
        Check("zlé meškanie", run.ArrivalDelay == TimeSpan.FromMinutes(7) && row.Cells["cArrivalDelay"].ErrorText.Length > 0);

        grid.ClearSelection();
        row.Selected = true;
        Invoke("RevertSelected");
        Pump.Events();
        Check("vrátiť", !run.IsChanged);
    }

    /// <summary>
    ///     Kontrola koľajového diagramu bez klikania - vlak potiahnutý myšou na inú koľaj a o 10 minút neskôr.
    /// </summary>
    public void CheckDiagram(List<string> log, DateTime day)
    {
        void Check(string what, bool ok) => log.Add($"kontrola {what}: {(ok ? "OK" : "CHYBA")}");

        ShowTrackDiagram(true, day);
        var diagram = (Control)Field("trackDiagram")!;
        var type = diagram.GetType();
        var hitBar = type.GetMethod("HitBar", Program.Any)!;
        var laneAt = type.GetMethod("LaneAt", Program.Any)!;
        var lanes = (System.Collections.IList)type.GetField("_lanes", Program.Any)!.GetValue(diagram)!;

        TrainRun? run = null;
        var at = Point.Empty;
        for (var y = 0; y < diagram.Height && run is null; y += 2)
            for (var x = 0; x < diagram.Width; x += 2)
            {
                if (hitBar.Invoke(diagram, [new Point(x, y)]) is not { } result)
                    continue;

                var bar = result.GetType().GetField("Item1")!.GetValue(result)!;
                var candidate = (TrainRun)bar.GetType().GetProperty("Run")!.GetValue(bar)!;
                if (candidate is { IsFinished: false, Suppressed: false, PlannedArrival: not null } && !candidate.IsDone(SimEventKind.Arrival))
                {
                    run = candidate;
                    at = new Point(x, y);
                    break;
                }
            }

        if (run is null)
        {
            Check("diagram - vlak na ťahanie", false);
            return;
        }

        var lane = (int)laneAt.Invoke(diagram, [at.Y])!;
        var targetY = Enumerable.Range(0, diagram.Height).FirstOrDefault(y =>
            (int)laneAt.Invoke(diagram, [y])! is var other && other >= 0 && other != lane &&
            TrackAllocator.CanUse((GVDEditor.Domain.Entities.Track)lanes[other]!.GetType().GetProperty("Track")!.GetValue(lanes[other])!), -1);
        var target = (GVDEditor.Domain.Entities.Track)lanes[(int)laneAt.Invoke(diagram, [targetY])!]!.GetType().GetProperty("Track")!.GetValue(
            lanes[(int)laneAt.Invoke(diagram, [targetY])!])!;
        var hourWidth = (int)type.GetProperty("HourWidth")!.GetValue(diagram)!;
        var delay = run.ArrivalDelay;

        void Mouse(string name, int x, int y) =>
            type.GetMethod(name, Program.Any, [typeof(MouseEventArgs)])!.Invoke(diagram, [new MouseEventArgs(MouseButtons.Left, 1, x, y, 0)]);

        Mouse("OnMouseDown", at.X, at.Y);
        Pump.Events();
        var selected = (System.Collections.Generic.List<TrainRun>)Invoke("SelectedRuns")!;
        Check("diagram - výber v tabuľke", selected.Count == 1 && selected[0] == run);

        var dx = (int)Math.Round(10 * hourWidth / 60.0);
        Mouse("OnMouseMove", at.X + dx / 2, targetY);
        Mouse("OnMouseMove", at.X + dx, targetY);
        Mouse("OnMouseUp", at.X + dx, targetY);
        Pump.Events();

        Check("diagram - koľaj ťahaním", run.Track.Key == target.Key && run.TrackSource == TrackSource.Manual);
        Check("diagram - meškanie ťahaním", run.ArrivalDelay == delay + TimeSpan.FromMinutes(10));

        Invoke("RevertSelected");
        Pump.Events();
        Check("diagram - vrátiť", !run.IsChanged);

        ShowTrackDiagram(false, day);
    }

    private DateTime? Planned(string number, GtnMessageType type)
    {
        var run = Engine?.Runs?.FirstOrDefault(candidate => candidate.Train.Number == number);
        return type == GtnMessageType.Arrival ? run?.PlannedArrival : run?.PlannedDeparture;
    }

    private object? Field(string name) => Form.GetType().GetField(name, Program.Any)!.GetValue(Form);

    private object? Invoke(string name, params object?[] args)
    {
        var method = Form.GetType().GetMethods(Program.Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length);
        return method.Invoke(Form, args);
    }

    public void Dispose()
    {
        Form.Close();
        Form.Dispose();
        _iniss.Dispose();
    }
}
