using System.Reflection;
using System.Xml.Linq;
using ExControls;
using GTNSimulator.XML;
using ToolsCore;
using ToolsCore.Commands;
using ToolsCore.XML;

namespace GTNSimulatorTests;

/// <summary>
///     Ukotviteľné panely hlavného okna bez zobrazenia: plán s koľajovým diagramom v dokumente, záznam a ručná správa
///     dole, obnovenie rozloženia z nastavení a návrat k predvolenému.
/// </summary>
[TestClass]
public sealed class DockingFormTests
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    // konstruktor okna config.xml nezapisuje (nastavenia sa ukladaju az po zmene obsluhou), zatvorenie by ho zapisalo -
    // okna sa preto len zahodia
    private static Form MainWindow(string? layout = null)
    {
        var styles = Styles<GTNSimulatorStyle>.CreateDefault();
        var session = new AppSession<GTNSimulatorConfig, GTNSimulatorStyle>(new GTNSimulatorConfig { DockLayout = layout }, styles,
            styles.StyleList[0]);
        // inicializator vlastnosti obide setter - tema okien cita styl z GlobSettings
        session.UsingStyle = styles.StyleList[0];
        // okno je v programe interne
        var type = typeof(GTNSimulatorConfig).Assembly.GetType("GTNSimulator.FMain", true)!;
        var form = (Form)Activator.CreateInstance(type, session)!;
        form.CreateControl();
        return form;
    }

    private static T Field<T>(Control control, string name) => (T)control.GetType().GetField(name, Any)!.GetValue(control)!;

    private static ExDockManager Dock(Form form) => Field<ExDockManager>(form, "_dock");

    private static ExDockPanel Panel(Form form, string key) => Dock(form).Panels.Single(panel => panel.Key == key);

    [STATestMethod]
    public void HlavneOkno_PlanVDokumenteZaznamARucnaSpravaDole()
    {
        using var form = MainWindow();

        CollectionAssert.AreEqual(new[] { "Log", "ManualMessage" }, Dock(form).Panels.Select(panel => panel.Key).ToArray());
        Assert.AreSame(Dock(form).Document, Field<SplitContainer>(form, "splitPlan").Parent);
        Assert.AreSame(Panel(form, "Log"), Field<RichTextBox>(form, "rtbLog").Parent);
        Assert.AreSame(Panel(form, "ManualMessage"), Field<Panel>(form, "pManual").Parent);
        Assert.AreEqual((ExDockSide.Bottom, ExDockSide.Bottom), (Panel(form, "Log").Side, Panel(form, "ManualMessage").Side));
        Assert.IsTrue(Panel(form, "Log").IsShown);
        Assert.IsFalse(Panel(form, "ManualMessage").IsShown, "ručná správa je predvolene skrytá ako doteraz");
        Assert.IsTrue(Field<SplitContainer>(form, "splitMain").IsDisposed, "deliaci list návrhu sa zahodí");
    }

    [STATestMethod]
    public void RucnaSprava_JePodZaznamomNieVedlaNeho()
    {
        using var form = MainWindow();

        // skupiny panelov v ulozenom rozlozeni: riadky jedneho rozdelenia, zaznam prvy
        var layout = XDocument.Parse(Dock(form).SaveLayout());
        XElement Group(string key) => layout.Descendants("panel").Single(panel => (string?)panel.Attribute("key") == key).Parent!;
        var split = Group("Log").Parent!;
        Assert.AreSame(split, Group("ManualMessage").Parent, layout.ToString());
        Assert.AreEqual("split", split.Name.LocalName);
        Assert.AreEqual("rows", (string?)split.Attribute("arrange"));
        Assert.IsLessThan(split.Elements().ToList().IndexOf(Group("ManualMessage")), split.Elements().ToList().IndexOf(Group("Log")));
    }

    [STATestMethod]
    public void Rozlozenie_ZobrazenaRucnaSpravaSaObnoviAPrikazVratiPredvolene()
    {
        string layout;
        using (var first = MainWindow())
        {
            Dock(first).ShowPanel(Panel(first, "ManualMessage"), false);
            Dock(first).HidePanel(Panel(first, "Log"));
            layout = Dock(first).SaveLayout();
        }

        using var form = MainWindow(layout);
        Assert.IsTrue(Panel(form, "ManualMessage").IsShown);
        Assert.IsFalse(Panel(form, "Log").IsShown);

        var commands = Field<CommandSet>(form, "_commands");
        commands["ShowLog"].Execute();
        Assert.IsTrue(Panel(form, "Log").IsShown);

        commands["ResetLayout"].Execute();
        Assert.IsTrue(Panel(form, "Log").IsShown);
        Assert.IsFalse(Panel(form, "ManualMessage").IsShown);

        commands["ManualMessage"].Execute();
        Assert.IsTrue(Panel(form, "ManualMessage").IsShown);
    }

    [STATestMethod]
    public void Rozlozenie_NeplatneOstavaPredvolene()
    {
        using var form = MainWindow("<nie je rozlozenie />");

        Assert.IsTrue(Panel(form, "Log").IsShown);
        Assert.IsFalse(Panel(form, "ManualMessage").IsShown);
    }
}
