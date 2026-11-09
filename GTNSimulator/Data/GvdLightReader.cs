using System.Text.RegularExpressions;
using GVDEditor.Domain.Entities;
using GTNSimulator.Properties;
using GTNSimulator.Tools;
using ToolsCore.Iniss.Tools;
using static ToolsCore.Iniss.Tools.ParseUtils;
using static ToolsCore.Iniss.Tools.PathUtils;

namespace GTNSimulator.Data;

/// <summary>
///     Načítanie grafikonu v rozsahu, ktorý potrebuje simulátor GTN.
/// </summary>
/// <remarks>
///     Plní tie isté entity ako GVDEditor (<see cref="Train" />, <see cref="Track" />, <see cref="GVDInfo" />),
///     ale nepotrebuje zvukovú banku ani tabule, takže sa zaobíde bez globálnych dát GVDEditora. Číta iba
///     <c>GRAFIKON.txt</c>, <c>TRTYPES.txt</c>, <c>POZICE_A.txt</c>, <c>EXPORT3A.txt</c>, <c>EXPORT3B.txt</c>
///     a <c>POZICE.txt</c>.
/// </remarks>
public static class GvdLightReader
{
    private const string FILE_GRAFIKON = "GRAFIKON.txt";
    private const string FILE_TRTYPES = "TRTYPES.txt";
    private const string FILE_POZICE_A = "POZICE_A.txt";
    private const string FILE_POZICE = "POZICE.txt";
    private const string FileExport3A = "EXPORT3A.txt";
    private const string FileExport3B = "EXPORT3B.txt";

    /// <summary>
    ///     Načíta grafikon z priečinka.
    /// </summary>
    /// <param name="gvdDir">Priečinok s grafikonom (napr. <c>...\DATA\Novem.2008</c>).</param>
    /// <returns>Načítaný grafikon.</returns>
    /// <exception cref="DirectoryNotFoundException">Ak priečinok neexistuje.</exception>
    /// <exception cref="FormatException">Ak je niektorý zo súborov poškodený.</exception>
    public static GvdData Read(string gvdDir)
    {
        if (string.IsNullOrEmpty(gvdDir))
            throw new ArgumentNullException(nameof(gvdDir));
        if (!Directory.Exists(gvdDir))
            throw new DirectoryNotFoundException(Texts.F(Resources.Error_GvdMissing, gvdDir));

        var info = ReadInfo(gvdDir);
        var trainTypes = ReadTrainTypes(gvdDir);
        var tracks = ReadTracks(gvdDir);
        var trains = ReadTrains(gvdDir, trainTypes);
        var trainDays = ReadTrainDays(gvdDir, trains);

        ReadTrainTracks(gvdDir, trains, tracks);

        return new GvdData(info, trainTypes, tracks, trains, trainDays);
    }

    /// <summary>
    ///     Načíta informácie o grafikone zo súboru <c>GRAFIKON.txt</c>.
    /// </summary>
    /// <param name="gvdDir">Priečinok s grafikonom.</param>
    /// <returns>Informácie o grafikone.</returns>
    public static GVDInfo ReadInfo(string gvdDir)
    {
        var config = new TxtProps(CombinePath(gvdDir, FILE_GRAFIKON)!);

        var id = int.Parse(config.Get("IDSTATION"), CultureInfo.InvariantCulture);
        var name = Unquote(config.Get("NAMESTATION", id.ToString(CultureInfo.InvariantCulture)))!;

        return new GVDInfo
        {
            Category = int.Parse(config.Get("CATEGORY", "1"), CultureInfo.InvariantCulture),
            Subcat = int.Parse(config.Get("SUBCAT", "0"), CultureInfo.InvariantCulture),
            ThisStation = new Station(id.ToString(CultureInfo.InvariantCulture), name),
            TrainCount = int.Parse(config.Get("TRAINCOUNT", "0"), CultureInfo.InvariantCulture),
            StartValidTimeTable = ParseDateOnlyAlts(config.Get("START_VALID_TIMETABLE", "")),
            EndValidTimeTable = ParseDateOnlyAlts(config.Get("END_VALID_TIMETABLE", "")),
            StartValidData = ParseDateOnlyAlts(config.Get("START_VALID_DATA", "")),
            EndValidData = ParseDateOnlyAlts(config.Get("END_VALID_DATA", "")),
            CreateData = ParseDateOnlyAlts(config.Get("CREATE_DATA", ""))
        };
    }

    private static List<TrainType> ReadTrainTypes(string gvdDir)
    {
        var file = CombinePath(gvdDir, FILE_TRTYPES);
        if (!File.Exists(file))
        {
            // TRTYPES.txt býva aj o úroveň vyššie, v spoločnom priečinku DATA.
            var parent = Path.GetDirectoryName(Path.GetFullPath(gvdDir));
            file = parent is null ? file : CombinePath(parent, FILE_TRTYPES);
        }

        var types = new List<TrainType>();
        if (!File.Exists(file))
            return types;

        ForEachRow(file, (row, line) =>
        {
            var key = row[0];

            if (Regex.IsMatch(key, "^X[1-9]$") || Regex.IsMatch(key, "^R[1-9]$") ||
                Regex.IsMatch(key, "^Os[1-9]$") || Regex.IsMatch(key, "^Sl[1-9]$"))
            {
                types.Add(new TrainType(key, row[1], row[2]));
                return;
            }

            var type = new TrainType(key)
            {
                Key = row.Count > 1 ? row[1] : key,
                TextInTable = row.Count > 2 ? row[2] : key
            };

            types.Add(type);
        });

        return types;
    }

    private static List<Track> ReadTracks(string gvdDir)
    {
        var tracks = new List<Track> { Track.None };

        ForEachRow(CombinePath(gvdDir, FILE_POZICE_A)!, (row, line) =>
        {
            if (row[0] == Track.None.Key)
                return;

            tracks.Add(new Track
            {
                Key = row[0],
                Name = row[1],
                FullName = row[2],
                TrackName = row[4],
                SoundName = row[6],
                Platform = new Platform(row[5], row[3], row[7])
            });
        });

        return tracks;
    }

    private static List<Train> ReadTrains(string gvdDir, List<TrainType> trainTypes)
    {
        var trains = new List<Train>();

        ForEachRow(CombinePath(gvdDir, FileExport3A)!, (row, line) =>
        {
            var routing = Routing.Parse(row[5]);
            if (routing is null)
                throw new FormatException(Texts.F(Resources.Error_UnknownRouting, row[5]));

            var flags = row.ElementAtOrDefaultStr(6);

            var train = new Train
            {
                ID = int.Parse(row[0], CultureInfo.InvariantCulture),
                Number = row[1],
                Name = row[2],
                Type = GetOrAddTrainType(trainTypes, row[3]),
                Variant = int.Parse(row[4], CultureInfo.InvariantCulture),
                Routing = routing,
                IsMedzistatny = flags.Contains('M'),
                IsMiestenkovy = flags.Contains('R'),
                IsMimoriadny = flags.Contains('X'),
                IsDialkovy = flags.Contains('D'),
                IsIbaLozkovy = flags.Contains('L'),
                IsNizkopodlazny = flags.Contains('N'),
                LineArrival = row.ElementAtOrDefaultStr(13),
                LineDeparture = row.ElementAtOrDefaultStr(14)
            };

            if (routing != Routing.Vychadzajuci)
            {
                train.Arrival = ParseTimeOrNull(row.ElementAtOrDefaultStr(7));
                train.StartingStation = GetStation(row.ElementAtOrDefaultStr(9));
            }

            if (routing != Routing.Konciaci)
            {
                train.Departure = ParseTimeOrNull(row.ElementAtOrDefaultStr(8));
                train.EndingStation = GetStation(row.ElementAtOrDefaultStr(10));
            }

            trains.Add(train);
        });

        trains.Sort((first, second) => first.ID.CompareTo(second.ID));

        return trains;
    }

    private static Dictionary<int, BitArray> ReadTrainDays(string gvdDir, List<Train> trains)
    {
        var days = new Dictionary<int, BitArray>();

        ForEachRow(CombinePath(gvdDir, FileExport3B)!, (row, line) =>
        {
            var id = int.Parse(row[0], CultureInfo.InvariantCulture);

            var train = trains.FirstOrDefault(candidate => candidate.ID == id);
            if (train is null)
                throw new FormatException(Texts.F(Resources.Error_UnknownTrainId, id, FileExport3A));

            train.ZaciatokPlatnosti = ParseDateOnlyAlts(row[1]);
            train.KoniecPlatnosti = ParseDateOnlyAlts(row[2]);

            days[id] = StringToBitArray(row.ElementAtOrDefaultStr(3));
        });

        return days;
    }

    private static void ReadTrainTracks(string gvdDir, List<Train> trains, List<Track> tracks)
    {
        var file = CombinePath(gvdDir, FILE_POZICE);
        if (!File.Exists(file))
            return;

        ForEachRow(file, (row, line) =>
        {
            var id = int.Parse(row[0], CultureInfo.InvariantCulture);

            var train = trains.FirstOrDefault(candidate => candidate.ID == id);
            if (train is null)
                throw new FormatException(Texts.F(Resources.Error_UnknownTrainId, id, FileExport3A));

            train.Track = Track.GetFromID(tracks, row[1]) ?? Track.None;
        });
    }

    private static TrainType GetOrAddTrainType(List<TrainType> trainTypes, string key)
    {
        var type = trainTypes.FirstOrDefault(candidate => candidate.Key == key);
        if (type is not null)
            return type;

        // Simulátor typ vlaku nikde nepotrebuje, preto kvôli nemu odmietať celý grafikon nemá zmysel.
        type = new TrainType(key);
        trainTypes.Add(type);

        return type;
    }

    private static TimeOnly? ParseTimeOrNull(string text) =>
        string.IsNullOrEmpty(text) ? null : TimeOnly.FromDateTime(ParseTime(text));

    private static Station GetStation(string id) =>
        string.IsNullOrEmpty(id) || id == "-1" ? Station.None : new Station(id, id);

    private static string? Unquote(string? text) =>
        text is not null && text.Length > 1 && text.StartsWith('"') && text.EndsWith('"')
            ? text.Substring(1, text.Length - 2)
            : text;

    private static void ForEachRow(string file, Action<CsvRow, int> action)
    {
        using var reader = new CsvFileReader(file);

        var line = 1;
        var row = new CsvRow();

        while (true)
        {
            var status = reader.ReadRow(row);

            if (LineIsEmpty(status))
            {
                line++;
                continue;
            }

            if (LineIsEof(status))
                break;

            try
            {
                action(row, line);
            }
            catch (Exception e)
            {
                throw new FormatException(
                    Texts.F(Resources.Error_FileLine, Path.GetFileName(file), line) + " " + e.Message, e);
            }

            line++;
        }
    }
}
