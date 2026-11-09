using GVDEditor.Domain.Entities;
using GTNSimulator.Diagram;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy rozloženia koľajového diagramu. Vlak 2005 prechádza stanicou 10:15/10:17 po koľaji 2, vlak 2011
///     23:50/00:05 po koľaji 3, koľaje stanice v poradí grafikonu sú 1A, 2, 3.
/// </summary>
[TestClass]
public class TrackDiagramTests
{
    private static readonly DateTime Day = SimulationFixture.FirstDay;

    private static SimulationOptions Options() => new()
    {
        Seed = 1,
        OnTimeProbability = 1.0,
        DelayGrowthProbability = 0.0,
        TrackChangeProbability = 0.0,
        DelayReasonProbability = 0.0
    };

    private static IReadOnlyList<TrainRun> Plan() => new DayPlanner(SimulationFixture.Gvd, Options()).Plan(Day);

    private static TrainRun Run(IReadOnlyList<TrainRun> runs, string number) =>
        runs.Single(run => run.Train.Number == number);

    private static Track TrackOf(string key) => SimulationFixture.Gvd.Tracks.Single(track => track.Key == key);

    private static DiagramLane Lane(IReadOnlyList<DiagramLane> lanes, string key) => lanes.Single(lane => lane.Track.Key == key);

    [TestMethod]
    public void Build_LanesInTimetableOrderWithTrainsOnThem()
    {
        var runs = Plan();

        var lanes = DiagramLanes.Build(SimulationFixture.Gvd.Tracks, [], runs);

        CollectionAssert.AreEqual(new[] { "1A", "2", "3" }, lanes.Select(lane => lane.Track.Key).ToArray());
        CollectionAssert.Contains(Lane(lanes, "2").Bars.Select(bar => bar.Run.Train.Number).ToArray(), "2005");
        CollectionAssert.Contains(Lane(lanes, "3").Bars.Select(bar => bar.Run.Train.Number).ToArray(), "2011");
        Assert.AreEqual(runs.Count, lanes.Sum(lane => lane.Bars.Count));
    }

    [TestMethod]
    public void Build_MarksExcludedTrack()
    {
        var lanes = DiagramLanes.Build(SimulationFixture.Gvd.Tracks, ["1a"], Plan());

        Assert.IsTrue(Lane(lanes, "1A").Excluded);
        Assert.IsFalse(Lane(lanes, "2").Excluded);
    }

    [TestMethod]
    public void Build_TrainsAtSameTime_GetOwnRows()
    {
        var runs = Plan();
        PlanEditing.SetTrack(Run(runs, "2011"), TrackOf("2"), new PlanEditContext(Options(), Day));
        PlanEditing.SetTrack(Run(runs, "2007"), TrackOf("2"), new PlanEditContext(Options(), Day));

        var lane = Lane(DiagramLanes.Build(SimulationFixture.Gvd.Tracks, [], runs), "2");

        Assert.AreEqual(2, lane.Rows);
        var rows = lane.Bars.Where(bar => bar.Run.Train.Number is "2007" or "2011").Select(bar => bar.Row).ToArray();
        CollectionAssert.AreEquivalent(new[] { 0, 1 }, rows);
        Assert.AreEqual(0, lane.Bars.Single(bar => bar.Run.Train.Number == "2005").Row, "vlak bez súbehu ostane v prvom riadku");
    }

    [TestMethod]
    public void Build_EmptyTrack_HasOneRow()
    {
        var lanes = DiagramLanes.Build(SimulationFixture.Gvd.Tracks, [], []);

        Assert.IsTrue(lanes.All(lane => lane.Rows == 1 && lane.Bars.Count == 0));
    }

    [TestMethod]
    public void Range_CoversWholeDayAndTrainsAfterMidnight()
    {
        var (start, end) = DiagramScale.Range(Day, Plan());

        Assert.AreEqual(Day, start);
        Assert.AreEqual(Day.AddDays(1).AddHours(1), end, "vlak 2011 drží koľaj po polnoci");
    }

    [TestMethod]
    public void Range_WithoutRuns_IsOneDay()
    {
        var (start, end) = DiagramScale.Range(Day, []);

        Assert.AreEqual(Day, start);
        Assert.AreEqual(Day.AddDays(1), end);
    }

    [TestMethod]
    public void Scale_ConvertsTimeAndPosition()
    {
        var scale = new DiagramScale(Day, Day.AddDays(1), 240);

        Assert.AreEqual(240 * 10.25, scale.X(Day.AddHours(10).AddMinutes(15)), 1e-9);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(15), scale.TimeAt(240 * 10.25));
        Assert.AreEqual(240 * 24, scale.Width);
    }

    [TestMethod]
    [DataRow(240, 0.0, 0)]
    [DataRow(240, 7.9, 2)]
    [DataRow(240, 6.0, 2)]
    [DataRow(240, -6.0, -2)]
    [DataRow(60, 59.0, 59)]
    public void Scale_DragMovesByWholeMinutes(int hourWidth, double dx, int minutes) =>
        Assert.AreEqual(minutes, new DiagramScale(Day, Day.AddDays(1), hourWidth).Minutes(dx));

    [TestMethod]
    public void Scale_HourWidthIsLimited()
    {
        Assert.AreEqual(DiagramScale.MinHourWidth, new DiagramScale(Day, Day.AddDays(1), 1).HourWidth);
        Assert.AreEqual(DiagramScale.MaxHourWidth, new DiagramScale(Day, Day.AddDays(1), 100000).HourWidth);
    }
}
