using System.Net;
using System.Net.Sockets;
using GTNSimulator.Gtn;
using ToolsCore.Iniss.Tools;

namespace GTNSimulator.DocScreenshots;

/// <summary>
///     Falošný INISS pre snímky: prijíma správy GTN cez TCP a zapisuje ich do <c>GTN.LOG</c> tak, ako to robí
///     INISS - s dopočítaným meškaním a koľajou, neznámu koľaj odmietne.
/// </summary>
internal sealed class FakeIniss : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly string _logPath;
    private readonly Func<string, GtnMessageType, DateTime?> _planned;
    private readonly HashSet<string> _tracks;
    private readonly object _sync = new();
    private readonly CancellationTokenSource _stop = new();

    /// <param name="logPath">Cesta k logu rozhrania.</param>
    /// <param name="planned">Čas z grafikonu podľa čísla vlaku a typu správy (príchod pri 0802, odchod pri 0803).</param>
    /// <param name="tracks">Kľúče koľají stanice - iná koľaj je chyba.</param>
    public FakeIniss(string logPath, Func<string, GtnMessageType, DateTime?> planned, IEnumerable<string> tracks)
    {
        _logPath = logPath;
        _planned = planned;
        _tracks = new HashSet<string>(tracks, StringComparer.Ordinal);
        _listener.Start();
        _ = Task.Run(AcceptAsync);
    }

    /// <summary>
    ///     Port, na ktorom falošný INISS počúva.
    /// </summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    ///     Počet zapísaných riadkov logu.
    /// </summary>
    public int Lines { get; private set; }

    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => ReadAsync(client));
        }
    }

    private async Task ReadAsync(TcpClient client)
    {
        using (client)
        using (var reader = new StreamReader(client.GetStream(), Encodings.Win1250))
        {
            try
            {
                while (await reader.ReadLineAsync(_stop.Token) is { } line)
                    if (line.Length > 0)
                        Write(line);
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
                // spojenie zavrete
            }
        }
    }

    private void Write(string line)
    {
        var result = Process(line);

        lock (_sync)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)}: \"{line}\" - {result}\r\n",
                Encodings.Win1250);
            Lines++;
        }
    }

    private string Process(string line)
    {
        if (!GtnEncoder.TryDecode(line, out var message, out _) || message?.Type is null)
            return "unknown message type";

        var track = message.Track?.Trim() ?? "";
        if (message.Type == GtnMessageType.DelayReason)
            return "OK,reason " + message.DelayReason;
        if (message.Type == GtnMessageType.Transfer)
            return "OK, " + (message.TransferKind == GtnTransferKind.Express ? "Urýchlený prestup" : "Prestup");
        if (track.Length > 0 && !_tracks.Contains(track))
            return "unknown track " + track;

        if (message.Type == GtnMessageType.Route)
            return "OK, track " + track;

        var planned = _planned(message.TrainNumber.Trim(), message.Type);
        if (planned is null)
            return $"train {message.TrainNumber.Trim()} not found";

        var delay = (int)Math.Round((message.Time - planned.Value).TotalMinutes);
        return string.Create(CultureInfo.InvariantCulture, $"OK, delay {delay} min., delay {delay} min., track {track}");
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}
