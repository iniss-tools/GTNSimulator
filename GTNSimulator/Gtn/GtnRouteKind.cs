namespace GTNSimulator.Gtn;

/// <summary>
///     Druh vlakovej cesty - jednoznakový príznak správy 080-4.
/// </summary>
public enum GtnRouteKind
{
    /// <summary>
    ///     Vjazdová (príchodová) vlaková cesta. INISS nastavuje vlaku príznak 1 a koľaj dosadí vždy.
    /// </summary>
    Entry = 0,

    /// <summary>
    ///     Odchodová vlaková cesta. INISS nastavuje vlaku príznak 2.
    /// </summary>
    Exit = 1,

    /// <summary>
    ///     Zavedenie čísla vlaku (ZCV). INISS nastavuje vlaku príznak 4.
    /// </summary>
    /// <remarks>
    ///     Koľaj sa dosadzuje presne tak isto ako pri <see cref="Exit" /> - kód sa vetví iba na to, či je
    ///     príznak nula alebo nie. Jediný rozdiel je bit, ktorý sa vlaku nastaví. Na tieto bity čaká automatika
    ///     vlaku; stavový diagram ich požaduje kľúčom <c>Wait</c> a v Kalendári akcií vlaku im zodpovedá stĺpec
    ///     "Čakať" s položkami VVC, OVC, ZCV, Vj a Odj - v tomto poradí.
    ///     Príručka k ZCV dodáva, že pri nasadení s ILTIS sa nepoužíva a namiesto neho sa používa Vj, teda
    ///     správa 080-2. Príznak sa nastaví iba pre hodnoty 0, 1 a 2; iný znak INISS prijme, ale nenastaví
    ///     nič a koľaj dosadí ako pri <see cref="Exit" />.
    /// </remarks>
    TrainNumberSet = 2
}
