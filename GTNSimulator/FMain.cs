using ExControls;
using GTNSimulator.Gtn;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;
using GTNSimulator.Tools;
using GTNSimulator.XML;
using Serilog;
using ToolsCore.Commands;
using ToolsCore.Forms;
using ToolsCore.Iniss.Tools;
using ToolsCore.Tools;
using ToolsCore.Updates;
using ToolsCore.XML;
using ToolsCore;
using Utils = ToolsCore.Tools.Utils;

namespace GTNSimulator;

/// <summary>
///     Hlavné okno simulátora GTN: plán dňa v tabuľke, ovládanie behu a ručné správy.
/// </summary>
internal sealed partial class FMain : ExForm
{
    /// <summary>
    ///     Adresa dokumentácie programu.
    /// </summary>
    private const string DocsLink = "https://iniss-tools.fvat.sk/docs/gtnsimulator/";

    private readonly AppSession<GTNSimulatorConfig, GTNSimulatorStyle> _session;
    private readonly CommandLine _commandLine;
    private readonly CommandSet _commands = new();

    /// <summary>
    ///     Vytvorí hlavné okno bez argumentov príkazového riadka.
    /// </summary>
    /// <param name="session">Nastavenia programu.</param>
    public FMain(AppSession<GTNSimulatorConfig, GTNSimulatorStyle> session) : this(session, CommandLine.Empty)
    {
    }

    /// <summary>
    ///     Vytvorí hlavné okno.
    /// </summary>
    /// <param name="session">Nastavenia programu.</param>
    /// <param name="commandLine">Argumenty príkazového riadka - vykonajú sa po zobrazení okna.</param>
    public FMain(AppSession<GTNSimulatorConfig, GTNSimulatorStyle> session, CommandLine commandLine)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));

        InitializeComponent();
        InitDocking();
        InitToolbar();

        this.ApplyThemeAndFonts();
        AppDock.Restore(_dock, Config.DockLayout);
        ApplySearchBoxTheme();
        ApplyMenuMode();

        CreateCommands();
        _commands.ApplyShortcuts(Config.Shortcuts);

        InitGrid();
        InitView();
        InitManual();

        UpdateTitle();
        UpdateStatus();
        UpdateConnectionLabel();
        UpdateCommandStates();
    }

    private GTNSimulatorConfig Config => _session.Config;

    #region Príkazy

    /// <summary>
    ///     Príkazy hlavného okna naviazané naraz na ponuku, panel nástrojov a kontextovú ponuku tabuľky.
    /// </summary>
    private void CreateCommands()
    {
        Func<bool> gvd = () => _gvd is not null;
        Func<bool> plan = () => _engine?.Timeline is not null;
        Func<bool> selection = () => plan() && SelectedRuns().Count > 0;
        // server INISS2 prijima len spravy o pohybe vlaku
        Func<bool> iniss3Selection = () => selection() && _gvd is { IsIniss2: false };

        _commands.Add(GtnSimCommands.OpenGvd, ChooseGvd).Bind(tsmimOpenGvd, tssbOpenGvd);
        _commands.Add(GtnSimCommands.ReloadGvd, () => LoadGvd(_gvdDir!), () => _gvdDir is not null).Bind(tsmimReloadGvd, tsbReloadGvd);
        _commands.Add(GtnSimCommands.OpenScenario, OpenScenario, gvd).Bind(tsmimOpenScenario, tsbOpenScenario);
        _commands.Add(GtnSimCommands.SaveScenario, SaveScenario, plan).Bind(tsmimSaveScenario, tsbSaveScenario);
        _commands.Add(GtnSimCommands.Tracks, ShowTracks, gvd).Bind(tsmimTracks, tsbTracks);
        _commands.Add(GtnSimCommands.Exit, Close).Bind(tsmimExit);

        _commands.Add(GtnSimCommands.Connect, ToggleConnection).Bind(tsmimConnect, tsbConnect);
        _commands.Add(GtnSimCommands.Plan, PlanDay, gvd).Bind(tsmimPlan, tsbPlan);
        _commands.Add(GtnSimCommands.Start, StartRun, () => plan() && !_engine!.IsRunning).Bind(tsmimStart, tsbStart);
        _commands.Add(GtnSimCommands.Pause, PauseRun, () => _engine is { IsRunning: true }).Bind(tsmimPause, tsbPause);
        _commands.Add(GtnSimCommands.Stop, StopRun, plan).Bind(tsmimStop, tsbStop);
        _commands.Add(GtnSimCommands.Jump, JumpRun, plan).Bind(tsmimJump, tsbJump);
        _commands.Add(GtnSimCommands.Now, RunNow, gvd).Bind(tsmimNow, tsbNow);
        _commands.Add(GtnSimCommands.SendToIniss, () => SetSendToIniss(!_sendToIniss)).Bind(tsmimSendToIniss, tsbSendToIniss);

        _commands.Add(GtnSimCommands.SendNext, SendNextNow, selection).Bind(tsmimSendNext, cmimSendNext, tssbSendNow);
        _commands.Add(GtnSimCommands.SendEntryRoute, () => SendNow(SimEventKind.EntryRoute), selection)
            .Bind(tsmimSendEntryRoute, cmimSendEntryRoute, tsmiSendNowEntryRoute);
        _commands.Add(GtnSimCommands.SendReason, () => SendNow(SimEventKind.DelayReason), iniss3Selection)
            .Bind(tsmimSendReason, cmimSendReason, tsmiSendNowReason);
        _commands.Add(GtnSimCommands.SendArrival, () => SendNow(SimEventKind.Arrival), selection)
            .Bind(tsmimSendArrival, cmimSendArrival, tsmiSendNowArrival);
        _commands.Add(GtnSimCommands.SendExitRoute, () => SendNow(SimEventKind.ExitRoute), selection)
            .Bind(tsmimSendExitRoute, cmimSendExitRoute, tsmiSendNowExitRoute);
        _commands.Add(GtnSimCommands.SendDeparture, () => SendNow(SimEventKind.Departure), selection)
            .Bind(tsmimSendDeparture, cmimSendDeparture, tsmiSendNowDeparture);
        _commands.Add(GtnSimCommands.Transfer, () => SendTransfer(GtnTransferKind.Normal), iniss3Selection)
            .Bind(tsmimTransfer, cmimTransfer);
        _commands.Add(GtnSimCommands.ExpressTransfer, () => SendTransfer(GtnTransferKind.Express), iniss3Selection)
            .Bind(tsmimExpressTransfer, cmimExpressTransfer);
        _commands.Add(GtnSimCommands.AddDelay, AddDelay, plan).Bind(tsmimAddDelay, cmimAddDelay, tsbAddDelay);
        _commands.Add(GtnSimCommands.TrackClosure, CloseTrack, plan).Bind(tsmimTrackClosure, tsbTrackClosure);
        _commands.Add(GtnSimCommands.Suppress, ToggleSuppressed, selection).Bind(tsmimSuppress, cmimSuppress, tsbSuppress);
        _commands.Add(GtnSimCommands.Revert, RevertSelected, selection).Bind(tsmimRevert, cmimRevert, tsbRevert);

        _commands.Add(GtnSimCommands.TrackDiagram, () => SetTrackDiagramVisible(!Config.View.TrackDiagram)).Bind(tsmimTrackDiagram, tsbTrackDiagram);
        _commands.Add(GtnSimCommands.HideFinished, () => SetHideFinished(!Config.View.HideFinished)).Bind(tsmimHideFinished, tsbHideFinished);
        _commands.Add(GtnSimCommands.Follow, () => SetFollow(!Config.View.FollowCurrent)).Bind(tsmimFollow, tsbFollow);
        _commands.Add(GtnSimCommands.Find, FocusSearch).Bind(tsmimFind);
        _commands.Add(GtnSimCommands.TopMost, () => SetTopMost(!Config.View.TopMost)).Bind(tsmimTopMost);
        CreateViewCommands();

        _commands.Add(GtnSimCommands.AppSettings, ShowAppSettings).Bind(tsmimAppSettings, tsbAppSettings);
        _commands.Add(GtnSimCommands.Docs, () => Utils.OpenShell(DocsLink)).Bind(tsmimDocs);
        _commands.Add(GtnSimCommands.InfoApp, ShowInfoApp).Bind(tsmimInfoApp);
        _commands.Add(GtnSimCommands.CheckUpdates, () => AppUpdates.CheckNow(this), () => AppUpdates.Available).Bind(tsmimCheckUpdates);
        _commands.Add(GtnSimCommands.ShowLogs, ErrorReport.OpenLogFolder).Bind(tsmimShowLogs);
    }

    /// <summary>
    ///     Povolí príkazy podľa stavu grafikonu, plánu a výberu. Prepínače dostanú svoj stav.
    /// </summary>
    private void UpdateCommandStates()
    {
        _commands.UpdateStates();

        var selected = SelectedRuns();
        var hasPlan = _engine?.Timeline is not null;

        tsmimSendNow.Enabled = hasPlan && selected.Count > 0;
        cmimSendNow.Enabled = tsmimSendNow.Enabled;

        SetChecked(GtnSimCommands.SendToIniss, _sendToIniss);
        SetChecked(GtnSimCommands.TrackDiagram, Config.View.TrackDiagram);
        SetChecked(GtnSimCommands.HideFinished, Config.View.HideFinished);
        SetChecked(GtnSimCommands.Follow, Config.View.FollowCurrent);
        SetChecked(GtnSimCommands.TopMost, Config.View.TopMost);
        SetChecked(GtnSimCommands.Suppress, selected.Count > 0 && selected.All(run => run.Suppressed));

        // jednotlive udalosti len ked na ne este niektory vybrany vlak caka
        foreach (var (command, kind) in new[]
                 {
                     (GtnSimCommands.SendEntryRoute, SimEventKind.EntryRoute),
                     (GtnSimCommands.SendReason, SimEventKind.DelayReason),
                     (GtnSimCommands.SendArrival, SimEventKind.Arrival),
                     (GtnSimCommands.SendExitRoute, SimEventKind.ExitRoute),
                     (GtnSimCommands.SendDeparture, SimEventKind.Departure)
                 })
        {
            var pending = selected.Any(run => run.Events.Any(simEvent => simEvent.Kind == kind && !simEvent.IsDone));
            foreach (var item in _commands[command.Id].Items)
                item.Enabled &= pending;
        }
    }

    private void SetChecked(CommandInfo command, bool value)
    {
        foreach (var item in _commands[command.Id].Items)
            switch (item)
            {
                case ToolStripMenuItem menuItem:
                    menuItem.Checked = value;
                    break;
                case ToolStripButton button:
                    button.Checked = value;
                    break;
            }
    }

    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // pocas upravy bunky a pisania patria klavesy prvku (Ctrl+Del, Ctrl+D...), funkcne klavesy ostavaju prikazom
        var key = keyData & Keys.KeyCode;
        var typing = dgvPlan.IsCurrentCellInEditMode || tstbSearch.Focused || FocusedControl() is TextBoxBase or ComboBox;
        if (typing && key is < Keys.F1 or > Keys.F24)
            return base.ProcessCmdKey(ref msg, keyData);

        return _commands.ProcessShortcut(keyData) || base.ProcessCmdKey(ref msg, keyData);
    }

    #endregion

    #region Vzhľad a nastavenia

    /// <summary>
    ///     Panely nástrojov podľa nastavenia zobrazenia (ponuka je vždy).
    /// </summary>
    private void ApplyMenuMode()
    {
        // Visible prvku je false, kym okno nie je zobrazene - stav sa preto berie z nastavenia
        toolStripMain.Visible = Config.ShowToolbar;
        toolStripPlan.Visible = Config.ShowToolbar;
    }

    /// <summary>
    ///     Prekreslí okno po zmene nastavení programu.
    /// </summary>
    private void UpdateMainUi()
    {
        this.ApplyThemeAndFonts();
        ApplySearchBoxTheme();
        ApplyMenuMode();
        _commands.ApplyShortcuts(Config.Shortcuts);
        AppInit.MsgBoxStyleInit(_session.UsingStyle, Config);

        dgvPlan.FillLastVisibleColumn();
        UpdateGridFonts();
        RefreshAllRows();
        UpdateStatus();
        UpdateConnectionLabel();
        RestartGtnLog();
        Invalidate(true);
    }

    private void ShowAppSettings()
    {
        using var form = new FAppSettings(_session, _commands);
        // po kazdom ulozeni (aj tlacidlom Pouzit pri otvorenom okne)
        form.Applied += (_, _) =>
        {
            UpdateMainUi();
            FillManualLineTracks();
            AppendLog(LogKind.Info, Resources.Log_SettingsChanged);
        };
        form.ShowDialog(this);
    }

    private void ShowInfoApp()
    {
        using var form = new FAboutApp(Resources.AboutAppDescription, Resources.gtn);
        form.ShowDialog(this);
    }

    /// <summary>
    ///     Uloží nastavenia programu - po každej zmene, ktorú si okno pamätá (zobrazenie, rýchlosť, koľaje).
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            var dir = AppPaths.ConfigDir;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            XmlSerialization.WriteData(PathUtils.CombinePath(dir, FileConsts.FileConfig)!, Config);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Warning(e, "Nastavenia simulatora sa nepodarilo ulozit");
            AppendLog(LogKind.Error, Texts.F(Resources.Log_ConfigSaveFailed, e.Message));
        }
    }

    /// <summary>
    ///     Pole hľadania v paneli nástrojov - téma mu dá farbu panela, takže by v ňom nebolo vidno, kde sa píše.
    /// </summary>
    private void ApplySearchBoxTheme()
    {
        var box = _session.UsingStyle.ControlsColorScheme.Box;
        tstbSearch.BorderStyle = BorderStyle.FixedSingle;
        tstbSearch.BackColor = box.BackColor;
        tstbSearch.ForeColor = box.ForeColor;
    }

    private void UpdateTitle() =>
        Text = _gvd is null
            ? Resources.Main_Title
            : Texts.F(Resources.Main_TitleStation, _gvd.Info.ThisStation.Name, _gvd.StationId);

    #endregion

    #region Prepínače zobrazenia

    /// <summary>
    ///     Stav prepínačov zobrazenia z nastavení.
    /// </summary>
    private void InitView()
    {
        _sendToIniss = Config.View.SendToIniss;
        TopMost = Config.View.TopMost;
        InitDiagram();
    }

    private void SetSendToIniss(bool value)
    {
        _sendToIniss = value;
        Config.View.SendToIniss = value;
        SaveConfig();
        UpdateCommandStates();
        AppendLog(LogKind.Info, value ? Resources.Log_SendOn : Resources.Log_SendOff);
    }

    private void SetHideFinished(bool value)
    {
        Config.View.HideFinished = value;
        SaveConfig();
        ApplyFilter();
        UpdateCommandStates();
    }

    private void SetFollow(bool value)
    {
        Config.View.FollowCurrent = value;
        SaveConfig();
        UpdateCommandStates();
    }

    private void SetTopMost(bool value)
    {
        TopMost = value;
        Config.View.TopMost = value;
        SaveConfig();
        UpdateCommandStates();
    }

    private void FocusSearch()
    {
        tstbSearch.Focus();
        tstbSearch.SelectAll();
    }

    #endregion

    private void FMain_Load(object? sender, EventArgs e)
    {
        AppRegistry.RegisterJumpList(GlobalResources.RRecentFiles);
        SetRecentGvd();
        AppUpdates.CheckOnStartup(this, Config);

        // grafikon zadany ako argument ma prednost pred naposledy otvorenym
        var path = _commandLine.Gvd;
        if (path is null && Config.Startup == StartupType.LastProject)
            path = AppRegistry.GetLastProject();

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            LoadGvd(path);

        ApplyDiagramVisible();

        // az ked je okno zobrazene - hlasenia o chybnych argumentoch a zaznam behu maju byt vidno
        BeginInvoke(new Action(RunCommandLine));
    }

    /// <summary>
    ///     Vykoná argumenty príkazového riadka - deň, čas a rýchlosť, pripojenie, scenár a spustenie.
    /// </summary>
    private void RunCommandLine()
    {
        var args = _commandLine;
        var errors = args.Errors.ToList();
        if (args.Gvd is { } gvd && !Directory.Exists(gvd))
            errors.Add(Texts.F(Resources.Args_GvdMissing, gvd));
        if (errors.Count > 0)
            Utils.ShowWarning(Texts.F(Resources.Args_Errors, string.Join(Environment.NewLine, errors)));

        if (args.Speed is { } speed)
        {
            // len pre tento beh - rychlost z nastaveni sa nemeni
            _speedUpdating = true;
            SelectSpeed(speed);
            _speedUpdating = false;
            _engine?.SetTimeFactor(SelectedSpeed);
        }

        if (args.Day is { } day)
            dtpDay.Value = day.ToDateTime(TimeOnly.MinValue);
        if (args.From is { } from)
            dtpFrom.Value = dtpDay.Value.Date + from.ToTimeSpan();

        if (args.Connect && _client is not { IsConnected: true })
            ToggleConnection();

        if (args.Scenario is { } scenario)
        {
            if (_gvd is null)
                AppendLog(LogKind.Warning, Texts.F(Resources.Args_NoGvd, scenario));
            else
                OpenScenario(scenario);
        }
        else if (args.Now)
        {
            RunNow();
        }
        else if (args.Plan || args.Start)
        {
            PlanDay();
        }

        if (args.Start && _engine is { IsRunning: false })
            StartRun();
    }

    private void FMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        tmrClock.Stop();
        DisposeEngine();

        _client?.Dispose();
        _client = null;

        // nastavenia (aj rozlozenie panelov) sa ulozia az po zatvoreni okna - InitDocking
        StopGtnLog();
    }
}
