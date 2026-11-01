using ILTISSimulator.Tools;
using ILTISSimulator.XML;
using ToolsCore.XML;

namespace ILTISSimulatorTests;

/// <summary>
///     Testy nastavení programu v config.xml a formátu meškania v tabuľke.
/// </summary>
[TestClass]
public class ConfigTests
{
    [TestMethod]
    public void Config_RoundTripsThroughXml()
    {
        var config = new IltisConfig();
        config.Connection.Host = "10.0.0.5";
        config.Connection.Port = 4005;
        config.Connection.WatchGtnLog = false;
        config.Simulation.Seed = 42;
        config.Simulation.TrackChangePercent = 15;
        config.Simulation.SendLineTrack = false;
        config.Simulation.LineTrack = "2A";
        config.View.HideFinished = true;
        config.ExcludedTracks.Set(5613600, ["BUS", "VYLUKA"]);
        config.SetGrid("Plan", new GridLayout { Id = "Plan", Columns = [new GridColumnLayout { Name = "cResponse", Order = 3, Visible = false, Width = 140 }] });

        var path = Path.Combine(Path.GetTempPath(), "ConfigTests-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            XmlSerialization.WriteData(path, config);
            var read = XmlSerialization.ReadData<IltisConfig>(path);

            Assert.AreEqual("10.0.0.5", read.Connection.Host);
            Assert.AreEqual(4005, read.Connection.Port);
            Assert.IsFalse(read.Connection.WatchGtnLog);
            Assert.AreEqual(42, read.Simulation.Seed);
            Assert.AreEqual(15, read.Simulation.TrackChangePercent);
            Assert.IsFalse(read.Simulation.SendLineTrack);
            Assert.AreEqual("2A", read.Simulation.LineTrack);
            Assert.IsTrue(read.View.HideFinished);
            CollectionAssert.AreEquivalent(new[] { "BUS", "VYLUKA" }, read.ExcludedTracks.Get(5613600).ToArray());
            Assert.AreEqual(0, read.ExcludedTracks.Get(1).Count);
            var column = read.GridOf("Plan")!.Columns.Single();
            Assert.IsFalse(column.Visible);
            Assert.AreEqual(140, column.Width);
            Assert.AreEqual(3, column.Order);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Config_CopyDoesNotShareNestedSettings()
    {
        var config = new IltisConfig();
        var copy = config with { };

        copy.Connection.Port = 1;
        copy.ExcludedTracks.Set(1, ["X"]);
        copy.SetGrid("Plan", new GridLayout { Id = "Plan" });

        Assert.AreEqual(4000, config.Connection.Port);
        Assert.AreEqual(0, config.ExcludedTracks.Get(1).Count);
        Assert.IsEmpty(config.Grids);
    }

    [TestMethod]
    [DataRow(0, 0, "")]
    [DataRow(5, 0, "+5")]
    [DataRow(-3, 0, "-3")]
    [DataRow(5, 30, "+5:30")]
    [DataRow(125, 0, "+125")]
    public void DelayFormat_RoundTrips(int minutes, int seconds, string text)
    {
        var delay = new TimeSpan(0, Math.Abs(minutes), seconds) * Math.Sign(minutes == 0 ? 1 : minutes);

        Assert.AreEqual(text, DelayFormat.Format(delay));
        Assert.IsTrue(DelayFormat.TryParse(text, out var parsed));
        Assert.AreEqual(delay, parsed);
    }

    [TestMethod]
    [DataRow("5", 5)]
    [DataRow(" +7 ", 7)]
    [DataRow("-0", 0)]
    public void DelayFormat_AcceptsPlainMinutes(string text, int minutes)
    {
        Assert.IsTrue(DelayFormat.TryParse(text, out var delay));
        Assert.AreEqual(TimeSpan.FromMinutes(minutes), delay);
    }

    [TestMethod]
    [DataRow("x")]
    [DataRow("5:60")]
    [DataRow("1:2:3")]
    [DataRow("5.5")]
    public void DelayFormat_RejectsOtherText(string text) => Assert.IsFalse(DelayFormat.TryParse(text, out _));
}
