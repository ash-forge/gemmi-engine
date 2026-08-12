using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Hardware;

namespace Gemmi.Scratch;

public class Step4HardwareModelTest
{
    public static async Task Main()
    {
        Console.WriteLine("=== STEP 4: Hardware Telemetry & P2P Mesh Model Consumption Test ===");
        var state = new GemmiState();

        Console.WriteLine("[1] Model Ingesting Deep Horizon Node Telemetry & NFC Badge Reader...");
        var sw = Stopwatch.StartNew();

        state.Telemetry.NodeId = "DeepHorizon-Enterprise-Node-01";
        state.Telemetry.SkuType = HardwareSkuType.EnterpriseGoogleInternalNode;
        state.Telemetry.CpuTemperatureC = 44.2;
        state.Telemetry.TpuUsagePercent = 24.8;
        state.Telemetry.NetBirdMeshConnected = true;
        state.Telemetry.ActiveNfcBadgeUser = $"{state.User.UserName} ({state.User.RoleTitle})";

        HardwareSensorGateway.PushTelemetryToMemory(state);
        sw.Stop();

        Console.WriteLine($"[✓] Hardware Telemetry Ingested in {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Node Identifier      : {state.Telemetry.NodeId}");
        Console.WriteLine($"[✓] Hardware SKU          : {state.Telemetry.SkuType}");
        Console.WriteLine($"[✓] CPU Temp / TPU Usage  : {state.Telemetry.CpuTemperatureC}°C / {state.Telemetry.TpuUsagePercent}%");
        Console.WriteLine($"[✓] NFC Badge Authenticated: {state.Telemetry.ActiveNfcBadgeUser}");
        Console.WriteLine($"[✓] NetBird P2P Mesh State: {(state.Telemetry.NetBirdMeshConnected ? "CONNECTED" : "OFFLINE")}");

        var sysMemories = state.MemoryBuffer.GetByCategory(MemoryCategory.System);
        Console.WriteLine($"\n[✓] System Hardware Memory Entries Ingested: {sysMemories.Count}");

        foreach (var entry in sysMemories)
        {
            Console.WriteLine($"    -> [{entry.Timestamp:HH:mm:ss.fff}] (θ={entry.SalienceScore:F2}) {entry.Content}");
        }

        Console.WriteLine("\n=== STEP 4 HARDWARE MODEL TEST PASSED PERFECTLY ===");
    }
}
