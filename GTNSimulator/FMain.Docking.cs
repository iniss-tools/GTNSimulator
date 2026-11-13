using ExControls;
using GTNSimulator.Properties;
using ToolsCore.Tools;

namespace GTNSimulator;

/// <summary>
///     Ukotviteľné panely (ExDockManager): tabuľka plánu s koľajovým diagramom v dokumente, záznam a ručná správa dole.
/// </summary>
internal sealed partial class FMain
{
    // kluce panelov v ulozenom rozlozeni (config.xml) - nemenit
    private const string LogPanelKey = "Log";
    private const string ManualPanelKey = "ManualMessage";

    private readonly ExDockManager _dock = AppDock.Create();
    private readonly ExDockPanel _logPanel = new(LogPanelKey, Resources.Panel_Log);
    private readonly ExDockPanel _manualPanel = new(ManualPanelKey, Resources.Panel_ManualMessage);
    private string _defaultLayout = "";

    /// <summary>
    ///     Návrh okna má plán a záznam v deliacom liste a ručnú správu pod ním. Za behu ide plán s diagramom do dokumentu,
    ///     záznam a ručná správa do ukotviteľných panelov (ručná správa predvolene skrytá) a deliaci list sa zahodí;
    ///     rozloženie z nastavení sa obnoví až po téme a písmach.
    /// </summary>
    private void InitDocking()
    {
        SuspendLayout();
        var designed = splitMain;
        splitPlan.Parent?.Controls.Remove(splitPlan);
        rtbLog.Parent?.Controls.Remove(rtbLog);
        Controls.Remove(pManual);
        splitPlan.Dock = DockStyle.Fill;
        _dock.Document.Controls.Add(splitPlan);
        rtbLog.Dock = DockStyle.Fill;
        _logPanel.Controls.Add(rtbLog);
        // v navrhu sa panel prisposobi obsahu a je skryty - v ukotvitelnom paneli ho vyplni a pri malej vyske sa posuva
        pManual.AutoSize = false;
        pManual.AutoScroll = true;
        pManual.Dock = DockStyle.Fill;
        pManual.Visible = true;
        _manualPanel.Controls.Add(pManual);

        // na miesto deliaceho listu - pred panelmi ukotvenymi k okraju, aby vyplnil zvysok okna
        var index = Controls.GetChildIndex(designed);
        Controls.Remove(designed);
        designed.Dispose();
        Controls.Add(_dock);
        Controls.SetChildIndex(_dock, index);

        _dock.AddPanel(_logPanel, ExDockSide.Bottom);
        // pod zaznamom cez celu sirku okna ako v navrhu (pridanie k zone by ju dalo vedla zaznamu)
        _dock.AddPanel(_manualPanel, _logPanel, ExDockSide.Bottom);
        // vyska zony je v logickych bodoch (96 DPI) - spravca ju prepocita podla DPI okna sam
        _dock.SetZoneSize(ExDockSide.Bottom, 220);
        _dock.HidePanel(_manualPanel);
        ResumeLayout(true);

        _defaultLayout = _dock.SaveLayout();
        _dock.PanelStateChanged += (_, e) =>
        {
            if (e.Panel == _manualPanel && _manualPanel.IsShown)
                FillManualFromSelection();
        };
        // rozlozenie sa uklada pri zatvoreni okna spolu s ostatnymi nastaveniami (az ked zatvorenie nikto nezrusil)
        FormClosed += (_, _) =>
        {
            Config.DockLayout = _dock.SaveLayout();
            SaveConfig();
        };
    }

    private void CreateViewCommands()
    {
        _commands.Add(GtnSimCommands.ShowLog, () => _dock.ShowPanel(_logPanel)).Bind(tsmimShowLog);
        _commands.Add(GtnSimCommands.ManualMessage, () => _dock.ShowPanel(_manualPanel)).Bind(tsmimManualMessage, tsbManualMessage);
        _commands.Add(GtnSimCommands.ResetLayout, () => _dock.LoadLayout(_defaultLayout)).Bind(tsmimResetLayout);
    }

    /// <summary>
    ///     Prvok s fokusom aj vo vnorených kontajneroch (ukotviteľné panely sú <see cref="ContainerControl" /> a okno vidí
    ///     len ich).
    /// </summary>
    private Control? FocusedControl()
    {
        var control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
            control = inner;
        return control;
    }
}
