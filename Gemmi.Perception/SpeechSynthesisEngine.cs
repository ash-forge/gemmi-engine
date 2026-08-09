using System;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

[SupportedOSPlatform("windows")]
public class SpeechSynthesisEngine
{
    private readonly SpeechSynthesizer _synth = new();

    public SpeechSynthesisEngine()
    {
        try
        {
            _synth.SetOutputToDefaultAudioDevice();
            _synth.Rate = 1;  // Natural speaking rate
            _synth.Volume = 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeechSynthesizer Init Warning]: {ex.Message}");
        }
    }

    public async Task SpeakAsync(string text, GemmiState state)
    {
        state.Perception.LastObservedContext = $"Speaking out loud: '{text}'";
        
        await Task.Run(() =>
        {
            try
            {
                _synth.Speak(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Speech Synthesizer Warning]: {ex.Message}");
            }
        });
    }
}
