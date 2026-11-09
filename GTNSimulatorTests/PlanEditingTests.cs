using GVDEditor.Domain.Entities;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy zmien naplánovanej jazdy. Vlak 2005 prechádza stanicou 10:15/10:17 po koľaji 2, vlak 2007
///     z nej vychádza o 23:50, vlak 2011 prechádza 23:50/00:05 po koľaji 3. Všetky idú načas, vjazdová cesta
///     sa stavia 4 minúty pred príchodom, odchodová 2 minúty pred odchodom.
/// </summary>
[TestClass]
public class PlanEditingTests
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

    private static (IReadOnlyList<TrainRun> Runs, SimulationTimeline Timeline) Plan()
    {
        var runs = new DayPlanner(SimulationFixture.Gvd, Options()).Plan(Day);
        return (runs, new SimulationTimeline(runs));
    }

    private static TrainRun Run(IReadOnlyList<TrainRun> runs, string number) =>
        runs.Single(run => run.Train.Number == number);

    private static PlanEditContext At(int hour, int minute, int second = 0) =>
        new(Options(), Day.AddHours(hour).AddMinutes(minute).AddSeconds(second));

    private static Track TrackOf(string key) => SimulationFixture.Gvd.Tracks.Single(track => track.Key == key);

    private static SimEvent Pending(TrainRun run, SimEventKind kind) =>
        run.Events.Single(simEvent => simEvent.Kind == kind && !simEvent.IsDone);

    private static PlanEditError Edit(SimulationTimeline timeline, TrainRun run, Func<TrainRun, PlanEditError> edit)
    {
        var error = edit(run);
        timeline.Sync(run);
        return error;
    }

    [TestMethod]
    public void SetArrivalDelay_MovesArrivalWithRoutesAndShiftsDeparture()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        Assert.AreEqual(PlanEditError.None, PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(10), At(9, 0)));

        Assert.AreEqual(Day.AddHours(10).AddMinutes(21), Pending(run, SimEventKind.EntryRoute).Time);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(25), Pending(run, SimEventKind.Arrival).Time);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(25), Pending(run, SimEventKind.ExitRoute).Time);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(27), Pending(run, SimEventKind.Departure).Time);
        Assert.AreEqual(TimeSpan.FromMinutes(10), run.DepartureDelay);
        Assert.IsTrue(run.IsChanged);
    }

    [TestMethod]
    public void SetArrivalDelay_AfterArrivalWasSent_IsRejected()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");

        timeline.DueUpTo(Day.AddHours(10).AddMinutes(15));

        Assert.AreEqual(PlanEditError.ArrivalSent, PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(3), At(10, 15)));
        Assert.AreEqual(TimeSpan.Zero, run.ArrivalDelay);
    }

    [TestMethod]
    public void SetArrivalDelay_OnOriginatingTrain_IsRejected()
    {
        var (runs, _) = Plan();

        Assert.AreEqual(PlanEditError.NoArrival,
            PlanEditing.SetArrivalDelay(Run(runs, "2007"), TimeSpan.FromMinutes(3), At(9, 0)));
    }

    [TestMethod]
    [DataRow(-61)]
    [DataRow(17 * 60 + 1)]
    public void SetArrivalDelay_OutOfInissWindow_IsRejected(int minutes)
    {
        var (runs, _) = Plan();

        Assert.AreEqual(PlanEditError.DelayOutOfRange,
            PlanEditing.SetArrivalDelay(Run(runs, "2005"), TimeSpan.FromMinutes(minutes), At(9, 0)));
    }

    [TestMethod]
    public void SetDepartureDelay_KeepsMinimumStay()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");
        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(10), At(9, 0));

        // Pobyt 2 minúty sa smie skrátiť na jednu - meškanie na odchode teda najmenej 9 minút.
        Assert.AreEqual(PlanEditError.None, PlanEditing.SetDepartureDelay(run, TimeSpan.FromMinutes(9), At(9, 0)));
        Assert.AreEqual(PlanEditError.DepartureTooEarly,
            PlanEditing.SetDepartureDelay(run, TimeSpan.FromMinutes(8), At(9, 0)));
        Assert.AreEqual(TimeSpan.FromMinutes(9), run.DepartureDelay);
    }

    [TestMethod]
    public void SetArrivalDelay_LowerDelay_ShiftsDepartureButNeverBelowMinimum()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");
        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(10), At(9, 0));
        PlanEditing.SetDepartureDelay(run, TimeSpan.FromMinutes(12), At(9, 0));

        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(4), At(9, 0));
        Assert.AreEqual(TimeSpan.FromMinutes(6), run.DepartureDelay);

        PlanEditing.SetDepartureDelay(run, TimeSpan.FromMinutes(3), At(9, 0));
        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(20), At(9, 0));
        Assert.AreEqual(TimeSpan.FromMinutes(19), run.DepartureDelay);
    }

    [TestMethod]
    public void SetTrack_BeforeAnyMessage_ChangesTrackOfEveryEvent()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        Assert.AreEqual(PlanEditError.None, PlanEditing.SetTrack(run, TrackOf("1A"), At(9, 0)));

        Assert.AreEqual(TrackSource.Manual, run.TrackSource);
        Assert.IsTrue(run.Events.All(simEvent => simEvent.Track.Key == "1A"));
        Assert.IsFalse(run.Events.Any(simEvent => simEvent.IsCorrection));
    }

    [TestMethod]
    public void SetTrack_BackToDrawnTrack_RestoresSourceAndRunIsUnchanged()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        PlanEditing.SetTrack(run, TrackOf("1A"), At(9, 0));
        PlanEditing.SetTrack(run, TrackOf("2"), At(9, 0));

        Assert.AreEqual(TrackSource.Timetable, run.TrackSource);
        Assert.IsFalse(run.IsChanged);
    }

    [TestMethod]
    public void SetTrack_AfterEntryRouteWasSent_SendsNewRouteNow()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(11));

        Assert.AreEqual(PlanEditError.None, Edit(timeline, run, r => PlanEditing.SetTrack(r, TrackOf("1A"), At(10, 12))));

        var correction = Pending(run, SimEventKind.EntryRoute);
        Assert.IsTrue(correction.IsCorrection);
        Assert.AreEqual("1A", correction.Track.Key);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(12), correction.Time);
        Assert.AreEqual("1A", Pending(run, SimEventKind.Arrival).Track.Key);

        var sent = timeline.DueUpTo(Day.AddHours(10).AddMinutes(12));
        CollectionAssert.AreEqual(new[] { correction }, sent.ToArray());
    }

    [TestMethod]
    public void SetTrack_TwiceAfterRouteWasSent_KeepsSingleCorrection()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(11));

        PlanEditing.SetTrack(run, TrackOf("1A"), At(10, 12));
        PlanEditing.SetTrack(run, TrackOf("3"), At(10, 12));

        Assert.AreEqual(1, run.Events.Count(simEvent => simEvent.IsCorrection));
        Assert.AreEqual("3", Pending(run, SimEventKind.EntryRoute).Track.Key);

        // Späť na koľaj, s ktorou cesta odišla - dodatočná správa už netreba.
        PlanEditing.SetTrack(run, TrackOf("2"), At(10, 12));
        Assert.IsFalse(run.Events.Any(simEvent => simEvent.IsCorrection));
    }

    [TestMethod]
    public void SetTrack_WhenArrivalTimeAlreadyPassed_CorrectionGoesBeforeArrival()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(11));

        // Meškanie znížené tak, že príchod "už mal byť" - vydá sa hneď, nová cesta musí ísť pred ním.
        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(-5), At(10, 12));
        Edit(timeline, run, r => PlanEditing.SetTrack(r, TrackOf("1A"), At(10, 12)));

        var sent = timeline.DueUpTo(Day.AddHours(10).AddMinutes(12));
        CollectionAssert.AreEqual(
            new[] { SimEventKind.EntryRoute, SimEventKind.Arrival, SimEventKind.ExitRoute, SimEventKind.Departure },
            sent.Select(simEvent => simEvent.Kind).ToArray());
        Assert.AreEqual("1A", sent[0].Track.Key);
    }

    [TestMethod]
    public void SetTrack_AfterDeparture_IsRejected()
    {
        var (runs, timeline) = Plan();
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(17));

        Assert.AreEqual(PlanEditError.Finished, PlanEditing.SetTrack(Run(runs, "2005"), TrackOf("1A"), At(10, 18)));
    }

    [TestMethod]
    public void SetTrack_UnusableTrack_IsRejected()
    {
        var (runs, _) = Plan();

        Assert.AreEqual(PlanEditError.TrackUnusable, PlanEditing.SetTrack(Run(runs, "2005"), Track.None, At(9, 0)));
    }

    [TestMethod]
    public void SetDelayReason_BeforeRoute_TravelsWithEntryRoute()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        PlanEditing.SetDelayReason(run, 2, At(9, 0));

        var reason = Pending(run, SimEventKind.DelayReason);
        Assert.AreEqual(2, reason.DelayReason);
        Assert.AreEqual(Pending(run, SimEventKind.EntryRoute).Time, reason.Time);
    }

    [TestMethod]
    public void SetDelayReason_AfterReasonWasSent_SendsNewOneAndZeroClearsIt()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        Edit(timeline, run, r => PlanEditing.SetDelayReason(r, 2, At(9, 0)));
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(11));

        PlanEditing.SetDelayReason(run, 4, At(10, 12));
        Assert.AreEqual(4, Pending(run, SimEventKind.DelayReason).DelayReason);
        Assert.IsTrue(Pending(run, SimEventKind.DelayReason).IsCorrection);

        PlanEditing.SetDelayReason(run, 0, At(10, 12));
        Assert.AreEqual(0, Pending(run, SimEventKind.DelayReason).DelayReason);

        // Návrat na odoslaný dôvod - netreba nič posielať.
        PlanEditing.SetDelayReason(run, 2, At(10, 12));
        Assert.IsFalse(run.Events.Any(simEvent => simEvent.Kind == SimEventKind.DelayReason && !simEvent.IsDone));
    }

    [TestMethod]
    public void SetDelayReason_PendingReasonRemovedWithZero()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        PlanEditing.SetDelayReason(run, 2, At(9, 0));
        PlanEditing.SetDelayReason(run, 0, At(9, 0));

        Assert.IsFalse(run.Events.Any(simEvent => simEvent.Kind == SimEventKind.DelayReason));
    }

    [TestMethod]
    public void SetSuppressed_DropsPendingEventsAndRestoresThemLater()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        var total = timeline.Total;

        Edit(timeline, run, r => PlanEditing.SetSuppressed(r, true, At(9, 0)));
        Assert.AreEqual(0, run.Events.Count);
        Assert.AreEqual(total - 4, timeline.Total);
        Assert.IsTrue(run.IsChanged);

        // Po zapnutí o 10:16 ide všetko, čo sa nestihlo ohlásiť - aj príchod s pôvodným časom.
        Edit(timeline, run, r => PlanEditing.SetSuppressed(r, false, At(10, 16)));
        var sent = timeline.DueUpTo(Day.AddHours(10).AddMinutes(16));

        CollectionAssert.AreEqual(new[] { SimEventKind.EntryRoute, SimEventKind.Arrival, SimEventKind.ExitRoute },
            sent.Select(simEvent => simEvent.Kind).ToArray());
        Assert.IsFalse(run.IsChanged);
    }

    [TestMethod]
    public void SendNow_Arrival_TakesDelayFromCurrentTime()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");

        Edit(timeline, run, r => PlanEditing.SendNow(r, SimEventKind.Arrival, At(10, 20, 30)));

        Assert.AreEqual(new TimeSpan(0, 5, 30), run.ArrivalDelay);
        // Odchod sa posunul o rovnaký rozdiel, odchodová cesta pripadla presne na teraz.
        var sent = timeline.DueUpTo(Day.AddHours(10).AddMinutes(20).AddSeconds(30));
        CollectionAssert.AreEqual(new[] { SimEventKind.EntryRoute, SimEventKind.Arrival, SimEventKind.ExitRoute },
            sent.Select(simEvent => simEvent.Kind).ToArray());
        Assert.AreEqual(Day.AddHours(10).AddMinutes(20).AddSeconds(30), sent[1].Time);
    }

    [TestMethod]
    public void SendNow_EntryRoute_MovesOnlyTheRoute()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");

        Edit(timeline, run, r => PlanEditing.SendNow(r, SimEventKind.EntryRoute, At(10, 5)));

        Assert.AreEqual(Day.AddHours(10).AddMinutes(5), Pending(run, SimEventKind.EntryRoute).Time);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(15), Pending(run, SimEventKind.Arrival).Time);
        Assert.AreEqual(TimeSpan.Zero, run.ArrivalDelay);
        Assert.AreEqual(1, timeline.DueUpTo(Day.AddHours(10).AddMinutes(5)).Count);
    }

    [TestMethod]
    public void SendNow_Departure_BeforeArrival_ArrivesAtTheSameTime()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");

        Assert.AreEqual(PlanEditError.None, PlanEditing.SendNow(run, SimEventKind.Departure, At(10, 30)));

        Assert.AreEqual(run.ActualArrival, run.ActualDeparture);
        Assert.AreEqual(Day.AddHours(10).AddMinutes(30), run.ActualDeparture);
    }

    [TestMethod]
    public void SendNow_EventThatAlreadyHappened_IsRejected()
    {
        var (runs, timeline) = Plan();
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(11));

        Assert.AreEqual(PlanEditError.NotPending,
            PlanEditing.SendNow(Run(runs, "2005"), SimEventKind.EntryRoute, At(10, 12)));
    }

    [TestMethod]
    public void AddDelay_GoesToArrivalUntilItIsSentThenToDeparture()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");

        PlanEditing.AddDelay(run, TimeSpan.FromMinutes(5), At(9, 0));
        Assert.AreEqual(TimeSpan.FromMinutes(5), run.ArrivalDelay);
        Assert.AreEqual(TimeSpan.FromMinutes(5), run.DepartureDelay);

        timeline.Sync(run);
        timeline.DueUpTo(Day.AddHours(10).AddMinutes(20));

        PlanEditing.AddDelay(run, TimeSpan.FromMinutes(3), At(10, 20));
        Assert.AreEqual(TimeSpan.FromMinutes(5), run.ArrivalDelay);
        Assert.AreEqual(TimeSpan.FromMinutes(8), run.DepartureDelay);
    }

    [TestMethod]
    public void Revert_RestoresDrawnValues()
    {
        var (runs, _) = Plan();
        var run = Run(runs, "2005");
        PlanEditing.SetArrivalDelay(run, TimeSpan.FromMinutes(7), At(9, 0));
        PlanEditing.SetTrack(run, TrackOf("3"), At(9, 0));
        PlanEditing.SetDelayReason(run, 1, At(9, 0));
        PlanEditing.SetSuppressed(run, true, At(9, 0));

        Assert.AreEqual(PlanEditError.None, PlanEditing.Revert(run, At(9, 0)));

        Assert.IsFalse(run.IsChanged);
        Assert.AreEqual(4, run.Events.Count);
    }

    [TestMethod]
    public void Edits_AfterRunFinished_AreRejected()
    {
        var (runs, timeline) = Plan();
        var run = Run(runs, "2005");
        timeline.DueUpTo(Day.AddHours(11));

        Assert.IsTrue(run.IsFinished);
        Assert.AreEqual(PlanEditError.Finished, PlanEditing.SetDelayReason(run, 1, At(11, 0)));
        Assert.AreEqual(PlanEditError.Finished, PlanEditing.SetSuppressed(run, true, At(11, 0)));
        Assert.AreEqual(PlanEditError.Finished, PlanEditing.Revert(run, At(11, 0)));
    }

    [TestMethod]
    public void Conflicts_TwoTrainsOnOneTrackAtOnce_AreReported()
    {
        var (runs, _) = Plan();
        var originating = Run(runs, "2007");
        var passing = Run(runs, "2011");

        Assert.AreEqual(0, PlanConflicts.All(runs).Count);

        PlanEditing.SetTrack(originating, TrackOf("1A"), At(9, 0));
        PlanEditing.SetTrack(passing, TrackOf("1A"), At(9, 0));

        CollectionAssert.AreEqual(new[] { passing }, PlanConflicts.With(originating, runs).ToArray());
        CollectionAssert.AreEquivalent(new[] { originating, passing }, PlanConflicts.All(runs).ToArray());

        // Vlak bez správ sa do súbehu nepočíta.
        PlanEditing.SetSuppressed(passing, true, At(9, 0));
        Assert.AreEqual(0, PlanConflicts.All(runs).Count);
    }
}
