using ExControls;
using GTNSimulator.Gtn;
using GTNSimulator.Net;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;
using GTNSimulator.Tools;
using Serilog;

namespace GTNSimulator;

internal sealed partial class FMain
{
    /// <summary>
    ///     Zrýchlenia virtuálneho času v zozname na paneli nástrojov.
    /// </summary>
    private static readonly int[] Speeds = [1, 2, 5, 10, 30, 60, 120, 240, 600];

    private SimulationEngine? _engine;
    private GtnClient? _client;
    private volatile bool _sendToIniss = true;
    private int _sentCount;

    private ExDateTimePicker dtpDay = null!;
    private ExDateTimePicker dtpFrom = null!;
    private bool _speedUpdating;

    #region Panel nástrojov

    /// <summary>
    ///     Výber dňa a času na paneli nástrojov a zoznam zrýchlení. Prvky ExControls sa do panela vkladajú za behu -
    ///     návrhár hostiteľa prvku <see cref="ToolStripControlHost" /> neuloží.
    /// </summary>
    private void InitToolbar()
    {
        dtpDay = new ExDateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd.MM.yyyy",
            Width = LogicalToDeviceUnits(100),
            Value = DateTime.Today
        };
        dtpFrom = new ExDateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm:ss",
            ShowUpDown = true,
            Width = LogicalToDeviceUnits(80),
            Value = DateTime.Now
        };

        toolStripMain.Items.Insert(toolStripMain.Items.IndexOf(tslDay) + 1, new ToolStripControlHost(dtpDay) { Name = "tshDay" });
        toolStripMain.Items.Insert(toolStripMain.Items.IndexOf(tslFrom) + 1, new ToolStripControlHost(dtpFrom) { Name = "tshFrom" });

        _speedUpdating = true;
        foreach (var speed in Speeds)
            tscbSpeed.Items.Add(Texts.F(Resources.Main_Speed, speed));

        SelectSpeed(Config.Simulation.Speed);
        _speedUpdating = false;

        ttMain.SetToolTip(tbManual, Resources.Main_ManualTip);
        ttMain.SetToolTip(cbManualNow, Resources.Main_ManualNowTip);
        ttMain.SetToolTip(cbManualRegular, Resources.Main_ManualRegularTip);
        tmrClock.Start();
    }

    private void SelectSpeed(int speed)
    {
        var index = Array.FindIndex(Speeds, candidate => candidate >= speed);
        tscbSpeed.SelectedIndex = index < 0 ? Speeds.Length - 1 : index;
    }

    private int SelectedSpeed => tscbSpeed.SelectedIndex < 0 ? Config.Simulation.Speed : Speeds[tscbSpeed.SelectedIndex];

    private void tscbSpeed_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_speedUpdating)
            return;

        Config.Simulation.Speed = SelectedSpeed;
        _engine?.SetTimeFactor(SelectedSpeed);
        SaveConfig();
    }

    private void SetSpeed(int speed)
    {
        _speedUpdating = true;
        SelectSpeed(speed);
        _speedUpdating = false;

        Config.Simulation.Speed = SelectedSpeed;
        _engine?.SetTimeFactor(SelectedSpeed);
    }

    #endregion

    #region Spojenie

    private void ToggleConnection()
    {
        if (_client is { IsConnected: true })
        {
            Log.Information("Spojenie s INISS {Host}:{Port} zatvorene", _client.Host, _client.Port);
            _client.Dispose();
            _client = null;
            AppendLog(LogKind.Info, Resources.Log_Disconnected);
            UpdateConnectionLabel();
            return;
        }

        var host = (_commandLine.Host ?? Config.Connection.Host).Trim();
        var port = _commandLine.Port ?? _gvd?.Iniss2?.GtnPort ?? Config.Connection.Port;

        try
        {
            _client?.Dispose();
            _client = new GtnClient(host, port);
            _client.Connect();

            AppendLog(LogKind.Connected, Texts.F(Resources.Log_Connected, _client.Host, _client.Port));
        }
        catch (Exception e) when (e is System.Net.Sockets.SocketException or IOException or ArgumentException)
        {
            _client = null;
            Log.Warning(e, "Pripojenie k INISS {Host}:{Port} zlyhalo", host, port);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_ConnectFailed, host, port, e.Message));
        }

        UpdateConnectionLabel();
    }

    private void UpdateConnectionLabel()
    {
        var connected = _client is { IsConnected: true };

        tslConnection.Text = connected
            ? Texts.F(Resources.Main_ConnectedTo, _client!.Host, _client.Port)
            : Resources.Main_Disconnected;
        tslConnection.ForeColor = Palette.Of(connected ? PaletteColor.Green : PaletteColor.Red, toolStripMain.BackColor);

        tsbConnect.Text = connected ? Resources.Main_Disconnect : Resources.Main_Connect;
        tsbConnect.ToolTipText = connected ? Resources.Main_DisconnectTip : Resources.Main_ConnectTip;
        tsbConnect.IconName = connected ? "link-dismiss" : "link";
        tsmimConnect.Text = connected ? Resources.Main_DisconnectMenu : Resources.Main_ConnectMenu;
        tsmimConnect.IconName = tsbConnect.IconName;
    }

    #endregion

    #region Plán a beh

    private SimulationOptions ReadOptions()
    {
        var settings = Config.Simulation;

        return new SimulationOptions
        {
            Seed = _commandLine.Seed ?? settings.Seed,
            TimeFactor = SelectedSpeed,
            OnTimeProbability = settings.OnTimePercent / 100.0,
            MeanDelayMinutes = settings.MeanDelayMinutes,
            TrackChangeProbability = settings.TrackChangePercent / 100.0,
            DelayReasonProbability = settings.DelayReasonPercent / 100.0,
            EntryRouteLead = TimeSpan.FromMinutes(settings.EntryRouteLeadMinutes),
            ExitRouteLead = TimeSpan.FromMinutes(settings.ExitRouteLeadMinutes),
            SendLineTrack = settings.SendLineTrack,
            LineTrack = settings.LineTrack,
            DelayReasons = UsableReasons().Select(reason => reason.Number).ToList(),
            ExcludedTrackKeys = new HashSet<string>(_excludedTracks, StringComparer.OrdinalIgnoreCase)
        };
    }

    private void PlanDay()
    {
        if (_gvd is null || !ConfirmDiscardChanges())
            return;

        var day = dtpDay.Value.Date;
        Plan(day, day + dtpFrom.Value.TimeOfDay);
    }

    /// <summary>
    ///     Naplánuje deň a pripraví beh od zadaného času.
    /// </summary>
    /// <param name="day">Deň.</param>
    /// <param name="from">Od kedy - skoršie udalosti sa preskočia.</param>
    /// <param name="options">Nastavenia simulácie, predvolene z nastavení programu.</param>
    /// <param name="prepare">Zmeny jázd pred začiatkom - viď <see cref="SimulationEngine.PlanDay" />.</param>
    /// <returns><see langword="true" />, ak sa plánovanie podarilo.</returns>
    private bool Plan(DateTime day, DateTime from, SimulationOptions? options = null,
        Action<IReadOnlyList<TrainRun>, PlanEditContext>? prepare = null)
    {
        if (_gvd is null)
            return false;

        try
        {
            DisposeEngine();

            _engine = new SimulationEngine(_gvd, options ?? ReadOptions());
            _engine.Log += Engine_Log;
            _engine.EventDue += Engine_EventDue;
            _engine.PlanChanged += Engine_PlanChanged;
            _engine.PlanDay(day, from, prepare);

            _sentCount = 0;
            ClearResponses();
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            DisposeEngine();
            Log.Error(e, "Naplanovanie dna {Day:yyyy-MM-dd} od {From:HH:mm} zlyhalo", day, from);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_PlanFailed, e.Message));
            return false;
        }
        finally
        {
            FillGrid();
            UpdateStatus();
            UpdateCommandStates();
        }

        return true;
    }

    private void StartRun()
    {
        if (_engine?.Timeline is null)
            return;

        _engine.Start();
        UpdateClock();
        UpdateCommandStates();
    }

    private void PauseRun()
    {
        _engine?.Pause();
        UpdateClock();
        UpdateCommandStates();
    }

    private void StopRun()
    {
        if (_engine is null)
            return;

        _engine.Stop();
        _sentCount = 0;
        ClearResponses();
        UpdateClock();
        UpdateStatus();
        UpdateCommandStates();
    }

    private void JumpRun()
    {
        if (_engine?.Timeline is null)
            return;

        _engine.JumpTo(_engine.Day + dtpFrom.Value.TimeOfDay);
        RefreshAllRows();
        UpdateClock();
        UpdateStatus();
        UpdateCommandStates();
    }

    /// <summary>
    ///     Reálny čas: dnešný deň od aktuálneho času so zrýchlením 1×. Automatika INISSu sa riadi jeho hodinami,
    ///     takže len tak sedia časy v správach s tým, kedy INISS hlási. Už naplánovaný dnešok sa neprepláva -
    ///     posunie sa na teraz a ručné zmeny ostanú.
    /// </summary>
    private void RunNow()
    {
        var now = DateTime.Now;

        dtpDay.Value = now.Date;
        dtpFrom.Value = now;
        SetSpeed(1);
        SaveConfig();

        if (_engine?.Timeline is not null && _engine.Day == now.Date)
        {
            if (_engine.VirtualTime < now)
                _engine.JumpTo(now);
            else
                AppendLog(LogKind.Warning, Texts.F(Resources.Log_AheadOfNow, _engine.VirtualTime));

            RefreshAllRows();
        }
        else if (!ConfirmDiscardChanges() || !Plan(now.Date, now))
        {
            return;
        }

        StartRun();
    }

    private void tmrClock_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        UpdateStatus();
        UpdateConnectionLabel();
        UpdateCommandStates();
        RefreshManualTime();
        PollGtnLog();
    }

    private void UpdateClock()
    {
        if (_engine?.Timeline is null)
        {
            tslClock.Text = @"--:--:--";
            UpdateDiagramClock();
            return;
        }

        var time = _engine.VirtualTime;
        tslClock.Text = time.Date == _engine.Day
            ? time.ToString("HH:mm:ss", CultureInfo.CurrentCulture)
            : time.ToString("d.M. HH:mm:ss", CultureInfo.CurrentCulture);
        tslClock.ForeColor = _engine.IsRunning
            ? Palette.Of(PaletteColor.Green, toolStripMain.BackColor)
            : toolStripMain.ForeColor;
        UpdateDiagramClock();
    }

    private void UpdateStatus()
    {
        if (_gvd is null)
        {
            tsslStation.Text = Resources.Main_StatusNoGvd;
        }
        else
        {
            var usable = UsableTracks().Count;
            var reasons = Texts.Count(UsableReasons().Count, Resources.Main_ReasonCountOne, Resources.Main_ReasonCountFew,
                Resources.Main_ReasonCountMany);

            tsslStation.Text = Texts.F(Resources.Main_StatusGvd, _gvd.Info.ThisStation.Name, _gvd.StationId,
                _gvd.Trains.Count, usable - _excludedTracks.Count(key => UsableTracks().Any(track => track.Key == key)), usable,
                reasons);
            tsslStation.ToolTipText = Texts.F(Resources.Main_StatusGvdTip, _gvdDir, StationField, _gvd.Info.StartValidData,
                _gvd.Info.EndValidData);
        }

        if (_engine?.Timeline is null)
        {
            tsslPlan.Text = Resources.Main_StatusNoPlan;
            tsslNext.Text = "";
        }
        else
        {
            var (done, total) = _engine.Progress();
            tsslPlan.Text = Texts.F(Resources.Main_StatusEvents, done, total);

            var next = _engine.NextEvent();
            tsslNext.Text = next is null
                ? Resources.Main_StatusDayDone
                : Texts.F(Resources.Main_StatusNext, next.Time, Texts.Of(next.Kind), next.Run.Train.Number);
        }

        tsslSent.Text = Texts.F(Resources.Main_StatusSent, _sentCount);
    }

    private void DisposeEngine()
    {
        if (_engine is null)
            return;

        _engine.EventDue -= Engine_EventDue;
        _engine.Log -= Engine_Log;
        _engine.PlanChanged -= Engine_PlanChanged;
        _engine.Dispose();
        _engine = null;
    }

    #endregion

    #region Odosielanie

    private void Engine_EventDue(object? sender, SimEventDueEventArgs e)
    {
        var wire = GtnEncoder.Encode(e.Message);
        var send = _sendToIniss;
        var client = _client;
        string? error = null;

        if (send)
        {
            if (client is null)
                error = Resources.Log_NoConnection;
            else
                try
                {
                    client.Send(e.Message);
                    Interlocked.Increment(ref _sentCount);
                }
                catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or ObjectDisposedException
                                               or InvalidOperationException)
                {
                    Log.Warning(ex, "Spravu {Message} sa nepodarilo odoslat", e.Message);
                    error = ex.Message;
                }
        }

        RunOnUi(() =>
        {
            if (error is null)
            {
                AppendLog(send ? LogKind.Sent : LogKind.Info, $"{e.SimEvent}  ->  \"{wire}\"");
                if (send)
                    ExpectResponse(e.SimEvent.Run, wire);
            }
            else
            {
                AppendLog(LogKind.Error, Texts.F(Resources.Log_NotSent, e.SimEvent, error));
            }

            RefreshRow(e.SimEvent.Run);
            FollowRun(e.SimEvent.Run);
        });
    }

    private void Engine_Log(object? sender, string text) => RunOnUi(() => AppendLog(LogKind.Info, text));

    private void Engine_PlanChanged(object? sender, PlanChangedEventArgs e) => RunOnUi(() =>
    {
        UpdateConflicts();
        RefreshAllRows();
        UpdateStatus();
        UpdateCommandStates();
    });

    /// <summary>
    ///     Odošle správu mimo plánu - hlásenie o prestupe alebo ručnú správu.
    /// </summary>
    /// <param name="text">Text správy.</param>
    /// <returns><see langword="true" />, ak správa odišla.</returns>
    private bool SendRaw(string text)
    {
        if (_client is null)
        {
            AppendLog(LogKind.Error, Resources.Log_NoConnection);
            return false;
        }

        try
        {
            _client.SendRaw(text);
            Interlocked.Increment(ref _sentCount);
            AppendLog(LogKind.Sent, $"\"{text}\"");
            UpdateStatus();
            return true;
        }
        catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException or ObjectDisposedException
                                      or InvalidOperationException)
        {
            Log.Warning(e, "Rucnu spravu {Message} sa nepodarilo odoslat", text);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_SendFailed, e.Message));
            return false;
        }
    }

    private void RunOnUi(Action action)
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        try
        {
            BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
            // okno sa medzitym zavrelo
        }
        catch (InvalidOperationException)
        {
            // handle okna uz neexistuje
        }
    }

    #endregion

    #region Zmeny vybraných vlakov

    /// <summary>
    ///     Zmení vybrané vlaky a ohlási, ktoré sa zmeniť nedali.
    /// </summary>
    private void EditSelected(Func<TrainRun, PlanEditContext, PlanEditError> edit, bool sendNow = false)
    {
        if (_engine is null)
            return;

        var runs = SelectedRuns();
        if (runs.Count == 0)
            return;

        ReportFailures(_engine.Edit(runs, edit));

        // "teraz" znamena teraz aj pri pozastavenej simulacii
        if (sendNow)
            _engine.Pump(_engine.VirtualTime);
    }

    private void ReportFailures(IReadOnlyDictionary<TrainRun, PlanEditError> failed)
    {
        foreach (var (run, error) in failed)
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_EditFailed, run.Train.Number, Texts.Of(error)));
    }

    private void SendNextNow() => EditSelected((run, context) => run.NextEvent is { } next
        ? PlanEditing.SendNow(run, next.Kind, context)
        : PlanEditError.NotPending, true);

    private void SendNow(SimEventKind kind) =>
        EditSelected((run, context) => PlanEditing.SendNow(run, kind, context), true);

    private void ToggleSuppressed()
    {
        var suppress = !SelectedRuns().All(run => run.Suppressed);
        EditSelected((run, context) => PlanEditing.SetSuppressed(run, suppress, context));
    }

    private void RevertSelected() => EditSelected(PlanEditing.Revert);

    private void AddDelay()
    {
        if (_engine is null)
            return;

        var selected = SelectedRuns();
        using var dialog = new FAddDelay(selected.Count, _engine.VirtualTime);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var extra = TimeSpan.FromMinutes(dialog.Minutes);
        var runs = dialog.AllFrom is { } from
            ? _engine.Runs!.Where(run => !run.IsFinished && !run.Suppressed && (run.ActualArrival ?? run.ActualDeparture) >= from).ToList()
            : selected;

        var failed = _engine.Edit(runs, (run, context) => PlanEditing.AddDelay(run, extra, context));
        ReportFailures(failed);
        AppendLog(LogKind.Info, Texts.F(Resources.Log_DelayAdded, dialog.Minutes, runs.Count - failed.Count));
    }

    /// <summary>
    ///     Výluka koľaje v intervale - vlaky, ktoré koľaj v tom čase držia, sa presunú na voľnú koľaj.
    /// </summary>
    private void CloseTrack()
    {
        if (_engine?.Runs is not { } runs || _gvd is null)
            return;

        using var dialog = new FTrackClosure(_gvd.Tracks, runs, _engine.VirtualTime, SelectedRuns().FirstOrDefault()?.Track);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Track is not { } closed)
            return;

        var affected = dialog.Affected;
        var candidates = TrackClosure.Candidates(_gvd.Tracks, closed, _excludedTracks);
        var failed = _engine.Edit(affected, (run, context) => TrackClosure.MoveOff(run, runs, candidates, context));

        foreach (var run in affected.Where(run => !failed.ContainsKey(run)))
            AppendLog(LogKind.Info, Texts.F(Resources.Log_ClosureMoved, run.Train.Number, closed.Key, run.Track?.Key));
        ReportFailures(failed);
        AppendLog(LogKind.Info, Texts.F(Resources.Log_Closure, closed.Key, dialog.From, dialog.To, affected.Count - failed.Count,
            affected.Count));
    }

    /// <summary>
    ///     Hlásenie o prestupe (080-6) pre vybrané vlaky. INISS ho porovnáva so svojimi hodinami (±60 s), preto ide
    ///     so skutočným časom, nie s virtuálnym.
    /// </summary>
    private void SendTransfer(GtnTransferKind kind)
    {
        foreach (var run in SelectedRuns())
        {
            var message = new GtnMessage
            {
                Type = GtnMessageType.Transfer,
                StationField = StationField,
                TrainNumber = run.Train.Number,
                Time = DateTime.Now,
                TransferKind = kind
            };

            SendRaw(GtnEncoder.Encode(message));
        }
    }

    #endregion
}
