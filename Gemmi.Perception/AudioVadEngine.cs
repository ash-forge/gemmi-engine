using System;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class AudioVadEngine
{
    private bool _isListening;

    public bool VoiceActivityDetected { get; private set; }
    public double AmbientNoiseLevelDb { get; private set; } = -45.0;

    public async Task StartVadLoopAsync(GemmiState state, CancellationToken cancellationToken)
    {
        _isListening = true;
        state.Perception.AudioVadActive = true;

        while (_isListening && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(200, cancellationToken);

            // Simulate continuous ambient VAD polling
            var rand = Random.Shared.NextDouble();
            VoiceActivityDetected = rand > 0.85;

            if (VoiceActivityDetected)
            {
                state.Perception.LastObservedContext = "Voice activity detected in room stream";
            }
        }
    }

    public void Stop() => _isListening = false;
}
