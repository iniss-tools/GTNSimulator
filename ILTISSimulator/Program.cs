using ILTISSimulator.Properties;
using ILTISSimulator.Tools;
using ILTISSimulator.XML;
using ToolsCore;
using Utils = ToolsCore.Tools.Utils;

namespace ILTISSimulator;

internal static class Program
{
    /// <summary>
    ///     Vstupný bod aplikácie - nastavenia programu a argumenty príkazového riadka sa načítajú tu a hlavné okno
    ///     ich dostane v konštruktore.
    /// </summary>
    /// <param name="args">Argumenty príkazového riadka - viď <see cref="CommandLine" />.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        var session = AppInit.Initialization<IltisConfig, IltisStyle>();
        var commandLine = CommandLine.Parse(args);

        if (commandLine.Help)
        {
            Utils.ShowInfo(Resources.Args_Help);
            return;
        }

        AppInit.Run(() => new FMain(session, commandLine));
    }
}
