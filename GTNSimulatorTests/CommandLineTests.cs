using GTNSimulator.Tools;

namespace GTNSimulatorTests;

/// <summary>
///     Testy argumentov príkazového riadka.
/// </summary>
[TestClass]
public class CommandLineTests
{
    private static CommandLine Parse(params string[] args) => CommandLine.Parse(args);

    [TestMethod]
    public void Parse_NoArguments_IsEmpty()
    {
        var args = Parse();

        Assert.IsNull(args.Gvd);
        Assert.IsNull(args.Scenario);
        Assert.IsFalse(args.Plan || args.Start || args.Now || args.Connect || args.Help);
        Assert.AreEqual(0, args.Errors.Count);
    }

    [TestMethod]
    public void Parse_PathsWithoutOption_AreGvdAndScenario()
    {
        var args = Parse(@"C:\INISS\DATA\Stanica.2027", @"C:\Scenare\porucha.GTN");

        Assert.AreEqual(@"C:\INISS\DATA\Stanica.2027", args.Gvd);
        Assert.AreEqual(@"C:\Scenare\porucha.GTN", args.Scenario);
        Assert.AreEqual(0, args.Errors.Count);
    }

    [TestMethod]
    public void Parse_ValuesWithSpaceOrEquals()
    {
        var args = Parse("--day", "2026-10-20", "--from=14:30", "--speed", "10", "--seed=42", "--host", "192.168.1.5", "--port=7001",
            "--scenario", "den.gtn");

        Assert.AreEqual(new DateOnly(2026, 10, 20), args.Day);
        Assert.AreEqual(new TimeOnly(14, 30), args.From);
        Assert.AreEqual(10, args.Speed);
        Assert.AreEqual(42, args.Seed);
        Assert.AreEqual("192.168.1.5", args.Host);
        Assert.AreEqual(7001, args.Port);
        Assert.AreEqual("den.gtn", args.Scenario);
        Assert.AreEqual(0, args.Errors.Count);
    }

    [TestMethod]
    [DataRow("20.10.2026")]
    [DataRow("2026-10-20")]
    public void Parse_DayInIsoOrSlovakFormat(string value) =>
        Assert.AreEqual(new DateOnly(2026, 10, 20), Parse("--day", value).Day);

    [TestMethod]
    public void Parse_Flags()
    {
        var args = Parse("--connect", "--PLAN", "--start");

        Assert.IsTrue(args.Connect);
        Assert.IsTrue(args.Plan);
        Assert.IsTrue(args.Start);
        Assert.AreEqual(0, args.Errors.Count);
    }

    [TestMethod]
    [DataRow("--help")]
    [DataRow("/?")]
    [DataRow("-h")]
    public void Parse_Help(string arg) => Assert.IsTrue(Parse(arg).Help);

    [TestMethod]
    [DataRow("--day", "31.2.2026")]
    [DataRow("--from", "25:00")]
    [DataRow("--speed", "0")]
    [DataRow("--speed", "601")]
    [DataRow("--seed", "-1")]
    [DataRow("--port", "70000")]
    [DataRow("--host", " ")]
    public void Parse_BadValue_IsSkippedAndReported(string name, string value)
    {
        var args = Parse(name, value, "--plan");

        Assert.AreEqual(1, args.Errors.Count);
        Assert.IsTrue(args.Plan, "ďalšie argumenty platia");
        Assert.IsNull(args.Day);
        Assert.IsNull(args.From);
        Assert.IsNull(args.Speed);
        Assert.IsNull(args.Seed);
        Assert.IsNull(args.Port);
        Assert.IsNull(args.Host);
    }

    [TestMethod]
    public void Parse_MissingValue_DoesNotTakeNextOption()
    {
        var args = Parse("--day", "--start");

        Assert.IsNull(args.Day);
        Assert.IsTrue(args.Start);
        Assert.AreEqual(1, args.Errors.Count);
    }

    [TestMethod]
    [DataRow("--unknown")]
    [DataRow("--plan=yes")]
    public void Parse_UnknownOrFlagWithValue_IsReported(string arg) => Assert.AreEqual(1, Parse(arg).Errors.Count);

    [TestMethod]
    public void Parse_SecondGvd_IsReported()
    {
        var args = Parse(@"C:\A", @"C:\B");

        Assert.AreEqual(@"C:\A", args.Gvd);
        Assert.AreEqual(1, args.Errors.Count);
    }

    [TestMethod]
    public void Parse_NowWithDay_IsReportedAndIgnored()
    {
        var args = Parse("--now", "--day", "2026-10-20");

        Assert.IsFalse(args.Now);
        Assert.AreEqual(new DateOnly(2026, 10, 20), args.Day);
        Assert.AreEqual(1, args.Errors.Count);
    }
}
