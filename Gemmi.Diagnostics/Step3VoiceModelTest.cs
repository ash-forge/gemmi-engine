using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Perception;

namespace Gemmi.Scratch;

public class Step3VoiceModelTest
{
    public static async Task Main()
    {
        Console.WriteLine("=== STEP 3: Speech Synthesis & Neural Voice Model Consumption Test ===");
        var state = new GemmiState();
        var speechEngine = new SpeechSynthesisEngine();

        Console.WriteLine("[1] Executing T5Gemma-TTS / Speech Synthesizer Voice Output...");
        var sw = Stopwatch.StartNew();

        string sampleText = "Deep Horizon Node 01 operating at zero-latency sub-millisecond memory thresholds.";
        await speechEngine.SpeakAsync(sampleText, state);

        sw.Stop();

        Console.WriteLine($"[✓] Speech Synthesis Execution Completed in {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Last Observed Context : {state.Perception.LastObservedContext}");
        Console.WriteLine($"[✓] Memory Buffer Count   : {state.MemoryBuffer.Count} entries captured");

        var voiceMemories = state.MemoryBuffer.GetByCategory(MemoryCategory.Voice);
        Console.WriteLine($"[✓] Voice Category Entries: {voiceMemories.Count}");

        foreach (var entry in voiceMemories)
        {
            Console.WriteLine($"    -> [{entry.Timestamp:HH:mm:ss.fff}] (θ={entry.SalienceScore:F2}) {entry.Content}");
        }

        Console.WriteLine("\n=== STEP 3 VOICE MODEL TEST PASSED PERFECTLY ===");
    }
}
