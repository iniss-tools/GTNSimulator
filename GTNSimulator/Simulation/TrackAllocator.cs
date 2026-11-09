using GVDEditor.Domain.Entities;
using GTNSimulator.Gtn;

namespace GTNSimulator.Simulation;

/// <summary>
///     Prideľovanie staničných koľají. Každý vlak drží koľaj v intervale od postavenia vjazdovej cesty po
///     odchod, takže na jednej koľaji nikdy nestoja dva vlaky naraz.
/// </summary>
public sealed class TrackAllocator
{
    private readonly Dictionary<string, List<Reservation>> _reservations = new();
    private readonly List<Track> _tracks;

    /// <summary>
    ///     Vytvorí prideľovač nad koľajami stanice.
    /// </summary>
    /// <param name="tracks">Koľaje z grafikonu. Vynechajú sa tie, ktoré sa nedajú ohlásiť - viď
    ///     <see cref="CanUse" />.</param>
    /// <param name="excludedKeys">Kľúče koľají, ktoré simulátor nemá prideľovať, hoci ohlásiť by sa dali.</param>
    public TrackAllocator(IEnumerable<Track> tracks, IEnumerable<string>? excludedKeys = null)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        var excluded = new HashSet<string>(excludedKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        _tracks = tracks.Where(CanUse).Where(track => !excluded.Contains(track.Key)).ToList();

        foreach (var track in _tracks)
            _reservations[track.Key] = new List<Reservation>();
    }

    /// <summary>
    ///     Zistí, či sa koľaj dá vôbec ohlásiť. Nedefinovaná koľaj a koľaj s kľúčom dlhším, než je pole koľaje
    ///     v správe, sa do správy nezmestí - napríklad pomocná koľaj VYLUKA v grafikone Košíc.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <returns><see langword="true" />, ak sa koľaj dá poslať INISSu.</returns>
    public static bool CanUse(Track? track) =>
        track is not null && track != Track.None && !string.IsNullOrWhiteSpace(track.TrackName) &&
        track.Key.Length <= GtnEncoder.LenTrack;

    /// <summary>
    ///     Koľaje, na ktoré môže prideľovač vlak postaviť.
    /// </summary>
    public IReadOnlyList<Track> Tracks => _tracks;

    /// <summary>
    ///     Zistí, či koľaj spravuje tento prideľovač. Koľaj vylúčená používateľom spravovaná nie je, takže sa
    ///     ani neobsadzuje.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <returns><see langword="true" />, ak je koľaj v zozname <see cref="Tracks" />.</returns>
    public bool IsManaged(Track track) => track is not null && _reservations.ContainsKey(track.Key);

    /// <summary>
    ///     Zistí, či je koľaj v zadanom intervale voľná.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <param name="from">Začiatok intervalu.</param>
    /// <param name="to">Koniec intervalu.</param>
    /// <returns><see langword="true" />, ak sa interval neprekrýva so žiadnou rezerváciou.</returns>
    public bool IsFree(Track track, DateTime from, DateTime to)
    {
        if (track is null || !_reservations.TryGetValue(track.Key, out var taken))
            return false;

        return taken.All(reservation => reservation.To <= from || to <= reservation.From);
    }

    /// <summary>
    ///     Zapíše rezerváciu koľaje, ak je v zadanom intervale voľná.
    /// </summary>
    /// <param name="track">Koľaj.</param>
    /// <param name="from">Začiatok intervalu.</param>
    /// <param name="to">Koniec intervalu.</param>
    /// <returns><see langword="true" />, ak sa rezervácia podarila.</returns>
    public bool TryReserve(Track track, DateTime from, DateTime to)
    {
        if (!IsFree(track, from, to))
            return false;

        _reservations[track.Key].Add(new Reservation(from, to));
        return true;
    }

    /// <summary>
    ///     Vyberie a zarezervuje náhodnú voľnú koľaj.
    /// </summary>
    /// <param name="from">Začiatok intervalu.</param>
    /// <param name="to">Koniec intervalu.</param>
    /// <param name="random">Generátor náhody.</param>
    /// <param name="preferPlatformOf">Ak je zadaná, uprednostnia sa koľaje pri tom istom nástupišti.</param>
    /// <param name="except">Koľaj, ktorá sa vybrať nemá.</param>
    /// <returns>Zarezervovaná koľaj, alebo <see langword="null" />, ak žiadna voľná nie je.</returns>
    public Track? ReserveAny(DateTime from, DateTime to, Random random, Track? preferPlatformOf = null, Track? except = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        var free = _tracks
            .Where(track => (except is null || track.Key != except.Key) && IsFree(track, from, to))
            .ToList();

        if (free.Count == 0)
            return null;

        var platform = preferPlatformOf?.Platform?.Key;
        if (!string.IsNullOrEmpty(platform))
        {
            var samePlatform = free.Where(track => track.Platform?.Key == platform).ToList();
            if (samePlatform.Count > 0)
                free = samePlatform;
        }

        var chosen = free[random.Next(free.Count)];
        _reservations[chosen.Key].Add(new Reservation(from, to));

        return chosen;
    }

    /// <summary>
    ///     Zahodí všetky rezervácie.
    /// </summary>
    public void Reset()
    {
        foreach (var taken in _reservations.Values)
            taken.Clear();
    }

    private readonly struct Reservation
    {
        public Reservation(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }

        public DateTime From { get; }
        public DateTime To { get; }
    }
}
