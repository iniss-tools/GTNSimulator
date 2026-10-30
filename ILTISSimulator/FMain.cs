using ExControls;
using ILTISSimulator.Gtn;
using ILTISSimulator.Properties;
using ILTISSimulator.Simulation;
using ILTISSimulator.Tools;
using ILTISSimulator.XML;
using ToolsCore.Commands;
using ToolsCore.Forms;
using ToolsCore.Iniss.Tools;
using ToolsCore.Tools;
using ToolsCore.Updates;
using ToolsCore.XML;
using ToolsCore;
using Utils = ToolsCore.Tools.Utils;

namespace ILTISSimulator;

/// <summary>
///     Hlavné okno simulátora ILTISu: plán dňa v tabuľke, ovládanie behu a ručné správy.
/// </summary>
internal sealed partial class FMain : ExForm
{
    /// <summary>
    ///     Adresa dokumentácie programu.
    /// </summary>
    private const string DocsLink = "https://iniss-tools.fvat.sk/docs/iltissimulator/";

    private readonly AppSession<IltisConfig, IltisStyle> _session;
    private readonly CommandLine _commandLine;
    private readonly CommandSet _commands = new();

    /// <summary>
    ///     Vytvorí hlavné okno bez argumentov príkazového riadka.
    /// </summary>
    /// <param name="session">Nastavenia programu.</param>
    public FMain(AppSession<IltisConfig, IltisStyle> session) : this(session, CommandLine.Empty)
    {
    }

    /// <summary>
    ///     Vytvorí hlavné okno.
    /// </summary>
    /// <param name="session">Nastavenia programu.</param>
    /// <param name="commandLine">Argumenty príkazového riadka - vykonajú sa po zobrazení okna.</param>
    public FMain(AppSession<IltisConfig, IltisStyle> session, CommandLine commandLine)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));

        InitializeComponent();
        InitToolbar();

        this.ApplyThemeAndFonts();
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

    private IltisConfig Config => _session.Config;

    #region Príkazy

    /// <summary>
    ///     Príkazy hlavného okna naviazané naraz na ponuku, panel nástrojov a kontextovú ponuku tabuľky.
    /// </summary>
    private void CreateCommands()
    {
        Func<bool> gvd = () => _gvd is not null;
        Func<bool> plan = () => _engine?.Timeline is not null;
        Func<bool> selection = () => plan() && SelectedRuns().Count > 0;

        _commands.Add(IltisCommands.OpenGvd, ChooseGvd).Bind(tsmimOpenGvd, tssbOpenGvd);
        _commands.Add(IltisCommands.ReloadGvd, () => LoadGvd(_gvdDir!), () => _gvdDir is not null).Bind(tsmimReloadGvd, tsbReloadGvd);
        _commands.Add(IltisCommands.OpenScenario, OpenScenario, gvd).Bind(tsmimOpenScenario, tsbOpenScenario);
        _commands.Add(IltisCommands.SaveScenario, SaveScenario, plan).Bind(tsmimSaveScenario, tsbSaveScenario);
        _commands.Add(IltisCommands.Tracks, ShowTracks, gvd).Bind(tsmimTracks, tsbTracks);
        _commands.Add(IltisCommands.Exit, Close).Bind(tsmimExit);

        _commands.Add(IltisCommands.Connect, ToggleConnection).Bind(tsmimConnect, tsbConnect);
        _commands.Add(IltisCommands.Plan, PlanDay, gvd).Bind(tsmimPlan, tsbPlan);
        _commands.Add(IltisCommands.Start, StartRun, () => plan() && !_engine!.IsRunning).Bind(tsmimStart, tsbStart);
        _commands.Add(IltisCommands.Pause, PauseRun, () => _engine is { IsRunning: true }).Bind(tsmimPause, tsbPause);
        _commands.Add(IltisCommands.Stop, StopRun, plan).Bind(tsmimStop, tsbStop);
        _commands.Add(IltisCommands.Jump, JumpRun, plan).Bind(tsmimJump, tsbJump);
        _commands.Add(IltisCommands.Now, RunNow, gvd).Bind(tsmimNow, tsbNow);
        _commands.Add(IltisCommands.SendToIniss, () => SetSendToIniss(!_sendToIniss)).Bind(tsmimSendToIniss, tsbSendToIniss);

        _commands.Add(IltisCommands.SendNext, SendNextNow, selection).Bind(tsmimSendNext, cmimSendNext, tssbSendNow);
        _commands.Add(IltisCommands.SendEntryRoute, () => SendNow(SimEventKind.EntryRoute), selection)
            .Bind(tsmimSendEntryRoute, cmimSendEntryRoute, tsmiSendNowEntryRoute);
        _commands.Add(IltisCommands.SendReason, () => SendNow(SimEventKind.DelayReason), selection)
            .Bind(tsmimSendReason, cmimSendReason, tsmiSendNowReason);
        _commands.Add(IltisCommands.SendArrival, () => SendNow(SimEventKind.Arrival), selection)
            .Bind(tsmimSendArrival, cmimSendArrival, tsmiSendNowArrival);
        _commands.Add(IltisCommands.SendExitRoute, () => SendNow(SimEventKind.ExitRoute), selection)
            .Bind(tsmimSendExitRoute, cmimSendExitRoute, tsmiSendNowExitRoute);
        _commands.Add(IltisCommands.SendDeparture, () => SendNow(SimEventKind.Departure), selection)
            .Bind(tsmimSendDeparture, cmimSendDeparture, tsmiSendNowDeparture);
        _commands.Add(IltisCommands.Transfer, () => SendTransfer(GtnTransferKind.Normal), selection)
            .Bind(tsmimTransfer, cmimTransfer);
        _commands.Add(IltisCommands.ExpressTransfer, () => SendTransfer(GtnTransferKind.Express), selection)
            .Bind(tsmimExpressTransfer, cmimExpressTransfer);
        _commands.Add(IltisCommands.AddDelay, AddDelay, plan).Bind(tsmimAddDelay, cmimAddDelay, tsbAddDelay);
        _commands.Add(IltisCommands.TrackClosure, CloseTrack, plan).Bind(tsmimTrackClosure, tsbTrackClosure);
        _commands.Add(IltisCommands.Suppress, ToggleSuppressed, selection).Bind(tsmimSuppress, cmimSuppress, tsbSuppress);
        _commands.Add(IltisCommands.Revert, RevertSelected, selection).Bind(tsmimRevert, cmimRevert, tsbRevert);

        _commands.Add(IltisCommands.ManualMessage, () => SetManualVisible(!Config.View.ManualMessage)).Bind(tsmimManualMessage, tsbManualMessage);
        _commands.Add(IltisCommands.TrackDiagram, () => SetTrackDiagramVisible(!Config.View.TrackDiagram)).Bind(tsmimTrackDiagram, tsbTrackDiagram);
        _commands.Add(IltisCommands.HideFinished, () => SetHideFinished(!Config.View.HideFinished)).Bind(tsmimHideFinished, tsbHideFinished);
        _commands.Add(IltisCommands.Follow, () => SetFollow(!Config.View.FollowCurrent)).Bind(tsmimFollow, tsbFollow);
        _commands.Add(IltisCommands.Find, FocusSearch).Bind(tsmimFind);
        _commands.Add(IltisCommands.TopMost, () => SetTopMost(!Config.View.TopMost)).Bind(tsmimTopMost);

        _commands.Add(IltisCommands.AppSettings, ShowAppSettings).Bind(tsmimAppSettings, tsbAppSettings);
        _commands.Add(IltisCommands.Docs, () => Utils.OpenShell(DocsLink)).Bind(tsmimDocs);
        _commands.Add(IltisCommands.InfoApp, ShowInfoApp).Bind(tsmimInfoApp);
        _commands.Add(IltisCommands.CheckUpdates, () => AppUpdates.CheckNow(this), () => AppUpdates.Available).Bind(tsmimCheckUpdates);
        _commands.Add(IltisCommands.ShowLogs, ErrorReport.OpenLogFolder).Bind(tsmimShowLogs);
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

        SetChecked(IltisCommands.SendToIniss, _sendToIniss);
        SetChecked(IltisCommands.ManualMessage, Config.View.ManualMessage);
        SetChecked(IltisCommands.TrackDiagram, Config.View.TrackDiagram);
        SetChecked(IltisCommands.HideFinished, Config.View.HideFinished);
        SetChecked(IltisCommands.Follow, Config.View.FollowCurrent);
        SetChecked(IltisCommands.TopMost, Config.View.TopMost);
        SetChecked(IltisCommands.Suppress, selected.Count > 0 && selected.All(run => run.Suppressed));

        // jednotlive udalosti len ked na ne este niektory vybrany vlak caka
        foreach (var (command, kind) in new[]
                 {
                     (IltisCommands.SendEntryRoute, SimEventKind.EntryRoute),
                     (IltisCommands.SendReason, SimEventKind.DelayReason),
                     (IltisCommands.SendArrival, SimEventKind.Arrival),
                     (IltisCommands.SendExitRoute, SimEventKind.ExitRoute),
                     (IltisCommands.SendDeparture, SimEventKind.Departure)
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
        var typing = dgvPlan.IsCurrentCellInEditMode || tstbSearch.Focused || ActiveControl is TextBoxBase or ComboBox;
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

        ApplyGridColumns();
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
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        UpdateMainUi();
        FillManualLineTracks();
        AppendLog(LogKind.Info, Resources.Log_SettingsChanged);
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
            Log.Exception(e);
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
        pManual.Visible = Config.View.ManualMessage;
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

    private void SetManualVisible(bool value)
    {
        pManual.Visible = value;
        Config.View.ManualMessage = value;
        SaveConfig();

        if (value)
            FillManualFromSelection();

        UpdateCommandStates();
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

        if (splitMain.Height > splitMain.Panel1MinSize + splitMain.Panel2MinSize + splitMain.SplitterWidth)
            splitMain.SplitterDistance = splitMain.Height * 65 / 100;
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

        StopGtnLog();
        SaveConfig();
    }
}
