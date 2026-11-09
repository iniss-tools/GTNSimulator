using GVDEditor.Domain.Entities;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy výluky koľaje. Vlak 2005 prechádza stanicou 10:15/10:17 po koľaji 2, vlak 2007 z nej vychádza o 23:50,
///     vlak 2011 prechádza 23:50/00:05 po koľaji 3. Koľaje 1A a 2 sú pri nástupišti 2, koľaj 3 pri nástupišti 3.
/// </summary>
[TestClass]
public class TrackClosureTests
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

    private static DateTime At(int hour, int minute) => Day.AddHours(hour).AddMinutes(minute);

    private static PlanEditContext Context(int hour, int minute) => new(Options(), At(hour, minute));

    private static Track TrackOf(string key) => SimulationFixture.Gvd.Tracks.Single(track => track.Key == key);

    private static string[] Keys(IEnumerable<Track> tracks) => tracks.Select(track => track.Key).ToArray();

    [TestMethod]
    public void Affected_ReturnsRunsHoldingTrackInInterval()
    {
        var runs = Plan();

        CollectionAssert.AreEqual(new[] { "2005" },
            TrackClosure.Affected(runs, TrackOf("2"), At(10, 0), At(11, 0)).Select(run => run.Train.Number).ToArray());
        Assert.AreEqual(0, TrackClosure.Affected(runs, TrackOf("2"), At(11, 0), At(12, 0)).Count);
    }

    [TestMethod]
    public void Affected_OverMidnight_IncludesTrainLeavingNextDay()
    {
        var runs = Plan();

        CollectionAssert.AreEqual(new[] { "2011" },
            TrackClosure.Affected(runs, TrackOf("3"), Day.AddDays(1), Day.AddDays(1).AddHours(1))
                .Select(run => run.Train.Number).ToArray());
    }

    [TestMethod]
    public void Affected_SkipsSuppressedRun()
    {
        var runs = Plan();
        PlanEditing.SetSuppressed(Run(runs, "2005"), true, Context(9, 0));

        Assert.AreEqual(0, TrackClosure.Affected(runs, TrackOf("2"), At(10, 0), At(11, 0)).Count);
    }

    [TestMethod]
    public void Candidates_PreferTracksAtSamePlatform()
    {
        var tracks = SimulationFixture.Gvd.Tracks;

        CollectionAssert.AreEqual(new[] { "1A", "3" }, Keys(TrackClosure.Candidates(tracks, TrackOf("2"), [])));
        CollectionAssert.AreEqual(new[] { "1A", "2" }, Keys(TrackClosure.Candidates(tracks, TrackOf("3"), [])));
    }

    [TestMethod]
    public void Candidates_SkipExcludedTracks()
    {
        CollectionAssert.AreEqual(new[] { "3" },
            Keys(TrackClosure.Candidates(SimulationFixture.Gvd.Tracks, TrackOf("2"), ["1a"])));
    }

    [TestMethod]
    public void MoveOff_MovesTrainToFreeTrackAtSamePlatform()
    {
        var runs = Plan();
        var run = Run(runs, "2005");
        var candidates = TrackClosure.Candidates(SimulationFixture.Gvd.Tracks, TrackOf("2"), []);

        Assert.AreEqual(PlanEditError.None, TrackClosure.MoveOff(run, runs, candidates, Context(9, 0)));

        Assert.AreEqual("1A", run.Track?.Key);
        Assert.AreEqual(TrackSource.Manual, run.TrackSource);
        Assert.IsTrue(run.Events.All(simEvent => simEvent.Track.Key == "1A"));
    }

    [TestMethod]
    public void MoveOff_SkipsTrackOccupiedByAnotherTrain()
    {
        var runs = Plan();
        PlanEditing.SetTrack(Run(runs, "2007"), TrackOf("1A"), Context(9, 0));
        var run = Run(runs, "2011");
        var candidates = TrackClosure.Candidates(SimulationFixture.Gvd.Tracks, TrackOf("3"), []);

        Assert.AreEqual(PlanEditError.None, TrackClosure.MoveOff(run, runs, candidates, Context(9, 0)));

        Assert.AreEqual("2", run.Track?.Key);
    }

    [TestMethod]
    public void MoveOff_WithoutFreeTrack_LeavesTrainOnClosedTrack()
    {
        var runs = Plan();
        PlanEditing.SetTrack(Run(runs, "2007"), TrackOf("1A"), Context(9, 0));
        var run = Run(runs, "2011");

        Assert.AreEqual(PlanEditError.NoFreeTrack, TrackClosure.MoveOff(run, runs, [TrackOf("1A")], Context(9, 0)));

        Assert.AreEqual("3", run.Track?.Key);
    }

    [TestMethod]
    public void MoveOff_TrainStandingOnTrack_IsNotMoved()
    {
        var runs = Plan();
        var timeline = new SimulationTimeline(runs);
        var run = Run(runs, "2005");
        timeline.DueUpTo(At(10, 15));

        Assert.AreEqual(PlanEditError.StandingOnTrack,
            TrackClosure.MoveOff(run, runs, [TrackOf("1A")], Context(10, 15)));

        Assert.AreEqual("2", run.Track?.Key);
    }

    [TestMethod]
    public void MoveOff_AfterEntryRouteWasSent_SendsNewRoute()
    {
        var runs = Plan();
        var timeline = new SimulationTimeline(runs);
        var run = Run(runs, "2005");
        timeline.DueUpTo(At(10, 11));

        Assert.AreEqual(PlanEditError.None, TrackClosure.MoveOff(run, runs, [TrackOf("1A")], Context(10, 12)));
        timeline.Sync(run);

        var correction = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.EntryRoute && !simEvent.IsDone);
        Assert.IsTrue(correction.IsCorrection);
        Assert.AreEqual("1A", correction.Track.Key);
    }
}
