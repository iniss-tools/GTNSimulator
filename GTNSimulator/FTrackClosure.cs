using ExControls;
using GVDEditor.Domain.Entities;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;
using GTNSimulator.Tools;
using ToolsCore.Tools;

namespace GTNSimulator;

/// <summary>
///     Výluka koľaje v intervale - výber koľaje a času. Okno ukazuje, ktorých vlakov sa výluka týka.
/// </summary>
internal sealed partial class FTrackClosure : ExForm
{
    // viac cisel vlakov by sa do riadku nezmestilo
    private const int MaxListed = 8;

    private readonly DateTime _day;
    private readonly IReadOnlyList<TrainRun> _runs;

    /// <summary>
    ///     Vytvorí dialóg.
    /// </summary>
    /// <param name="tracks">Koľaje stanice.</param>
    /// <param name="runs">Jazdy dňa.</param>
    /// <param name="now">Aktuálny virtuálny čas - predvolený začiatok výluky.</param>
    /// <param name="selected">Predvolená koľaj, napríklad koľaj vybraného vlaku.</param>
    public FTrackClosure(IEnumerable<Track> tracks, IReadOnlyList<TrainRun> runs, DateTime now, Track? selected)
    {
        InitializeComponent();
        this.ApplyThemeAndFonts();

        _runs = runs;
        _day = now.Date;

        foreach (var track in tracks.Where(TrackAllocator.CanUse))
        {
            var index = cbTrack.Items.Add(new Item(track));
            if (track.Key == selected?.Key)
                cbTrack.SelectedIndex = index;
        }

        if (cbTrack.SelectedIndex < 0 && cbTrack.Items.Count > 0)
            cbTrack.SelectedIndex = 0;

        var start = now.AddSeconds(-now.Second);
        dtpFrom.Value = start;
        dtpTo.Value = start.AddHours(2);
        UpdateAffected();
    }

    /// <summary>
    ///     Vylúčená koľaj.
    /// </summary>
    public Track? Track => (cbTrack.SelectedItem as Item)?.Track;

    /// <summary>
    ///     Začiatok výluky.
    /// </summary>
    public DateTime From => _day + dtpFrom.Value.TimeOfDay;

    /// <summary>
    ///     Koniec výluky. Čas skorší než začiatok patrí nasledujúcemu dňu.
    /// </summary>
    public DateTime To
    {
        get
        {
            var to = _day + dtpTo.Value.TimeOfDay;
            return to <= From ? to.AddDays(1) : to;
        }
    }

    /// <summary>
    ///     Jazdy, ktorých sa výluka týka.
    /// </summary>
    public IReadOnlyList<TrainRun> Affected =>
        Track is { } track ? TrackClosure.Affected(_runs, track, From, To) : [];

    private void Range_Changed(object? sender, EventArgs e) => UpdateAffected();

    private void UpdateAffected()
    {
        var affected = Affected;
        var numbers = string.Join(", ", affected.Take(MaxListed).Select(run => run.Train.Number)) +
                      (affected.Count > MaxListed ? ", …" : "");
        var format = affected.Count switch
        {
            0 => Resources.Closure_AffectedNone,
            1 => Resources.Closure_AffectedOne,
            <= 4 => Resources.Closure_AffectedFew,
            _ => Resources.Closure_AffectedMany
        };

        lAffected.Text = Texts.F(format, affected.Count, numbers);
        bOk.Enabled = affected.Count > 0;
    }

    private sealed class Item
    {
        public Item(Track track) => Track = track;

        public Track Track { get; }

        public override string ToString() => Texts.Of(Track);
    }
}
