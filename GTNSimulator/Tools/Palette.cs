using ToolsCore.Icons;

namespace GTNSimulator.Tools;

/// <summary>
///     Farby stavov pre svetlé aj tmavé pozadie - rovnaké odtiene ako ikony banky <see cref="AppIcons" />.
/// </summary>
internal static class Palette
{
    private static readonly Dictionary<PaletteColor, (Color Light, Color Dark)> Colors = new()
    {
        [PaletteColor.Red] = (ColorTranslator.FromHtml("#C42B1C"), ColorTranslator.FromHtml("#F47C70")),
        [PaletteColor.Orange] = (ColorTranslator.FromHtml("#C25A0E"), ColorTranslator.FromHtml("#E08855")),
        [PaletteColor.Green] = (ColorTranslator.FromHtml("#1E7A35"), ColorTranslator.FromHtml("#6CC07A")),
        [PaletteColor.Blue] = (ColorTranslator.FromHtml("#2F65CA"), ColorTranslator.FromHtml("#7AA7F8")),
        [PaletteColor.Gold] = (ColorTranslator.FromHtml("#9A6C10"), ColorTranslator.FromHtml("#DCB67A"))
    };

    /// <summary>
    ///     Farba stavu čitateľná na danom pozadí.
    /// </summary>
    /// <param name="color">Stav.</param>
    /// <param name="background">Pozadie, na ktorom bude text.</param>
    /// <returns>Farba.</returns>
    public static Color Of(PaletteColor color, Color background)
    {
        var (light, dark) = Colors[color];
        return AppIcons.IsDarkBackground(background) ? dark : light;
    }

    /// <summary>
    ///     Stlmená farba textu - v polovici medzi textom a pozadím.
    /// </summary>
    /// <param name="fore">Farba textu.</param>
    /// <param name="background">Pozadie.</param>
    /// <returns>Farba.</returns>
    public static Color Dim(Color fore, Color background) =>
        Color.FromArgb((fore.R + background.R) / 2, (fore.G + background.G) / 2, (fore.B + background.B) / 2);
}
