using System.Net.Sockets;
using ILTISSimulator.Gtn;
using ILTISSimulator.Properties;
using Serilog;

namespace ILTISSimulator.Net;

/// <summary>
///     Spojenie s INISSom. Drží jedno TCP spojenie otvorené a pri výpadku sa raz pokúsi znovu pripojiť
///     a správu odoslať.
/// </summary>
/// <remarks>
///     INISS počúva na porte z <c>HKLM\SOFTWARE\CHAPS\INISS\GTN\RecvPort</c>. Každá správa musí končiť
///     CR/LF, o čo sa stará <see cref="GtnEncoder.EncodeLine" />.
/// </remarks>
public sealed class GtnClient : IDisposable
{
    private readonly object _sync = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <summary>
    ///     Vytvorí klienta. Spojenie sa nadviaže až pri <see cref="Connect" /> alebo prvom odoslaní.
    /// </summary>
    /// <param name="host">Adresa INISSu.</param>
    /// <param name="port">Port, na ktorom INISS počúva.</param>
    public GtnClient(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentNullException(nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), Resources.Error_Port);

        Host = host;
        Port = port;
    }

    /// <summary>
    ///     Adresa INISSu.
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Port, na ktorom INISS počúva.
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Či je spojenie nadviazané.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            lock (_sync)
                return _client is { Connected: true };
        }
    }

    /// <summary>
    ///     Nadviaže spojenie. Ak už spojenie beží, zavrie sa a nadviaže znova.
    /// </summary>
    public void Connect()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            Reconnect();
        }
    }

    /// <summary>
    ///     Odošle jednu správu.
    /// </summary>
    /// <param name="message">Správa.</param>
    public void Send(GtnMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Write(GtnEncoder.EncodeLine(message));
    }

    /// <summary>
    ///     Odošle viac správ jedným zápisom. INISS ich rozdelí podľa CR/LF.
    /// </summary>
    /// <param name="messages">Správy.</param>
    public void Send(IEnumerable<GtnMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var payload = GtnEncoder.EncodeBatch(messages);
        if (payload.Length != 0)
            Write(payload);
    }

    /// <summary>
    ///     Odošle správu tak, ako je zadaná, bez akejkoľvek kontroly. Slúži na ručné pokusy - INISS na
    ///     nezmysel odpovie chybou v <c>GTN.LOG</c>.
    /// </summary>
    /// <param name="text">Text správy. Ak nekončí CR ani LF, ukončenie sa doplní.</param>
    public void SendRaw(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var payload = text.EndsWith('\r') || text.EndsWith('\n') ? text : text + GtnEncoder.Terminator;
        Write(payload);
    }

    /// <summary>
    ///     Zavrie spojenie.
    /// </summary>
    public void Close()
    {
        lock (_sync)
            Drop();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            Drop();
            _disposed = true;
        }
    }

    private void Write(string payload)
    {
        var bytes = Encoding.ASCII.GetBytes(payload);

        lock (_sync)
        {
            ThrowIfDisposed();

            try
            {
                EnsureConnected();
                _stream!.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Jeden pokus o obnovenie spojenia - INISS pri reštarte spojenie zahodí.
                Log.Warning(e, "Spojenie s INISS {Host}:{Port} prerusene pri zapise - pripaja sa znova", Host, Port);
                Reconnect();
                _stream!.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
        }
    }

    private void EnsureConnected()
    {
        if (_client is { Connected: true } && _stream is not null)
            return;

        Reconnect();
    }

    private void Reconnect()
    {
        Drop();

        _client = new TcpClient(Host, Port);
        _stream = _client.GetStream();
        Log.Information("Pripojene k INISS {Host}:{Port}", Host, Port);
    }

    private void Drop()
    {
        try
        {
            _stream?.Dispose();
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            // Spojenie sa zatvára, na chybe pri zatváraní nezáleží.
        }

        _stream = null;

        _client?.Close();
        _client = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
