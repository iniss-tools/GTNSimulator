using GTNSimulator.Monitor;
using ToolsCore.Iniss.Tools;

namespace GTNSimulatorTests;

/// <summary>
///     Testy čítania logu rozhrania INISSu (GTN.LOG) - tvar riadkov podľa skutočného logu INISSu 3.39.
/// </summary>
[TestClass]
public class GtnLogTailTests
{
    private string _dir = null!;

    [TestInitialize]
    public void Init()
    {
        _dir = Path.Combine(Path.GetTempPath(), "GtnLogTailTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_dir, true);

    private static void Append(string path, string text) => File.AppendAllText(path, text, Encodings.Win1250);

    [TestMethod]
    public void TryParse_AcceptedMessage()
    {
        Assert.IsTrue(GtnLogEntry.TryParse(
            "11.10.2026 11:07:22: \"08021360001912  10111107003P  \" - OK, delay 8 min., delay 8 min., track 3P", out var entry));

        Assert.AreEqual(new DateTime(2026, 10, 11, 11, 7, 22), entry?.Logged);
        Assert.AreEqual("08021360001912  10111107003P  ", entry?.Message);
        Assert.AreEqual("OK, delay 8 min., delay 8 min., track 3P", entry?.Result);
        Assert.IsTrue(entry?.Ok);
        Assert.AreEqual("OK, delay 8 min., track 3P", entry?.Summary);
    }

    [TestMethod]
    public void TryParse_RejectedMessage()
    {
        Assert.IsTrue(GtnLogEntry.TryParse(
            "31.08.2026 12:34:34: \"08041360001966  08311740000ZZZ \" - unknown track ZZZ (Košice)", out var entry));

        Assert.IsFalse(entry?.Ok);
        Assert.AreEqual("unknown track ZZZ (Košice)", entry?.Result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Program spustený")]
    [DataRow("11.10.2026 11:07:22 \"0802\" - OK")]
    [DataRow("xx.10.2026 11:07:22: \"0802\" - OK")]
    public void TryParse_OtherLines_AreSkipped(string line) => Assert.IsFalse(GtnLogEntry.TryParse(line, out _));

    [TestMethod]
    public void Poll_ReturnsOnlyNewCompleteLines()
    {
        var path = Path.Combine(_dir, "GTN.LOG");
        Append(path, "11.10.2026 11:00:00: \"0804\" - OK, track 1\r\n");

        var tail = new GtnLogTail(path);
        Assert.AreEqual(0, tail.Poll().Count, "riadky spred začiatku sledovania sa preskočia");

        Append(path, "11.10.2026 11:01:00: \"0802\" - OK, track 2\r\n11.10.2026 11:02:00: \"0803\" - OK, tr");
        var first = tail.Poll();
        CollectionAssert.AreEqual(new[] { "0802" }, first.Select(entry => entry.Message).ToArray());

        Append(path, "ack 2\r\n");
        var second = tail.Poll();
        CollectionAssert.AreEqual(new[] { "OK, track 2" }, second.Select(entry => entry.Result).ToArray());
        Assert.AreEqual(0, tail.Poll().Count);
    }

    [TestMethod]
    public void Poll_AfterInissStartsNewLog_ReadsFromBeginning()
    {
        var path = Path.Combine(_dir, "GTN.LOG");
        Append(path, new string('x', 200) + "\r\n");
        var tail = new GtnLogTail(path);

        // INISS odlozi plny log do GTN.BAK a zacne novy
        File.WriteAllText(path, "11.10.2026 11:03:00: \"0804\" - OK, track 3\r\n", Encodings.Win1250);

        CollectionAssert.AreEqual(new[] { "0804" }, tail.Poll().Select(entry => entry.Message).ToArray());
    }

    [TestMethod]
    public void Poll_MissingFile_ReturnsNothing()
    {
        var tail = new GtnLogTail(Path.Combine(_dir, "GTN.LOG"));

        Assert.IsFalse(tail.Exists);
        Assert.AreEqual(0, tail.Poll().Count);
    }

    [TestMethod]
    public void Find_LooksForLogInInissInstallation()
    {
        var gvd = Path.Combine(_dir, "DATA", "Stanica.2027");
        Directory.CreateDirectory(gvd);
        Directory.CreateDirectory(Path.Combine(_dir, "Logy"));
        var log = Path.Combine(_dir, "Logy", "GTN.LOG");
        File.WriteAllText(log, "");

        Assert.AreEqual(Path.GetFullPath(log), GtnLogTail.Find(gvd));
    }

    [TestMethod]
    public void Find_WithoutLog_ReturnsNull()
    {
        var gvd = Path.Combine(_dir, "DATA", "Stanica.2027");
        Directory.CreateDirectory(gvd);

        Assert.IsNull(GtnLogTail.Find(gvd));
    }
}
