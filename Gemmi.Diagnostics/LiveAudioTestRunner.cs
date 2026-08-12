using System;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Perception;

namespace Gemmi.Scratch;

public class LiveAudioTestRunner
{
    public static async Task Main()
    {
        Console.WriteLine("=== Gemmi Live Audio Passthrough Test ===");
        var state = new GemmiState();
        var speechEngine = new SpeechSynthesisEngine();

        Console.WriteLine($"[+] Voice Persona Selected: {state.Voice.Gender} Voice");
        Console.WriteLine("[+] Playing Audio Out Loud over Server Audio Passthrough to Tablet...");

        string message = "Hello Daniel! Gemmi Second Brain is online and connected over audio passthrough. All sub-millisecond memory networks are operating at peak speed.";
        
        await speechEngine.SpeakAsync(message, state);

        Console.WriteLine("[✓] Speech Output Complete!");
        Console.WriteLine($"[✓] State Context Updated: '{state.Perception.LastObservedContext}'");
    }
}
