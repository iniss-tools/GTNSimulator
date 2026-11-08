namespace ILTISSimulator.DocScreenshots;

/// <summary>
///     Snímky okien ILTISSimulatora pre jednu tému.
/// </summary>
internal sealed class Shots(Program.Options options, string theme, List<string> log)
{
    /// <summary>
    ///     Simulovaný deň - pondelok v platnosti ukážkového grafikonu.
    /// </summary>
    private static readonly DateTime Day = new(2027, 3, 15);

    private int _count;

    public int Run(string gvdDir)
    {
        using var main = new MainWindow(gvdDir);

        Shot("hlavne-okno/hlavne-okno", main.Form, () => main.PrepareRunningDay(Day));
        if (options.Check)
        {
            main.CheckEditing(log);
            main.CheckDiagram(log, Day);
        }
        Shot("hlavne-okno/rucna-sprava", main.Form, () => main.ShowManualMessage(true));
        main.ShowManualMessage(false);
        Shot("plan/kolajovy-diagram", main.Form, () => main.ShowTrackDiagram(true, Day));
        main.ShowTrackDiagram(false, Day);

        // v nastaveniach ma byt predvolene spojenie, nie port falosneho INISSu
        Program.Session.Config.Connection.Port = 4000;
        foreach (var (slug, panel) in new[]
                 {
                     ("nastavenia-programu", "pGeneral"),
                     ("simulacia", "pSimulation"),
                     ("spojenie", "pConnection"),
                     ("vzhlad", "pAppearance"),
                     ("klavesove-skratky", "pShortcuts"),
                     ("styly", "pStyles"),
                     ("pisma", "pFonts"),
                 })
            Dialog($"nastavenia-programu/{slug}", () => MainWindow.CreateSettings(panel));
        Dialog("plan/pridat-meskanie", () => MainWindow.CreateDialog("ILTISSimulator.FAddDelay", 2, Day.AddHours(9).AddMinutes(15)));
        Dialog("plan/kolaje", main.CreateTracks);
        Dialog("plan/vyluka-kolaje", () => main.CreateTrackClosure(Day));

        return _count;
    }

    private void Dialog(string name, Func<Form> create)
    {
        if (options.Only is { } only && !name.Contains(only, StringComparison.Ordinal))
            return;

        using var form = create();
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(80, 80);
        form.Show();
        Pump.Events(40);
        form.Refresh();
        Pump.Events(20);

        Shot(name, form, () => { });
        form.Close();
    }

    private void Shot(string name, Form form, Action prepare)
    {
        if (options.Only is { } only && !name.Contains(only, StringComparison.Ordinal))
            return;

        prepare();
        Pump.Events();

        var file = Path.Combine(options.OutDir, $"{name}-{theme}.png");
        WindowCapture.Save(form, file);
        log.Add("snímka: " + file);
        _count++;
    }
}
