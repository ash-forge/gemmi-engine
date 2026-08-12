using System;
using System.Diagnostics;
using Gemmi.Core;

namespace Gemmi.Scratch;

public class MemoryEngineBenchmark
{
    public static void Main()
    {
        Console.WriteLine("=== Gemmi High-Speed Memory Engine Benchmark ===");

        var buffer = new WorkingMemoryBuffer(1000);
        var graph = new EpisodicMemoryGraph();
        var queryEngine = new MemoryQueryEngine(buffer, graph);

        // 1. Populate RAM Ring Buffer & Semantic Graph
        Console.WriteLine("[+] Populating in-memory RAM observations & semantic graph...");
        for (int i = 0; i < 500; i++)
        {
            buffer.AddObservation(
                MemoryCategory.Vision,
                $"PaliGemma 2 Landmark Observation #{i}: Building {i % 5} Coffee Lounge",
                salienceScore: (float)(i % 100) / 100.0f
            );
        }

        graph.LinkConcepts("Coffee Lounge", "Building 4", MemoryCategory.Location, MemoryCategory.Location);
        graph.LinkConcepts("Building 4", "Daniel Desk", MemoryCategory.Location, MemoryCategory.System);

        // 2. Perform Sub-5ms Query
        Console.WriteLine("[+] Executing real-time sub-5ms memory query for 'Coffee Lounge'...");
        var sw = Stopwatch.StartNew();
        var result = queryEngine.QuerySubFiveMs("Coffee Lounge");
        sw.Stop();

        Console.WriteLine($"\n=== BENCHMARK RESULTS ===");
        Console.WriteLine($"[✓] Query Latency        : {result.QueryLatency.TotalMilliseconds:F4} ms");
        Console.WriteLine($"[✓] Relevant Entries Found: {result.RelevantEntries.Count}");
        Console.WriteLine($"[✓] Associated Concepts   : {result.AssociatedConcepts.Count}");
        Console.WriteLine($"[✓] Highest Salience (θ)  : {result.HighestSalience:F2}");
        Console.WriteLine("=========================");
    }
}
