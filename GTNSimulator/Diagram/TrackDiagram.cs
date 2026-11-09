using System.ComponentModel;
using System.Drawing.Drawing2D;
using ExControls;
using GVDEditor.Domain.Entities;
using GTNSimulator.Properties;
using GTNSimulator.Simulation;
using GTNSimulator.Tools;

namespace GTNSimulator.Diagram;

/// <summary>
///     Koľajový diagram - koľaje stanice pod sebou, vodorovne čas a každý vlak ako pruh od postavenia vchodovej
///     cesty po uvoľnenie koľaje. Vlak sa dá potiahnuť na inú koľaj (zvisle) a posunúť v čase (vodorovne), výber
///     zdieľa s tabuľkou plánu. Koliesko myši posúva čas, s Shift koľaje, s Ctrl mení mierku.
/// </summary>
internal sealed class TrackDiagram : Control, IThemeable
{
    private static readonly (int Label, int Minor)[] Steps =
        [(5, 1), (10, 5), (15, 5), (30, 10), (60, 15), (120, 30), (180, 60), (360, 60)];

    private readonly HScrollBar _hScroll = new() { Visible = false };
    private readonly VScrollBar _vScroll = new() { Visible = false };
    private readonly ToolTip _toolTip = new();

    private IReadOnlyList<DiagramLane> _lanes = [];
    private IReadOnlyList<TrainRun> _runs = [];
    private IReadOnlyList<Track> _tracks = [];
    private IReadOnlyCollection<string> _excluded = [];
    private DateTime _day;
    private DiagramScale? _scale;
    private int _hourWidth = DiagramScale.DefaultHourWidth;
    private DateTime? _now;
    private HashSet<TrainRun> _selected = [];
    private ISet<TrainRun> _conflicts = new HashSet<TrainRun>();
    private Drag? _drag;
    private object? _tipTarget;
    private int _headerWidth;
    private int _rowHeight;
    private bool _updatingScroll;
    private bool _darkScrollBars;
    private Font? _boldFont;

    private Color _back = SystemColors.Window;
    private Color _fore = SystemColors.WindowText;
    private Color _panelBack = SystemColors.Control;
    private Color _panelFore = SystemColors.ControlText;
    private Color _highlight = SystemColors.Highlight;

    /// <summary>
    ///     Vytvorí prázdny diagram.
    /// </summary>
    public TrackDiagram()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);

        Controls.Add(_hScroll);
        Controls.Add(_vScroll);
        _hScroll.ValueChanged += Scroll_ValueChanged;
        _vScroll.ValueChanged += Scroll_ValueChanged;
    }

    /// <summary>
    ///     Používateľ zmenil výber vlakov.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    ///     Používateľ potiahol vlak na inú koľaj alebo v čase.
    /// </summary>
    public event EventHandler<DiagramMoveEventArgs>? RunMoved;

    /// <summary>
    ///     Používateľ zmenil mierku.
    /// </summary>
    public event EventHandler? HourWidthChanged;

    /// <summary>
    ///     Šírka hodiny v bodoch.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HourWidth
    {
        get => _hourWidth;
        set
        {
            var width = Math.Clamp(value, DiagramScale.MinHourWidth, DiagramScale.MaxHourWidth);
            if (width == _hourWidth)
                return;

            _hourWidth = width;
            _scale = _scale?.WithHourWidth(width);
            UpdateScrollBars();
            Invalidate();
        }
    }

    /// <summary>
    ///     Vlaky vybrané v diagrame.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyCollection<TrainRun> SelectedRuns => _selected;

    private int RowHeight => _rowHeight > 0 ? _rowHeight : Font.Height + LogicalToDeviceUnits(8);

    private int AxisHeight => Font.Height + LogicalToDeviceUnits(10);

    private Rectangle Plot => new(_headerWidth, AxisHeight,
        Math.Max(0, ClientSize.Width - _headerWidth - (_vScroll.Visible ? _vScroll.Width : 0)),
        Math.Max(0, ClientSize.Height - AxisHeight - (_hScroll.Visible ? _hScroll.Height : 0)));

    private int ContentHeight => _lanes.Sum(lane => lane.Rows) * RowHeight;

    private int OffsetX => _hScroll.Visible ? _hScroll.Value : 0;

    private int OffsetY => _vScroll.Visible ? _vScroll.Value : 0;

    /// <inheritdoc />
    public void ApplyTheme(ExTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _back = theme.BoxBackColor;
        _fore = theme.BoxForeColor;
        _panelBack = theme.PanelBackColor;
        _panelFore = theme.PanelForeColor;
        _highlight = theme.HighlightBackColor;
        BackColor = _back;
        ForeColor = _fore;

        _darkScrollBars = theme.DarkScrollBars;
        if (IsHandleCreated)
            ApplyScrollBarTheme();
        Invalidate();
    }

    /// <summary>
    ///     Nový plán - diagram sa zostaví znova a posunie na začiatok behu.
    /// </summary>
    /// <param name="day">Deň plánu.</param>
    /// <param name="runs">Jazdy dňa, alebo <see langword="null" />, ak nie je naplánované.</param>
    /// <param name="tracks">Koľaje stanice.</param>
    /// <param name="excluded">Kľúče koľají vynechaných z plánovania.</param>
    /// <param name="start">Čas, ktorý má byť po zostavení vidno.</param>
    public void SetPlan(DateTime day, IReadOnlyList<TrainRun>? runs, IReadOnlyList<Track> tracks,
        IReadOnlyCollection<string> excluded, DateTime start)
    {
        _day = day;
        _runs = runs ?? [];
        _tracks = tracks;
        _excluded = excluded;
        _selected.Clear();
        CancelDrag();

        RefreshPlan();
        if (_scale is not null)
            SetOffsetX(_scale.X(start) - Plot.Width / 4.0);
    }

    /// <summary>
    ///     Plán sa zmenil (meškanie, koľaj, odoslané udalosti) - pruhy sa rozložia znova, poloha ostane.
    /// </summary>
    public void RefreshPlan()
    {
        if (_runs.Count == 0)
        {
            _lanes = [];
            _scale = null;
        }
        else
        {
            _lanes = DiagramLanes.Build(_tracks, _excluded, _runs);
            var (start, end) = DiagramScale.Range(_day, _runs);
            _scale = new DiagramScale(start, end, _hourWidth);
        }

        _selected.IntersectWith(_runs);
        MeasureHeader();
        UpdateScrollBars();
        Invalidate();
    }

    /// <summary>
    ///     Iné koľaje vynechané z plánovania - sú šrafované.
    /// </summary>
    /// <param name="excluded">Kľúče koľají vynechaných z plánovania.</param>
    public void SetExcluded(IReadOnlyCollection<string> excluded)
    {
        _excluded = excluded;
        RefreshPlan();
    }

    /// <summary>
    ///     Vlaky v súbehu na koľaji - majú červený okraj.
    /// </summary>
    /// <param name="conflicts">Jazdy v súbehu.</param>
    public void SetConflicts(ISet<TrainRun> conflicts)
    {
        _conflicts = conflicts;
        Invalidate();
    }

    /// <summary>
    ///     Aktuálny virtuálny čas - zvislá čiara.
    /// </summary>
    /// <param name="now">Čas, alebo <see langword="null" /> bez čiary.</param>
    public void SetNow(DateTime? now)
    {
        if (now == _now)
            return;

        _now = now;
        Invalidate();
    }

    /// <summary>
    ///     Výber vlakov podľa tabuľky. Ak vybraný vlak nie je vidno, diagram sa naň posunie.
    /// </summary>
    /// <param name="runs">Vybrané jazdy.</param>
    public void SetSelection(IEnumerable<TrainRun> runs)
    {
        var selected = new HashSet<TrainRun>(runs);
        if (selected.SetEquals(_selected))
            return;

        _selected = selected;
        if (selected.Count == 1)
            ScrollTo(selected.First());

        Invalidate();
    }

    /// <summary>
    ///     Posunie diagram tak, aby bolo vidno daný čas - ak už je vidno, nič nemení.
    /// </summary>
    /// <param name="time">Čas.</param>
    public void ShowTime(DateTime time)
    {
        if (_scale is null || !_hScroll.Visible)
            return;

        var plot = Plot;
        var x = _scale.X(time) - OffsetX;
        if (x < plot.Width * 0.05 || x > plot.Width * 0.95)
            SetOffsetX(_scale.X(time) - plot.Width / 4.0);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _boldFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Rozloženie a posúvanie

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyScrollBarTheme();
    }

    // tema posuvnikov potrebuje handle - pri teme pred zobrazenim okna sa nastavi az s nim
    private void ApplyScrollBarTheme()
    {
        var theme = _darkScrollBars ? WindowsTheme.DarkExplorer : WindowsTheme.Explorer;
        _hScroll.SetTheme(theme);
        _vScroll.SetTheme(theme);
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        _boldFont?.Dispose();
        _boldFont = null;
        MeasureHeader();
        UpdateScrollBars();
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateScrollBars();
    }

    private void MeasureHeader()
    {
        var widest = _lanes.Select(lane => TextRenderer.MeasureText(lane.Track.Key, Font).Width).DefaultIfEmpty(0).Max();
        _headerWidth = Math.Max(widest, TextRenderer.MeasureText("00", Font).Width) + LogicalToDeviceUnits(16);
    }

    private void UpdateScrollBars()
    {
        if (_scale is null)
        {
            _hScroll.Visible = _vScroll.Visible = false;
            return;
        }

        var width = _scale.Width;
        var thickness = SystemInformation.HorizontalScrollBarHeight;

        // pri malo kolajach sa riadky natiahnu na volnu vysku, najviac na dva riadky textu
        var rows = Math.Max(1, _lanes.Sum(lane => lane.Rows));
        var free = ClientSize.Height - AxisHeight - (width > ClientSize.Width - _headerWidth ? thickness : 0);
        _rowHeight = Math.Clamp(free / rows, Font.Height + LogicalToDeviceUnits(8), 2 * Font.Height + LogicalToDeviceUnits(8));

        var height = ContentHeight;
        bool horizontal = false, vertical = false;

        // posuvnik jedneho smeru uberie miesto druhemu - dva prechody staci
        for (var pass = 0; pass < 2; pass++)
        {
            horizontal = width > ClientSize.Width - _headerWidth - (vertical ? thickness : 0);
            vertical = height > ClientSize.Height - AxisHeight - (horizontal ? thickness : 0);
        }

        _updatingScroll = true;
        _hScroll.Visible = horizontal;
        _vScroll.Visible = vertical;

        var plot = Plot;
        _hScroll.SetBounds(plot.Left, plot.Bottom, plot.Width, thickness);
        _vScroll.SetBounds(plot.Right, plot.Top, thickness, plot.Height);
        Setup(_hScroll, width, plot.Width);
        Setup(_vScroll, height, plot.Height);
        _updatingScroll = false;
    }

    private static void Setup(ScrollBar bar, int content, int view)
    {
        view = Math.Max(1, view);
        bar.Minimum = 0;
        bar.Maximum = Math.Max(0, content - 1);
        bar.LargeChange = Math.Max(1, Math.Min(view, content));
        bar.SmallChange = Math.Max(1, view / 10);
        bar.Value = Math.Clamp(bar.Value, 0, MaxValue(bar));
    }

    private static int MaxValue(ScrollBar bar) => Math.Max(0, bar.Maximum - bar.LargeChange + 1);

    private void SetOffsetX(double value)
    {
        if (_hScroll.Visible)
            _hScroll.Value = (int)Math.Clamp(Math.Round(value), 0, MaxValue(_hScroll));
    }

    private void SetOffsetY(double value)
    {
        if (_vScroll.Visible)
            _vScroll.Value = (int)Math.Clamp(Math.Round(value), 0, MaxValue(_vScroll));
    }

    private void Scroll_ValueChanged(object? sender, EventArgs e)
    {
        if (!_updatingScroll)
            Invalidate();
    }

    private void ScrollTo(TrainRun run)
    {
        if (_scale is null)
            return;

        var plot = Plot;
        var from = _scale.X(run.OccupiedFrom) - OffsetX;
        var to = _scale.X(run.OccupiedTo) - OffsetX;
        if (to < 0 || from > plot.Width)
            SetOffsetX(_scale.X(run.OccupiedFrom) - plot.Width / 4.0);

        var top = 0;
        foreach (var lane in _lanes)
        {
            if (lane.Bars.FirstOrDefault(bar => bar.Run == run) is { } bar)
            {
                var y = top + bar.Row * RowHeight - OffsetY;
                if (y < 0 || y + RowHeight > plot.Height)
                    SetOffsetY(top + bar.Row * RowHeight - plot.Height / 2.0);
                return;
            }

            top += lane.Rows * RowHeight;
        }
    }

    private double XOf(DateTime time) => Plot.Left + _scale!.X(time) - OffsetX;

    private DateTime TimeAtX(int x) => _scale!.TimeAt(x - Plot.Left + OffsetX);

    private int LaneTop(int index)
    {
        var top = Plot.Top - OffsetY;
        for (var i = 0; i < index; i++)
            top += _lanes[i].Rows * RowHeight;

        return top;
    }

    private int LaneAt(int y)
    {
        var top = Plot.Top - OffsetY;
        for (var i = 0; i < _lanes.Count; i++)
        {
            var bottom = top + _lanes[i].Rows * RowHeight;
            if (y >= top && y < bottom)
                return i;

            top = bottom;
        }

        return -1;
    }

    private Rectangle BarRect(DateTime from, DateTime to, int top)
    {
        var left = (int)Math.Floor(XOf(from));
        var right = (int)Math.Ceiling(XOf(to));
        var inset = LogicalToDeviceUnits(2);

        return new Rectangle(left, top + inset, Math.Max(LogicalToDeviceUnits(3), right - left), RowHeight - 2 * inset);
    }

    private (DiagramBar Bar, int Lane, Rectangle Rect)? HitBar(Point point)
    {
        if (_scale is null || !Plot.Contains(point))
            return null;

        var lane = LaneAt(point.Y);
        if (lane < 0)
            return null;

        var top = LaneTop(lane);
        foreach (var bar in _lanes[lane].Bars.Reverse())
        {
            var rect = BarRect(bar.Run.OccupiedFrom, bar.Run.OccupiedTo, top + bar.Row * RowHeight);
            if (rect.Contains(point))
                return (bar, lane, rect);
        }

        return null;
    }

    #endregion

    #region Myš a klávesnica

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (HitBar(e.Location) is not { } hit)
            return;

        var run = hit.Bar.Run;
        if (e.Button == MouseButtons.Left)
        {
            if ((ModifierKeys & Keys.Control) != 0)
            {
                if (!_selected.Remove(run))
                    _selected.Add(run);
            }
            else if (!_selected.Contains(run) || _selected.Count > 1)
            {
                _selected = [run];
            }

            OnSelectionChanged();

            if (!run.IsFinished && (ModifierKeys & Keys.Control) == 0)
            {
                _drag = new Drag(hit.Bar, hit.Lane, e.Location);
                Capture = true;
            }
        }
        else if (e.Button == MouseButtons.Right && !_selected.Contains(run))
        {
            _selected = [run];
            OnSelectionChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_drag is { } drag && _scale is not null)
        {
            var size = SystemInformation.DragSize;
            if (!drag.Moving && Math.Abs(e.X - drag.Start.X) <= size.Width && Math.Abs(e.Y - drag.Start.Y) <= size.Height)
                return;

            drag.Moving = true;
            var lane = LaneAt(e.Y);
            drag.TargetLane = lane >= 0 && TrackAllocator.CanUse(_lanes[lane].Track) ? lane : drag.Lane;
            drag.Minutes = _scale.Minutes(e.X - drag.Start.X);
            UpdateTip(null);
            Invalidate();
            return;
        }

        var hit = HitBar(e.Location);
        Cursor = hit is { } h && !h.Bar.Run.IsFinished ? Cursors.SizeAll : Cursors.Default;

        if (hit is { } bar)
            UpdateTip(bar.Bar.Run);
        else if (e.X < _headerWidth && e.Y >= AxisHeight && LaneAt(e.Y) is >= 0 and var lane)
            UpdateTip(_lanes[lane]);
        else
            UpdateTip(null);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_drag is not { Moving: true } drag)
        {
            CancelDrag();
            return;
        }

        _drag = null;
        Capture = false;
        Invalidate();

        var track = drag.TargetLane != drag.Lane ? _lanes[drag.TargetLane].Track : null;
        if (track is not null || drag.Minutes != 0)
            RunMoved?.Invoke(this, new DiagramMoveEventArgs(drag.Bar.Run, track, drag.Minutes));
    }

    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture)
            CancelDrag();
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        UpdateTip(null);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && _drag is not null)
        {
            CancelDrag();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_scale is null)
            return;

        var steps = e.Delta / 120.0;
        var plot = Plot;

        if ((ModifierKeys & Keys.Control) != 0)
        {
            // cas pod mysou ostane na mieste
            var anchor = Math.Clamp(e.X, plot.Left, Math.Max(plot.Left, plot.Right - 1));
            var time = TimeAtX(anchor);
            HourWidth = (int)Math.Round(_hourWidth * Math.Pow(1.25, steps));
            SetOffsetX(_scale.X(time) - (anchor - plot.Left));
            HourWidthChanged?.Invoke(this, EventArgs.Empty);
        }
        else if ((ModifierKeys & Keys.Shift) != 0)
        {
            SetOffsetY(OffsetY - steps * RowHeight * 3);
        }
        else
        {
            SetOffsetX(OffsetX - steps * plot.Width / 8.0);
        }

        if (e is HandledMouseEventArgs handled)
            handled.Handled = true;
    }

    private void OnSelectionChanged()
    {
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelDrag()
    {
        if (_drag is null)
            return;

        _drag = null;
        Capture = false;
        Invalidate();
    }

    private void UpdateTip(object? target)
    {
        if (Equals(target, _tipTarget))
            return;

        _tipTarget = target;
        _toolTip.SetToolTip(this, target switch
        {
            TrainRun run => RunTip(run),
            DiagramLane lane => lane.Excluded ? Texts.Of(lane.Track) + Environment.NewLine + Resources.Diagram_TipExcluded : Texts.Of(lane.Track),
            _ => null
        });
    }

    private string RunTip(TrainRun run)
    {
        var lines = new List<string>
        {
            string.Join(" ", new[] { run.Train.Number, run.Train.Type?.Key, run.Train.Name }.Where(part => !string.IsNullOrWhiteSpace(part))),
            Texts.F(Resources.Diagram_TipTimes, TimeText(run.PlannedArrival, run.ArrivalDelay), TimeText(run.PlannedDeparture, run.DepartureDelay)),
            Texts.F(Resources.Diagram_TipTrack, run.Track?.Key, Texts.Of(run.TrackSource), run.OccupiedFrom, run.OccupiedTo)
        };

        if (_conflicts.Contains(run))
            lines.Add(Resources.Diagram_TipConflict);
        if (run.Suppressed)
            lines.Add(Resources.Diagram_TipSuppressed);
        if (!run.IsFinished)
            lines.Add(Resources.Diagram_TipDrag);

        return string.Join(Environment.NewLine, lines);
    }

    private static string TimeText(DateTime? planned, TimeSpan delay) => planned is { } time
        ? (time.ToString("HH:mm", CultureInfo.CurrentCulture) + " " + DelayFormat.Format(delay)).Trim()
        : "–";

    #endregion

    #region Kreslenie

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(_back);

        if (_scale is null)
        {
            TextRenderer.DrawText(g, Resources.Diagram_Empty, Font, ClientRectangle, Blend(_back, _fore, 0.6),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        var plot = Plot;
        var (labelStep, minorStep) = TickSteps();

        g.SetClip(plot);
        PaintLanes(g, plot);
        PaintGrid(g, plot, labelStep, minorStep);
        PaintBars(g);
        PaintNow(g, plot, true);
        PaintDrag(g);
        g.ResetClip();

        PaintAxis(g, plot, labelStep, minorStep);
        PaintHeader(g, plot);
    }

    private (int Label, int Minor) TickSteps()
    {
        var label = TextRenderer.MeasureText("00:00", Font).Width + LogicalToDeviceUnits(14);
        foreach (var step in Steps)
            if (step.Label * _hourWidth / 60.0 >= label)
                return step;

        return Steps[^1];
    }

    private void PaintLanes(Graphics g, Rectangle plot)
    {
        using var alternate = new SolidBrush(Blend(_back, _fore, 0.035));
        using var hatch = new HatchBrush(HatchStyle.BackwardDiagonal, Blend(_back, _fore, 0.12), _back);
        using var line = new Pen(Blend(_back, _fore, 0.18));

        for (var i = 0; i < _lanes.Count; i++)
        {
            var top = LaneTop(i);
            var height = _lanes[i].Rows * RowHeight;
            if (top > plot.Bottom || top + height < plot.Top)
                continue;

            var rect = new Rectangle(plot.Left, top, plot.Width, height);
            if (_lanes[i].Excluded)
                g.FillRectangle(hatch, rect);
            else if (i % 2 == 1)
                g.FillRectangle(alternate, rect);

            g.DrawLine(line, plot.Left, top + height - 1, plot.Right, top + height - 1);
        }
    }

    private void PaintGrid(Graphics g, Rectangle plot, int labelStep, int minorStep)
    {
        using var major = new Pen(Blend(_back, _fore, 0.16));
        using var minor = new Pen(Blend(_back, _fore, 0.07));
        using var midnight = new Pen(Blend(_back, _fore, 0.45));

        foreach (var (time, x) in Ticks(plot, minorStep))
        {
            var pen = time.TimeOfDay == TimeSpan.Zero ? midnight : (int)time.TimeOfDay.TotalMinutes % labelStep == 0 ? major : minor;
            g.DrawLine(pen, x, plot.Top, x, plot.Bottom);
        }
    }

    private IEnumerable<(DateTime Time, int X)> Ticks(Rectangle plot, int stepMinutes)
    {
        var first = TimeAtX(plot.Left);
        var minutes = (int)Math.Floor((first - _scale!.Start).TotalMinutes / stepMinutes) * stepMinutes;

        for (var time = _scale.Start.AddMinutes(minutes); time <= _scale.End; time = time.AddMinutes(stepMinutes))
        {
            var x = (int)Math.Round(XOf(time));
            if (x > plot.Right)
                yield break;
            if (x >= plot.Left)
                yield return (time, x);
        }
    }

    private void PaintBars(Graphics g)
    {
        var plot = Plot;

        for (var i = 0; i < _lanes.Count; i++)
        {
            var top = LaneTop(i);
            if (top > plot.Bottom || top + _lanes[i].Rows * RowHeight < plot.Top)
                continue;

            foreach (var bar in _lanes[i].Bars)
            {
                var run = bar.Run;
                if (XOf(run.OccupiedTo) < plot.Left || XOf(run.OccupiedFrom) > plot.Right)
                    continue;

                var dragged = _drag is { Moving: true } drag && drag.Bar == bar;
                PaintBar(g, run, run.OccupiedFrom, run.OccupiedTo, top + bar.Row * RowHeight, dragged ? 0.35 : 1.0);
            }
        }
    }

    private void PaintBar(Graphics g, TrainRun run, DateTime from, DateTime to, int top, double opacity)
    {
        var rect = BarRect(from, to, top);
        var (fill, border, text) = BarColors(run);
        var selected = _selected.Contains(run);
        var conflict = _conflicts.Contains(run);

        if (selected)
            fill = Blend(_back, _highlight, 0.45);
        if (opacity < 1)
        {
            fill = Blend(_back, fill, opacity);
            border = Blend(_back, border, opacity);
            text = Blend(_back, text, opacity);
        }

        if (run.Suppressed)
        {
            using var hatch = new HatchBrush(HatchStyle.BackwardDiagonal, Blend(_back, border, 0.35), fill);
            g.FillRectangle(hatch, rect);
        }
        else
        {
            using var brush = new SolidBrush(fill);
            g.FillRectangle(brush, rect);
        }

        // pobyt v stanici (od prichodu po odchod) je sytejsi nez vlakove cesty a rezerva
        var shift = from - run.OccupiedFrom;
        var stayFrom = (run.ActualArrival ?? run.ActualDeparture) + shift;
        var stayTo = (run.ActualDeparture ?? run.ActualArrival) + shift;
        if (stayFrom is { } a && stayTo is { } d)
        {
            var stay = BarRect(a, d, top);
            stay.Width = Math.Max(stay.Width, LogicalToDeviceUnits(2));
            using var stayBrush = new SolidBrush(Blend(fill, border, 0.45));
            g.FillRectangle(stayBrush, Rectangle.Intersect(stay, rect));
        }

        var accent = conflict ? Palette.Of(PaletteColor.Red, _back) : selected ? _highlight : border;
        using (var pen = new Pen(accent, LogicalToDeviceUnits(conflict || selected ? 2 : 1)) { Alignment = PenAlignment.Inset })
        {
            if (run.Suppressed)
                pen.DashStyle = DashStyle.Dash;
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }

        var label = run.Train.Number;
        var delay = DelayFormat.Format(run.PlannedArrival.HasValue ? run.ArrivalDelay : run.DepartureDelay);
        if (delay.Length > 0)
            label += " " + delay;

        // text, ktory sa do pruhu nezmesti, ide za neho
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                      TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        var font = run.IsChanged ? BoldFont : Font;
        var padding = LogicalToDeviceUnits(3);
        var textWidth = TextRenderer.MeasureText(g, label, font, Size.Empty, flags).Width;
        var textRect = textWidth + 2 * padding <= rect.Width
            ? Rectangle.Inflate(rect, -padding, 0)
            : new Rectangle(rect.Right + padding, rect.Top, textWidth + padding, rect.Height);
        TextRenderer.DrawText(g, label, font, textRect, text, flags);
    }

    private (Color Fill, Color Border, Color Text) BarColors(TrainRun run)
    {
        if (run.IsFinished || run.Suppressed)
        {
            var dim = Blend(_back, _fore, 0.4);
            return (Blend(_back, _fore, 0.1), dim, dim);
        }

        var color = Palette.Of(run.Events.Any(simEvent => simEvent.IsDone) ? PaletteColor.Green : PaletteColor.Blue, _back);
        return (Blend(_back, color, 0.22), color, _fore);
    }

    private void PaintNow(Graphics g, Rectangle plot, bool line)
    {
        if (_now is not { } now)
            return;

        var x = (int)Math.Round(XOf(now));
        if (x < plot.Left || x > plot.Right)
            return;

        var color = Palette.Of(PaletteColor.Orange, _back);
        if (line)
        {
            using var pen = new Pen(color, LogicalToDeviceUnits(2));
            g.DrawLine(pen, x, plot.Top, x, plot.Bottom);
            return;
        }

        // znacka na casovej osi
        var size = LogicalToDeviceUnits(5);
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, [new Point(x - size, plot.Top - size), new Point(x + size, plot.Top - size), new Point(x, plot.Top)]);
    }

    private void PaintDrag(Graphics g)
    {
        if (_drag is not { Moving: true } drag)
            return;

        var run = drag.Bar.Run;
        var shift = TimeSpan.FromMinutes(drag.Minutes);

        // ako PlanEditing.AddDelay: po ohlasenom prichode sa posuva len odchod
        var arrivalSent = run.PlannedArrival.HasValue && run.IsDone(SimEventKind.Arrival);
        var from = arrivalSent ? run.OccupiedFrom : run.OccupiedFrom + shift;
        var to = run.OccupiedTo + shift;
        var top = LaneTop(drag.TargetLane);
        PaintBar(g, run, from, to, top, 1.0);

        var info = Texts.F(Resources.Diagram_DragInfo, _lanes[drag.TargetLane].Track.Key, drag.Minutes);
        var size = TextRenderer.MeasureText(info, Font);
        var rect = BarRect(from, to, top);
        var box = new Rectangle(rect.Left, rect.Top - size.Height - LogicalToDeviceUnits(4), size.Width + LogicalToDeviceUnits(8),
            size.Height + LogicalToDeviceUnits(2));
        if (box.Top < Plot.Top)
            box.Y = rect.Bottom + LogicalToDeviceUnits(2);

        using var back = new SolidBrush(_panelBack);
        using var pen = new Pen(Blend(_back, _fore, 0.4));
        g.FillRectangle(back, box);
        g.DrawRectangle(pen, box.X, box.Y, box.Width - 1, box.Height - 1);
        TextRenderer.DrawText(g, info, Font, box, _panelFore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    private void PaintAxis(Graphics g, Rectangle plot, int labelStep, int minorStep)
    {
        var axis = new Rectangle(plot.Left, 0, plot.Width, plot.Top);
        using (var back = new SolidBrush(_panelBack))
            g.FillRectangle(back, new Rectangle(0, 0, ClientSize.Width, plot.Top));

        g.SetClip(axis);
        using var tick = new Pen(Blend(_panelBack, _panelFore, 0.35));
        var tickHeight = LogicalToDeviceUnits(4);

        foreach (var (time, x) in Ticks(plot, minorStep))
        {
            var labelled = (int)time.TimeOfDay.TotalMinutes % labelStep == 0;
            g.DrawLine(tick, x, plot.Top - (labelled ? 2 * tickHeight : tickHeight), x, plot.Top);
            if (!labelled)
                continue;

            var midnight = time.TimeOfDay == TimeSpan.Zero;
            var text = time.ToString(midnight ? "d.M." : "HH:mm", CultureInfo.CurrentCulture);
            TextRenderer.DrawText(g, text, midnight ? BoldFont : Font, new Point(x + LogicalToDeviceUnits(3), LogicalToDeviceUnits(2)),
                _panelFore, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        PaintNow(g, plot, false);
        g.ResetClip();

        using var border = new Pen(Blend(_panelBack, _panelFore, 0.25));
        g.DrawLine(border, 0, plot.Top - 1, ClientSize.Width, plot.Top - 1);
    }

    private void PaintHeader(Graphics g, Rectangle plot)
    {
        var header = new Rectangle(0, plot.Top, _headerWidth, plot.Height);
        using (var back = new SolidBrush(_panelBack))
            g.FillRectangle(back, header);

        g.SetClip(header);
        using var line = new Pen(Blend(_panelBack, _panelFore, 0.18));

        for (var i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            var top = LaneTop(i);
            var height = lane.Rows * RowHeight;
            var rect = new Rectangle(LogicalToDeviceUnits(6), top, _headerWidth - LogicalToDeviceUnits(8), Math.Min(height, RowHeight));
            var color = lane.Excluded || !TrackAllocator.CanUse(lane.Track) ? Blend(_panelBack, _panelFore, 0.5) : _panelFore;

            TextRenderer.DrawText(g, lane.Track.Key, Font, rect, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            g.DrawLine(line, 0, top + height - 1, _headerWidth, top + height - 1);
        }

        g.ResetClip();

        using var border = new Pen(Blend(_panelBack, _panelFore, 0.25));
        g.DrawLine(border, _headerWidth - 1, 0, _headerWidth - 1, plot.Bottom);
    }

    private Font BoldFont => _boldFont ??= new Font(Font, FontStyle.Bold);

    private static Color Blend(Color from, Color to, double amount) => Color.FromArgb(
        (int)Math.Round(from.R + (to.R - from.R) * amount),
        (int)Math.Round(from.G + (to.G - from.G) * amount),
        (int)Math.Round(from.B + (to.B - from.B) * amount));

    #endregion

    /// <summary>
    ///     Ťahanie vlaku - odkiaľ a kam.
    /// </summary>
    private sealed class Drag(DiagramBar bar, int lane, Point start)
    {
        public DiagramBar Bar { get; } = bar;

        public int Lane { get; } = lane;

        public Point Start { get; } = start;

        public bool Moving { get; set; }

        public int TargetLane { get; set; } = lane;

        public int Minutes { get; set; }
    }
}
