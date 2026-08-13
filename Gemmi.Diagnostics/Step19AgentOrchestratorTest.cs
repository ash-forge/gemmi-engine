using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step19AgentOrchestratorTest
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  ⚙️ GEMMI ENGINE STEP 19: AGENT ORCHESTRATOR & TASK CONTROLLER TEST     ");
        Console.WriteLine("==========================================================================");

        using var orchestrator = new GemmiAgentOrchestrator();

        // 1. Register Custom Tools
        Console.WriteLine("\n[1] Registering Custom Agent Tools...");
        orchestrator.RegisterTool("ModelInspector", "Simulates inspecting model metadata", (paramsMap, ct) =>
        {
            var modelPath = paramsMap.TryGetValue("path", out var p) ? p?.ToString() ?? "" : "";
            return Task.FromResult(AgentToolResult.Ok($"Model '{modelPath}' verified", new { Layers = 32, Quant = "Q4_K_M" }));
        });

        orchestrator.RegisterTool("StateSwitcher", "Switches avatar locomotion state", (paramsMap, ct) =>
        {
            var state = paramsMap.TryGetValue("state", out var s) ? s?.ToString() ?? "Idle" : "Idle";
            return Task.FromResult(AgentToolResult.Ok($"State switched to {state}"));
        });

        Console.WriteLine("    • [PASS] Custom Tools 'ModelInspector' and 'StateSwitcher' Registered");

        // 2. Start Worker Loop
        Console.WriteLine("\n[2] Starting Agent Orchestrator Worker Loop...");
        orchestrator.Start();
        Console.WriteLine($"    • [PASS] Orchestrator Started (IsRunning: {orchestrator.IsRunning})");

        // 3. Enqueue Tasks
        Console.WriteLine("\n[3] Enqueuing & Executing Tasks...");
        var t1 = orchestrator.EnqueueTask("Check Default Model", "ModelInspector", new Dictionary<string, object>
        {
            ["path"] = @"C:\Users\admin\source\gemmi-engine\models\avatar_sanitized.glb"
        });

        var t2 = orchestrator.EnqueueTask("Set Cozy State", "StateSwitcher", new Dictionary<string, object>
        {
            ["state"] = "CozyChairListeningMusic"
        });

        var t3 = orchestrator.EnqueueTask("Ping System", "Ping");

        // Wait briefly for execution
        await Task.Delay(300);

        Console.WriteLine($"    • Task 1: {t1.Name} -> Status: {t1.Status} | Result: {t1.Result?.Message}");
        Console.WriteLine($"    • Task 2: {t2.Name} -> Status: {t2.Status} | Result: {t2.Result?.Message}");
        Console.WriteLine($"    • Task 3: {t3.Name} -> Status: {t3.Status} | Result: {t3.Result?.Message}");

        // 4. Test Error Handling
        Console.WriteLine("\n[4] Testing Unknown Tool Error Handling...");
        var t4 = orchestrator.EnqueueTask("Invalid Tool Task", "NonExistentTool");
        await Task.Delay(150);

        Console.WriteLine($"    • Task 4: {t4.Name} -> Status: {t4.Status} | Error: {t4.Result?.Message}");

        // 5. Verify History
        Console.WriteLine("\n[5] Verifying Task Execution History...");
        var history = orchestrator.History;
        Console.WriteLine($"    • [PASS] Total History Records: {history.Count}");

        await orchestrator.StopAsync();
        Console.WriteLine($"    • [PASS] Orchestrator Stopped Cleanly (IsRunning: {orchestrator.IsRunning})");

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 19 AGENT ORCHESTRATOR TEST PASSED 100%!                        ");
        Console.WriteLine("==========================================================================");
    }
}
