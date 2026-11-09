using System.Diagnostics.CodeAnalysis;
using GTNSimulator.Properties;
using GTNSimulator.Tools;
using ToolsCore.Iniss.Tools;

namespace GTNSimulator.Gtn;

/// <summary>
///     Prevod správ rozhrania GTN na drôt a späť.
/// </summary>
/// <remarks>
///     Rozloženie polí zodpovedá tomu, ako správy rozoberá <c>INISSb.EXE</c> vo vlákne <c>ListenFuncGTN</c>.
///     Parser je pevne pozičný - odkrajuje presné počty znakov a posúva pozíciu:
///     <list type="table">
///         <item><description>0, dĺžka 4 - typ správy</description></item>
///         <item><description>4, dĺžka 6 - stanica</description></item>
///         <item><description>10, dĺžka 6 - číslo vlaku (trimuje sa)</description></item>
///         <item><description>16, dĺžka 10 - dátum a čas <c>MMddHHmmss</c></description></item>
///         <item><description>26, dĺžka 1 - iba 0804: druh vlakovej cesty</description></item>
///         <item><description>26 (0802/0803) resp. 27 (0804), dĺžka 4 - koľaj (trimuje sa)</description></item>
///         <item><description>30 (0803) resp. 31 (0804), dĺžka 4 - traťová koľaj (trimuje sa)</description></item>
///         <item><description>30 (0802), 34 (0803) resp. 35 (0804), dĺžka 10 - pravidelný čas <c>MMddHHmmss</c>
///             (netrimuje sa)</description></item>
///     </list>
///     Polia za koncom reťazca vracia INISS ako prázdne, nie ako chybu.
/// </remarks>
public static class GtnEncoder
{
    /// <summary>
    ///     Ukončenie správy. INISS číta zo socketu, kým nepríde CR alebo LF.
    /// </summary>
    public const string Terminator = "\r\n";

    /// <summary>
    ///     Znaky, ktoré INISS berie ako oddeľovač správ. Ide o znakovú množinu, nie o pevnú dvojicu - v jednom
    ///     zápise na socket teda môže byť viac správ oddelených ľubovoľnou kombináciou CR a LF.
    /// </summary>
    public static readonly char[] Separators = { '\r', '\n' };

    /// <summary>
    ///     Formát dátumu a času v správe. Rok správa nenesie, INISS si ho dopočíta - viď <see cref="ResolveYear" />.
    /// </summary>
    public const string TimeFormat = "MMddHHmmss";

    /// <summary>
    ///     O koľko mesiacov dozadu od aktuálneho mesiaca dopočíta INISS ešte tento rok.
    /// </summary>
    public const int MonthsBack = 6;

    /// <summary>
    ///     O koľko mesiacov dopredu od aktuálneho mesiaca dopočíta INISS ešte tento rok.
    /// </summary>
    public const int MonthsAhead = 5;

    /// <summary>
    ///     Najstarší dátum, ktorý INISS prijme. Čas skladá cez <c>mktime</c>, ktorá staršie dátumy odmieta
    ///     a zaloguje ich ako <c>invalid date</c>.
    /// </summary>
    public static readonly DateTime MinTime = new(1970, 1, 1);

    /// <summary>
    ///     Pozícia poľa s typom správy.
    /// </summary>
    public const int PosType = 0;

    /// <summary>
    ///     Dĺžka poľa s typom správy.
    /// </summary>
    public const int LenType = 4;

    /// <summary>
    ///     Pozícia poľa so stanicou.
    /// </summary>
    public const int PosStation = 4;

    /// <summary>
    ///     Dĺžka poľa so stanicou.
    /// </summary>
    public const int LenStation = 6;

    /// <summary>
    ///     Pozícia poľa s číslom vlaku.
    /// </summary>
    public const int PosTrain = 10;

    /// <summary>
    ///     Dĺžka poľa s číslom vlaku.
    /// </summary>
    public const int LenTrain = 6;

    /// <summary>
    ///     Pozícia poľa s dátumom a časom.
    /// </summary>
    public const int PosTime = 16;

    /// <summary>
    ///     Dĺžka poľa s dátumom a časom.
    /// </summary>
    public const int LenTime = 10;

    /// <summary>
    ///     Pozícia poľa s druhom vlakovej cesty (iba 0804).
    /// </summary>
    public const int PosRouteKind = 26;

    /// <summary>
    ///     Dĺžka poľa s druhom vlakovej cesty.
    /// </summary>
    public const int LenRouteKind = 1;

    /// <summary>
    ///     Dĺžka poľa s koľajou.
    /// </summary>
    public const int LenTrack = 4;

    /// <summary>
    ///     Dĺžka poľa s pravidelným časom - rovnaký formát ako čas v hlavičke.
    /// </summary>
    public const int LenRegularTime = LenTime;

    /// <summary>
    ///     Pozícia poľa s dôvodom meškania (iba 080-5).
    /// </summary>
    public const int PosDelayReason = 26;

    /// <summary>
    ///     Dĺžka poľa s dôvodom meškania.
    /// </summary>
    public const int LenDelayReason = 2;

    /// <summary>
    ///     Pozícia poľa s druhom prestupu (iba 080-6).
    /// </summary>
    public const int PosTransferKind = 26;

    /// <summary>
    ///     Dĺžka poľa s druhom prestupu.
    /// </summary>
    public const int LenTransferKind = 1;

    /// <summary>
    ///     Najväčší časový rozdiel, ktorý INISS pri správe 080-6 pripustí. Táto správa sa neporovnáva
    ///     s grafikonom, ale s hodinami INISSu - staršia alebo novšia sa zahodí ako <c>out of date</c>.
    /// </summary>
    public static readonly TimeSpan TransferFreshness = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     Dĺžka spoločnej hlavičky všetkých typov správ (typ + stanica + vlak + čas).
    /// </summary>
    public const int HeaderLength = PosTime + LenTime;

    /// <summary>
    ///     Zostaví správu do podoby, v akej ide na drôt (bez ukončenia).
    /// </summary>
    /// <param name="message">Správa.</param>
    /// <returns>Text správy.</returns>
    /// <exception cref="ArgumentNullException">Ak je <paramref name="message" /> alebo jeho typ <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Ak sa niektoré pole nezmestí do svojej pevnej dĺžky.</exception>
    public static string Encode(GtnMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Type is null)
            throw new ArgumentNullException(nameof(message), Resources.Error_MessageTypeNull);

        var builder = new StringBuilder(HeaderLength + LenRouteKind + 2 * LenTrack);

        builder.Append(Fixed(message.Type!.Key!, LenType, Resources.Field_MessageType));
        builder.Append(Fixed(message.StationField, LenStation, Resources.Field_Station));
        builder.Append(Fixed(message.TrainNumber, LenTrain, Resources.Field_TrainNumber));
        builder.Append(message.Time.ToString(TimeFormat, CultureInfo.InvariantCulture));

        if (message.Type == GtnMessageType.DelayReason)
        {
            builder.Append(Fixed(message.DelayReason, LenDelayReason, Resources.Field_DelayReason));
            return builder.ToString();
        }

        if (message.Type == GtnMessageType.Transfer)
        {
            builder.Append((int)message.TransferKind);
            return builder.ToString();
        }

        if (message.Type == GtnMessageType.Route)
            builder.Append((int)message.RouteKind);

        builder.Append(Fixed(message.Track, LenTrack, Resources.Field_Track));

        // pravidelny cas ma pevnu poziciu za kolajami - chybajucu tratovu kolaj pred nim treba doplnit medzerami
        if (CarriesLineTrack(message.Type) && (message.LineTrack != null || message.RegularTime != null))
            builder.Append(Fixed(message.LineTrack ?? "", LenTrack, Resources.Field_LineTrack));

        if (message.RegularTime is { } regularTime)
            builder.Append(regularTime.ToString(TimeFormat, CultureInfo.InvariantCulture));

        return builder.ToString();
    }

    /// <summary>
    ///     Zostaví správu aj s ukončením, teda tak, ako sa zapisuje na socket.
    /// </summary>
    /// <param name="message">Správa.</param>
    /// <returns>Text správy ukončený <see cref="Terminator" />.</returns>
    public static string EncodeLine(GtnMessage message) => Encode(message) + Terminator;

    /// <summary>
    ///     Zostaví viac správ do jedného zápisu na socket.
    /// </summary>
    /// <param name="messages">Správy.</param>
    /// <returns>Text všetkých správ, každá ukončená <see cref="Terminator" />.</returns>
    public static string EncodeBatch(IEnumerable<GtnMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var builder = new StringBuilder();
        foreach (var message in messages)
            builder.Append(EncodeLine(message));

        return builder.ToString();
    }

    /// <summary>
    ///     Rozdelí prijatý blok textu na jednotlivé správy rovnako, ako to robí INISS - podľa znakovej množiny
    ///     CR/LF, pričom prázdne úseky preskakuje.
    /// </summary>
    /// <param name="raw">Prijatý text.</param>
    /// <returns>Jednotlivé správy.</returns>
    public static IEnumerable<string> Split(string raw) => string.IsNullOrEmpty(raw)
        ? Array.Empty<string>()
        : raw.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    ///     Rozoberie jednu správu rovnakým spôsobom, akým ju číta INISS. Slúži na overenie enkodéra a na
    ///     rozbor zachytených správ zo súboru <c>GTN.LOG</c>.
    /// </summary>
    /// <param name="text">Text jednej správy (bez ukončenia).</param>
    /// <param name="reference">Čas, voči ktorému sa dopočíta rok - INISS na to používa aktuálny čas.</param>
    /// <param name="message">Rozobratá správa, ak sa rozbor podaril.</param>
    /// <param name="error">Chyba v takom znení, v akom ju do <c>GTN.LOG</c> zapíše INISS.</param>
    /// <returns><see langword="true" />, ak sa správu podarilo rozobrať.</returns>
    public static bool TryDecode(string text, DateTime reference, [NotNullWhen(true)] out GtnMessage? message,
        [NotNullWhen(false)] out string? error)
    {
        message = null;
        error = null;

        ArgumentNullException.ThrowIfNull(text);

        var type = GtnMessageType.FromCode(Field(text, PosType, LenType));
        if (type is null)
        {
            error = "unknown message type";
            return false;
        }

        if (!TryParseTime(Field(text, PosTime, LenTime), reference, out var time, out error))
            return false;

        message = new GtnMessage
        {
            Type = type,
            StationField = Field(text, PosStation, LenStation),
            TrainNumber = Field(text, PosTrain, LenTrain).Trim(),
            Time = time
        };

        if (type == GtnMessageType.DelayReason)
        {
            message.DelayReason = Field(text, PosDelayReason, LenDelayReason).Trim();
            return true;
        }

        if (type == GtnMessageType.Transfer)
        {
            message.TransferKind = Field(text, PosTransferKind, LenTransferKind) == "1"
                ? GtnTransferKind.Express
                : GtnTransferKind.Normal;
            return true;
        }

        var posTrack = type == GtnMessageType.Route ? PosRouteKind + LenRouteKind : PosRouteKind;
        message.Track = Field(text, posTrack, LenTrack).Trim();

        if (type == GtnMessageType.Route)
            message.RouteKind = ParseRouteKind(Field(text, PosRouteKind, LenRouteKind));

        var posRegularTime = posTrack + LenTrack;
        if (CarriesLineTrack(type))
        {
            var lineTrack = Field(text, posRegularTime, LenTrack).Trim();
            message.LineTrack = lineTrack.Length == 0 ? null : lineTrack;
            posRegularTime += LenTrack;
        }

        var regularField = Field(text, posRegularTime, LenRegularTime);
        if (IsMissingTime(regularField))
            return true;

        if (!TryParseTime(regularField, reference, out var regularTime, out error))
        {
            message = null;
            return false;
        }

        message.RegularTime = regularTime;
        return true;
    }

    /// <summary>
    ///     Rozoberie jednu správu, pričom rok dopočíta voči aktuálnemu času.
    /// </summary>
    /// <param name="text">Text jednej správy (bez ukončenia).</param>
    /// <param name="message">Rozobratá správa, ak sa rozbor podaril.</param>
    /// <param name="error">Chyba v takom znení, v akom ju do <c>GTN.LOG</c> zapíše INISS.</param>
    /// <returns><see langword="true" />, ak sa správu podarilo rozobrať.</returns>
    public static bool TryDecode(string text, [NotNullWhen(true)] out GtnMessage? message,
        [NotNullWhen(false)] out string? error) =>
        TryDecode(text, DateTime.Now, out message, out error);

    /// <summary>
    ///     Dopočíta rok k mesiacu zo správy rovnako, ako to robí INISS: mesiac správy má ležať v okne od šiestich
    ///     mesiacov dozadu po päť mesiacov dopredu. Pri rozdiele mesiacov (správa mínus teraz) 6 a viac je to
    ///     minulý rok, pri -7 a menej budúci rok, inak tento rok - napríklad v decembri januárová správa patrí
    ///     do budúceho roka a v januári decembrová do minulého.
    /// </summary>
    /// <param name="month">Mesiac zo správy.</param>
    /// <param name="reference">Čas, voči ktorému sa rok dopočítava.</param>
    /// <returns>Rok, ktorý INISS dosadí.</returns>
    public static int ResolveYear(int month, DateTime reference)
    {
        var difference = month - reference.Month;

        if (difference > MonthsAhead)
            return reference.Year - 1;

        return difference < -MonthsBack ? reference.Year + 1 : reference.Year;
    }

    /// <summary>
    ///     Zistí, či INISS prečíta zo správy ten istý rok, aký do nej vložil simulátor. Vracia
    ///     <see langword="false" /> pre udalosti, ktorých mesiac leží mimo okna <see cref="MonthsBack" /> mesiacov
    ///     dozadu až <see cref="MonthsAhead" /> mesiacov dopredu od aktuálneho mesiaca.
    /// </summary>
    /// <param name="time">Čas udalosti.</param>
    /// <param name="reference">Čas, voči ktorému INISS rok dopočítava.</param>
    /// <returns><see langword="true" />, ak sa rok zachová.</returns>
    public static bool PreservesYear(DateTime time, DateTime reference) =>
        ResolveYear(time.Month, reference) == time.Year;

    /// <summary>
    ///     Odkrojí pole z pevnej pozície rovnako, ako to robí INISS - pole siahajúce za koniec reťazca sa
    ///     skráti, pole za koncom reťazca je prázdne.
    /// </summary>
    /// <param name="text">Text správy.</param>
    /// <param name="position">Pozícia poľa.</param>
    /// <param name="length">Dĺžka poľa.</param>
    /// <returns>Obsah poľa.</returns>
    public static string Field(string text, int position, int length)
    {
        if (text is null || position >= text.Length)
            return "";

        return position + length > text.Length ? text.Substring(position) : text.Substring(position, length);
    }

    /// <summary>
    ///     Zapíše poradové číslo dôvodu meškania do podoby, v akej ide na drôt.
    /// </summary>
    /// <param name="reason">Poradové číslo dôvodu v skupine <c>dz</c> zvukovej banky, číslované od jednotky.
    ///     Nula dôvod zmaže.</param>
    /// <returns>Dvojznakové pole dôvodu.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Ak sa číslo nezmestí do dvoch znakov.</exception>
    public static string FormatDelayReason(int reason)
    {
        if (reason is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(reason), Resources.Error_ReasonRangeZero);

        return reason.PadZeros(LenDelayReason);
    }

    private static string Fixed(string value, int length, string fieldName)
    {
        value ??= "";
        if (value.Length > length)
            throw new ArgumentException(Texts.F(Resources.Error_FieldTooLong, fieldName, value.Length, length));

        return value.PadRight(length);
    }

    /// <remarks>
    ///     INISS berie pravidelný čas ako chýbajúci, keď pole obsahuje len medzery a nuly v ľubovoľnom zložení
    ///     (vrátane prázdneho alebo skráteného poľa). Inak ho rozoberá rovnako ako čas v hlavičke a chyba správu
    ///     zastaví.
    /// </remarks>
    private static bool IsMissingTime(string field) => field.All(c => c is ' ' or '0');

    private static bool CarriesLineTrack(GtnMessageType? type) =>
        type == GtnMessageType.Departure || type == GtnMessageType.Route;

    private static GtnRouteKind ParseRouteKind(string field) => field switch
    {
        "1" => GtnRouteKind.Exit,
        "2" => GtnRouteKind.TrainNumberSet,
        _ => GtnRouteKind.Entry
    };

    /// <remarks>
    ///     Mieri na správanie <c>INISSb.EXE</c>: pole musí mať presne desať znakov a päť dvojíc musí byť
    ///     číselných, inak ide o <c>unrecognized date/time format</c>. Hodnoty sa ďalej nekontrolujú, iba sa
    ///     poskladajú do štruktúry <c>tm</c> a nechajú normalizovať funkciou <c>mktime</c> - mesiac 29 teda
    ///     nie je chyba, ale posun o dva roky a štyri mesiace. Chybou je až to, keď výsledok <c>mktime</c>
    ///     nedokáže zapísať, čo INISS zaloguje ako <c>invalid date</c>.
    /// </remarks>
    private static bool TryParseTime(string field, DateTime reference, out DateTime time,
        [NotNullWhen(false)] out string? error)
    {
        time = DateTime.MinValue;
        error = null;

        if (field is null || field.Length != LenTime)
        {
            error = "unrecognized date/time format";
            return false;
        }

        var parts = new int[LenTime / 2];
        for (var i = 0; i < parts.Length; i++)
            if (!int.TryParse(field.AsSpan(i * 2, 2), NumberStyles.AllowLeadingWhite, CultureInfo.InvariantCulture,
                    out parts[i]))
            {
                error = "unrecognized date/time format";
                return false;
            }

        var month = parts[0];
        var year = ResolveYear(month, reference);

        try
        {
            time = new DateTime(year, 1, 1)
                .AddMonths(month - 1)
                .AddDays(parts[1] - 1)
                .AddHours(parts[2])
                .AddMinutes(parts[3])
                .AddSeconds(parts[4]);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "invalid date";
            return false;
        }

        if (time >= MinTime)
            return true;

        error = "invalid date";
        return false;
    }
}
