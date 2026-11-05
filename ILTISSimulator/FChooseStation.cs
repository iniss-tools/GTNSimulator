using ExControls;
using ILTISSimulator.Data;
using ToolsCore.Tools;

namespace ILTISSimulator;

/// <summary>
///     Výber stanice INISS2, keď otvorený priečinok obsahuje viac staníc.
/// </summary>
internal sealed partial class FChooseStation : ExForm
{
    /// <summary>
    ///     Vytvorí dialóg nad stanicami.
    /// </summary>
    /// <param name="stations">Stanice na výber.</param>
    public FChooseStation(IEnumerable<Iniss2Station> stations)
    {
        InitializeComponent();
        this.ApplyThemeAndFonts();

        lbStations.Items.AddRange(stations.Cast<object>().ToArray());
        if (lbStations.Items.Count > 0)
            lbStations.SelectedIndex = 0;
    }

    /// <summary>
    ///     Vybraná stanica.
    /// </summary>
    public Iniss2Station? Selected => lbStations.SelectedItem as Iniss2Station;

    private void lbStations_DoubleClick(object sender, EventArgs e)
    {
        if (Selected is null)
            return;

        DialogResult = DialogResult.OK;
        Close();
    }
}
