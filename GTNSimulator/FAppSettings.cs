using GTNSimulator.Properties;
using GTNSimulator.XML;
using ToolsCore;
using ToolsCore.Commands;
using ToolsCore.Forms;
using ToolsCore.Iniss.Tools;
using ToolsCore.XML;

namespace GTNSimulator;

/// <summary>
///     Nastavenia programu - spoločné stránky ToolsCore a stránky Simulácia a Spojenie s INISSom.
/// </summary>
internal sealed partial class FAppSettings : FAppSettingsBase
{
    private readonly AppSession<GTNSimulatorConfig, GTNSimulatorStyle> _session;

    /// <summary>
    ///     Vytvorí okno nad kópiou nastavení - do programu sa prenesú až po uložení.
    /// </summary>
    /// <param name="session">Nastavenia programu.</param>
    /// <param name="commands">Príkazy hlavného okna - strom skratiek podľa jeho ponuky; null = podľa katalógu.</param>
    public FAppSettings(AppSession<GTNSimulatorConfig, GTNSimulatorStyle> session, IEnumerable<AppCommand>? commands = null)
        : base(session.Config, new Styles<GTNSimulatorStyle>(session.Styles), session.UsingStyle)
    {
        _session = session;
        InitializeComponent();

        SetCommands(GtnSimCommands.All, commands);
    }

    private new GTNSimulatorConfig Config => (GTNSimulatorConfig)base.Config;

    private new Styles<GTNSimulatorStyle> Styles => (Styles<GTNSimulatorStyle>)base.Styles;

    /// <inheritdoc />

    /// <inheritdoc />
    protected override string ReopenLastText => Resources.Settings_ReopenLast;


    /// <inheritdoc />
    protected override void OnLoad()
    {
        base.OnLoad();

        var simulation = Config.Simulation;
        SetValue(nudSeed, simulation.Seed);
        SetValue(nudOnTime, simulation.OnTimePercent);
        SetValue(nudMeanDelay, simulation.MeanDelayMinutes);
        SetValue(nudTrackChange, simulation.TrackChangePercent);
        SetValue(nudReason, simulation.DelayReasonPercent);
        SetValue(nudEntryLead, simulation.EntryRouteLeadMinutes);
        SetValue(nudExitLead, simulation.ExitRouteLeadMinutes);
        cboxLineTrack.Checked = simulation.SendLineTrack;
        tbLineTrack.Text = simulation.LineTrack;
        UpdateLineTrackFields();

        var connection = Config.Connection;
        tbHost.Text = connection.Host;
        SetValue(nudPort, connection.Port);
        cboxWatchGtnLog.Checked = connection.WatchGtnLog;
        tbGtnLog.Text = connection.GtnLog;
        UpdateGtnLogFields();
    }

    private static void SetValue(NumericUpDown box, int value) => box.Value = Math.Clamp(value, (int)box.Minimum, (int)box.Maximum);

    /// <inheritdoc />
    protected override bool OnSaving()
    {
        if (string.IsNullOrWhiteSpace(tbHost.Text))
        {
            ToolsCore.Tools.Utils.ShowWarning(Resources.Settings_HostEmpty);
            optionsView.ShowPanel(pConnection.Name);
            return false;
        }

        if (cboxLineTrack.Checked && string.IsNullOrWhiteSpace(tbLineTrack.Text))
        {
            ToolsCore.Tools.Utils.ShowWarning(Resources.Settings_LineTrackEmpty);
            optionsView.ShowPanel(pSimulation.Name);
            return false;
        }


        Config.Simulation = Config.Simulation with
        {
            Seed = decimal.ToInt32(nudSeed.Value),
            OnTimePercent = decimal.ToInt32(nudOnTime.Value),
            MeanDelayMinutes = decimal.ToInt32(nudMeanDelay.Value),
            TrackChangePercent = decimal.ToInt32(nudTrackChange.Value),
            DelayReasonPercent = decimal.ToInt32(nudReason.Value),
            EntryRouteLeadMinutes = decimal.ToInt32(nudEntryLead.Value),
            ExitRouteLeadMinutes = decimal.ToInt32(nudExitLead.Value),
            SendLineTrack = cboxLineTrack.Checked,
            LineTrack = tbLineTrack.Text.Trim()
        };
        Config.Connection = Config.Connection with
        {
            Host = tbHost.Text.Trim(),
            Port = decimal.ToInt32(nudPort.Value),
            WatchGtnLog = cboxWatchGtnLog.Checked,
            GtnLog = tbGtnLog.Text.Trim()
        };

        return base.OnSaving();
    }

    /// <inheritdoc />
    protected override void SaveData()
    {
        // kopie - okno po Pouzit ostava otvorene a dalsie upravy v nom nesmu menit nastavenia programu
        _session.Config = Config with { };
        _session.Styles = new Styles<GTNSimulatorStyle>(Styles);
        _session.UsingStyle = _session.Styles[UsingStyle.Name];

        var dir = AppPaths.ConfigDir;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        Styles<GTNSimulatorStyle>.WriteData(PathUtils.CombinePath(dir, FileConsts.FileStyles)!, _session.Styles);
        XmlSerialization.WriteData(PathUtils.CombinePath(dir, FileConsts.FileConfig)!, _session.Config);
    }

    private void cboxWatchGtnLog_CheckedChanged(object? sender, EventArgs e) => UpdateGtnLogFields();

    private void cboxLineTrack_CheckedChanged(object? sender, EventArgs e) => UpdateLineTrackFields();

    private void UpdateLineTrackFields() => tbLineTrack.Enabled = cboxLineTrack.Checked;

    private void UpdateGtnLogFields()
    {
        tbGtnLog.Enabled = cboxWatchGtnLog.Checked;
        bBrowseGtnLog.Enabled = cboxWatchGtnLog.Checked;
    }

    private void bBrowseGtnLog_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = Resources.Settings_GtnLogFilter,
            FileName = Path.GetFileName(tbGtnLog.Text),
            CheckFileExists = false
        };

        var dir = Path.GetDirectoryName(tbGtnLog.Text);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            dialog.InitialDirectory = dir;

        if (dialog.ShowDialog(this) == DialogResult.OK)
            tbGtnLog.Text = dialog.FileName;
    }
}
