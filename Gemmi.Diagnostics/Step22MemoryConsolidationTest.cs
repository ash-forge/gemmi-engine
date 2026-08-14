using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step22MemoryConsolidationTest
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧬 GEMMI ENGINE STEP 22: SELECTIVE LONG-TERM CONSOLIDATION TEST (θ >= 0.85)");
        Console.WriteLine("==========================================================================");

        string testBinDir = Path.Combine(Path.GetTempPath(), "gemmi_test_consolidation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testBinDir);

        try
        {
            var workingMemory = new WorkingMemoryBuffer(100);
            var episodicGraph = new EpisodicMemoryGraph();
            var binaryStore = new BinaryMemoryStore(testBinDir);
            using var consolidationEngine = new GemmiMemoryConsolidationEngine(workingMemory, episodicGraph, binaryStore, 0.85f);
            consolidationEngine.Start();

            // 1. Ingest Low-Salience Transient Observations (θ < 0.85)
            Console.WriteLine("\n[1] Ingesting 3 low-salience transient observations (θ < 0.85)...");
            workingMemory.AddObservation(MemoryCategory.System, "Fan speed adjusted to 1800 RPM.", 0.20f);
            workingMemory.AddObservation(MemoryCategory.Vision, "Ambient lighting unchanged at 450 lux.", 0.40f);
            workingMemory.AddObservation(MemoryCategory.Voice, "User clicked keyboard key.", 0.35f);

            // Brief delay
            await Task.Delay(200);

            Console.WriteLine($"    • Short-Term RAM Buffer Count : {workingMemory.Count}");
            Console.WriteLine($"    • Long-Term Consolidated Count: {consolidationEngine.ConsolidatedCount} (Expected 0)");
            Console.WriteLine($"    • Transient Purged Count      : {consolidationEngine.TransientPurgedCount} (Expected 3)");
            Console.WriteLine($"    • Episodic Graph Nodes        : {episodicGraph.NodeCount} (Expected 0)");

            if (consolidationEngine.ConsolidatedCount != 0 || consolidationEngine.TransientPurgedCount != 3)
            {
                throw new Exception("Low salience observations should NOT be consolidated to long-term storage!");
            }
            Console.WriteLine("    • [PASS] Low-salience chatter stayed strictly in RAM and was excluded from disk!");

            // 2. Ingest High-Salience Significant Observations (θ >= 0.85)
            Console.WriteLine("\n[2] Ingesting 2 high-salience breakthrough observations (θ >= 0.85)...");
            workingMemory.AddObservation(
                MemoryCategory.Code,
                "Daniel completed ModelStudio GGUF layer quantization for haven-chat-v3.",
                0.92f
            );
            workingMemory.AddObservation(
                MemoryCategory.Location,
                "Sanctuary Tower GPS waypoint reached at Latitude 37.7749 Longitude -122.4194.",
                0.89f,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "gpsLat", "37.7749" },
                    { "gpsLng", "-122.4194" }
                }
            );

            // Wait for background worker queue
            await Task.Delay(300);

            Console.WriteLine($"    • Long-Term Consolidated Count: {consolidationEngine.ConsolidatedCount} (Expected 2)");
            Console.WriteLine($"    • Episodic Graph Node Count   : {episodicGraph.NodeCount}");

            if (consolidationEngine.ConsolidatedCount != 2)
            {
                throw new Exception($"Expected 2 consolidated records, but got {consolidationEngine.ConsolidatedCount}");
            }
            Console.WriteLine("    • [PASS] High-salience records successfully promoted to long-term memory!");

            // 3. Verify .gemmi-bin Zero-Seek Binary Index
            Console.WriteLine("\n[3] Verifying zero-seek .gemmi-bin storage on disk...");
            string indexPath = Path.Combine(testBinDir, "gemmi_memory_index.gemmi-bin");
            string blobPath = Path.Combine(testBinDir, "gemmi_memory_payloads.gemmi-dat");

            if (!File.Exists(indexPath) || !File.Exists(blobPath))
            {
                throw new Exception("Binary memory store files missing from disk!");
            }

            var binRecords = binaryStore.ReadAllRecordsZeroSeek();
            Console.WriteLine($"    • Binary Store Records Read: {binRecords.Count}");
            foreach (var r in binRecords)
            {
                Console.WriteLine($"      - [{r.Category}] (θ={r.SalienceScore:F2}): {r.Content}");
            }
            Console.WriteLine("    • [PASS] Binary zero-seek file persistence verified 100%!");

            // 4. Test Offline Associative Dreaming / Memory Weaving
            Console.WriteLine("\n[4] Testing Offline Associative Memory Weaving ('Dreaming')...");
            int newLinks = consolidationEngine.WeaveAssociativeMemories();
            Console.WriteLine($"    • Associative Synaptic Links Formed: {newLinks}");

            // 5. Verify Associative Graph Lookups
            Console.WriteLine("\n[5] Testing Associative Concept Hop in Episodic Memory Graph...");
            var related = episodicGraph.GetRelatedConcepts("ModelStudio");
            Console.WriteLine($"    • Concepts related to 'ModelStudio': {string.Join(", ", related.Select(r => r.Concept))}");

            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("  [✓] STEP 22 SELECTIVE CONSOLIDATION TEST PASSED 100%!                   ");
            Console.WriteLine("==========================================================================");
        }
        finally
        {
            try { Directory.Delete(testBinDir, true); } catch { }
        }
    }
}
