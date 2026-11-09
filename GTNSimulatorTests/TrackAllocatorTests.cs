using GVDEditor.Domain.Entities;
using GTNSimulator.Simulation;

namespace GTNSimulatorTests;

/// <summary>
///     Testy prideľovania koľají.
/// </summary>
[TestClass]
public class TrackAllocatorTests
{
    private static readonly DateTime Noon = new(2024, 1, 1, 12, 0, 0);

    private static TrackAllocator NewAllocator() => new(SimulationFixture.Gvd.Tracks);

    private static Track Track(string key) => SimulationFixture.Gvd.Tracks.Single(track => track.Key == key);

    [TestMethod]
    public void Ctor_SkipsUndefinedTracks()
    {
        // Track.None (riadok "N" v súbore) nie je miesto, kam sa dá postaviť vlak.
        CollectionAssert.AreEquivalent(new[] { "1A", "2", "3" },
            NewAllocator().Tracks.Select(track => track.Key).ToArray());
    }

    [TestMethod]
    public void Ctor_SkipsTracksThatDoNotFitTheMessageField()
    {
        var tracks = SimulationFixture.Gvd.Tracks.ToList();
        tracks.Add(new Track("VYLUKA", "VYLUKA", "výluková koľaj", tracks[1].Platform, "", "VYLUKA"));

        CollectionAssert.DoesNotContain(new TrackAllocator(tracks).Tracks.Select(track => track.Key).ToArray(), "VYLUKA");
    }

    [TestMethod]
    public void Ctor_SkipsExcludedTracks()
    {
        var allocator = new TrackAllocator(SimulationFixture.Gvd.Tracks, new[] { "2" });

        CollectionAssert.AreEquivalent(new[] { "1A", "3" }, allocator.Tracks.Select(track => track.Key).ToArray());
        Assert.IsFalse(allocator.IsManaged(Track("2")));
        Assert.IsTrue(allocator.IsManaged(Track("3")));
    }

    [TestMethod]
    public void Ctor_ExcludedKeysAreCaseInsensitive()
    {
        Assert.IsFalse(new TrackAllocator(SimulationFixture.Gvd.Tracks, new[] { "1a" }).IsManaged(Track("1A")));
    }

    [TestMethod]
    public void ReserveAny_NeverReturnsExcludedTrack()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var allocator = new TrackAllocator(SimulationFixture.Gvd.Tracks, new[] { "2" });
            var chosen = allocator.ReserveAny(Noon, Noon.AddMinutes(10), new Random(seed));

            Assert.AreNotEqual("2", chosen!.Key, $"seed {seed}");
        }
    }

    [TestMethod]
    public void CanUse_RejectsWhatCannotBeAnnounced()
    {
        Assert.IsFalse(TrackAllocator.CanUse(null));
        Assert.IsFalse(TrackAllocator.CanUse(GVDEditor.Domain.Entities.Track.None));
        Assert.IsFalse(TrackAllocator.CanUse(new Track("5", "5", "Kolej bez nazvu", Track("1A").Platform, "", "")));
        Assert.IsTrue(TrackAllocator.CanUse(Track("1A")));
    }

    [TestMethod]
    public void TryReserve_SameTrackOverlappingWindow_Fails()
    {
        var allocator = NewAllocator();

        Assert.IsTrue(allocator.TryReserve(Track("2"), Noon, Noon.AddMinutes(10)));
        Assert.IsFalse(allocator.TryReserve(Track("2"), Noon.AddMinutes(5), Noon.AddMinutes(15)));
    }

    [TestMethod]
    public void TryReserve_TouchingWindows_Succeeds()
    {
        var allocator = NewAllocator();

        Assert.IsTrue(allocator.TryReserve(Track("2"), Noon, Noon.AddMinutes(10)));
        Assert.IsTrue(allocator.TryReserve(Track("2"), Noon.AddMinutes(10), Noon.AddMinutes(20)));
    }

    [TestMethod]
    public void IsFree_OtherTrackIsUnaffected()
    {
        var allocator = NewAllocator();
        allocator.TryReserve(Track("2"), Noon, Noon.AddMinutes(10));

        Assert.IsFalse(allocator.IsFree(Track("2"), Noon, Noon.AddMinutes(10)));
        Assert.IsTrue(allocator.IsFree(Track("3"), Noon, Noon.AddMinutes(10)));
    }

    [TestMethod]
    public void ReserveAny_NeverReturnsOccupiedTrack()
    {
        var allocator = NewAllocator();
        var random = new Random(7);

        var first = allocator.ReserveAny(Noon, Noon.AddMinutes(10), random);
        var second = allocator.ReserveAny(Noon, Noon.AddMinutes(10), random);
        var third = allocator.ReserveAny(Noon, Noon.AddMinutes(10), random);

        CollectionAssert.AreEquivalent(new[] { "1A", "2", "3" }, new[] { first!.Key, second!.Key, third!.Key });
        Assert.IsNull(allocator.ReserveAny(Noon, Noon.AddMinutes(10), random));
    }

    [TestMethod]
    public void ReserveAny_RespectsExcludedTrack()
    {
        var allocator = NewAllocator();

        var chosen = allocator.ReserveAny(Noon, Noon.AddMinutes(10), new Random(1), except: Track("2"));

        Assert.AreNotEqual("2", chosen!.Key);
    }

    [TestMethod]
    public void ReserveAny_PrefersSamePlatform()
    {
        var allocator = NewAllocator();

        // Koľaje 1A a 2 sú pri nástupišti 2, koľaj 3 pri nástupišti 3.
        for (var i = 0; i < 20; i++)
        {
            allocator.Reset();
            var chosen = allocator.ReserveAny(Noon, Noon.AddMinutes(10), new Random(i), Track("1A"));

            Assert.AreEqual("2", chosen!.Platform.Key, $"seed {i} vybral koľaj {chosen.Key}");
        }
    }

    [TestMethod]
    public void Reset_ReleasesEverything()
    {
        var allocator = NewAllocator();
        allocator.TryReserve(Track("2"), Noon, Noon.AddMinutes(10));

        allocator.Reset();

        Assert.IsTrue(allocator.IsFree(Track("2"), Noon, Noon.AddMinutes(10)));
    }
}
