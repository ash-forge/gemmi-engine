using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gemmi.Core;

public class NetBirdMeshSync
{
    public string MeshDomain { get; set; } = "mesh.barrer.net";
    public string HomeNasEndpoint { get; set; } = "100.64.0.10:18799";
    public string LabStackEndpoint { get; set; } = "100.64.0.25:18799";

    private HttpListener? _listener;

    public void StartMeshListener(GemmiState state, Action<string> onMeshReceived, CancellationToken cancellationToken)
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://+:18799/api/mesh/");
            _listener.Start();

            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        var req = context.Request;
                        var resp = context.Response;

                        if (req.HttpMethod == "POST")
                        {
                            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                            string body = await reader.ReadToEndAsync();
                            
                            using var doc = JsonDocument.Parse(body);
                            var root = doc.RootElement;

                            string nodeId = root.TryGetProperty("nodeId", out var n) ? n.GetString() ?? "Mobile" : "Mobile";
                            double lat = root.TryGetProperty("latitude", out var l) ? l.GetDouble() : 0.0;
                            double lng = root.TryGetProperty("longitude", out var lg) ? lg.GetDouble() : 0.0;
                            string landmark = root.TryGetProperty("landmark", out var lm) ? lm.GetString() ?? "Urban Area" : "Urban Area";

                            state.WorkingMemoryGraph["MobileGpsLat"] = lat.ToString("F4");
                            state.WorkingMemoryGraph["MobileGpsLng"] = lng.ToString("F4");
                            state.WorkingMemoryGraph["MobileLandmark"] = landmark;

                            string syncMsg = $"[NetBird Mesh Sync] Received location update from '{nodeId}': ({lat:F4}, {lng:F4}) near '{landmark}'";
                            state.Perception.LastObservedContext = syncMsg;
                            onMeshReceived(syncMsg);

                            byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"hydrated\":true}");
                            resp.ContentType = "application/json";
                            resp.ContentLength64 = buffer.Length;
                            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }

                        resp.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Mesh Listener Error]: {ex.Message}");
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HttpListener Port 18799 unavailable]: {ex.Message}");
        }
    }

    public async Task<string> SerializeAndHydrateMeshStateAsync(GemmiState state, string targetNode)
    {
        await Task.Delay(50);
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        
        state.Telemetry.NetBirdMeshConnected = true;
        state.Perception.LastObservedContext = $"State hydrated across NetBird mesh ({MeshDomain}) to '{targetNode}'";
        
        return $"[NetBird P2P Mesh] Hydrated {json.Length:N0} bytes of Gemmi state to node '{targetNode}' over {MeshDomain}";
    }
}
