using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gemmi.Core;

public class SpatialTelemetryFrame
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public long TimestampMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public int FrameIndex { get; set; }
    public Dictionary<string, float[]> Joints { get; set; } = new(); // 15-Point Matrix [X, Y, Z, RotX, RotY, RotZ]
    public Dictionary<string, float> MorphWeights { get; set; } = new(); // FACS Blendshapes (jawOpen, mouthSmile, eyeBlink)
    public float[] CameraPosition { get; set; } = new float[] { 0, 1.5f, 3.0f };
    public float[] CameraRotation { get; set; } = new float[] { 0, 0, 0 };
    public string CurrentLocomotionState { get; set; } = "Idle";
    public float CurrentSpeed { get; set; }
    public List<SpatialObservedObject> VisibleObjects { get; set; } = new();
}

public class SpatialObservedObject
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public float[] Position { get; set; } = new float[3];
    public float Distance { get; set; }
    public bool IsInLineOfSight { get; set; }
    public string ZoneClassification { get; set; } = "Social";
}

public class GemmiNetworkServer
{
    private readonly int _port;
    private HttpListener? _httpListener;
    private readonly ConcurrentDictionary<string, WebSocket> _activeClients = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public event Action<string, string>? OnClientCommandReceived;

    public GemmiNetworkServer(int port = 8088)
    {
        _port = port;
    }

    public bool IsRunning => _isRunning;
    public int ConnectedClientCount => _activeClients.Count;

    public void Start()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{_port}/");
        _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _httpListener.Start();

        _isRunning = true;
        Console.WriteLine($"[GemmiNetworkServer] Real-Time 60FPS Spatial Telemetry Server started on ws://localhost:{_port}/");

        _ = Task.Run(() => AcceptClientsAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        _httpListener?.Stop();

        foreach (var client in _activeClients.Values)
        {
            if (client.State == WebSocketState.Open)
            {
                try { client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None).Wait(); } catch { }
            }
        }

        _activeClients.Clear();
        _isRunning = false;
        Console.WriteLine("[GemmiNetworkServer] Server stopped.");
    }

    public async Task BroadcastTelemetryAsync(SpatialTelemetryFrame frame)
    {
        if (_activeClients.IsEmpty) return;

        string json = JsonSerializer.Serialize(frame, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        byte[] buffer = Encoding.UTF8.GetBytes(json);

        var deadClients = new List<string>();

        foreach (var (id, client) in _activeClients)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    deadClients.Add(id);
                }
            }
            else
            {
                deadClients.Add(id);
            }
        }

        foreach (var id in deadClients)
        {
            _activeClients.TryRemove(id, out _);
        }
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener != null && _httpListener.IsListening)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => ProcessWebSocketClientAsync(context, cancellationToken));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;
                Console.WriteLine($"[GemmiNetworkServer] Accept error: {ex.Message}");
            }
        }
    }

    private async Task ProcessWebSocketClientAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        string clientId = Guid.NewGuid().ToString("N");
        HttpListenerWebSocketContext wsContext;

        try
        {
            wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = wsContext.WebSocket;
            _activeClients[clientId] = webSocket;

            Console.WriteLine($"[GemmiNetworkServer] Client connected: {clientId} (Total: {_activeClients.Count})");

            byte[] buffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string commandStr = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnClientCommandReceived?.Invoke(clientId, commandStr);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GemmiNetworkServer] Client {clientId} exception: {ex.Message}");
        }
        finally
        {
            _activeClients.TryRemove(clientId, out _);
            Console.WriteLine($"[GemmiNetworkServer] Client disconnected: {clientId} (Remaining: {_activeClients.Count})");
        }
    }
}
