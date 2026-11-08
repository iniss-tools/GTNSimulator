using ToolsCore.Iniss.Tools;

namespace ILTISSimulator.Monitor;

/// <summary>
///     Priebežné čítanie logu rozhrania INISSu - pri každom volaní <see cref="Poll" /> vráti len riadky, ktoré
///     medzitým pribudli. INISS log pri prekročení veľkosti odloží (<c>GTN.BAK</c>) a začne nový; kratší súbor
///     sa preto číta znova od začiatku. Server INISS2 začína každý deň nový súbor.
/// </summary>
public sealed class GtnLogTail
{
    private readonly Decoder _decoder = Encodings.Win1250.GetDecoder();
    private readonly StringBuilder _pending = new();
    private readonly Func<string> _currentPath;
    private long _position;

    /// <summary>
    ///     Začne sledovať log. Riadky, ktoré v ňom už sú, sa preskočia.
    /// </summary>
    /// <param name="path">Cesta k logu.</param>
    public GtnLogTail(string path) : this(ConstantPath(path))
    {
    }

    /// <summary>
    ///     Začne sledovať log, ktorého súbor sa mení. Riadky, ktoré v aktuálnom súbore už sú, sa preskočia, nový súbor
    ///     sa číta od začiatku.
    /// </summary>
    /// <param name="currentPath">Vráti cestu k aktuálnemu súboru.</param>
    public GtnLogTail(Func<string> currentPath)
    {
        _currentPath = currentPath ?? throw new ArgumentNullException(nameof(currentPath));
        Path = currentPath();
        _position = Length();
    }

    /// <summary>
    ///     Cesta k logu.
    /// </summary>
    public string Path { get; private set; }

    /// <summary>
    ///     Či log existuje.
    /// </summary>
    public bool Exists => File.Exists(Path);

    /// <summary>
    ///     Prečíta riadky, ktoré pribudli od posledného volania. Neúplný posledný riadok počká na ďalšie volanie.
    /// </summary>
    /// <returns>Rozobrané záznamy o správach; iné riadky sa preskočia.</returns>
    public IReadOnlyList<GtnLogEntry> Poll()
    {
        var path = _currentPath();
        if (path != Path)
        {
            Path = path;
            Restart();
        }

        var length = Length();
        if (length < _position)
        {
            // novy log po odlozeni stareho
            Restart();
        }

        if (length == _position)
            return [];

        byte[] bytes;
        try
        {
            using var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            stream.Seek(_position, SeekOrigin.Begin);
            bytes = new byte[length - _position];
            stream.ReadExactly(bytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        _position = length;

        var chars = new char[_decoder.GetCharCount(bytes, 0, bytes.Length)];
        _decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
        _pending.Append(chars);

        var text = _pending.ToString();
        var last = text.LastIndexOf('\n');
        if (last < 0)
            return [];

        _pending.Clear();
        _pending.Append(text, last + 1, text.Length - last - 1);

        var entries = new List<GtnLogEntry>();
        foreach (var line in text[..last].Split('\n'))
            if (GtnLogEntry.TryParse(line.TrimEnd('\r'), out var entry))
                entries.Add(entry);

        return entries;
    }

    private static Func<string> ConstantPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return () => path;
    }

    private void Restart()
    {
        _position = 0;
        _pending.Clear();
        _decoder.Reset();
    }

    private long Length()
    {
        try
        {
            var info = new FileInfo(Path);
            return info.Exists ? info.Length : 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    /// <summary>
    ///     Nájde log rozhrania v inštalácii INISSu, ku ktorej patrí grafikon (<c>&lt;INISS&gt;\DATA\stanica.rok</c>).
    ///     Prehľadá priečinok programu aj jeho podpriečinky (<c>Logy</c>, <c>DATA</c>) a tieň vo VirtualStore,
    ///     kam zapisuje INISS spustený bez práv správcu z <c>Program Files</c>. Z viacerých vyberie naposledy zapísaný.
    /// </summary>
    /// <param name="gvdDir">Priečinok grafikonu.</param>
    /// <returns>Cesta k logu, alebo <see langword="null" />, ak sa nenašiel.</returns>
    public static string? Find(string gvdDir)
    {
        ArgumentNullException.ThrowIfNull(gvdDir);

        var install = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(gvdDir)));
        if (install is null || !Directory.Exists(install))
            return null;

        var roots = new List<string> { install };
        var root = System.IO.Path.GetPathRoot(install);
        if (root is not null)
            roots.Add(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VirtualStore", install[root.Length..]));

        var options = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true };

        return roots
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "GTN.LOG", options))
            .Select(file => new FileInfo(file))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }
}
