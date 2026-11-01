using System.Reflection;
using ToolsCore.Iniss.Entities;
using ToolsCore.Iniss.Tools;

namespace ILTISSimulator.DocScreenshots;

/// <summary>
///     Fiktívna inštalácia INISS pre snímky - stanica Dolné Mesto z harnessu GVDEditora (čísla staníc 99xxxxx,
///     mimo skutočných), doplnená o dôvody meškania v zvukovej banke a prázdny log rozhrania.
/// </summary>
internal static class Demo
{
    /// <summary>
    ///     Dôvody meškania v skupine <c>dz</c> - posledný INISS nevie vybrať, ako v skutočných bankách.
    /// </summary>
    private static readonly (string Key, string Text)[] Reasons =
    [
        ("Nehoda", "z dôvodu nehody na trati"),
        ("Pocasie", "vplyvom nepriaznivého počasia"),
        ("Prevadzka", "z prevádzkových dôvodov"),
        ("Technika", "z technických príčin"),
        ("Vyluka", "z dôvodu výlukových prác")
    ];

    /// <summary>
    ///     Zostaví inštaláciu do <paramref name="root" /> a vráti priečinok grafikonu.
    /// </summary>
    public static string Build(string root, List<string> log)
    {
        // DemoInstallation otvara inst. cez kontext editora z harnessu GVDEditora - staci predvolene nastavenie
        var harness = Assembly.Load("GVDEditor.DocScreenshots");
        var style = ToolsCore.XML.Styles<GVDEditor.Config.GVDEditorStyle>.CreateStyle(ToolsCore.XML.StyleNames.Light, ToolsCore.XML.SchemeBase.Light);
        var session = new ToolsCore.AppSession<GVDEditor.Config.GVDEditorConfig, GVDEditor.Config.GVDEditorStyle>(
            new GVDEditor.Config.GVDEditorConfig(), new ToolsCore.XML.Styles<GVDEditor.Config.GVDEditorStyle>(new List<GVDEditor.Config.GVDEditorStyle> { style }), style);
        harness.GetType("GVDEditor.DocScreenshots.Program", true)!.GetProperty("Context", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, Activator.CreateInstance(typeof(GVDEditor.Config.GVDEditorConfig).Assembly.GetType("GVDEditor.EditorContext", true)!, session));

        var installation = harness.GetType("GVDEditor.DocScreenshots.DemoInstallation", true)!;
        var gvdDir = (string)installation.GetMethod("Build", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [root, log])!;

        AddDelayReasons(Path.Combine(root, "RAWBANK"));

        var logs = Path.Combine(root, "Logy");
        Directory.CreateDirectory(logs);
        File.WriteAllText(Path.Combine(logs, "GTN.LOG"), "");

        log.Add("inštalácia: " + gvdDir);
        return gvdDir;
    }

    private static void AddDelayReasons(string bankDir)
    {
        var languages = RawBankParser.ReadFyzBankFile(bankDir, out _);
        var language = FyzLanguage.GetBasicLanguage(languages) ?? languages[0];

        RawBankParser.ReadFyzZvukFile(bankDir, language);
        var group = new FyzGroup(language, "dz", "dz", @"dz\");
        foreach (var (key, text) in Reasons)
            group.Sounds.Add(new FyzSound(group, key, key, key + ".WAV", "", text, 1500));

        language.Groups.Add(group);
        RawBankParser.WriteFyzZvukFile(bankDir, language);
    }
}
