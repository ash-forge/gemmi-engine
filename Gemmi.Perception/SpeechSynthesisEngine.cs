using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

[SupportedOSPlatform("windows")]
public class SpeechSynthesisEngine
{
    private readonly SpeechSynthesizer _synth = new();
    private readonly string _t5GemmaPath = @"C:\Users\admin\gemma4-turbo-family\t5gemma-tts-2b-2b";

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
        
        bool neuralSpeechSuccess = false;
        if (Directory.Exists(_t5GemmaPath))
        {
            neuralSpeechSuccess = await TryT5GemmaSpeechAsync(text);
        }

        if (!neuralSpeechSuccess)
        {
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

    private async Task<bool> TryT5GemmaSpeechAsync(string text)
    {
        try
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "t5gemma_tts_sidecar.py");
            if (!File.Exists(scriptPath))
            {
                // Fallback to local python scratch test path
                scriptPath = @"C:\Users\admin\.gemini\antigravity-cli\brain\4892e880-a9ff-41fb-b7b7-5e990ca73e75\scratch\test_t5gemma_tts.py";
            }

            if (!File.Exists(scriptPath)) return false;

            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Users\admin\AppData\Local\Python\bin\python.exe",
                Arguments = $"\"{scriptPath}\" \"{text}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[T5Gemma-TTS Sidecar Warning]: {ex.Message}");
        }
        return false;
    }
}
