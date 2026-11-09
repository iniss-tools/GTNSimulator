using GTNSimulator.Tools;

namespace GTNSimulator;

internal sealed partial class FMain
{
    /// <summary>
    ///     Najviac riadkov v zázname - staršie sa zahodia, aby celodenný beh nespomalil okno.
    /// </summary>
    private const int MaxLogLines = 5000;

    private enum LogKind
    {
        Info,
        Sent,
        Warning,
        Error,
        Connected
    }

    private void AppendLog(LogKind kind, string text)
    {
        var back = rtbLog.BackColor;
        var color = kind switch
        {
            LogKind.Error => Palette.Of(PaletteColor.Red, back),
            LogKind.Warning => Palette.Of(PaletteColor.Orange, back),
            LogKind.Connected => Palette.Of(PaletteColor.Green, back),
            LogKind.Sent => Palette.Of(PaletteColor.Blue, back),
            _ => rtbLog.ForeColor
        };

        if (rtbLog.GetLineFromCharIndex(rtbLog.TextLength) > MaxLogLines)
        {
            rtbLog.Select(0, rtbLog.GetFirstCharIndexFromLine(MaxLogLines / 5));
            rtbLog.ReadOnly = false;
            rtbLog.SelectedText = "";
            rtbLog.ReadOnly = true;
        }

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\r\n");
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }
}
