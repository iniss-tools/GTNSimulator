using GTNSimulator.Gtn;

namespace GTNSimulatorTests;

/// <summary>
///     Testy enkodéra správ GTN. Očakávané hodnoty vychádzajú z rozboru <c>INISSb.EXE</c> a zo správ
///     zachytených v <c>INISSes\INISSBj\Logy\GTN.LOG</c>.
/// </summary>
[TestClass]
public class GtnEncoderTests
{
    /// <summary>
    ///     Blok, ktorý INISS prijal 29.08.2021 o 20:29:44. Odosielateľ poslal šesť správ naraz bez oddeľovača,
    ///     takže ich INISS spracoval ako jedinú správu a odpovedal <c>train 000008 not found</c>.
    /// </summary>
    private const string CapturedBlock =
        "08040000000000082920124000080461360019370829201540050802613600193708292016405" +
        "08036136006428082920164090803613600642808292021409" +
        "0803613600642808292021409";

    /// <summary>
    ///     Samostatná správa, ktorú INISS prijal 29.08.2021 o 20:30:34 a odmietol s
    ///     <c>unrecognized date/time format</c>.
    /// </summary>
    private const string CapturedShortMessage = "0803613600642808292021409";

    /// <summary>
    ///     Čas, kedy INISS zachytené správy prijal.
    /// </summary>
    private static readonly DateTime CaptureTime = new(2021, 8, 29, 20, 29, 44);

    [TestMethod]
    public void Encode_Arrival_HasVerifiedFieldLayout()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "2"
        };

        var text = GtnEncoder.Encode(message);

        Assert.AreEqual("0802", GtnEncoder.Field(text, GtnEncoder.PosType, GtnEncoder.LenType));
        Assert.AreEqual("613600", GtnEncoder.Field(text, GtnEncoder.PosStation, GtnEncoder.LenStation));
        Assert.AreEqual("2005  ", GtnEncoder.Field(text, GtnEncoder.PosTrain, GtnEncoder.LenTrain));
        Assert.AreEqual("0829201240", GtnEncoder.Field(text, GtnEncoder.PosTime, GtnEncoder.LenTime));
        Assert.AreEqual("2   ", GtnEncoder.Field(text, 26, GtnEncoder.LenTrack));
        Assert.AreEqual(30, text.Length);
    }

    [TestMethod]
    public void Encode_Route_PutsRouteKindBetweenTimeAndTrack()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Route,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            RouteKind = GtnRouteKind.Exit,
            Track = "1A"
        };

        var text = GtnEncoder.Encode(message);

        Assert.AreEqual("1", GtnEncoder.Field(text, GtnEncoder.PosRouteKind, GtnEncoder.LenRouteKind));
        Assert.AreEqual("1A  ", GtnEncoder.Field(text, 27, GtnEncoder.LenTrack));
        Assert.AreEqual(31, text.Length);
    }

    [TestMethod]
    public void Encode_Departure_AppendsLineTrackOnlyWhenSet()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Departure,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "2"
        };

        Assert.AreEqual(30, GtnEncoder.Encode(message).Length);

        message.LineTrack = "1";
        var text = GtnEncoder.Encode(message);

        Assert.AreEqual(34, text.Length);
        Assert.AreEqual("1   ", GtnEncoder.Field(text, 30, GtnEncoder.LenTrack));
    }

    [TestMethod]
    public void Encode_Route_AppendsLineTrackAfterTrack()
    {
        // 0804 nesie traťovú koľaj na pozícii 31, za druhom cesty a koľajou.
        var message = new GtnMessage
        {
            Type = GtnMessageType.Route,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            RouteKind = GtnRouteKind.Exit,
            Track = "1A",
            LineTrack = "2"
        };

        var text = GtnEncoder.Encode(message);

        Assert.AreEqual(35, text.Length);
        Assert.AreEqual("2   ", GtnEncoder.Field(text, 31, GtnEncoder.LenTrack));
        Assert.IsTrue(GtnEncoder.TryDecode(text, message.Time, out var decoded, out var error), error);
        Assert.AreEqual("1A", decoded!.Track);
        Assert.AreEqual("2", decoded.LineTrack);
    }

    [TestMethod]
    public void Encode_Arrival_IgnoresLineTrack()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "2",
            LineTrack = "1"
        };

        Assert.AreEqual(30, GtnEncoder.Encode(message).Length);
    }

    [TestMethod]
    public void Encode_RegularTime_FollowsTracksAtFixedPosition()
    {
        var regular = new DateTime(2026, 10, 20, 14, 10, 0);
        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival, StationField = "123400", TrainNumber = "1234",
            Time = new DateTime(2026, 10, 20, 14, 15, 0), Track = "2", RegularTime = regular
        };

        // 0802: koľaj 26-29, pravidelný čas 30-39
        Assert.AreEqual("08021234001234  10201415002   1020141000", GtnEncoder.Encode(message));

        // 0803 bez traťovej koľaje: jej miesto 30-33 ostane prázdne, pravidelný čas 34-43
        message.Type = GtnMessageType.Departure;
        Assert.AreEqual("08031234001234  10201415002       1020141000", GtnEncoder.Encode(message));

        message.LineTrack = "1";
        Assert.AreEqual("08031234001234  10201415002   1   1020141000", GtnEncoder.Encode(message));

        // 0804: druh cesty 26, koľaj 27-30, traťová koľaj 31-34, pravidelný čas 35-44
        message.Type = GtnMessageType.Route;
        message.RouteKind = GtnRouteKind.Exit;
        message.LineTrack = null;
        Assert.AreEqual("08041234001234  102014150012       1020141000", GtnEncoder.Encode(message));
    }

    [TestMethod]
    public void TryDecode_RegularTime_RoundTripsForEveryType()
    {
        var reference = new DateTime(2026, 10, 20, 14, 0, 0);
        var regular = new DateTime(2026, 10, 20, 14, 10, 0);

        foreach (var type in new[] { GtnMessageType.Arrival, GtnMessageType.Departure, GtnMessageType.Route })
        {
            var original = new GtnMessage
            {
                Type = type, StationField = "123400", TrainNumber = "1234", Time = new DateTime(2026, 10, 20, 14, 15, 0),
                RouteKind = GtnRouteKind.Entry, Track = "2", RegularTime = regular
            };

            Assert.IsTrue(GtnEncoder.TryDecode(GtnEncoder.Encode(original), reference, out var decoded, out var error), error);
            Assert.AreEqual(regular, decoded!.RegularTime, type.Key);
            Assert.AreEqual("2", decoded.Track, type.Key);
            Assert.IsNull(decoded.LineTrack, type.Key);
        }
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("0000000000")]
    [DataRow("          ")]
    [DataRow(" 0 0  00")]
    [DataRow("000")]
    public void TryDecode_RegularTimeOfSpacesAndZeros_IsMissing(string field)
    {
        // INISS berie pole len z medzier a núl (aj skrátené) ako chýbajúci pravidelný čas.
        Assert.IsTrue(GtnEncoder.TryDecode("0802136000190   08311824002   " + field, CaptureTime, out var message, out var error), error);
        Assert.IsNull(message!.RegularTime);
    }

    [TestMethod]
    [DataRow("083118")]
    [DataRow("08311824xx")]
    [DataRow("1")]
    public void TryDecode_InvalidRegularTime_StopsMessageLikeHeaderTime(string field)
    {
        // Neprázdne pole musí mať presne 10 znakov s číselnými dvojicami, inak INISS správu odmietne.
        Assert.IsFalse(GtnEncoder.TryDecode("08031360006428  08311839004   1   " + field, CaptureTime, out var message, out var error));
        Assert.IsNull(message);
        Assert.AreEqual("unrecognized date/time format", error);
    }

    [TestMethod]
    public void TryDecode_RegularTime_UsesSameYearRuleAsHeader()
    {
        // V januári patrí decembrový pravidelný čas do minulého roka.
        var reference = new DateTime(2027, 1, 2, 0, 10, 0);

        Assert.IsTrue(GtnEncoder.TryDecode("08021234001234  01020015002   1231235500", reference, out var message, out var error), error);
        Assert.AreEqual(new DateTime(2026, 12, 31, 23, 55, 0), message!.RegularTime);
    }

    [TestMethod]
    public void Encode_TooLongField_Throws()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "12345"
        };

        Assert.ThrowsExactly<ArgumentException>(() => GtnEncoder.Encode(message));
    }

    [TestMethod]
    public void EncodeLine_EndsWithCrLf()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.Arrival,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "2"
        };

        StringAssert.EndsWith(GtnEncoder.EncodeLine(message), "\r\n");
    }

    [TestMethod]
    public void Split_SeparatesOnAnyCombinationOfCrAndLf()
    {
        var raw = "0802A\r\n0803B\n0804C\r\r\n0802D";

        CollectionAssert.AreEqual(new[] { "0802A", "0803B", "0804C", "0802D" }, GtnEncoder.Split(raw).ToArray());
    }

    [TestMethod]
    public void Split_CapturedBlockWithoutSeparators_YieldsSingleMessage()
    {
        // Presne preto INISS z celého bloku spracoval iba prvú správu.
        Assert.AreEqual(1, GtnEncoder.Split(CapturedBlock).Count());
    }

    [TestMethod]
    public void TryDecode_CapturedBlock_ReadsTrainNumberInissReported()
    {
        var decoded = GtnEncoder.TryDecode(CapturedBlock, CaptureTime, out var message, out var error);

        Assert.IsTrue(decoded, error);
        Assert.AreEqual(GtnMessageType.Route, message!.Type);
        // INISS na túto správu odpovedal "train 000008 not found" - číslo vlaku číta zo znakov 10 až 15.
        Assert.AreEqual("000008", message.TrainNumber);
    }

    [TestMethod]
    public void TryDecode_CapturedShortMessage_FailsExactlyAsIniss()
    {
        var decoded = GtnEncoder.TryDecode(CapturedShortMessage, CaptureTime, out _, out var error);

        Assert.IsFalse(decoded);
        Assert.AreEqual("unrecognized date/time format", error);
    }

    [TestMethod]
    public void TryDecode_UnknownType_ReportsUnknownMessageType()
    {
        var decoded = GtnEncoder.TryDecode("0801" + "613600" + "2005  " + "0829201240", CaptureTime, out _, out var error);

        Assert.IsFalse(decoded);
        Assert.AreEqual("unknown message type", error);
    }

    [TestMethod]
    public void TryDecode_AlternativeCodes_AreReadAsTheirCounterparts()
    {
        // INISS spracuje 1802 až 1804 rovnako ako 0802 až 0804; 1805 nepozná.
        Assert.IsTrue(GtnEncoder.TryDecode("1802613600" + "2005  " + "0829201240" + "2", CaptureTime, out var arrival, out var error), error);
        Assert.AreEqual(GtnMessageType.Arrival, arrival!.Type);
        Assert.IsTrue(GtnEncoder.TryDecode("1803613600" + "2005  " + "0829201240" + "2   1", CaptureTime, out var departure, out error), error);
        Assert.AreEqual(GtnMessageType.Departure, departure!.Type);
        Assert.AreEqual("1", departure.LineTrack);
        Assert.IsTrue(GtnEncoder.TryDecode("1804613600" + "2005  " + "0829201240" + "12", CaptureTime, out var route, out error), error);
        Assert.AreEqual(GtnMessageType.Route, route!.Type);

        Assert.IsFalse(GtnEncoder.TryDecode("1805613600" + "2005  " + "0829201240" + "01", CaptureTime, out _, out error));
        Assert.AreEqual("unknown message type", error);
    }

    [TestMethod]
    public void TryDecode_MonthOutOfRange_IsNormalisedNotRejected()
    {
        // INISS hodnoty nekontroluje, iba ich nechá normalizovať funkciou mktime.
        var decoded = GtnEncoder.TryDecode("08020000000000082920124000", CaptureTime, out var message, out var error);

        Assert.IsTrue(decoded, error);
        Assert.AreEqual(new DateTime(2022, 5, 20, 12, 40, 0), message!.Time);
    }

    [TestMethod]
    [DataRow(10, 10, 2026)]
    [DataRow(9, 10, 2026)]
    [DataRow(4, 10, 2026)]
    [DataRow(3, 10, 2027)]
    [DataRow(1, 10, 2027)]
    [DataRow(12, 10, 2026)]
    [DataRow(12, 6, 2025)]
    [DataRow(11, 6, 2026)]
    [DataRow(1, 6, 2026)]
    [DataRow(1, 8, 2027)]
    [DataRow(2, 8, 2026)]
    public void ResolveYear_KeepsMonthWithinSixBackAndFiveAhead(int month, int currentMonth, int expectedYear)
    {
        // INISS: rozdiel mesiacov (správa mínus teraz) 6 a viac = minulý rok, -7 a menej = budúci rok.
        Assert.AreEqual(expectedYear, GtnEncoder.ResolveYear(month, new DateTime(2026, currentMonth, 15)));
    }

    [TestMethod]
    public void ResolveYear_WorksAcrossNewYear()
    {
        Assert.AreEqual(2025, GtnEncoder.ResolveYear(1, new DateTime(2024, 12, 31, 23, 55, 0)));
        Assert.AreEqual(2024, GtnEncoder.ResolveYear(12, new DateTime(2025, 1, 1, 0, 5, 0)));
    }

    [TestMethod]
    public void PreservesYear_IsFalseOutsideInissWindow()
    {
        var reference = new DateTime(2024, 8, 29);

        Assert.IsTrue(GtnEncoder.PreservesYear(new DateTime(2024, 8, 29, 20, 0, 0), reference));
        Assert.IsTrue(GtnEncoder.PreservesYear(new DateTime(2024, 10, 1, 20, 0, 0), reference));
        Assert.IsTrue(GtnEncoder.PreservesYear(new DateTime(2024, 2, 1, 20, 0, 0), reference));
        Assert.IsTrue(GtnEncoder.PreservesYear(new DateTime(2025, 1, 31, 20, 0, 0), reference));
        Assert.IsFalse(GtnEncoder.PreservesYear(new DateTime(2024, 1, 31, 20, 0, 0), reference));
        Assert.IsFalse(GtnEncoder.PreservesYear(new DateTime(2025, 2, 1, 20, 0, 0), reference));
    }

    [TestMethod]
    public void EncodeThenDecode_RoundTripsEveryField()
    {
        var reference = new DateTime(2024, 8, 29, 20, 0, 0);

        var original = new GtnMessage
        {
            Type = GtnMessageType.Departure,
            StationField = "613600",
            TrainNumber = "2005",
            Time = new DateTime(2024, 8, 29, 20, 12, 40),
            Track = "1A",
            LineTrack = "1"
        };

        Assert.IsTrue(GtnEncoder.TryDecode(GtnEncoder.Encode(original), reference, out var decoded, out var error), error);

        Assert.AreEqual(original.Type, decoded!.Type);
        Assert.AreEqual(original.StationField, decoded.StationField);
        Assert.AreEqual(original.TrainNumber, decoded.TrainNumber);
        Assert.AreEqual(original.Time, decoded.Time);
        Assert.AreEqual(original.Track, decoded.Track);
        Assert.AreEqual(original.LineTrack, decoded.LineTrack);
    }

    [TestMethod]
    public void EncodeBatch_ThenSplit_YieldsOriginalMessages()
    {
        var reference = new DateTime(2024, 8, 29, 20, 0, 0);

        var messages = new[]
        {
            new GtnMessage
            {
                Type = GtnMessageType.Route, StationField = "613600", TrainNumber = "2005",
                Time = new DateTime(2024, 8, 29, 20, 12, 0), RouteKind = GtnRouteKind.Entry, Track = "2"
            },
            new GtnMessage
            {
                Type = GtnMessageType.Arrival, StationField = "613600", TrainNumber = "2005",
                Time = new DateTime(2024, 8, 29, 20, 15, 0), Track = "2"
            }
        };

        var parts = GtnEncoder.Split(GtnEncoder.EncodeBatch(messages)).ToArray();

        Assert.AreEqual(2, parts.Length);
        Assert.IsTrue(GtnEncoder.TryDecode(parts[1], reference, out var second, out var error), error);
        Assert.AreEqual(GtnMessageType.Arrival, second!.Type);
        Assert.AreEqual(new DateTime(2024, 8, 29, 20, 15, 0), second.Time);
    }

    [TestMethod]
    public void Encode_ReproducesMessagesAcceptedByLiveIniss()
    {
        // Presné reťazce, ktoré bežiaci INISS (stanica Košice) prijal 31.08.2026 a zalogoval ako OK.
        var route = new GtnMessage
        {
            Type = GtnMessageType.Route, StationField = "136000", TrainNumber = "6428",
            Time = new DateTime(2026, 8, 31, 18, 30, 0), RouteKind = GtnRouteKind.Exit, Track = "4"
        };
        Assert.AreEqual("08041360006428  083118300014   ", GtnEncoder.Encode(route));

        var arrival = new GtnMessage
        {
            Type = GtnMessageType.Arrival, StationField = "136000", TrainNumber = "190",
            Time = new DateTime(2026, 8, 31, 18, 24, 0), Track = "2"
        };
        Assert.AreEqual("0802136000190   08311824002   ", GtnEncoder.Encode(arrival));

        var departure = new GtnMessage
        {
            Type = GtnMessageType.Departure, StationField = "136000", TrainNumber = "6428",
            Time = new DateTime(2026, 8, 31, 18, 39, 0), Track = "4"
        };
        Assert.AreEqual("08031360006428  08311839004   ", GtnEncoder.Encode(departure));
    }

    [TestMethod]
    public void Encode_DelayReason_ReproducesMessagesAcceptedByLiveIniss()
    {
        // Presné reťazce, ktoré bežiaci INISS 3.39 prijal a zalogoval ako "OK,reason ...".
        var message = new GtnMessage
        {
            Type = GtnMessageType.DelayReason, StationField = "136000", TrainNumber = "241",
            Time = new DateTime(2026, 8, 31, 14, 12, 0), DelayReason = "01"
        };
        Assert.AreEqual("0805136000241   083114120001", GtnEncoder.Encode(message));

        message.DelayReason = "00";
        Assert.AreEqual("0805136000241   083114120000", GtnEncoder.Encode(message));
        Assert.AreEqual(28, GtnEncoder.Encode(message).Length);
    }

    [TestMethod]
    public void Encode_Transfer_ReproducesMessagesAcceptedByLiveIniss()
    {
        // INISS na tieto odpovedal "OK, Urychlený přestup" a "OK, Přestup".
        var express = new GtnMessage
        {
            Type = GtnMessageType.Transfer, StationField = "136000", TrainNumber = "241",
            Time = new DateTime(2026, 8, 31, 14, 35, 35), TransferKind = GtnTransferKind.Express
        };
        Assert.AreEqual("0806136000241   08311435351", GtnEncoder.Encode(express));

        var normal = new GtnMessage
        {
            Type = GtnMessageType.Transfer, StationField = "136000", TrainNumber = "186",
            Time = new DateTime(2026, 8, 31, 14, 35, 38), TransferKind = GtnTransferKind.Normal
        };
        Assert.AreEqual("0806136000186   08311435380", GtnEncoder.Encode(normal));
        Assert.AreEqual(27, GtnEncoder.Encode(normal).Length);
    }

    [TestMethod]
    public void Encode_DelayReasonAndTransfer_CarryNoTrack()
    {
        var message = new GtnMessage
        {
            Type = GtnMessageType.DelayReason, StationField = "136000", TrainNumber = "241",
            Time = new DateTime(2026, 8, 31, 14, 12, 0), DelayReason = "03", Track = "5A"
        };

        // Koľaj sa v týchto správach neposiela, aj keby bola v modeli nastavená.
        StringAssert.EndsWith(GtnEncoder.Encode(message), "03");
    }

    [TestMethod]
    public void TryDecode_DelayReasonAndTransfer_RoundTrip()
    {
        var reference = new DateTime(2026, 8, 31, 14, 0, 0);

        Assert.IsTrue(GtnEncoder.TryDecode("0805136000241   083114120004", reference, out var reason, out var error), error);
        Assert.AreEqual(GtnMessageType.DelayReason, reason!.Type);
        Assert.AreEqual("04", reason.DelayReason);

        Assert.IsTrue(GtnEncoder.TryDecode("0806136000241   08311435351", reference, out var transfer, out error), error);
        Assert.AreEqual(GtnMessageType.Transfer, transfer!.Type);
        Assert.AreEqual(GtnTransferKind.Express, transfer.TransferKind);

        Assert.IsTrue(GtnEncoder.TryDecode("0806136000186   08311435380", reference, out var normal, out error), error);
        Assert.AreEqual(GtnTransferKind.Normal, normal!.TransferKind);
    }

    [TestMethod]
    public void FormatDelayReason_PadsToTwoDigits()
    {
        Assert.AreEqual("00", GtnEncoder.FormatDelayReason(0));
        Assert.AreEqual("04", GtnEncoder.FormatDelayReason(4));
        Assert.AreEqual("99", GtnEncoder.FormatDelayReason(99));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GtnEncoder.FormatDelayReason(100));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GtnEncoder.FormatDelayReason(-1));
    }

    [TestMethod]
    public void TryDecode_LeadingSpaceInTimePair_IsAccepted()
    {
        // INISS dvojice číta cez atoi, ktorá vedúcu medzeru preskočí - overené zachytenou správou,
        // ktorá prešla cez dátum a padla až na hľadaní vlaku.
        var decoded = GtnEncoder.TryDecode("08035613600189   08311216015", CaptureTime, out var message, out var error);

        Assert.IsTrue(decoded, error);
        Assert.AreEqual("0189", message!.TrainNumber);
    }

    [TestMethod]
    public void Field_BeyondEndOfMessage_IsEmptyNotError()
    {
        // INISS chýbajúce koncové pole nepovažuje za chybu - preto je traťová koľaj v 0803 a 0804 voliteľná.
        Assert.AreEqual("", GtnEncoder.Field("0802", 26, GtnEncoder.LenTrack));
        Assert.AreEqual("02", GtnEncoder.Field("0802", 2, GtnEncoder.LenTrack));
    }
}
