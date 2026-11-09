using GTNSimulator.Data;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy scenára dňa - zachytenie ručných zmien, súbor a ich prenesenie na nový plán.
/// </summary>
[TestClass]
public class DayScenarioTests
{
    private static readonly DateTime Day = SimulationFixture.FirstDay;

    private static SimulationOptions Options() => new()
    {
        Seed = 7,
        OnTimeProbability = 0.5,
        TrackChangeProbability = 0.3,
        DelayReasonProbability = 1.0,
        DelayReasons = [1, 2, 3]
    };

    private static List<int> Reasons => [1, 2, 3];

    private static TrainRun Run(IReadOnlyList<TrainRun> runs, string number) => runs.Single(run => run.Train.Number == number);

    private static IReadOnlyList<TrainRun> PlanWithChanges(SimulationOptions options)
    {
        var runs = new DayPlanner(SimulationFixture.Gvd, options).Plan(Day);
        var context = new PlanEditContext(options, Day);
        var track = SimulationFixture.Gvd.Tracks.Single(candidate => candidate.Key == "1A");

        PlanEditing.SetArrivalDelay(Run(runs, "2005"), TimeSpan.FromSeconds(7 * 60 + 30), context);
        PlanEditing.SetTrack(Run(runs, "2005"), track, context);
        PlanEditing.SetDelayReason(Run(runs, "2007"), 2, context);
        PlanEditing.SetSuppressed(Run(runs, "2011"), true, context);

        return runs;
    }

    [TestMethod]
    public void Capture_KeepsOnlyChangedValues()
    {
        var options = Options();
        var runs = PlanWithChanges(options);

        var scenario = DayScenario.Capture(5461360, Day, Day.AddHours(6), options, runs);

        Assert.AreEqual(3, scenario.Changes.Count);
        var change = scenario.Changes.Single(candidate => candidate.Train == "2005");
        Assert.AreEqual(450, change.ArrivalDelaySeconds);
        Assert.AreEqual("1A", change.Track);
        Assert.IsNull(change.DelayReason, "dôvod sa nemenil");
        Assert.IsTrue(scenario.Changes.Single(candidate => candidate.Train == "2011").Suppressed);
        Assert.AreEqual(new TimeOnly(6, 0), scenario.From);
        Assert.AreEqual(7, scenario.Seed);
    }

    [TestMethod]
    public void FileRoundTrip_RebuildsTheSameDay()
    {
        var options = Options();
        var original = PlanWithChanges(options);
        var path = Path.Combine(Path.GetTempPath(), "DayScenarioTests-" + Guid.NewGuid().ToString("N") + ScenarioFile.Extension);

        try
        {
            ScenarioFile.Write(path, DayScenario.Capture(5461360, Day, Day, options, original));
            var scenario = ScenarioFile.Read(path);

            var replayOptions = scenario.ToOptions(Reasons, 1);
            var replay = new DayPlanner(SimulationFixture.Gvd, replayOptions).Plan(Day);
            var failed = scenario.Apply(replay, SimulationFixture.Gvd.Tracks, new PlanEditContext(replayOptions, Day));

            Assert.AreEqual(0, failed.Count);
            foreach (var run in original)
            {
                var copy = Run(replay, run.Train.Number);
                Assert.AreEqual(run.Current.ArrivalDelay, copy.Current.ArrivalDelay, run.Train.Number);
                Assert.AreEqual(run.Current.DepartureDelay, copy.Current.DepartureDelay, run.Train.Number);
                Assert.AreEqual(run.Track.Key, copy.Track.Key, run.Train.Number);
                Assert.AreEqual(run.DelayReason, copy.DelayReason, run.Train.Number);
                Assert.AreEqual(run.Suppressed, copy.Suppressed, run.Train.Number);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Apply_UnknownTrain_IsReported()
    {
        var options = Options();
        var runs = new DayPlanner(SimulationFixture.Gvd, options).Plan(Day);
        var scenario = new DayScenario { Changes = [new RunChange { Train = "9999", Suppressed = true }] };

        var failed = scenario.Apply(runs, SimulationFixture.Gvd.Tracks, new PlanEditContext(options, Day));

        Assert.AreEqual(1, failed.Count);
        Assert.AreEqual(PlanEditError.NotPending, failed[0].Error);
    }

    [TestMethod]
    public void Engine_PlanDayWithPrepare_AppliesChangesBeforeSkipping()
    {
        var options = Options();
        var scenario = DayScenario.Capture(5461360, Day, Day, options, PlanWithChanges(options));

        using var engine = new SimulationEngine(SimulationFixture.Gvd, scenario.ToOptions(Reasons, 1));
        engine.PlanDay(Day, Day.AddHours(10).AddMinutes(16),
            (runs, context) => Assert.AreEqual(0, scenario.Apply(runs, SimulationFixture.Gvd.Tracks, context).Count));

        var run = Run(engine.Runs!, "2005");
        Assert.AreEqual(TimeSpan.FromSeconds(450), run.ArrivalDelay);

        // prichod s meskanim 10:22:30 este caka, hoci povodny cas 10:15 bol pred zaciatkom
        Assert.IsFalse(run.IsDone(SimEventKind.Arrival));
        Assert.IsTrue(run.IsChanged);
    }
}
