namespace GTNSimulator.Simulation;

/// <summary>
///     Druh udalosti, ktorú simulátor hlási INISSu.
/// </summary>
public enum SimEventKind
{
    /// <summary>
    ///     Postavená vjazdová vlaková cesta - správa 080-4 s príznakom 0.
    /// </summary>
    EntryRoute,

    /// <summary>
    ///     Dôvod meškania - správa 080-5. Posiela sa pred príchodom, aby ho INISS mal k dispozícii skôr, než
    ///     odbaví hlásenie o meškaní.
    /// </summary>
    DelayReason,

    /// <summary>
    ///     Príchod vlaku - správa 080-2. Z jej času si INISS odvodí meškanie na príchode.
    /// </summary>
    Arrival,

    /// <summary>
    ///     Postavená odchodová vlaková cesta - správa 080-4 s príznakom 1.
    /// </summary>
    ExitRoute,

    /// <summary>
    ///     Odchod vlaku - správa 080-3. Z jej času si INISS odvodí meškanie na odchode.
    /// </summary>
    Departure
}
