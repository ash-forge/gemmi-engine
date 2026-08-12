using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Perception;

namespace Gemmi.Scratch;

public class Step7FullBinaryMemoryExperienceTest
{
    public static async Task Main()
    {
        Console.WriteLine("=== STEP 7: Full Native Binary Memory (.gemmi-bin) Agent Inspection Test ===");
        var state = new GemmiState();
        var binaryStore = state.BinaryStore;

        // 1. Multimodal Sensory Batch Ingestion (5,000 Entries)
        Console.WriteLine("\n[1] Agent Writing 5,000 Multimodal Sensory Memories to .gemmi-bin...");
        var swIngest = Stopwatch.StartNew();

        string[] landmarks = { "Building 4 Coffee Lounge", "Deep Horizon Lab Bench 02", "P2P NetBird Gateway Node", "Whiteboard Canvas Room" };
        string[] concepts = { "C# .NET 10 Rev 3 Silicon Bus", "PaliGemma 2 Spatial Screen Frame", "T5Gemma-TTS Neural Voice Synthesizer", "Dopamine Novelty Spike (+0.30)" };

        for (int i = 1; i <= 5000; i++)
        {
            var cat = (MemoryCategory)(i % 6);
            string concept = concepts[i % concepts.Length];
            string landmark = landmarks[i % landmarks.Length];
            string content = $"Observation #{i:D4}: Concept '{concept}' logged at '{landmark}' by User Daniel.";
            float salience = 0.50f + (i % 50) * 0.01f;

            await binaryStore.AppendRecordAsync(cat, content, salience);
        }

        swIngest.Stop();
        Console.WriteLine($"[✓] Ingested 5,000 Records to .gemmi-bin in {swIngest.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Write Latency per Memory Entry: {swIngest.Elapsed.TotalMilliseconds / 5000.0:F4} ms");

        // 2. Zero-Seek MemoryMappedFile Retrieval Benchmark
        Console.WriteLine("\n[2] Agent Reading Back All 5,000 Binary Memory Records via MemoryMappedFile...");
        var swRead = Stopwatch.StartNew();

        var allRecords = binaryStore.ReadAllRecordsZeroSeek();

        swRead.Stop();
        Console.WriteLine($"[✓] Retrieved {allRecords.Count} Memory Records in {swRead.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Retrieval Latency per Memory Record: {swRead.Elapsed.TotalMilliseconds / allRecords.Count:F4} ms ({swRead.Elapsed.TotalMilliseconds * 1000.0 / allRecords.Count:F1} microseconds!)");

        // 3. Sub-Millisecond Context Query Search
        Console.WriteLine("\n[3] Executing Microsecond Search for 'PaliGemma 2' in Binary Store...");
        var swSearch = Stopwatch.StartNew();

        var paliGemmaMatches = allRecords.Where(r => r.Content.Contains("PaliGemma 2", StringComparison.OrdinalIgnoreCase)).ToList();

        swSearch.Stop();
        Console.WriteLine($"[✓] Found {paliGemmaMatches.Count} High-Salience Matches in {swSearch.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"    Sample Match 1: [{paliGemmaMatches[0].Timestamp:HH:mm:ss}] ({paliGemmaMatches[0].Category}) (θ={paliGemmaMatches[0].SalienceScore:F2}) {paliGemmaMatches[0].Content}");
        Console.WriteLine($"    Sample Match 2: [{paliGemmaMatches[paliGemmaMatches.Count / 2].Timestamp:HH:mm:ss}] ({paliGemmaMatches[paliGemmaMatches.Count / 2].Category}) (θ={paliGemmaMatches[paliGemmaMatches.Count / 2].SalienceScore:F2}) {paliGemmaMatches[paliGemmaMatches.Count / 2].Content}");

        // 4. Memory Graph Integration Test
        Console.WriteLine("\n[4] Linking Binary Memory Concepts to Episodic Memory Graph...");
        state.MemoryGraph.LinkConcepts("PaliGemma 2", "Sub-5ms Binary Memory Store", MemoryCategory.Vision, MemoryCategory.System);
        state.MemoryGraph.LinkConcepts("Sub-5ms Binary Memory Store", "Daniel L8 Principal Architect", MemoryCategory.System, MemoryCategory.Thought);

        var graphWalk = state.MemoryGraph.GetRelatedConcepts("PaliGemma 2");
        Console.WriteLine($"[✓] Episodic Memory Graph Walk returned {graphWalk.Count} connected concepts:");
        foreach (var node in graphWalk)
        {
            Console.WriteLine($"    -> Concept: '{node.Concept}' ({node.Category}) Weight={node.Weight:F2}");
        }

        Console.WriteLine("\n=== STEP 7 FULL BINARY MEMORY AGENT INSPECTION COMPLETE ===");
    }
}
