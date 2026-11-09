namespace GTNSimulator.Simulation;

/// <summary>
///     Pravdepodobnostný model meškania. Väčšina vlakov ide načas, zvyšok dostane meškanie z exponenciálneho
///     rozdelenia zaokrúhlené na celé minúty.
/// </summary>
public sealed class DelayModel
{
    private readonly SimulationOptions _options;
    private readonly Random _random;

    /// <summary>
    ///     Vytvorí model nad zadanými nastaveniami a generátorom náhody.
    /// </summary>
    /// <param name="options">Nastavenia simulácie.</param>
    /// <param name="random">Generátor náhody. Zdieľa sa s ostatnými časťami simulácie, aby celý deň závisel
    ///     len od jedného seedu.</param>
    public DelayModel(SimulationOptions options, Random random)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    ///     Vylosuje meškanie vlaku pri príchode do stanice.
    /// </summary>
    /// <returns>Meškanie zaokrúhlené na celé minúty, prípadne nula.</returns>
    public TimeSpan NextArrivalDelay()
    {
        if (_random.NextDouble() < _options.OnTimeProbability)
            return TimeSpan.Zero;

        // Exponenciálne rozdelenie posunuté o minútu, aby "meškajúci" vlak nedostal nulu.
        var minutes = 1.0 + -_options.MeanDelayMinutes * Math.Log(1.0 - _random.NextDouble());

        return Clamp(TimeSpan.FromMinutes(Math.Round(minutes)));
    }

    /// <summary>
    ///     Odvodí meškanie na odchode z meškania na príchode. Vlak môže časť meškania stiahnuť skrátením
    ///     pobytu (najviac na jednu minútu, viď kapitola 7.2 návodu k INISSu), alebo mu meškanie ešte narastie.
    /// </summary>
    /// <param name="arrivalDelay">Meškanie na príchode.</param>
    /// <param name="plannedStay">Pravidelný pobyt v stanici podľa grafikonu.</param>
    /// <returns>Meškanie na odchode.</returns>
    public TimeSpan NextDepartureDelay(TimeSpan arrivalDelay, TimeSpan plannedStay)
    {
        var delay = arrivalDelay;

        var recoverable = plannedStay - TimeSpan.FromMinutes(1);
        if (delay > TimeSpan.Zero && recoverable > TimeSpan.Zero &&
            _random.NextDouble() < _options.DelayRecoveryProbability)
        {
            var recovered = TimeSpan.FromMinutes(Math.Floor(recoverable.TotalMinutes));
            if (recovered > delay)
                recovered = delay;

            delay -= recovered;
        }

        if (_options.MaxDelayGrowthMinutes > 0 && _random.NextDouble() < _options.DelayGrowthProbability)
            delay += TimeSpan.FromMinutes(1 + _random.Next(_options.MaxDelayGrowthMinutes));

        return Clamp(delay);
    }

    private TimeSpan Clamp(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            return TimeSpan.Zero;

        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}
