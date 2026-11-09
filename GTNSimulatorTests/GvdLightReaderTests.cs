using System.Reflection;
using GVDEditor.Domain.Entities;
using GTNSimulator.Data;
using GTNSimulator.Gtn;

namespace GTNSimulatorTests;

/// <summary>
///     Testy načítania grafikonu nad malým testovacím priečinkom v <c>TestData</c>.
/// </summary>
[TestClass]
public class GvdLightReaderTests
{
    private static GvdData _gvd = null!;

    /// <summary>
    ///     Prvý deň platnosti testovacieho grafikonu.
    /// </summary>
    private static readonly DateTime FirstDay = new(2024, 1, 1);

    [ClassInitialize]
    public static void Load(TestContext context)
    {
        var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "TestData");
        _gvd = GvdLightReader.Read(dir);
    }

    [TestMethod]
    public void Read_LoadsStationFromGrafikon()
    {
        Assert.AreEqual("5461360", _gvd.Info.ThisStation.ID);
        Assert.AreEqual("Testov", _gvd.Info.ThisStation.Name);
        Assert.AreEqual(5461360, _gvd.StationId);
        Assert.AreEqual(DateOnly.FromDateTime(FirstDay), _gvd.Info.StartValidData);
    }

    [TestMethod]
    public void Read_LoadsTracksWithNoneFirst()
    {
        Assert.AreEqual(Track.None, _gvd.Tracks[0]);
        CollectionAssert.AreEqual(new[] { "N", "1A", "2", "3" },_gvd.Tracks.Select(track => track.Key).ToArray());

        var track = _gvd.Tracks.Single(candidate => candidate.Key == "1A");
        Assert.AreEqual("Kolej 1A", track.FullName);
        Assert.AreEqual("2", track.Platform.Key);
        Assert.AreEqual("0104", track.SoundName);
    }

    [TestMethod]
    public void Read_LoadsAllTrainsSortedById()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, _gvd.Trains.Select(train => train.ID).ToArray());
        CollectionAssert.AreEqual(new[] { "2005", "2007", "2009", "2011", "2011" },
            _gvd.Trains.Select(train => train.Number).ToArray());
    }

    [TestMethod]
    public void Read_PassingTrain_HasBothTimesAndTrack()
    {
        var train = GetTrain(1);

        Assert.AreEqual(Routing.Prechadzajuci, train.Routing);
        Assert.AreEqual(new TimeOnly(10, 15), train.Arrival);
        Assert.AreEqual(new TimeOnly(10, 17), train.Departure);
        Assert.AreEqual("2", train.Track.Key);
        Assert.IsTrue(GvdData.HasRealTrack(train));
    }

    [TestMethod]
    public void Read_StartingTrain_HasNoArrival()
    {
        var train = GetTrain(2);

        Assert.AreEqual(Routing.Vychadzajuci, train.Routing);
        Assert.IsFalse(train.Arrival.HasValue);
        Assert.AreEqual(new TimeOnly(23, 50), train.Departure);
    }

    [TestMethod]
    public void Read_TrainWithoutRealTrack_IsRecognised()
    {
        // Vlak 2 má v POZICE.txt nedefinovanú koľaj - koľaj mu musí vybrať simulátor.
        Assert.IsFalse(GvdData.HasRealTrack(GetTrain(2)));
    }

    [TestMethod]
    public void Read_EndingTrain_HasNoDepartureAndKeepsFlagsAndType()
    {
        var train = GetTrain(3);

        Assert.AreEqual(Routing.Konciaci, train.Routing);
        Assert.IsFalse(train.Departure.HasValue);
        Assert.IsTrue(train.IsMedzistatny);
        Assert.AreEqual("R", train.Type.Key);
        Assert.AreEqual("Nitra", train.Name);
    }

    [TestMethod]
    public void RunsOn_FollowsBitArrayFromExport3B()
    {
        var train = GetTrain(2);

        Assert.IsTrue(_gvd.RunsOn(train, FirstDay));
        Assert.IsFalse(_gvd.RunsOn(train, FirstDay.AddDays(1)));
        Assert.IsTrue(_gvd.RunsOn(train, FirstDay.AddDays(2)));
        Assert.IsFalse(_gvd.RunsOn(train, FirstDay.AddDays(5)));
    }

    [TestMethod]
    public void TrainsOn_SkipsTrainThatNeverRuns()
    {
        var running = _gvd.TrainsOn(FirstDay).Select(train => train.ID).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 4, 5 }, running);
    }

    [TestMethod]
    public void DepartureOn_TrainOverMidnight_MovesToNextDay()
    {
        var train = GetTrain(4);

        Assert.AreEqual(new DateTime(2024, 1, 2, 23, 50, 0), GvdData.ArrivalOn(train, FirstDay.AddDays(1)));
        Assert.AreEqual(new DateTime(2024, 1, 3, 0, 5, 0), GvdData.DepartureOn(train, FirstDay.AddDays(1)));
    }

    [TestMethod]
    public void GvdTrain_CanBeEncodedIntoArrivalMessage()
    {
        var train = GetTrain(1);
        var arrival = GvdData.ArrivalOn(train, FirstDay)!.Value;

        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival,
            StationField = StationCodec.Encode(_gvd.StationId),
            TrainNumber = train.Number,
            Time = arrival.AddMinutes(12),
            Track = train.Track.TrackName
        };

        Assert.IsTrue(GtnEncoder.TryDecode(GtnEncoder.Encode(message), arrival, out var decoded, out var error), error);

        Assert.AreEqual("613600", decoded!.StationField);
        Assert.AreEqual("2005", decoded.TrainNumber);
        Assert.AreEqual("2", decoded.Track);
        Assert.AreEqual(TimeSpan.FromMinutes(12), decoded.Time - arrival);
    }

    private static Train GetTrain(int id) => _gvd.Trains.Single(train => train.ID == id);
}
