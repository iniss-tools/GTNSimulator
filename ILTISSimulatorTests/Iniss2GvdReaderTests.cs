using ILTISSimulator.Data;
using ILTISSimulator.Gtn;
using ILTISSimulator.Monitor;
using ILTISSimulator.Simulation;
using ToolsCore.Iniss.Tools;

namespace ILTISSimulatorTests;

/// <summary>
///     Testy režimu INISS2 nad fiktívnou stanicou: prevod zastavení na vlaky simulátora a správy, ktoré server prijme.
/// </summary>
[TestClass]
public class Iniss2GvdReaderTests
{
    // pondelok v obdobi vlakov
    private static readonly DateTime Monday = new(2030, 1, 7);

    private string _serverDir = null!;

    private string DataDir => Path.Combine(_serverDir, "Data");

    private string StationDir => Path.Combine(DataDir, "2030", "01 Lipovec");

    [TestInitialize]
    public void Init()
    {
        _serverDir = Path.Combine(Path.GetTempPath(), "Iniss2GvdReaderTests-" + Guid.NewGuid().ToString("N"), "Server");

        Write(@"Config.xml", """<INISSCFG GTNPort="1090" />""");
        Write(@"DirList.xml", """<DL><g n="2030"><i s="5400010" d="01 Lipovec" /></g></DL>""");
        Write(@"2030\01 Lipovec\SR70.TXT", "; stanica\r\n5400010\r\n");
        Write(@"2030\01 Lipovec\PositionsDef.xml", """
            <POZICE>
              <pos k="1" n="1" tabkolej="1" zvukkolej="0100" />
              <pos k="2" n="2" tabkolej="2" zvukkolej="0200" />
              <pos k="-" n="" f="SEPARATOR" />
              <pos k="X" n="Zrušený" f="COMMON NOAUDIO" />
            </POZICE>
            """);
        Write(@"2030\01 Lipovec\Positions.xml", """
            <POZICESPOJU>
              <pos jd="v 6" cv="2002" p="2" o="2" />
              <pos cv="2002" p="1" />
              <pos cv="3003" o="2" />
              <pos cv="4004" p="1" />
              <pos jd="v 2" cv="5005" p="2" />
              <pos cv="5005" p="1" />
            </POZICESPOJU>
            """);
        Write(@"2030\GV.XML", """
            <GV f="1" l="C">
              <s>5400010</s>
              <v o="2030-01-07" d="2030-01-20">
                <v jd="jede v 1-5">
                  <z c="5400005" o="480" dcv="1001" dv="Os" nv="Javor" />
                  <z c="5400010" p="500" o="510" f="Z" dcv="2002" dv="Sp" nv="Buk" />
                  <z c="5400015" p="520" />
                </v>
                <v>
                  <z c="5400005" o="600" dcv="1001" dv="Os" />
                  <z c="5400010" p="620" o="630" f="Z" dcv="2002" dv="Sp" />
                  <z c="5400015" p="640" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v>
                  <z c="5400010" o="300" f="Z" dcv="3003" dv="Os" />
                  <z c="5400015" p="310" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v>
                  <z c="5400005" o="400" dcv="4004" dv="Os" />
                  <z c="5400010" p="410" f="Z" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v>
                  <z c="5400005" o="1430" dcv="5005" dv="R" />
                  <z c="5400010" p="10" cp="1" o="15" co="1" f="Z" />
                  <z c="5400015" p="30" cp="1" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v>
                  <z c="5400005" o="700" dcv="6006" dv="Bus" />
                  <z c="5400010" p="710" o="711" f="Z" />
                  <z c="5400015" p="720" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v f="D">
                  <z c="5400005" o="800" dcv="7007" dv="Os" />
                  <z c="5400010" p="810" o="811" f="Z" />
                  <z c="5400015" p="820" />
                </v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v>
                  <z c="5400005" o="900" dcv="8008" dv="Os" />
                  <z c="5400010" p="910" o="911" f="P Z" />
                  <z c="5400015" p="920" />
                </v>
              </v>
            </GV>
            """);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(Path.GetDirectoryName(_serverDir)!, true);

    private void Write(string relativeFile, string text)
    {
        var file = Path.Combine(DataDir, relativeFile);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text, Encodings.Win1250);
    }

    private GvdData Read() => Iniss2GvdReader.Read(StationDir);

    private static SimulationOptions OnTime() => new()
    {
        OnTimeProbability = 1.0,
        TrackChangeProbability = 0.0,
        DelayReasonProbability = 1.0,
        MinDelayForReason = TimeSpan.Zero,
        DelayReasons = [1]
    };

    private static IReadOnlyList<TrainRun> Plan(GvdData gvd, DateTime day) => new DayPlanner(gvd, OnTime()).Plan(day);

    [TestMethod]
    public void Detection_TellsStationFromGrafikon()
    {
        Assert.IsTrue(Iniss2GvdReader.IsStationDir(StationDir));
        Assert.IsFalse(Iniss2GvdReader.IsGrafikonDir(StationDir));
        Assert.IsTrue(Iniss2GvdReader.IsGrafikonDir(Path.GetDirectoryName(StationDir)!));
        Assert.IsFalse(Iniss2GvdReader.IsStationDir(Path.GetDirectoryName(StationDir)!));
        Assert.IsFalse(Iniss2GvdReader.IsStationDir(SimulationFixture.TestDataDir));
    }

    [TestMethod]
    public void FindStations_WorksFromEveryLevelOfInstallation()
    {
        Write("DirList.xml", """<DL><g n="2030"><i s="5400010" d="01 Lipovec" /><i s="5400020" d="02 Horany" /></g></DL>""");
        Write(@"2030\02 Horany\SR70.TXT", "5400020\r\n");
        Write(@"2030\02 Horany\PositionsDef.xml", "<POZICE />");

        var root = Path.GetDirectoryName(_serverDir)!;
        foreach (var dir in new[] { root, _serverDir, DataDir, Path.Combine(DataDir, "2030") })
        {
            var stations = Iniss2GvdReader.FindStations(dir);

            Assert.HasCount(2, stations!, dir);
            Assert.AreEqual(StationDir, stations![0].Directory);
            Assert.AreEqual("2030 / 01 Lipovec", stations[0].ToString());
        }
    }

    [TestMethod]
    public void FindStations_SkipsFoldersWithoutStationFiles()
    {
        Write("DirList.xml", """<DL><g n="2030"><i s="5400010" d="01 Lipovec" /><i s="5400020" d="02 Horany" /></g></DL>""");
        Write(@"2030\02 Horany\SR70.TXT", "5400020\r\n");

        Assert.HasCount(1, Iniss2GvdReader.FindStations(_serverDir)!);
    }

    [TestMethod]
    public void FindStations_StationWithoutNumberIsRejectedLikeOnServer()
    {
        Write("DirList.xml", """<DL><g n="2030"><i s="5400010" d="01 Lipovec" /><i s="5400020" d="02 Horany" /></g></DL>""");

        Assert.Throws<IOException>(() => Iniss2GvdReader.FindStations(_serverDir));
    }

    [TestMethod]
    public void FindStations_GrafikonOutsideDirListHasNoStation()
    {
        Write(@"2029\GV.XML", "<GV />");

        Assert.IsEmpty(Iniss2GvdReader.FindStations(Path.Combine(DataDir, "2029"))!);
    }

    [TestMethod]
    public void FindStations_OtherFolderIsNotIniss2()
    {
        Assert.IsNull(Iniss2GvdReader.FindStations(SimulationFixture.TestDataDir));
    }

    [TestMethod]
    public void Read_FillsStationAndServerSettings()
    {
        var gvd = Read();

        Assert.IsTrue(gvd.IsIniss2);
        Assert.AreEqual(5400010, gvd.StationId);
        Assert.AreEqual("01 Lipovec", gvd.Info.ThisStation.Name);
        Assert.AreEqual(1090, gvd.Iniss2?.GtnPort);
        Assert.AreEqual(new DateOnly(2030, 1, 7), gvd.Info.StartValidData);
        Assert.AreEqual(new DateOnly(2030, 1, 21), gvd.Info.EndValidData, "nocny vlak ide stanicou o den neskor");
        Assert.AreEqual(
            Path.Combine(_serverDir, "LOG", "GTN2030-01-07.log"),
            gvd.Iniss2?.GtnLogFile(new DateOnly(2030, 1, 7)));
    }

    [TestMethod]
    public void Read_GtnOffLeavesPortEmpty()
    {
        Write("Config.xml", "<INISSCFG />");

        Assert.IsNull(Read().Iniss2?.GtnPort);
    }

    [TestMethod]
    public void Read_KeepsOnlyRealTracks()
    {
        var keys = Read().Tracks.Select(track => track.Key).ToList();

        CollectionAssert.AreEqual(new[] { "N", "1", "2" }, keys);
    }

    [TestMethod]
    public void Read_SkipsPassingAndDVariantsButKeepsBuses()
    {
        var numbers = Read().Trains.Select(train => train.Number).ToList();

        CollectionAssert.AreEqual(new[] { "2002", "2002", "3003", "4004", "5005", "6006" }, numbers);
    }

    [TestMethod]
    public void Read_RoutingFollowsPositionInRoute()
    {
        var gvd = Read();

        Assert.AreEqual(GVDEditor.Domain.Entities.Routing.Prechadzajuci, gvd.Trains[0].Routing);
        Assert.AreEqual(GVDEditor.Domain.Entities.Routing.Vychadzajuci, gvd.Trains[2].Routing);
        Assert.IsNull(gvd.Trains[2].Arrival);
        Assert.AreEqual(GVDEditor.Domain.Entities.Routing.Konciaci, gvd.Trains[3].Routing);
        Assert.IsNull(gvd.Trains[3].Departure);
        Assert.AreEqual(new TimeOnly(8, 20), gvd.Trains[0].Arrival);
        Assert.AreEqual(new TimeOnly(8, 30), gvd.Trains[0].Departure);
    }

    [TestMethod]
    public void Read_NumberChangeInStation()
    {
        var gvd = Read();
        var train = gvd.Trains[0];

        Assert.AreEqual("1001", gvd.ArrivalNumber(train));
        Assert.AreEqual("2002", gvd.DepartureNumber(train));
        Assert.AreEqual("Sp", train.Type.Key);
        Assert.AreEqual("Buk", train.Name);
    }

    [TestMethod]
    public void Read_TerminatingTrainIsNamedByArrival()
    {
        var gvd = Read();
        var train = gvd.Trains[3];

        Assert.AreEqual("4004", train.Number);
        Assert.AreEqual("4004", gvd.ArrivalNumber(train));
    }

    [TestMethod]
    public void RunsOn_VariantsDoNotOverlap()
    {
        var gvd = Read();
        var weekday = gvd.Trains[0];
        var other = gvd.Trains[1];

        Assert.IsTrue(gvd.RunsOn(weekday, Monday));
        Assert.IsFalse(gvd.RunsOn(other, Monday), "den patri prvej variante");
        Assert.IsFalse(gvd.RunsOn(weekday, Monday.AddDays(5)));
        Assert.IsTrue(gvd.RunsOn(other, Monday.AddDays(5)));
    }

    [TestMethod]
    public void RunsOn_NightTrainCountsDayInStation()
    {
        var gvd = Read();
        var train = gvd.Trains[4];

        Assert.IsFalse(gvd.RunsOn(train, Monday), "prvy den ide stanicou az v utorok");
        Assert.IsTrue(gvd.RunsOn(train, Monday.AddDays(1)));
        Assert.IsTrue(gvd.RunsOn(train, new DateTime(2030, 1, 21)));
        Assert.AreEqual(new DateTime(2030, 1, 8, 0, 10, 0), GvdData.ArrivalOn(train, Monday.AddDays(1)));
    }

    [TestMethod]
    public void TrackOn_DependsOnDay()
    {
        var gvd = Read();

        Assert.AreEqual("1", gvd.TrackOn(gvd.Trains[0], Monday)?.Key);
        Assert.AreEqual("2", gvd.TrackOn(gvd.Trains[1], Monday.AddDays(5))?.Key, "sobota");
        Assert.AreEqual("1", gvd.TrackOn(gvd.Trains[1], Monday.AddDays(6))?.Key, "nedela");
    }

    [TestMethod]
    public void TrackOn_StartingTrainUsesDepartureTrack()
    {
        var gvd = Read();

        Assert.AreEqual("2", gvd.TrackOn(gvd.Trains[2], Monday)?.Key);
    }

    [TestMethod]
    public void TrackOn_NightTrainPicksByDayInStation()
    {
        var gvd = Read();
        var train = gvd.Trains[4];

        Assert.AreEqual("2", gvd.TrackOn(train, Monday.AddDays(1))?.Key, "utorok v stanici");
        Assert.AreEqual("1", gvd.TrackOn(train, Monday.AddDays(2))?.Key);
    }

    [TestMethod]
    public void Read_UnknownStationIsRejected()
    {
        Write("DirList.xml", """<DL><g n="2030"><i s="5400010" d="02 Horany" /></g></DL>""");
        Write(@"2030\02 Horany\SR70.TXT", "5400010\r\n");

        Assert.ThrowsExactly<FormatException>(Read);
    }

    [TestMethod]
    public void Plan_SkipsBusesAndUsesTimetableTrack()
    {
        var runs = Plan(Read(), Monday);

        Assert.IsFalse(runs.Any(run => run.Train.Number == "6006"));
        Assert.AreEqual("1", runs.Single(run => run.Train.Number == "2002").Track.Key);
    }

    [TestMethod]
    public void Plan_KeepsSameNumberAtDifferentTimes()
    {
        Write(@"2030\GV.XML", """
            <GV f="1" l="C">
              <s>5400010</s>
              <v o="2030-01-07" d="2030-01-20">
                <v><z c="5400010" o="300" f="Z" dcv="3003" /><z c="5400015" p="310" /></v>
              </v>
              <v o="2030-01-07" d="2030-01-20">
                <v><z c="5400010" o="900" f="Z" dcv="3003" /><z c="5400015" p="910" /></v>
              </v>
            </GV>
            """);

        Assert.HasCount(2, Plan(Read(), Monday));
    }

    [TestMethod]
    public void Events_NeverCarryDelayReason()
    {
        var runs = Plan(Read(), Monday);

        Assert.IsTrue(runs.Any(run => run.DelayReason != 0));
        Assert.IsFalse(runs.SelectMany(run => run.Events).Any(simEvent => simEvent.Kind == SimEventKind.DelayReason));
    }

    [TestMethod]
    public void Messages_CarryRegularTimeAndNumberByDirection()
    {
        var gvd = Read();
        var run = Plan(gvd, Monday).Single(candidate => candidate.Train.Number == "2002");
        var builder = new GtnMessageBuilder(gvd.StationId);

        var messages = run.Events.ToDictionary(simEvent => simEvent.Kind, builder.Build);
        var arrival = new DateTime(2030, 1, 7, 8, 20, 0);
        var departure = new DateTime(2030, 1, 7, 8, 30, 0);

        Assert.AreEqual("1001", messages[SimEventKind.EntryRoute].TrainNumber);
        Assert.AreEqual(arrival, messages[SimEventKind.EntryRoute].RegularTime);
        Assert.AreEqual("1001", messages[SimEventKind.Arrival].TrainNumber);
        Assert.AreEqual(arrival, messages[SimEventKind.Arrival].RegularTime);
        Assert.AreEqual("2002", messages[SimEventKind.ExitRoute].TrainNumber);
        Assert.AreEqual(departure, messages[SimEventKind.ExitRoute].RegularTime);
        Assert.AreEqual("2002", messages[SimEventKind.Departure].TrainNumber);
        Assert.AreEqual(departure, messages[SimEventKind.Departure].RegularTime);
    }

    [TestMethod]
    public void Messages_HaveLengthsTheServerAccepts()
    {
        var gvd = Read();
        var builder = new GtnMessageBuilder(gvd.StationId);

        foreach (var simEvent in Plan(gvd, Monday).SelectMany(run => run.Events))
        {
            var wire = GtnEncoder.Encode(builder.Build(simEvent));

            var expected = simEvent.Kind switch
            {
                SimEventKind.Arrival => 40,
                SimEventKind.Departure => 44,
                _ => 45
            };

            Assert.AreEqual(expected, wire.Length, wire);
        }
    }

    [TestMethod]
    public void Messages_Iniss3StayWithoutRegularTime()
    {
        var gvd = SimulationFixture.Gvd;
        var builder = new GtnMessageBuilder(gvd.StationId);
        var options = new SimulationOptions { DelayReasons = [] };

        foreach (var simEvent in new DayPlanner(gvd, options).Plan(SimulationFixture.FirstDay).SelectMany(run => run.Events))
            Assert.IsNull(builder.Build(simEvent).RegularTime);
    }

    [TestMethod]
    public void LogEntry_ParsesServerLine()
    {
        Assert.IsTrue(GtnLogEntry.TryParse(
            "07.01.2030 08:21:02: \"080200001020021 01070821001   0107082000\"\t - OK, zpoždění 1 min 2 s., kolej 1", out var entry));

        Assert.AreEqual("080200001020021 01070821001   0107082000", entry?.Message);
        Assert.AreEqual("OK, zpoždění 1 min 2 s., kolej 1", entry?.Result);
        Assert.IsTrue(entry?.Ok);
    }

    [TestMethod]
    public void LogEntry_ParsesServerRejection()
    {
        Assert.IsTrue(GtnLogEntry.TryParse("07.01.2030 08:21:02: \"0802\"\t - vlak 1001 nenalezen", out var entry));

        Assert.AreEqual("vlak 1001 nenalezen", entry?.Result);
        Assert.IsFalse(entry?.Ok);
    }

    [TestMethod]
    public void LogTail_SwitchesToNewDayFromItsStart()
    {
        var dir = Path.GetDirectoryName(_serverDir)!;
        var first = Path.Combine(dir, "GTN2030-01-07.log");
        var second = Path.Combine(dir, "GTN2030-01-08.log");
        File.WriteAllText(first, "07.01.2030 08:00:00: \"stara\"\t - OK\r\n", Encodings.Win1250);

        var current = first;
        var tail = new GtnLogTail(() => current);

        File.AppendAllText(first, "07.01.2030 23:59:00: \"nova\"\t - OK\r\n", Encodings.Win1250);
        Assert.AreEqual("nova", tail.Poll().Single().Message);

        File.WriteAllText(second, "08.01.2030 00:01:00: \"zajtra\"\t - OK\r\n", Encodings.Win1250);
        current = second;

        Assert.AreEqual("zajtra", tail.Poll().Single().Message);
        Assert.AreEqual(second, tail.Path);
    }
}
