namespace GTNSimulator.Gtn;

/// <summary>
///     Druh hlásenia o prestupe - jednoznakový príznak správy 080-6.
/// </summary>
public enum GtnTransferKind
{
    /// <summary>
    ///     Bežný prestup - to isté hlásenie, ktoré operátor spustí klávesou F8. INISS ho zaloguje ako
    ///     <c>Přestup</c>. Platí pre ľubovoľný znak okrem jednotky.
    /// </summary>
    Normal = 0,

    /// <summary>
    ///     Urýchlený prestup - hlásenie spúšťané klávesou Shift+F8. INISS ho zaloguje ako
    ///     <c>Urychlený přestup</c>.
    /// </summary>
    Express = 1
}
