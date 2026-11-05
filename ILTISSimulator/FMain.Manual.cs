using ILTISSimulator.Gtn;
using ILTISSimulator.Properties;
using ILTISSimulator.Simulation;
using ILTISSimulator.Tools;

namespace ILTISSimulator;

internal sealed partial class FMain
{
    private bool _manualUpdating;
    private string _manualGenerated = "";

    private void InitManual()
    {
        _manualUpdating = true;
        dtpManualTime.Value = DateTime.Now;
        dtpManualTime.Enabled = !cbManualNow.Checked;
        dtpManualRegular.Value = DateTime.Now;
        dtpManualRegular.Enabled = cbManualRegular.Checked;
        FillManualChoices();
        FillManualLineTracks();
        _manualUpdating = false;

        UpdateManualFields();
        UpdateManualPreview();
    }

    /// <summary>
    ///     Názov typu správy aj s jej kódom, napríklad <c>0802 - Príchod vlaku</c>.
    /// </summary>
    private static string Of(GtnMessageType type)
    {
        var name = type.Key switch
        {
            "0802" => Resources.MessageType_Arrival,
            "0803" => Resources.MessageType_Departure,
            "0804" => Resources.MessageType_Route,
            "0805" => Resources.MessageType_DelayReason,
            "0806" => Resources.MessageType_Transfer,
            _ => type.Name
        };

        return $"{type.Key} - {name}";
    }

    private static string Of(GtnRouteKind kind) => kind switch
    {
        GtnRouteKind.Exit => Resources.RouteKind_Exit,
        GtnRouteKind.TrainNumberSet => Resources.RouteKind_TrainNumberSet,
        _ => Resources.RouteKind_Entry
    };

    private static string Of(GtnTransferKind kind) =>
        kind == GtnTransferKind.Express ? Resources.TransferKind_Express : Resources.TransferKind_Normal;

    private void FillManualChoices()
    {
        Refill(cbManualType, ManualTypes(), Of);
        Refill(cbManualRouteKind, Enum.GetValues<GtnRouteKind>(), Of);
        Refill(cbManualTransferKind, Enum.GetValues<GtnTransferKind>(), Of);

        var reasons = new List<int> { 0 };
        reasons.AddRange(UsableReasons().Select(reason => reason.Number));
        Refill(cbManualDelayReason, reasons, number => number == 0 ? Resources.Manual_ReasonClear : ReasonText(number));
    }

    /// <summary>
    ///     Typy správ na výber. Server INISS2 prijíma len správy o pohybe vlaku.
    /// </summary>
    private IEnumerable<GtnMessageType> ManualTypes() => _gvd is { IsIniss2: true }
        ? GtnMessageType.GetValues().Where(type => type != GtnMessageType.DelayReason && type != GtnMessageType.Transfer)
        : GtnMessageType.GetValues();

    /// <summary>
    ///     Naplní zoznam možnosťami a pokúsi sa zachovať doterajší výber.
    /// </summary>
    private static void Refill<T>(ComboBox box, IEnumerable<T> values, Func<T, string> text)
    {
        var selected = box.SelectedItem is Choice<T> choice ? choice.Value : default;

        box.Items.Clear();
        foreach (var value in values)
            box.Items.Add(new Choice<T>(value, text(value)));

        if (box.Items.Count == 0)
            return;

        for (var i = 0; i < box.Items.Count; i++)
            if (box.Items[i] is Choice<T> item && Equals(item.Value, selected))
            {
                box.SelectedIndex = i;
                return;
            }

        box.SelectedIndex = 0;
    }

    private static T SelectedValue<T>(ComboBox box) => box.SelectedItem is Choice<T> choice ? choice.Value : default!;

    /// <summary>
    ///     Doplní do ručnej správy hodnoty z načítaného grafikonu - stanicu, čísla vlakov a koľaje.
    /// </summary>
    private void FillManualFromGvd()
    {
        if (_gvd is null)
            return;

        _manualUpdating = true;

        tbManualStation.Text = StationField;

        var trains = _gvd.Trains.Select(train => train.Number).Distinct()
            .OrderBy(number => number, StringComparer.OrdinalIgnoreCase).ToArray();
        cbManualTrain.Items.Clear();
        cbManualTrain.Items.AddRange(trains);
        if (cbManualTrain.Text.Length == 0 && trains.Length != 0)
            cbManualTrain.Text = trains[0];

        var tracks = UsableTracks().Select(track => track.Key).ToArray();
        cbManualTrack.Items.Clear();
        cbManualTrack.Items.Add("");
        cbManualTrack.Items.AddRange(tracks);

        if (cbManualTrack.Text.Length == 0 && tracks.Length != 0)
            cbManualTrack.Text = tracks[0];

        FillManualChoices();

        _manualUpdating = false;

        // bez pravidelneho casu server INISS2 spravu zahodi
        if (_gvd.IsIniss2)
            cbManualRegular.Checked = true;

        UpdateManualFields();
        UpdateManualPreview();
    }

    /// <summary>
    ///     Ponuka traťovej koľaje ručnej správy - prázdna (pole sa neposiela) a traťová koľaj z nastavení simulácie.
    ///     Staničné koľaje sa neponúkajú, traťová koľaj patrí trati.
    /// </summary>
    private void FillManualLineTracks()
    {
        var text = cbManualLineTrack.Text;

        cbManualLineTrack.Items.Clear();
        cbManualLineTrack.Items.Add("");
        if (Config.Simulation.LineTrack.Trim() is { Length: > 0 } lineTrack)
            cbManualLineTrack.Items.Add(lineTrack);

        cbManualLineTrack.Text = text;
    }

    /// <summary>
    ///     Predvyplní ručnú správu vlakom vybraným v tabuľke.
    /// </summary>
    private void FillManualFromSelection()
    {
        if (!pManual.Visible || SelectedRuns() is not [var run, ..])
            return;

        _manualUpdating = true;
        cbManualTrain.Text = run.Train.Number;
        if (run.Track?.Key is { Length: > 0 } track)
            cbManualTrack.Text = track;
        PrefillManualRegularTime();
        _manualUpdating = false;

        UpdateManualPreview();
    }

    /// <summary>
    ///     Pri zapnutom „teraz“ obnoví čas a text správy - len kým text nie je ručne prepísaný.
    /// </summary>
    private void RefreshManualTime()
    {
        if (!pManual.Visible || !cbManualNow.Checked || tbManual.Text.Trim() != _manualGenerated.Trim())
            return;

        _manualUpdating = true;
        dtpManualTime.Value = ManualNow(SelectedValue<GtnMessageType>(cbManualType));
        _manualUpdating = false;

        UpdateManualPreview();
    }

    private void Manual_Changed(object? sender, EventArgs e)
    {
        if (_manualUpdating)
            return;

        if (sender == cbManualType || sender == cbManualRouteKind)
        {
            UpdateManualFields();

            _manualUpdating = true;
            PrefillManualRegularTime();
            _manualUpdating = false;
        }

        UpdateManualPreview();
    }

    private void cbManualRegular_CheckedChanged(object? sender, EventArgs e)
    {
        dtpManualRegular.Enabled = cbManualRegular.Checked;

        if (cbManualRegular.Checked)
        {
            _manualUpdating = true;
            PrefillManualRegularTime();
            _manualUpdating = false;
        }

        Manual_Changed(sender, e);
    }

    /// <summary>
    ///     Doplní pravidelný čas z plánu vlaku vybraného v tabuľke - príchod pri 0802, odchod pri 0803 a 0804.
    ///     Server INISS2 páruje vjazdovú cestu podľa príchodu a číslo vlaku berie podľa smeru.
    /// </summary>
    private void PrefillManualRegularTime()
    {
        if (SelectedRuns() is not [var run, ..])
            return;

        var type = SelectedValue<GtnMessageType>(cbManualType);
        var arriving = type == GtnMessageType.Arrival ||
                       (run.Iniss2 && type == GtnMessageType.Route && SelectedValue<GtnRouteKind>(cbManualRouteKind) == GtnRouteKind.Entry);

        var planned = arriving
            ? run.PlannedArrival ?? run.PlannedDeparture
            : run.PlannedDeparture ?? run.PlannedArrival;

        if (planned is { } time)
            dtpManualRegular.Value = time;

        if (run.Iniss2)
            cbManualTrain.Text = arriving ? run.ArrivalNumber : run.DepartureNumber;
    }

    private void cbManualNow_CheckedChanged(object? sender, EventArgs e)
    {
        dtpManualTime.Enabled = !cbManualNow.Checked;

        if (cbManualNow.Checked)
            dtpManualTime.Value = ManualNow(SelectedValue<GtnMessageType>(cbManualType));

        Manual_Changed(sender, e);
    }

    /// <summary>
    ///     Čas „teraz“ pre ručnú správu - virtuálny čas naplánovaného dňa, aby správa sedela s grafikonom
    ///     simulovaného dňa. Hlásenie o prestupe INISS porovnáva so svojimi hodinami, preto ide so skutočným časom.
    /// </summary>
    private DateTime ManualNow(GtnMessageType? type) =>
        type == GtnMessageType.Transfer || _engine?.Timeline is null ? DateTime.Now : _engine.VirtualTime;

    /// <summary>
    ///     Zobrazí iba tie polia, ktoré vybraný typ správy nesie.
    /// </summary>
    private void UpdateManualFields()
    {
        var type = SelectedValue<GtnMessageType>(cbManualType);

        var trainMovement = type == GtnMessageType.Arrival || type == GtnMessageType.Departure || type == GtnMessageType.Route;

        fieldTrack.Visible = trainMovement;
        fieldLineTrack.Visible = type == GtnMessageType.Departure || type == GtnMessageType.Route;
        fieldRegularTime.Visible = trainMovement;
        fieldRouteKind.Visible = type == GtnMessageType.Route;
        fieldDelayReason.Visible = type == GtnMessageType.DelayReason;
        fieldTransferKind.Visible = type == GtnMessageType.Transfer;
    }

    /// <summary>
    ///     Zloží správu z polí ručnej správy.
    /// </summary>
    /// <returns>Správa alebo <see langword="null" />, ak nie je vybraný typ.</returns>
    private GtnMessage? BuildManualMessage()
    {
        var type = SelectedValue<GtnMessageType>(cbManualType);
        if (type is null)
            return null;

        var message = new GtnMessage
        {
            Type = type,
            StationField = tbManualStation.Text.Trim(),
            TrainNumber = cbManualTrain.Text.Trim(),
            Time = cbManualNow.Checked ? ManualNow(type) : dtpManualTime.Value
        };

        if (type == GtnMessageType.DelayReason)
        {
            message.DelayReason = GtnEncoder.FormatDelayReason(SelectedValue<int>(cbManualDelayReason));
            return message;
        }

        if (type == GtnMessageType.Transfer)
        {
            message.TransferKind = SelectedValue<GtnTransferKind>(cbManualTransferKind);
            return message;
        }

        message.Track = cbManualTrack.Text.Trim();
        message.RegularTime = cbManualRegular.Checked ? dtpManualRegular.Value : null;

        if (type == GtnMessageType.Route)
            message.RouteKind = SelectedValue<GtnRouteKind>(cbManualRouteKind);

        if (type == GtnMessageType.Departure || type == GtnMessageType.Route)
        {
            var lineTrack = cbManualLineTrack.Text.Trim();
            message.LineTrack = lineTrack.Length == 0 ? null : lineTrack;
        }

        return message;
    }

    private void UpdateManualPreview()
    {
        var message = BuildManualMessage();
        if (message is null)
            return;

        try
        {
            _manualGenerated = GtnEncoder.Encode(message);
        }
        catch (ArgumentException)
        {
            // niektore pole je dlhsie, nez sa don zmesti - text sa necha tak, ako bol
            return;
        }

        tbManual.Text = _manualGenerated;
    }

    private void bManualSend_Click(object? sender, EventArgs e)
    {
        // pri zapnutom "teraz" sa cas prepocita tesne pred odoslanim, aby ho INISS nezahodil ako stary;
        // ak si pouzivatel text prepisal, posle sa presne to, co napisal
        if (cbManualNow.Checked && tbManual.Text.Trim() == _manualGenerated.Trim())
            UpdateManualPreview();

        var text = tbManual.Text.Trim();
        if (text.Length == 0)
            return;

        if (GtnEncoder.TryDecode(text, out var decoded, out var error))
            AppendLog(LogKind.Info, Texts.F(Resources.Log_ManualDecoded, decoded));
        else
            AppendLog(LogKind.Warning, Texts.F(Resources.Log_ManualRejected, error));

        SendRaw(text);
    }

    private sealed class Choice<T>(T value, string text)
    {
        public T Value { get; } = value;

        public override string ToString() => text;
    }
}
