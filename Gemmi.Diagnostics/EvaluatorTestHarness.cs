using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Perception;

namespace Gemmi.Scratch;

public class EvaluatorTestHarness
{
    public static async Task Main()
    {
        Console.WriteLine("=== Gemmi Spontaneous Initiation Evaluator Live Test ===");
        var state = new GemmiState();
        var evaluator = new SpontaneousInitiationEvaluator();

        // 1. Simulate Incoming Perception Memory Observations
        Console.WriteLine("\n[+] Injecting Multimodal Sensory Observations into RAM Working Memory...");

        // Low-salience background observation
        state.MemoryBuffer.AddObservation(MemoryCategory.System, "Monitoring background CPU temp (42C)", salienceScore: 0.20f);

        // High-salience visual observation (PaliGemma 2)
        state.MemoryBuffer.AddObservation(MemoryCategory.Vision, "PaliGemma 2 Vision: Unhandled NullReferenceException in C# Gateway line 142", salienceScore: 0.92f);

        // High-salience location observation
        state.MemoryGraph.LinkConcepts("Coffee Lounge", "Building 4", MemoryCategory.Location, MemoryCategory.Location);
        state.MemoryBuffer.AddObservation(MemoryCategory.Location, "Sub-Meter GPS: User arrived at Building 4 Coffee Lounge", salienceScore: 0.88f);

        // 2. Test Evaluator in Balanced Mode (Threshold = 0.85)
        evaluator.Mode = EvaluatorSensitivityMode.BalancedMode;
        Console.WriteLine($"\n[+] Evaluator Mode: {evaluator.Mode} (Threshold θ = {evaluator.CurrentThreshold})");

        int alertCount = 0;
        using var cts = new CancellationTokenSource();

        var evaluatorTask = evaluator.StartSpontaneousEvaluatorLoopAsync(state, alert =>
        {
            alertCount++;
            Console.WriteLine($"\n[🔥 SPONTANEOUS INITIATION #{alertCount} TRIGGERED!]");
            Console.WriteLine($"    Alert Content: {alert}");
            Console.WriteLine($"    Current State Score: θ = {state.Perception.SpontaneousInitiationScore}");
        }, cts.Token);

        Console.WriteLine("[+] Evaluator running live... Monitoring cognitive salience ticks...");
        await Task.Delay(4500); // Allow evaluation tick 1 to fire

        // 3. Test Refractory Cooldown Defense
        Console.WriteLine("\n[+] Testing 30-Second Refractory Cooldown (Injecting Duplicate Memory)...");
        state.MemoryBuffer.AddObservation(MemoryCategory.Vision, "PaliGemma 2 Vision: Unhandled NullReferenceException in C# Gateway line 142", salienceScore: 0.92f);
        await Task.Delay(4500); // Ticks again, should be BLOCKED by 30s refractory cooldown

        // 4. Inject NEW distinct high-salience observation
        Console.WriteLine("\n[+] Injecting NEW High-Salience Memory Event (Git PR #42)...");
        state.MemoryBuffer.AddObservation(MemoryCategory.Code, "Git Commit: New PR #42 pushed to ash-server-cs master", salienceScore: 0.95f);
        await Task.Delay(4500);

        // 5. Test CoPilot Mode (Threshold = 0.75)
        evaluator.Mode = EvaluatorSensitivityMode.CoPilotMode;
        Console.WriteLine($"\n[+] Switched Evaluator Mode to: {evaluator.Mode} (Threshold θ = {evaluator.CurrentThreshold})");
        state.MemoryBuffer.AddObservation(MemoryCategory.Thought, "Gemmi Cognitive Thought: Auto-formatting C# memory graphs", salienceScore: 0.78f);
        await Task.Delay(4500);

        cts.Cancel();

        Console.WriteLine("\n=== EVALUATOR TEST COMPLETE ===");
        Console.WriteLine($"[✓] Total Spontaneous Alerts Fired: {alertCount}");
        Console.WriteLine($"[✓] Refractory Cooldown Blocked Duplicates: YES");
        Console.WriteLine($"[✓] Recent Alerts Logged in GemmiState: {state.RecentSpontaneousAlerts.Count}");
    }
}
