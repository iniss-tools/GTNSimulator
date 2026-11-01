using GVDEditor.Domain.Entities;
using ILTISSimulator.Data;
using ILTISSimulator.Gtn;
using ILTISSimulator.Properties;
using ILTISSimulator.Tools;
using Serilog;
using ToolsCore.Tools;

namespace ILTISSimulator;

internal sealed partial class FMain
{
    private GvdData? _gvd;
    private string? _gvdDir;
    private IReadOnlyList<DelayReason> _delayReasons = [];
    private ISet<string> _excludedTracks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private void ChooseGvd()
    {
        if (_gvdDir is not null && Directory.Exists(_gvdDir))
            fbdGvd.SelectedPath = _gvdDir;

        fbdGvd.Description = Resources.Main_ChooseGvdDir;
        fbdGvd.UseDescriptionForTitle = true;

        if (fbdGvd.ShowDialog(this) == DialogResult.OK)
            LoadGvd(fbdGvd.SelectedPath);
    }

    /// <summary>
    ///     Načíta grafikon. Doterajší plán sa zahodí - patril k inému grafikonu.
    /// </summary>
    /// <param name="dir">Priečinok s grafikonom (<c>DATA\stanica.rok</c>).</param>
    private void LoadGvd(string dir)
    {
        if (!ConfirmDiscardChanges())
            return;

        GvdData gvd;
        try
        {
            gvd = GvdLightReader.Read(dir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException
                                      or ArgumentException or KeyNotFoundException)
        {
            Log.Error(e, "Grafikon {Directory} sa nepodarilo nacitat", dir);
            AppendLog(LogKind.Error, Texts.F(Resources.Log_GvdLoadFailed, e.Message));
            return;
        }

        DisposeEngine();

        _gvd = gvd;
        _gvdDir = dir;
        _excludedTracks = Config.ExcludedTracks.Get(gvd.StationId);
        LoadDelayReasons(dir);

        AppRegistry.SetUsageOfProject(dir);
        AppRegistry.SetLastProject(dir);
        SetRecentGvd();

        FillGrid();
        FillManualFromGvd();
        RestartGtnLog();
        UpdateTitle();
        UpdateStatus();
        UpdateCommandStates();

        Log.Information("Grafikon {Directory} nacitany: {Trains} vlakov", dir, gvd.Trains.Count);
        AppendLog(LogKind.Info, Texts.F(Resources.Log_GvdLoaded, dir, gvd.Trains.Count, UsableTracks().Count));
    }

    /// <summary>
    ///     Nový plán alebo grafikon zahodí ručné zmeny - spýta sa, ak nejaké sú.
    /// </summary>
    /// <returns><see langword="true" />, ak sa môže pokračovať.</returns>
    private bool ConfirmDiscardChanges()
    {
        if (_engine?.Runs is not { } runs || !runs.Any(run => run.IsChanged))
            return true;

        return Utils.ShowQuestion(Resources.Main_DiscardChanges, MessageBoxButtons.YesNo) == DialogResult.Yes;
    }

    private void SetRecentGvd()
    {
        tsmimRecentGvd.DropDownItems.Clear();
        tssbOpenGvd.DropDownItems.Clear();

        foreach (var project in AppRegistry.GetOpenedProjects())
        {
            var path = project.Path;
            foreach (var items in new[] { tsmimRecentGvd.DropDownItems, tssbOpenGvd.DropDownItems })
            {
                var item = new ToolStripMenuItem(path);
                item.Click += (_, _) =>
                {
                    if (Directory.Exists(path))
                        LoadGvd(path);
                    else
                        AppendLog(LogKind.Error, Texts.F(Resources.Log_GvdMissing, path));
                };
                item.ApplyThemeAndFont();
                items.Add(item);
            }
        }

        tsmimRecentGvd.Enabled = tsmimRecentGvd.DropDownItems.Count > 0;
    }

    private List<Track> UsableTracks() => _gvd?.Tracks.Where(Simulation.TrackAllocator.CanUse).ToList() ?? [];

    private List<DelayReason> UsableReasons() => _delayReasons.Where(reason => reason.Usable).ToList();

    private void LoadDelayReasons(string gvdDir)
    {
        try
        {
            _delayReasons = DelayReasonReader.ReadForGvd(gvdDir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException)
        {
            _delayReasons = [];
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_ReasonsFailed, e.Message));
            return;
        }

        if (_delayReasons.Count == 0)
        {
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_ReasonsMissing, DelayReasonReader.SoundGroup));
            return;
        }

        foreach (var reason in _delayReasons)
            AppendLog(LogKind.Info, reason.Usable
                ? Texts.F(Resources.Log_Reason, reason.Number.ToString("00", CultureInfo.CurrentCulture), reason.Key, reason.Text)
                : Texts.F(Resources.Log_ReasonUnusable, reason.Number.ToString("00", CultureInfo.CurrentCulture), reason.Key));
    }

    /// <summary>
    ///     Text dôvodu meškania do tabuľky a zoznamov.
    /// </summary>
    /// <param name="number">Poradové číslo dôvodu, nula = bez dôvodu.</param>
    /// <returns>Text, pri nule prázdny.</returns>
    private string ReasonText(int number)
    {
        if (number == 0)
            return "";

        var reason = _delayReasons.FirstOrDefault(candidate => candidate.Number == number);

        return reason is null
            ? number.ToString("00", CultureInfo.CurrentCulture)
            : $"{reason.Number:00} - {reason.Text}";
    }

    private void ShowTracks()
    {
        if (_gvd is null)
            return;

        using var dialog = new FTracks(_gvd.Tracks, _excludedTracks);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _excludedTracks = dialog.ExcludedKeys;
        Config.ExcludedTracks.Set(_gvd.StationId, _excludedTracks);
        SaveConfig();
        trackDiagram.SetExcluded(_excludedTracks.ToList());

        UpdateStatus();
        AppendLog(LogKind.Info, _excludedTracks.Count == 0
            ? Resources.Log_NoExcludedTracks
            : Texts.F(Resources.Log_ExcludedTracks, string.Join(", ", _excludedTracks.OrderBy(key => key))));
    }

    /// <summary>
    ///     Pole stanice do správ načítaného grafikonu.
    /// </summary>
    private string StationField => _gvd is null ? "" : StationCodec.Encode(_gvd.StationId);
}
