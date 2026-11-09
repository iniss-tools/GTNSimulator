namespace GTNSimulator.Simulation;

/// <summary>
///     Stav udalosti simulácie.
/// </summary>
public enum SimEventState
{
    /// <summary>
    ///     Čaká na svoj čas.
    /// </summary>
    Pending,

    /// <summary>
    ///     Vydaná - odoslaná INISSu, alebo len zapísaná do záznamu, ak sa do INISSu neposiela.
    /// </summary>
    Emitted,

    /// <summary>
    ///     Preskočená - simulácia začala neskôr alebo jej čas preskočil skok vpred.
    /// </summary>
    Skipped
}
