using GVDEditor.Domain.Entities;
using ILTISSimulator.Properties;
using ILTISSimulator.Tools;
using ToolsCore.Iniss2;
using ToolsCore.Iniss2.Grafikon;

namespace ILTISSimulator.Data;

/// <summary>
///     Načítanie stanice INISS2 do rovnakých údajov, aké simulátor dostane z grafikonu INISS 3.39.
/// </summary>
/// <remarks>
///     Grafikon INISS2 má spoločné vlaky pre všetky stanice, simulátor pracuje s jednou. Každé zastavenie varianty
///     v stanici je samostatný vlak s dňami, keď platí práve táto varianta. Vlak idúci cez polnoc má platnosť posunutú
///     na deň, keď ide stanicou, aby sa dal plánovať podľa dňa v stanici.
/// </remarks>
public static class Iniss2GvdReader
{
    /// <summary>
    ///     Zistí, či je priečinok grafikon INISS2 so spoločnými vlakmi.
    /// </summary>
    /// <param name="dir">Priečinok.</param>
    /// <returns><see langword="true" />, ak je to grafikon.</returns>
    public static bool IsGrafikonDir(string dir) => File.Exists(Path.Combine(dir, GvFile.FileName));

    /// <summary>
    ///     Zistí, či je priečinok stanica v grafikone INISS2.
    /// </summary>
    /// <param name="dir">Priečinok.</param>
    /// <returns><see langword="true" />, ak je to stanica.</returns>
    public static bool IsStationDir(string dir) =>
        File.Exists(Path.Combine(dir, PositionsDefFile.FileName)) && Path.GetDirectoryName(Path.GetFullPath(dir)) is { } group && IsGrafikonDir(group);

    /// <summary>
    ///     Načíta stanicu.
    /// </summary>
    /// <param name="stationDir">Priečinok stanice (<c>Server\Data\grafikon\stanica</c>).</param>
    /// <returns>Načítaný grafikon stanice.</returns>
    /// <exception cref="FormatException">Ak stanica nie je v zozname grafikonov.</exception>
    public static GvdData Read(string stationDir)
    {
        ArgumentException.ThrowIfNullOrEmpty(stationDir);

        var fullDir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(stationDir));
        var groupDir = Path.GetDirectoryName(fullDir)!;
        var dataDir = Path.GetDirectoryName(groupDir) ?? groupDir;
        var serverDir = Path.GetDirectoryName(dataDir) ?? dataDir;

        var groupName = Path.GetFileName(groupDir);
        var stationName = Path.GetFileName(fullDir);

        var group = DirListXml.Read(dataDir).FirstOrDefault(candidate => string.Equals(candidate.Name, groupName, StringComparison.OrdinalIgnoreCase));
        var item = group?.Items.FirstOrDefault(candidate => string.Equals(candidate.Directory, stationName, StringComparison.OrdinalIgnoreCase));
        if (group is null || item is null)
            throw new FormatException(Texts.F(Resources.Error_Iniss2UnknownStation, stationName));

        var timetable = StationTimetable.Read(dataDir, group, item);
        var tracks = ReadTracks(timetable);
        var types = new List<TrainType>();
        var trains = new List<Train>();
        var days = new Dictionary<int, BitArray>();
        var calls = new Dictionary<int, Iniss2TrainCall>();

        foreach (var call in timetable.Calls.Where(CanBeSimulated))
        {
            var offset = DayOffset(call);
            var train = ToTrain(trains.Count + 1, call, offset, types);

            trains.Add(train);
            days[train.ID] = call.Train.ActiveDaysOf(call.Variant);
            calls[train.ID] = new Iniss2TrainCall(call, offset);
        }

        var installation = new Iniss2Installation(serverDir);
        var source = new Iniss2Source(timetable, tracks, calls, Iniss2Config.Read(dataDir).GtnPort, installation.GtnLogFile);

        return new GvdData(ReadInfo(item, trains), types, tracks, trains, days, source);
    }

    // s variantou s priznakom D server spravy neparuje, vlak bez casu alebo cisla sa nema ako ohlasit
    private static bool CanBeSimulated(StationCall call) =>
        !call.Variant.Flags.HasFlag(GvVariantFlags.D) && call.Stop.HasTime && call.Number.Length > 0;

    private static GVDInfo ReadInfo(DirListItem item, List<Train> trains)
    {
        var from = trains.Count == 0 ? default : trains.Min(train => train.ZaciatokPlatnosti);
        var to = trains.Count == 0 ? default : trains.Max(train => train.KoniecPlatnosti);

        return new GVDInfo
        {
            ThisStation = new Station(item.Station.ToString(CultureInfo.InvariantCulture), item.Directory),
            TrainCount = trains.Count,
            StartValidTimeTable = from,
            EndValidTimeTable = to,
            StartValidData = from,
            EndValidData = to
        };
    }

    private static List<Track> ReadTracks(StationTimetable timetable)
    {
        var tracks = new List<Track> { Track.None };

        tracks.AddRange(timetable.Tracks.Where(track => track.IsRealTrack).Select(track => new Track
        {
            Key = track.Key,
            Name = track.Name,
            FullName = track.Name,
            TrackName = track.TrackText.Length > 0 ? track.TrackText : track.Name,
            SoundName = track.TrackSound,
            Platform = new Platform(track.PlatformText, track.PlatformText, track.PlatformSound)
        }));

        return tracks;
    }

    private static Train ToTrain(int id, StationCall call, int offset, List<TrainType> types)
    {
        var stops = call.Variant.Stops;

        return new Train
        {
            ID = id,
            Number = call.Number,
            Name = call.TrainName,
            Type = GetOrAddType(types, call.TrainType),
            Variant = IndexOf(call),
            Routing = call.IsFirst ? Routing.Vychadzajuci : call.IsLast ? Routing.Konciaci : Routing.Prechadzajuci,
            // vo vychodzej stanici server spravu o prichode ignoruje, v cielovej spravu o odchode
            Arrival = call.IsFirst ? null : ToTime(call.Stop.Arrival),
            Departure = call.IsLast ? null : ToTime(call.Stop.Departure),
            StartingStation = ToStation(stops[0]),
            EndingStation = ToStation(stops[^1]),
            Track = Track.None,
            DateLimitText = call.Variant.Validity,
            ZaciatokPlatnosti = call.Train.From.AddDays(offset),
            KoniecPlatnosti = call.Train.To.AddDays(offset)
        };
    }

    private static int IndexOf(StationCall call)
    {
        for (var i = 0; i < call.Train.Variants.Count; i++)
        {
            if (ReferenceEquals(call.Train.Variants[i], call.Variant))
                return i;
        }

        return 0;
    }

    private static int DayOffset(StationCall call) => call.DateAtStation(call.Train.From).DayNumber - call.Train.From.DayNumber;

    private static TimeOnly? ToTime(int? minutes) => minutes is { } value ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(value % (24 * 60))) : null;

    private static Station ToStation(GvStop stop)
    {
        var id = stop.Station.ToString(CultureInfo.InvariantCulture);

        return new Station(id, id);
    }

    private static TrainType GetOrAddType(List<TrainType> types, string key)
    {
        var type = types.FirstOrDefault(candidate => candidate.Key == key);
        if (type is not null)
            return type;

        // server spoznava autobus podla presneho druhu Bus, preto kategoria druhu ostava rovna klucu
        type = new TrainType(key);
        types.Add(type);

        return type;
    }
}
