using System;
using System.IO;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Hardware;

namespace Gemmi.Perception;

public class SpeechRecognizedEventArgs : EventArgs
{
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

/// <summary>
/// Sovereign Full-Duplex Voice Dialogue Pipeline.
/// Orchestrates real-time 16kHz microphone stream capture, offline speech recognition (STT),
/// and speech synthesis (TTS) synchronized with 3D facial morph visemes.
/// </summary>
public class GemmiVoiceDialoguePipeline : IDisposable
{
    private readonly MicrophoneAudioSensor? _micSensor;
    private readonly GemmiFacialAnimationEngine _facialEngine;
    private bool _isListening;

    [SupportedOSPlatform("windows")]
    private SpeechRecognitionEngine? _recognizer;

    public event Action<string>? OnUserSpeechRecognized;
    public event Action<string>? OnGemmiSpeechSpoken;
    public event Action<float[]>? OnAudioWaveformSampled;

    public bool IsListening => _isListening;

    public GemmiVoiceDialoguePipeline(MicrophoneAudioSensor? micSensor, GemmiFacialAnimationEngine facialEngine)
    {
        _micSensor = micSensor;
        _facialEngine = facialEngine;

        if (_micSensor != null && OperatingSystem.IsWindows())
        {
            _micSensor.OnAudioBufferCaptured += HandleMicAudioBuffer;
        }

        if (OperatingSystem.IsWindows())
        {
            InitializeWindowsRecognizer();
        }
    }

    [SupportedOSPlatform("windows")]
    private void InitializeWindowsRecognizer()
    {
        try
        {
            _recognizer = new SpeechRecognitionEngine();
            
            // Build dictation and conversational grammar
            var grammar = new DictationGrammar();
            _recognizer.LoadGrammar(grammar);

            _recognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null && !string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Confidence > 0.35f)
                {
                    Console.WriteLine($"[GemmiVoice STT] Recognized: \"{e.Result.Text}\" (Conf: {e.Result.Confidence:F2})");
                    OnUserSpeechRecognized?.Invoke(e.Result.Text);
                }
            };

            _recognizer.SetInputToDefaultAudioDevice();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GemmiVoice Recognizer Init Notice]: {ex.Message}");
        }
    }

    public void StartListening()
    {
        if (_isListening) return;
        _isListening = true;

        if (OperatingSystem.IsWindows() && _recognizer != null)
        {
            try
            {
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                Console.WriteLine("[GemmiVoice] Live Speech Recognition active & listening on default microphone...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GemmiVoice Listen Notice]: {ex.Message}");
            }
        }
    }

    public void StopListening()
    {
        if (!_isListening) return;
        _isListening = false;

        if (OperatingSystem.IsWindows() && _recognizer != null)
        {
            try
            {
                _recognizer.RecognizeAsyncStop();
            }
            catch { }
        }
    }

    public async Task SpeakAsync(string text, float estimatedDurationSeconds = 2.8f)
    {
        Console.WriteLine($"[GemmiVoice TTS] Speaking: \"{text}\"");
        
        // 1. Trigger Facial Morph Visemes on 3D Avatar
        _facialEngine.StartSpeechAnimation(text, estimatedDurationSeconds);
        OnGemmiSpeechSpoken?.Invoke(text);

        // 2. Synthesize audio output on Windows
        if (OperatingSystem.IsWindows())
        {
            await Task.Run(() =>
            {
                try
                {
                    using var synth = new SpeechSynthesizer();
                    synth.SetOutputToDefaultAudioDevice();
                    synth.SelectVoiceByHints(VoiceGender.Female);
                    synth.Rate = 1;
                    synth.Speak(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GemmiVoice Synth Notice]: {ex.Message}");
                }
            });
        }
    }

    private void HandleMicAudioBuffer(byte[] buffer, int bytesRecorded)
    {
        int validLength = Math.Min(buffer.Length, bytesRecorded > 0 ? bytesRecorded : buffer.Length);

        // Compute 16-band normalized spectrum bands for UI waveform visualizer
        float[] bands = new float[16];
        int chunkSize = Math.Max(1, validLength / 16);
        for (int b = 0; b < 16; b++)
        {
            float sum = 0;
            int start = b * chunkSize;
            int end = Math.Min(validLength, start + chunkSize);
            for (int i = start; i < end; i += 2)
            {
                if (i + 1 < validLength)
                {
                    short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    sum += Math.Abs(sample) / 32768.0f;
                }
            }
            int count = Math.Max(1, (end - start) / 2);
            bands[b] = Math.Clamp((sum / count) * 4.0f, 0.0f, 1.0f);
        }

        OnAudioWaveformSampled?.Invoke(bands);
    }

    public void Dispose()
    {
        StopListening();
        if (OperatingSystem.IsWindows() && _recognizer != null)
        {
            _recognizer.Dispose();
        }
    }
}
