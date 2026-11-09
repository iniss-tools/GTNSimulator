using GTNSimulator.Data;

namespace GTNSimulatorTests;

/// <summary>
///     Testy načítania dôvodov meškania zo zvukovej banky.
/// </summary>
[TestClass]
public class DelayReasonReaderTests
{
    [TestMethod]
    public void IsUsable_LastItemOfGroupIsUnreachable()
    {
        // INISS testuje "číslo < počet" a potom siaha na položku "číslo - 1".
        Assert.IsFalse(DelayReasonReader.IsUsable(0, 5));
        Assert.IsTrue(DelayReasonReader.IsUsable(1, 5));
        Assert.IsTrue(DelayReasonReader.IsUsable(4, 5));
        Assert.IsFalse(DelayReasonReader.IsUsable(5, 5));
        Assert.IsFalse(DelayReasonReader.IsUsable(6, 5));
    }

    [TestMethod]
    public void IsUsable_SingleItemGroupHasNothingUsable()
    {
        Assert.IsFalse(DelayReasonReader.IsUsable(1, 1));
        Assert.IsFalse(DelayReasonReader.IsUsable(1, 0));
    }

    [TestMethod]
    public void ResolveRawBankDir_LooksTwoLevelsAboveTheTimetable()
    {
        var root = Path.Combine(Path.GetTempPath(), "gtn-bank-" + Guid.NewGuid().ToString("N"));
        var gvd = Path.Combine(root, "DATA", "Test.2025");
        Directory.CreateDirectory(gvd);

        try
        {
            Assert.IsNull(DelayReasonReader.ResolveRawBankDir(gvd), "banka ešte neexistuje");

            var bank = Path.Combine(root, DelayReasonReader.RawBankDirName);
            Directory.CreateDirectory(bank);

            Assert.AreEqual(bank, DelayReasonReader.ResolveRawBankDir(gvd));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void ResolveRawBankDir_HandlesMissingInput()
    {
        Assert.IsNull(DelayReasonReader.ResolveRawBankDir(null));
        Assert.IsNull(DelayReasonReader.ResolveRawBankDir(""));
    }

    [TestMethod]
    public void ReadForGvd_WithoutBank_ReturnsEmpty()
    {
        var root = Path.Combine(Path.GetTempPath(), "gtn-nobank-" + Guid.NewGuid().ToString("N"));
        var gvd = Path.Combine(root, "DATA", "Test.2025");
        Directory.CreateDirectory(gvd);

        try
        {
            Assert.AreEqual(0, DelayReasonReader.ReadForGvd(gvd).Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Read_MissingDirectory_Throws()
    {
        Assert.ThrowsExactly<DirectoryNotFoundException>(
            () => DelayReasonReader.Read(Path.Combine(Path.GetTempPath(), "gtn-ziadna-banka-" + Guid.NewGuid())));
    }
}
