using GTNSimulator.Data;
using GTNSimulator.Properties;

namespace GTNSimulator.Tools;

/// <summary>
///     Argumenty príkazového riadka: priečinok grafikonu a súbor scenára bez prepínača, ďalej
///     <c>--scenario</c>, <c>--day</c>, <c>--from</c>, <c>--speed</c>, <c>--seed</c>, <c>--host</c>, <c>--port</c>
///     s hodnotou (<c>--day 2026-10-20</c> alebo <c>--day=2026-10-20</c>) a <c>--connect</c>, <c>--plan</c>,
///     <c>--start</c>, <c>--now</c>, <c>--help</c>. Hodnoty z príkazového riadka platia do zatvorenia programu,
///     nastavenia sa nimi nemenia. Chybný argument sa vynechá a ohlási v <see cref="Errors" />.
/// </summary>
public sealed class CommandLine
{
    private static readonly string[] DayFormats = ["yyyy-MM-dd", "d.M.yyyy"];
    private static readonly string[] TimeFormats = ["H:mm", "H:mm:ss"];

    private readonly List<string> _errors = [];

    private CommandLine()
    {
    }

    /// <summary>
    ///     Bez argumentov.
    /// </summary>
    public static CommandLine Empty { get; } = new();

    /// <summary>
    ///     Priečinok grafikonu.
    /// </summary>
    public string? Gvd { get; private set; }

    /// <summary>
    ///     Súbor scenára dňa, ktorý sa otvorí po grafikone.
    /// </summary>
    public string? Scenario { get; private set; }

    /// <summary>
    ///     Deň simulácie.
    /// </summary>
    public DateOnly? Day { get; private set; }

    /// <summary>
    ///     Čas, od ktorého sa simuluje.
    /// </summary>
    public TimeOnly? From { get; private set; }

    /// <summary>
    ///     Zrýchlenie času.
    /// </summary>
    public int? Speed { get; private set; }

    /// <summary>
    ///     Seed generátora náhody namiesto seedu z nastavení.
    /// </summary>
    public int? Seed { get; private set; }

    /// <summary>
    ///     Adresa INISSu namiesto adresy z nastavení.
    /// </summary>
    public string? Host { get; private set; }

    /// <summary>
    ///     Port INISSu namiesto portu z nastavení.
    /// </summary>
    public int? Port { get; private set; }

    /// <summary>
    ///     Po štarte sa pripojiť k INISSu.
    /// </summary>
    public bool Connect { get; private set; }

    /// <summary>
    ///     Po štarte naplánovať deň.
    /// </summary>
    public bool Plan { get; private set; }

    /// <summary>
    ///     Po štarte spustiť simuláciu, ak treba, aj s naplánovaním dňa.
    /// </summary>
    public bool Start { get; private set; }

    /// <summary>
    ///     Po štarte spustiť dnešný deň od aktuálneho času v reálnom čase.
    /// </summary>
    public bool Now { get; private set; }

    /// <summary>
    ///     Zobraziť pomoc a skončiť.
    /// </summary>
    public bool Help { get; private set; }

    /// <summary>
    ///     Chybné argumenty s vysvetlením.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    ///     Rozloží argumenty programu.
    /// </summary>
    /// <param name="args">Argumenty bez názvu programu.</param>
    /// <returns>Rozložené argumenty.</returns>
    public static CommandLine Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var result = new CommandLine();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            if (arg is "/?" or "-?" or "-h")
            {
                result.Help = true;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                result.SetPath(arg);
                continue;
            }

            var separator = arg.IndexOf('=', StringComparison.Ordinal);
            var name = (separator < 0 ? arg : arg[..separator]).ToLowerInvariant();
            var inline = separator < 0 ? null : arg[(separator + 1)..];

            if (result.SetFlag(name))
            {
                if (inline is not null)
                    result.Error(Resources.Args_NoValue, name);
                continue;
            }

            if (!IsValueOption(name))
            {
                result.Error(Resources.Args_Unknown, arg);
                continue;
            }

            var value = inline;
            if (value is null)
            {
                if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    result.Error(Resources.Args_MissingValue, name);
                    continue;
                }

                value = args[++i];
            }

            result.SetValue(name, value);
        }

        if (result.Now && (result.Scenario is not null || result.Day.HasValue || result.From.HasValue))
        {
            result.Now = false;
            result.Error(Resources.Args_NowConflict, "--now");
        }

        return result;
    }

    private static bool IsValueOption(string name) =>
        name is "--scenario" or "--day" or "--from" or "--speed" or "--seed" or "--host" or "--port";

    private bool SetFlag(string name)
    {
        switch (name)
        {
            case "--connect":
                Connect = true;
                return true;
            case "--plan":
                Plan = true;
                return true;
            case "--start":
                Start = true;
                return true;
            case "--now":
                Now = true;
                return true;
            case "--help":
                Help = true;
                return true;
            default:
                return false;
        }
    }

    private void SetValue(string name, string value)
    {
        switch (name)
        {
            case "--scenario":
                Scenario = value;
                break;
            case "--day" when DateOnly.TryParseExact(value, DayFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day):
                Day = day;
                break;
            case "--from" when TimeOnly.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from):
                From = from;
                break;
            case "--speed" when TryNumber(value, 1, 600, out var speed):
                Speed = speed;
                break;
            case "--seed" when TryNumber(value, 1, 999999, out var seed):
                Seed = seed;
                break;
            case "--host" when !string.IsNullOrWhiteSpace(value):
                Host = value.Trim();
                break;
            case "--port" when TryNumber(value, 1, 65535, out var port):
                Port = port;
                break;
            default:
                Error(Resources.Args_BadValue, name, value);
                break;
        }
    }

    private void SetPath(string path)
    {
        if (path.EndsWith(ScenarioFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            if (Scenario is null)
                Scenario = path;
            else
                Error(Resources.Args_TwoPaths, path);
        }
        else if (Gvd is null)
        {
            Gvd = path;
        }
        else
        {
            Error(Resources.Args_TwoPaths, path);
        }
    }

    private static bool TryNumber(string value, int min, int max, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number >= min && number <= max;

    private void Error(string format, params object[] args) => _errors.Add(Texts.F(format, args));
}
