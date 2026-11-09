using ExControls;
using GTNSimulator.Properties;
using GTNSimulator.Tools;
using ToolsCore.Tools;

namespace GTNSimulator;

/// <summary>
///     Hromadné meškanie - vybraným vlakom alebo všetkým vlakom od zadaného času (napríklad porucha na trati).
/// </summary>
internal sealed partial class FAddDelay : ExForm
{
    private readonly DateTime _day;

    /// <summary>
    ///     Vytvorí dialóg.
    /// </summary>
    /// <param name="selected">Počet vybraných vlakov.</param>
    /// <param name="now">Aktuálny virtuálny čas - predvolený začiatok pre všetky vlaky.</param>
    public FAddDelay(int selected, DateTime now)
    {
        InitializeComponent();
        this.ApplyThemeAndFonts();

        _day = now.Date;
        rbSelected.Text = Texts.F(Resources.AddDelay_Selected, selected);
        rbSelected.Enabled = selected > 0;
        rbFrom.Checked = selected == 0;
        dtpFrom.Value = now;
        dtpFrom.Enabled = rbFrom.Checked;
    }

    /// <summary>
    ///     Prídavok k meškaniu v minútach.
    /// </summary>
    public int Minutes => decimal.ToInt32(nudMinutes.Value);

    /// <summary>
    ///     Začiatok, od ktorého sa meškanie pridá všetkým vlakom, alebo <see langword="null" />, ak len vybraným.
    /// </summary>
    public DateTime? AllFrom => rbFrom.Checked ? _day + dtpFrom.Value.TimeOfDay : null;

    private void rbFrom_CheckedChanged(object? sender, EventArgs e) => dtpFrom.Enabled = rbFrom.Checked;
}
