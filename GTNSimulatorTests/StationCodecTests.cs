using GTNSimulator.Gtn;

namespace GTNSimulatorTests;

/// <summary>
///     Testy kódovania stanice. Očakávané hodnoty sú overené naživo proti bežiacemu INISSu so stanicou
///     Košice (<c>IDSTATION=5613600</c>) - pole <c>136000</c> dostalo odpoveď <c>OK, track …</c>.
/// </summary>
[TestClass]
public class StationCodecTests
{
    private const int Kosice = 5613600;
    private const int BratislavaNoveMesto = 5614616;
    private const int Bardejov = 5616590;

    [TestMethod]
    public void Encode_ProducesFieldVerifiedAgainstLiveIniss()
    {
        Assert.AreEqual("136000", StationCodec.Encode(Kosice));
    }

    [TestMethod]
    public void Encode_KeepsLastFiveDigitsOfStationId()
    {
        Assert.AreEqual("146160", StationCodec.Encode(BratislavaNoveMesto));
        Assert.AreEqual("165900", StationCodec.Encode(Bardejov));
        Assert.AreEqual("332950", StationCodec.Encode(5433295));
    }

    [TestMethod]
    public void Encode_AlwaysProducesFieldOfExpectedLength()
    {
        Assert.AreEqual(GtnEncoder.LenStation, StationCodec.Encode(Kosice).Length);
        Assert.AreEqual(GtnEncoder.LenStation, StationCodec.Encode(1).Length);
    }

    [TestMethod]
    public void Encode_NegativeStation_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => StationCodec.Encode(-1));
    }

    [TestMethod]
    public void TryDecodeAsIniss_ReadsFirstFiveCharactersAsNumber()
    {
        Assert.IsTrue(StationCodec.TryDecodeAsIniss("136000", out var number));
        Assert.AreEqual(13600, number);
    }

    [TestMethod]
    public void TryDecodeAsIniss_AllowsSpacesAroundNumberOnly()
    {
        Assert.IsTrue(StationCodec.TryDecodeAsIniss(" 13600", out var leading));
        Assert.AreEqual(1360, leading);
        Assert.IsTrue(StationCodec.TryDecodeAsIniss("1360 0", out var trailing));
        Assert.AreEqual(1360, trailing);
        Assert.IsFalse(StationCodec.TryDecodeAsIniss("13 600", out _));
    }

    [TestMethod]
    public void TryDecodeAsIniss_IgnoresSixthCharacter()
    {
        // Overené naživo: pole "136009" dostalo rovnako "OK, track 3" ako "136000".
        Assert.IsTrue(StationCodec.TryDecodeAsIniss("136000", out var withZero));
        Assert.IsTrue(StationCodec.TryDecodeAsIniss("136009", out var withNine));

        Assert.AreEqual(withZero, withNine);
    }

    [TestMethod]
    public void TryDecodeAsIniss_NonNumericField_Fails()
    {
        // INISS to zaloguje ako "unknown station number format".
        Assert.IsFalse(StationCodec.TryDecodeAsIniss("61A600", out _));
        Assert.IsFalse(StationCodec.TryDecodeAsIniss("", out _));
    }

    [TestMethod]
    public void EncodeThenDecode_LandsOnNormalisedStation()
    {
        var field = StationCodec.Encode(Kosice);

        Assert.IsTrue(StationCodec.TryDecodeAsIniss(field, out var decoded));
        Assert.AreEqual(StationCodec.NormalizeAsIniss(Kosice), decoded);
        Assert.IsTrue(StationCodec.Matches(field, Kosice));
    }

    [TestMethod]
    public void Matches_RejectsFieldOfAnotherStation()
    {
        Assert.IsFalse(StationCodec.Matches(StationCodec.Encode(Bardejov), Kosice));
    }

    [TestMethod]
    public void SameStation_ComparesByEqualityOrLastFiveDigits()
    {
        // INISS: rovnosť, alebo rovnosť modulo 100 000, keď je jedno z čísel menšie ako 100 000.
        Assert.IsTrue(StationCodec.SameStation(13600, Kosice));
        Assert.IsTrue(StationCodec.SameStation(13600, 13600));
        Assert.IsTrue(StationCodec.SameStation(Kosice, Kosice));
        Assert.IsFalse(StationCodec.SameStation(5713600, Kosice));
        Assert.IsFalse(StationCodec.SameStation(16590, Kosice));
    }

    [TestMethod]
    public void NormalizeAsIniss_LosesStationPrefix()
    {
        // Prefix identifikátora sa cestou stráca - dve stanice s rovnakými poslednými piatimi číslicami
        // sú pre rozhranie GTN nerozlíšiteľné.
        Assert.AreEqual(13600, StationCodec.NormalizeAsIniss(5613600));
        Assert.AreEqual(StationCodec.NormalizeAsIniss(5613600), StationCodec.NormalizeAsIniss(5713600));
        Assert.IsTrue(StationCodec.Matches(StationCodec.Encode(5613600), 5713600));
        Assert.AreEqual(StationCodec.Encode(5613600), StationCodec.Encode(5713600));
    }
}
