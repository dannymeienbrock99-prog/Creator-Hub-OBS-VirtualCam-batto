using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CreatorHubLive.Services;

public sealed class ObsWebSocketClient : IAsyncDisposable
{
    private ClientWebSocket? _socket;
    private int _requestCounter;

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public async Task ConnectAsync(string host, int port, string password, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(new Uri($"ws://{host}:{port}"), cancellationToken);

        using JsonDocument hello = await ReceiveJsonAsync(cancellationToken);
        JsonElement helloData = hello.RootElement.GetProperty("d");
        string? authentication = null;

        if (helloData.TryGetProperty("authentication", out JsonElement auth))
        {
            string challenge = auth.GetProperty("challenge").GetString() ?? "";
            string salt = auth.GetProperty("salt").GetString() ?? "";
            string secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
            authentication = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
        }

        var identifyData = new Dictionary<string, object?> { ["rpcVersion"] = 1 };
        if (!string.IsNullOrWhiteSpace(authentication))
            identifyData["authentication"] = authentication;

        await SendJsonAsync(new { op = 1, d = identifyData }, cancellationToken);
        using JsonDocument identified = await ReceiveJsonAsync(cancellationToken);
        if (identified.RootElement.GetProperty("op").GetInt32() != 2)
            throw new InvalidOperationException("OBS WebSocket hat die Anmeldung nicht bestätigt.");
    }

    public Task StartStreamAsync(CancellationToken ct = default) => SendRequestAsync("StartStream", null, ct);
    public Task StopStreamAsync(CancellationToken ct = default) => SendRequestAsync("StopStream", null, ct);
    public Task StartVirtualCamAsync(CancellationToken ct = default) => SendRequestAsync("StartVirtualCam", null, ct);
    public Task StopVirtualCamAsync(CancellationToken ct = default) => SendRequestAsync("StopVirtualCam", null, ct);

    private async Task SendRequestAsync(string requestType, object? requestData, CancellationToken ct)
    {
        EnsureConnected();
        string requestId = Interlocked.Increment(ref _requestCounter).ToString();
        await SendJsonAsync(new { op = 6, d = new { requestType, requestId, requestData } }, ct);

        while (true)
        {
            using JsonDocument response = await ReceiveJsonAsync(ct);
            JsonElement root = response.RootElement;
            if (root.GetProperty("op").GetInt32() != 7)
                continue;

            JsonElement data = root.GetProperty("d");
            if (data.GetProperty("requestId").GetString() != requestId)
                continue;

            JsonElement status = data.GetProperty("requestStatus");
            if (!status.GetProperty("result").GetBoolean())
            {
                string message = status.TryGetProperty("comment", out JsonElement comment)
                    ? comment.GetString() ?? "Unbekannter OBS-Fehler"
                    : "Unbekannter OBS-Fehler";
                throw new InvalidOperationException(message);
            }
            return;
        }
    }

    private async Task SendJsonAsync(object payload, CancellationToken ct)
    {
        EnsureConnected();
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await _socket!.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task<JsonDocument> ReceiveJsonAsync(CancellationToken ct)
    {
        EnsureConnected();
        using var stream = new MemoryStream();
        byte[] buffer = new byte[8192];
        WebSocketReceiveResult result;
        do
        {
            result = await _socket!.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("OBS WebSocket wurde geschlossen.");
            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Keine Verbindung zu OBS WebSocket.");
    }

    public async Task DisconnectAsync()
    {
        if (_socket is null)
            return;
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Creator Hub Live beendet", CancellationToken.None);
        }
        catch { }
        finally
        {
            _socket.Dispose();
            _socket = null;
        }
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
