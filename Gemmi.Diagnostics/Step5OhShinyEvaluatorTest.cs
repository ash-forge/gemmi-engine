using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Perception;

namespace Gemmi.Scratch;

public class Step5OhShinyEvaluatorTest
{
    public static async Task Main()
    {
        Console.WriteLine("=== STEP 5: 'Oh Shiny!' Cognitive Evaluator Live Test ===");
        var state = new GemmiState();
        var evaluator = new SpontaneousInitiationEvaluator();

        // 1. Build a Rich Interconnected Semantic Graph in RAM
        Console.WriteLine("\n[1] Seeding Memory Graph with Interconnected Concepts...");
        state.MemoryGraph.LinkConcepts("C# Gateway", "Server Rack 01", MemoryCategory.Code, MemoryCategory.System);
        state.MemoryGraph.LinkConcepts("Server Rack 01", "Xeon E-2236 CPU", MemoryCategory.System, MemoryCategory.System);
        state.MemoryGraph.LinkConcepts("Xeon E-2236 CPU", "Building 4 Coffee Lounge", MemoryCategory.System, MemoryCategory.Location);
        state.MemoryGraph.LinkConcepts("Building 4 Coffee Lounge", "Dr. Haze Chronometers", MemoryCategory.Location, MemoryCategory.Thought);
        state.MemoryGraph.LinkConcepts("Dr. Haze Chronometers", "Quantum Time Warp", MemoryCategory.Thought, MemoryCategory.Thought);

        // 2. Inject Base Sensory Observations
        state.MemoryBuffer.AddObservation(MemoryCategory.Code, "C# Gateway L120", salienceScore: 0.60f);
        state.MemoryBuffer.AddObservation(MemoryCategory.Vision, "PaliGemma 2: Monitor 01 Screen Active", salienceScore: 0.65f);

        evaluator.Mode = EvaluatorSensitivityMode.BalancedMode;
        Console.WriteLine($"\n[2] Evaluator Running in {evaluator.Mode} (Threshold θ = {evaluator.CurrentThreshold})...");

        int alertCount = 0;
        using var cts = new CancellationTokenSource();

        var evaluatorTask = evaluator.StartSpontaneousEvaluatorLoopAsync(state, alert =>
        {
            alertCount++;
            Console.WriteLine($"\n[🔥 SPONTANEOUS COGNITIVE TICK #{alertCount}]");
            Console.WriteLine($"    Insight  : {alert}");
            Console.WriteLine($"    Salience : θ = {state.Perception.SpontaneousInitiationScore}");
        }, cts.Token);

        Console.WriteLine("[+] Monitoring live cognitive salience ticks & associative graph leaps...");
        
        // Wait 3 seconds to observe natural ticks + potential random associative leaps
        await Task.Delay(3500);

        // 3. Inject Brand-New Novel Observation (Triggers Dopamine Novelty Spike +0.30!)
        Console.WriteLine("\n[+] Injecting Brand-New Novel Memory ('NFC Badge Scanner Activated')...");
        state.MemoryBuffer.AddObservation(MemoryCategory.System, $"NFC Badge Scanner Activated: {state.User.UserName} ({state.User.RoleTitle})", salienceScore: 0.70f);
        await Task.Delay(3500);

        // 4. Inject another observation to trigger potential 'Oh Shiny!' random walk
        Console.WriteLine("\n[+] Injecting Memory ('Quantum Time Warp')...");
        state.MemoryBuffer.AddObservation(MemoryCategory.Thought, "Quantum Time Warp", salienceScore: 0.72f);
        await Task.Delay(3500);

        cts.Cancel();

        Console.WriteLine("\n=== STEP 5 'OH SHINY!' COGNITIVE TEST COMPLETE ===");
        Console.WriteLine($"[✓] Total Spontaneous Cognitive Invocations: {alertCount}");
        Console.WriteLine($"[✓] Recent Spontaneous Alerts in State    : {state.RecentSpontaneousAlerts.Count}");
    }
}
