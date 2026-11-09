using GTNSimulator.Gtn;
using GTNSimulator.Properties;
using GTNSimulator.Tools;

namespace GTNSimulator.Simulation;

/// <summary>
///     Nastavenia simulácie. Všetka náhoda ide cez jediný generátor založený na <see cref="Seed" />, takže
///     rovnaký seed a rovnaký grafikon dajú vždy rovnaký priebeh dňa.
/// </summary>
public sealed class SimulationOptions
{
    /// <summary>
    ///     Najdlhšie meškanie, ktoré má ešte zmysel posielať. INISS zahodí správu, ktorá je viac než 18 hodín
    ///     po plánovanom čase, preto je strop nižšie.
    /// </summary>
    public static readonly TimeSpan DelayCeiling = TimeSpan.FromHours(17);

    /// <summary>
    ///     Najdlhšie predstihnutie, ktoré INISS ešte prijme. Správa viac než 2 hodiny pred plánovaným časom
    ///     sa zahodí, preto musí byť predstih vlakovej cesty kratší.
    /// </summary>
    public static readonly TimeSpan LeadCeiling = TimeSpan.FromHours(2);

    /// <summary>
    ///     Seed generátora náhody.
    /// </summary>
    public int Seed { get; set; } = 1;

    /// <summary>
    ///     Koľkokrát rýchlejšie než v skutočnosti beží virtuálny čas.
    /// </summary>
    public double TimeFactor { get; set; } = 1.0;

    /// <summary>
    ///     Ako dlho pred príchodom sa postaví vjazdová vlaková cesta.
    /// </summary>
    public TimeSpan EntryRouteLead { get; set; } = TimeSpan.FromMinutes(4);

    /// <summary>
    ///     Ako dlho pred odchodom sa postaví odchodová vlaková cesta.
    /// </summary>
    public TimeSpan ExitRouteLead { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Pravdepodobnosť, že vlak príde načas.
    /// </summary>
    public double OnTimeProbability { get; set; } = 0.75;

    /// <summary>
    ///     Stredná hodnota meškania v minútach pre vlaky, ktoré načas neprídu.
    /// </summary>
    public double MeanDelayMinutes { get; set; } = 8.0;

    /// <summary>
    ///     Strop meškania.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(480);

    /// <summary>
    ///     Pravdepodobnosť, že meškanie počas pobytu v stanici ešte narastie.
    /// </summary>
    public double DelayGrowthProbability { get; set; } = 0.15;

    /// <summary>
    ///     Najväčší nárast meškania počas pobytu v minútach.
    /// </summary>
    public int MaxDelayGrowthMinutes { get; set; } = 6;

    /// <summary>
    ///     Pravdepodobnosť, že meškajúci vlak časť meškania stiahne skrátením pobytu. Podľa kapitoly 7.2
    ///     návodu k INISSu možno pobyt skrátiť najviac na jednu minútu.
    /// </summary>
    public double DelayRecoveryProbability { get; set; } = 0.4;

    /// <summary>
    ///     Pravdepodobnosť, že vlak pôjde na inú koľaj, než má v grafikone.
    /// </summary>
    public double TrackChangeProbability { get; set; } = 0.07;

    /// <summary>
    ///     Traťová koľaj, ktorú simulátor predvolene posiela v odchode.
    /// </summary>
    public const string DefaultLineTrack = "1";

    /// <summary>
    ///     Posielať v odchode 080-3 aj traťovú koľaj <see cref="LineTrack" />. Ako ILTIS - INISS potom nastaví
    ///     udalosť pre automatiku <c>Odj</c> a vlak v stanici ukončí (prepne ho do posledného stavu kategórie,
    ///     teda ho zo zoznamu zmaže). Bez traťovej koľaje odchod dosadí len koľaj a meškanie.
    /// </summary>
    public bool SendLineTrack { get; set; } = true;

    /// <summary>
    ///     Traťová koľaj odchodu - koľaj trate, na ktorú vlak zo stanice odchádza. INISS ju dosadí nasledujúcim
    ///     zastávkam trasy, vlaku v stanici zo správy nie.
    /// </summary>
    public string LineTrack { get; set; } = DefaultLineTrack;

    /// <summary>
    ///     Ako dlho po odchode zostáva koľaj obsadená.
    /// </summary>
    public TimeSpan TrackReleaseMargin { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Poradové čísla dôvodov meškania, z ktorých simulátor vyberá. Zoznam je vlastnosťou stanice, nie
    ///     grafikonu - načíta sa zo skupiny <c>dz</c> zvukovej banky cez
    ///     <see cref="Data.DelayReasonReader" />. Kým je prázdny, správy 080-5 sa neposielajú.
    /// </summary>
    public IList<int> DelayReasons { get; set; } = new List<int>();

    /// <summary>
    ///     Pravdepodobnosť, že dostatočne meškajúci vlak dostane aj dôvod meškania (správa 080-5).
    /// </summary>
    public double DelayReasonProbability { get; set; } = 0.6;

    /// <summary>
    ///     Od akého meškania má zmysel posielať dôvod.
    /// </summary>
    public TimeSpan MinDelayForReason { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Kľúče koľají, na ktoré simulátor nesmie postaviť vlak. Hodí sa na koľaje, ktoré sú v grafikone
    ///     vedené ako pozícia, ale vlak na ne v skutočnosti nechodí - napríklad koľaj BUS pre náhradnú
    ///     autobusovú dopravu. Vlaku, ktorý má vylúčenú koľaj priamo v grafikone, sa ponechá.
    /// </summary>
    public ISet<string> ExcludedTrackKeys { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Skontroluje, či sú nastavenia zmysluplné.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Ak je niektoré nastavenie mimo povoleného rozsahu.</exception>
    public void Validate()
    {
        CheckProbability(OnTimeProbability, nameof(OnTimeProbability));
        CheckProbability(DelayGrowthProbability, nameof(DelayGrowthProbability));
        CheckProbability(DelayRecoveryProbability, nameof(DelayRecoveryProbability));
        CheckProbability(TrackChangeProbability, nameof(TrackChangeProbability));
        CheckProbability(DelayReasonProbability, nameof(DelayReasonProbability));

        if (DelayReasons is null)
            throw new ArgumentNullException(nameof(DelayReasons), Resources.Error_ReasonsNull);

        if (DelayReasons.Any(reason => reason is < 1 or > 99))
            throw new ArgumentOutOfRangeException(nameof(DelayReasons), Resources.Error_ReasonRange);

        if (MinDelayForReason < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MinDelayForReason), Resources.Error_MinDelayForReason);

        if (TimeFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(TimeFactor), Resources.Error_TimeFactor);

        if (MeanDelayMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MeanDelayMinutes), Resources.Error_MeanDelay);

        if (MaxDelayGrowthMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxDelayGrowthMinutes), Resources.Error_DelayGrowth);

        if (MaxDelay <= TimeSpan.Zero || MaxDelay > DelayCeiling)
            throw new ArgumentOutOfRangeException(nameof(MaxDelay),
                Texts.F(Resources.Error_DelayCeiling, DelayCeiling.TotalHours));

        CheckLead(EntryRouteLead, nameof(EntryRouteLead));
        CheckLead(ExitRouteLead, nameof(ExitRouteLead));

        if (TrackReleaseMargin < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(TrackReleaseMargin), Resources.Error_TrackReleaseMargin);

        if (SendLineTrack && (string.IsNullOrWhiteSpace(LineTrack) || LineTrack.Trim().Length > GtnEncoder.LenTrack))
            throw new ArgumentOutOfRangeException(nameof(LineTrack), Texts.F(Resources.Error_LineTrack, GtnEncoder.LenTrack));
    }

    private static void CheckProbability(double value, string name)
    {
        if (value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(name, Resources.Error_Probability);
    }

    private static void CheckLead(TimeSpan value, string name)
    {
        if (value < TimeSpan.Zero || value >= LeadCeiling)
            throw new ArgumentOutOfRangeException(name,
                Texts.F(Resources.Error_LeadCeiling, LeadCeiling.TotalHours));
    }
}
