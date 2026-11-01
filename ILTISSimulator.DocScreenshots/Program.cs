using System.Reflection;
using ILTISSimulator.XML;
using ToolsCore;
using ToolsCore.XML;

namespace ILTISSimulator.DocScreenshots;

/// <summary>
///     Generátor snímok okien ILTISSimulatora do dokumentácie.
/// </summary>
/// <remarks>
///     Použitie: <c>ILTISSimulator.DocScreenshots [--out=priečinok] [--work=priečinok] [--only=text] [--theme=light|dark|both]</c>.
///     <list type="bullet">
///         <item><c>--out</c> – kam uložiť PNG; predvolene <c>iniss-tools-docs\static\img\iltissimulator</c>.</item>
///         <item><c>--work</c> – kde zostaviť fiktívnu inštaláciu INISS; predvolene <c>C:\INISS</c> (cesta je vidno v okne).
///             Existujúci priečinok bez značky <c>.docshots</c> sa nezmaže.</item>
///         <item><c>--only</c> – len snímky, ktorých cesta obsahuje daný text.</item>
///         <item><c>--timeout</c> – po koľkých minútach sa harness ukončí, ak ho zablokuje modálne okno (predvolene 5).</item>
///         <item><c>--check</c> – navyše skontroluje úpravy v tabuľke (zápis do buniek) a výsledok vypíše.</item>
///     </list>
///     Program beží pod vlastným menom, takže konfiguráciu (<c>%LocalAppData%\ILTISSimulator.DocScreenshots</c>) aj
///     register má oddelené od ILTISSimulatora – pri každom spustení začína s predvolenými nastaveniami.
/// </remarks>
internal static class Program
{
    public const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static AppSession<IltisConfig, IltisStyle> Session { get; private set; } = null!;

    [STAThread]
    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

        var log = new List<string>();

        // poistka: modálne okno (chyba, otázka) by harness zablokovalo navždy
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("Časový limit vypršal – pravdepodobne visí modálne okno. Posledné kroky:");
            foreach (var line in log.TakeLast(5))
                Console.Error.WriteLine("  " + line);
            Console.Error.WriteLine("Otvorené okná: " + string.Join(" | ", WindowCapture.ProcessWindowTitles()));
            Environment.Exit(2);
        }, null, TimeSpan.FromMinutes(options.TimeoutMinutes), Timeout.InfiniteTimeSpan);

        try
        {
            InitApp();
            var gvdDir = Demo.Build(options.WorkDir, log);

            var shots = 0;
            foreach (var theme in options.Themes)
            {
                SetTheme(theme);
                shots += new Shots(options, theme, log).Run(gvdDir);
            }

            log.Add($"hotovo: {shots} snímok do {options.OutDir}");
            Flush(log);
            return 0;
        }
        catch (Exception e)
        {
            log.Add("CHYBA: " + (e is TargetInvocationException { InnerException: { } inner } ? inner : e));
            Flush(log);
            return 1;
        }
    }

    private static void Flush(List<string> log)
    {
        foreach (var line in log)
            Console.WriteLine(line);
    }

    /// <summary>
    ///     Rovnaká inicializácia ako ILTISSimulator.Program.Main, s čistou konfiguráciou a slovenčinou.
    /// </summary>
    private static void InitApp()
    {
        if (Directory.Exists(AppPaths.DataDir))
            Directory.Delete(AppPaths.DataDir, true);

        Session = AppInit.Initialization<IltisConfig, IltisStyle>();

        // harness nebezi v Application.Run - synchronizacny kontext WinForms treba nastavit sam
        WindowsFormsSynchronizationContext.AutoInstall = false;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        Control.CheckForIllegalCrossThreadCalls = true;

        var culture = CultureInfo.CreateSpecificCulture("sk");
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private static void SetTheme(string theme)
    {
        // zabudovana schema harnessu (svetla alebo tmava), zvolena aj na stranke Vzhlad
        var style = Session.Styles[theme == "dark" ? StyleNames.Dark : StyleNames.Light];
        Session.Styles.Active = style.Name;
        Session.Styles.FollowSystem = false;
        Session.UsingStyle = style;
        AppInit.MsgBoxStyleInit(style, Session.Config);
    }

    internal sealed record Options(string OutDir, string WorkDir, string? Only, string[] Themes, int TimeoutMinutes, bool Check)
    {
        public static Options Parse(string[] args)
        {
            string? Arg(string name) =>
                args.Where(a => a.StartsWith(name + "=", StringComparison.Ordinal)).Select(a => a[(name.Length + 1)..]).FirstOrDefault();

            var theme = Arg("--theme") ?? "both";
            return new Options(
                Arg("--out") ?? DefaultOutDir(),
                Arg("--work") ?? @"C:\INISS",
                Arg("--only"),
                theme == "both" ? ["light", "dark"] : [theme],
                int.Parse(Arg("--timeout") ?? "5", CultureInfo.InvariantCulture),
                args.Contains("--check"));
        }

        private static string DefaultOutDir()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                var docs = Path.Combine(dir.FullName, "iniss-tools-docs");
                if (Directory.Exists(docs))
                    return Path.Combine(docs, "static", "img", "iltissimulator");
            }

            throw new DirectoryNotFoundException("Nenašiel sa priečinok iniss-tools-docs – zadaj --out.");
        }
    }
}
