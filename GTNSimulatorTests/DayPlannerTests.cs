using GVDEditor.Domain.Entities;
using GTNSimulator.Data;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy plánovania dňa - losovania meškaní, koľají a skladby udalostí.
/// </summary>
[TestClass]
public class DayPlannerTests
{
    private static SimulationOptions Options(int seed = 1) => new()
    {
        Seed = seed,
        OnTimeProbability = 0.5,
        TrackChangeProbability = 0.2,
        DelayReasonProbability = 0.0
    };

    private static IReadOnlyList<TrainRun> Plan(SimulationOptions options) =>
        new DayPlanner(SimulationFixture.Gvd, options).Plan(SimulationFixture.FirstDay);

    private static TrainRun Run(IReadOnlyList<TrainRun> runs, string number) =>
        runs.Single(run => run.Train.Number == number);

    [TestMethod]
    public void Plan_TakesOnlyTrainsRunningThatDay()
    {
        // Vlak 2009 má v EXPORT3B samé nuly, takže neide nikdy.
        CollectionAssert.AreEquivalent(new[] { "2005", "2007", "2011" },
            Plan(Options()).Select(run => run.Train.Number).ToArray());
    }

    [TestMethod]
    public void Plan_KeepsOneRunPerTrainNumber()
    {
        // Vlaky 4 a 5 majú v grafikone rovnaké číslo a idú v ten istý deň. INISS páruje správy iba podľa
        // čísla, takže simulátor smie poslať len jednu súvislú sadu udalostí - záznam s najnižším ID.
        var planner = new DayPlanner(SimulationFixture.Gvd, Options());
        var runs = planner.Plan(SimulationFixture.FirstDay);

        Assert.AreEqual(runs.Count, runs.Select(run => run.Train.Number).Distinct().Count());
        Assert.AreEqual(4, Run(runs, "2011").Train.ID);
        CollectionAssert.AreEqual(new[] { 5 }, planner.SkippedDuplicates.Select(train => train.ID).ToArray());
    }

    [TestMethod]
    public void Plan_SkipsBusesBeforePairingNumbers()
    {
        // INISS pri párovaní preskakuje vlaky druhu Bus - aj keď sa ich kľúč z TrTypes.txt volá inak (NAD).
        // Vlak 4 je autobus, takže číslo 2011 patrí vlaku 5 a ten už nie je dvojité číslo.
        var dir = Path.Combine(Path.GetTempPath(), "GTNSimulatorTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            foreach (var file in Directory.GetFiles(SimulationFixture.TestDataDir))
                File.Copy(file, Path.Combine(dir, Path.GetFileName(file)));

            File.WriteAllLines(Path.Combine(dir, "TRTYPES.txt"), ["Os", "R", "Bus,NAD,NAD"]);
            var export = Path.Combine(dir, "EXPORT3A.txt");
            File.WriteAllText(export, File.ReadAllText(export)
                .Replace("1,\"2005\",\"\",\"Os\"", "1,\"2005\",\"\",\"NAD\"", StringComparison.Ordinal)
                .Replace("4,\"2011\",\"\",\"Os\"", "4,\"2011\",\"\",\"Bus\"", StringComparison.Ordinal));

            var planner = new DayPlanner(GvdLightReader.Read(dir), Options());
            var runs = planner.Plan(SimulationFixture.FirstDay);

            CollectionAssert.AreEquivalent(new[] { 2, 5 }, runs.Select(run => run.Train.ID).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 4 }, planner.SkippedBuses.Select(train => train.ID).ToArray());
            Assert.IsEmpty(planner.SkippedDuplicates);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void IsBus_DecidesByBuiltInTypeNotKey()
    {
        Assert.IsTrue(GvdData.IsBus(new Train { Type = new TrainType("Bus") }));
        Assert.IsTrue(GvdData.IsBus(new Train { Type = new TrainType("Bus") { Key = "NAD", TextInTable = "NAD" } }));
        Assert.IsFalse(GvdData.IsBus(new Train { Type = new TrainType("X1", "Bus", "Bus") }));
        Assert.IsFalse(GvdData.IsBus(new Train { Type = new TrainType("Os") }));
        Assert.IsFalse(GvdData.IsBus(new Train()));
    }

    [TestMethod]
    public void Plan_IsDeterministicForSameSeed()
    {
        var first = Plan(Options(42));
        var second = Plan(Options(42));

        CollectionAssert.AreEqual(
            first.Select(run => $"{run.Train.Number}|{run.Track.Key}|{run.ArrivalDelay}|{run.DepartureDelay}").ToArray(),
            second.Select(run => $"{run.Train.Number}|{run.Track.Key}|{run.ArrivalDelay}|{run.DepartureDelay}").ToArray());
    }

    [TestMethod]
    public void Plan_DifferentSeedsEventuallyDiffer()
    {
        var seedSignatures = new HashSet<string>();
        for (var seed = 1; seed <= 20; seed++)
            seedSignatures.Add(string.Join(";", Plan(Options(seed)).Select(run => $"{run.ArrivalDelay}|{run.Track.Key}")));

        Assert.IsTrue(seedSignatures.Count > 1, "Rôzne seedy dali stále rovnaký deň.");
    }

    [TestMethod]
    public void Plan_AllOnTime_ProducesTimetableTimes()
    {
        var options = Options();
        options.OnTimeProbability = 1.0;
        options.DelayGrowthProbability = 0.0;

        foreach (var run in Plan(options))
        {
            Assert.AreEqual(TimeSpan.Zero, run.ArrivalDelay, $"vlak {run.Train.Number}");
            Assert.AreEqual(TimeSpan.Zero, run.DepartureDelay, $"vlak {run.Train.Number}");

            foreach (var simEvent in run.Events)
                Assert.AreEqual(simEvent.PlannedTime, simEvent.Time);
        }
    }

    [TestMethod]
    public void Plan_DelayShiftsEventTimes()
    {
        var options = Options();
        options.OnTimeProbability = 0.0;
        options.DelayGrowthProbability = 0.0;
        options.DelayRecoveryProbability = 0.0;

        foreach (var run in Plan(options).Where(run => run.PlannedArrival.HasValue))
        {
            Assert.IsTrue(run.ArrivalDelay > TimeSpan.Zero, $"vlak {run.Train.Number} nedostal meškanie");

            var arrival = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.Arrival);
            Assert.AreEqual(run.ArrivalDelay, arrival.Delay);
            Assert.AreEqual(run.PlannedArrival!.Value + run.ArrivalDelay, arrival.Time);
        }
    }

    [TestMethod]
    public void Plan_DelayNeverExceedsConfiguredMaximum()
    {
        var options = Options();
        options.OnTimeProbability = 0.0;
        options.MeanDelayMinutes = 400;
        options.MaxDelay = TimeSpan.FromMinutes(30);

        foreach (var run in Plan(options))
        {
            Assert.IsTrue(run.ArrivalDelay <= options.MaxDelay);
            Assert.IsTrue(run.DepartureDelay <= options.MaxDelay);
        }
    }

    [TestMethod]
    public void Plan_EventKindsFollowRouting()
    {
        var runs = Plan(Options());

        CollectionAssert.AreEqual(
            new[] { SimEventKind.EntryRoute, SimEventKind.Arrival, SimEventKind.ExitRoute, SimEventKind.Departure },
            Run(runs, "2005").Events.Select(simEvent => simEvent.Kind).ToArray());

        CollectionAssert.AreEqual(new[] { SimEventKind.ExitRoute, SimEventKind.Departure },
            Run(runs, "2007").Events.Select(simEvent => simEvent.Kind).ToArray());
    }

    [TestMethod]
    public void Plan_RoutesArePlacedAheadOfTheirEvent()
    {
        var options = Options();
        var runs = Plan(options);
        var run = Run(runs, "2005");

        var entry = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.EntryRoute);
        var arrival = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.Arrival);
        var exit = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.ExitRoute);
        var departure = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.Departure);

        Assert.AreEqual(options.EntryRouteLead, arrival.Time - entry.Time);
        Assert.AreEqual(options.ExitRouteLead, departure.Time - exit.Time);
    }

    [TestMethod]
    public void Plan_TrainWithoutTimetableTrack_GetsOneAssigned()
    {
        // Vlak 2007 má v POZICE.txt nedefinovanú koľaj.
        var run = Run(Plan(Options()), "2007");

        Assert.AreEqual(TrackSource.Assigned, run.TrackSource);
        Assert.AreNotEqual(Track.None, run.Track);
        Assert.IsFalse(string.IsNullOrWhiteSpace(run.Track.TrackName));
    }

    [TestMethod]
    public void Plan_NoTrackChange_KeepsTimetableTrack()
    {
        var options = Options();
        options.TrackChangeProbability = 0.0;

        var run = Run(Plan(options), "2005");

        Assert.AreEqual("2", run.Track.Key);
        Assert.AreEqual(TrackSource.Timetable, run.TrackSource);
    }

    [TestMethod]
    public void Plan_FullTrackChange_MovesTrainOffItsTimetableTrack()
    {
        var options = Options();
        options.TrackChangeProbability = 1.0;

        var run = Run(Plan(options), "2005");

        Assert.AreEqual(TrackSource.Changed, run.TrackSource);
        Assert.AreNotEqual("2", run.Track.Key);
    }

    [TestMethod]
    public void Plan_NeverPutsTwoTrainsOnOneTrackAtOnce()
    {
        for (var seed = 1; seed <= 50; seed++)
        {
            var runs = Plan(Options(seed))
                .Where(run => run.TrackSource != TrackSource.Conflict)
                .ToList();

            foreach (var a in runs)
            foreach (var b in runs)
            {
                if (ReferenceEquals(a, b) || a.Track.Key != b.Track.Key)
                    continue;

                var overlaps = a.OccupiedFrom < b.OccupiedTo && b.OccupiedFrom < a.OccupiedTo;
                Assert.IsFalse(overlaps,
                    $"seed {seed}: vlaky {a.Train.Number} a {b.Train.Number} stoja naraz na koľaji {a.Track.Key}");
            }
        }
    }

    [TestMethod]
    public void Plan_NeverAssignsExcludedTrack()
    {
        var options = Options();
        options.TrackChangeProbability = 1.0;
        options.ExcludedTrackKeys = new HashSet<string>(new[] { "3" }, StringComparer.OrdinalIgnoreCase);

        for (var seed = 1; seed <= 30; seed++)
        {
            options.Seed = seed;

            foreach (var run in Plan(options).Where(run => run.TrackSource != TrackSource.Timetable))
                Assert.AreNotEqual("3", run.Track.Key, $"seed {seed}: vlak {run.Train.Number} dostal vylúčenú koľaj");
        }
    }

    [TestMethod]
    public void Plan_ExcludedTrackFromTimetableIsKept()
    {
        // Vlak 2011 má v grafikone koľaj 3. Vylúčenie znamená "neprideľuj ju", nie "prepíš grafikon".
        var options = Options();
        options.TrackChangeProbability = 0.0;
        options.ExcludedTrackKeys = new HashSet<string>(new[] { "3" }, StringComparer.OrdinalIgnoreCase);

        var run = Run(Plan(options), "2011");

        Assert.AreEqual("3", run.Track.Key);
        Assert.AreEqual(TrackSource.Timetable, run.TrackSource);
    }

    [TestMethod]
    public void Plan_AllTracksExcluded_Throws()
    {
        var options = Options();
        options.ExcludedTrackKeys = new HashSet<string>(new[] { "1A", "2", "3" }, StringComparer.OrdinalIgnoreCase);

        Assert.ThrowsExactly<InvalidOperationException>(() => Plan(options));
    }

    [TestMethod]
    public void Plan_DelayedTrain_GetsReasonAndItsOwnEvent()
    {
        var options = Options();
        options.OnTimeProbability = 0.0;
        options.MeanDelayMinutes = 30;
        options.DelayReasonProbability = 1.0;
        options.DelayReasons = new List<int> { 3 };

        foreach (var run in Plan(options).Where(run => run.ArrivalDelay >= options.MinDelayForReason))
        {
            Assert.AreEqual(3, run.DelayReason, $"vlak {run.Train.Number}");

            var reason = run.Events.Single(simEvent => simEvent.Kind == SimEventKind.DelayReason);
            var entry = run.Events.First(simEvent => simEvent.Kind == SimEventKind.EntryRoute);

            // Dôvod ide spolu s vlakovou cestou, teda skôr než príchod.
            Assert.AreEqual(entry.Time, reason.Time);
        }
    }

    [TestMethod]
    public void Plan_OnTimeTrain_GetsNoReason()
    {
        var options = Options();
        options.OnTimeProbability = 1.0;
        options.DelayGrowthProbability = 0.0;
        options.DelayReasonProbability = 1.0;

        foreach (var run in Plan(options))
        {
            Assert.AreEqual(0, run.DelayReason);
            Assert.IsFalse(run.Events.Any(simEvent => simEvent.Kind == SimEventKind.DelayReason));
        }
    }

    [TestMethod]
    public void Plan_ReasonIsDrawnFromConfiguredList()
    {
        var options = Options();
        options.OnTimeProbability = 0.0;
        options.MeanDelayMinutes = 30;
        options.DelayReasonProbability = 1.0;
        options.DelayReasons = new List<int> { 2, 4 };

        for (var seed = 1; seed <= 20; seed++)
        {
            options.Seed = seed;

            foreach (var run in Plan(options).Where(run => run.DelayReason != 0))
                CollectionAssert.Contains(new[] { 2, 4 }, run.DelayReason, $"seed {seed}");
        }
    }

    [TestMethod]
    public void Plan_EmptyReasonList_SendsNoReasons()
    {
        var options = Options();
        options.OnTimeProbability = 0.0;
        options.MeanDelayMinutes = 30;
        options.DelayReasonProbability = 1.0;
        options.DelayReasons = new List<int>();

        Assert.IsTrue(Plan(options).All(run => run.DelayReason == 0));
    }

    [TestMethod]
    public void Plan_DrawnValuesMatchCurrentAndRunIsUnchanged()
    {
        foreach (var run in Plan(Options()))
        {
            Assert.AreEqual(run.Current, run.Drawn, $"vlak {run.Train.Number}");
            Assert.IsFalse(run.IsChanged, $"vlak {run.Train.Number}");
            Assert.IsTrue(run.Events.All(simEvent => simEvent.State == SimEventState.Pending));
        }
    }

    [TestMethod]
    public void Plan_OccupancyStartsAtEntryRouteAndEndsAfterDeparture()
    {
        var options = Options();
        options.OnTimeProbability = 1.0;
        options.DelayGrowthProbability = 0.0;

        var run = Run(Plan(options), "2005");

        Assert.AreEqual(run.PlannedArrival!.Value - options.EntryRouteLead, run.OccupiedFrom);
        Assert.AreEqual(run.PlannedDeparture!.Value + options.TrackReleaseMargin, run.OccupiedTo);
    }

    [TestMethod]
    public void Plan_TrainOverMidnight_KeepsDepartureAfterArrival()
    {
        var run = Run(Plan(Options()), "2011");

        Assert.IsTrue(run.ActualDeparture > run.ActualArrival);
        Assert.AreEqual(SimulationFixture.FirstDay.AddDays(1), run.PlannedDeparture!.Value.Date);
    }
}
