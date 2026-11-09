namespace GTNSimulator.Simulation;

/// <summary>
///     Prečo sa zmena jazdy nedá urobiť.
/// </summary>
public enum PlanEditError
{
    /// <summary>
    ///     Zmena prebehla.
    /// </summary>
    None,

    /// <summary>
    ///     Jazda v stanici už skončila - vlak odišiel, končiaci vlak prišiel.
    /// </summary>
    Finished,

    /// <summary>
    ///     Príchod už bol ohlásený.
    /// </summary>
    ArrivalSent,

    /// <summary>
    ///     Východzí vlak nemá príchod.
    /// </summary>
    NoArrival,

    /// <summary>
    ///     Končiaci vlak nemá odchod.
    /// </summary>
    NoDeparture,

    /// <summary>
    ///     Meškanie je mimo rozsahu, ktorý INISS prijme.
    /// </summary>
    DelayOutOfRange,

    /// <summary>
    ///     Vlak by odišiel skôr, než pobudne v stanici minútu po príchode.
    /// </summary>
    DepartureTooEarly,

    /// <summary>
    ///     Koľaj sa nedá ohlásiť.
    /// </summary>
    TrackUnusable,

    /// <summary>
    ///     Číslo dôvodu meškania je mimo rozsahu.
    /// </summary>
    ReasonOutOfRange,

    /// <summary>
    ///     Udalosť nečaká - už prebehla, alebo jazda takú nemá.
    /// </summary>
    NotPending,

    /// <summary>
    ///     Vlak už na koľaji stojí - pri výluke koľaje sa nepresúva.
    /// </summary>
    StandingOnTrack,

    /// <summary>
    ///     Počas obsadenia koľaje nie je voľná žiadna iná koľaj.
    /// </summary>
    NoFreeTrack
}
