using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public enum EvaluatorSensitivityMode
{
    FocusMode = 0,    // Threshold 0.95 (High focus, low interruption)
    BalancedMode = 1, // Threshold 0.85 (Standard ambient initiation)
    CoPilotMode = 2   // Threshold 0.75 (Active collaborative initiation)
}

public class SpontaneousInitiationEvaluator
{
    public EvaluatorSensitivityMode Mode { get; set; } = EvaluatorSensitivityMode.BalancedMode;

    public double CurrentThreshold => Mode switch
    {
        EvaluatorSensitivityMode.FocusMode => 0.95,
        EvaluatorSensitivityMode.CoPilotMode => 0.75,
        _ => 0.85
    };

    private readonly LocalLlamaInferenceEngine _llamaEngine = new();
    private readonly ConcurrentDictionary<string, DateTime> _refractoryCooldowns = new();
    private readonly TimeSpan _cooldownDuration = TimeSpan.FromSeconds(30);

    public async Task StartSpontaneousEvaluatorLoopAsync(GemmiState state, Action<string> onSpontaneousInitiate, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(3000, cancellationToken); // 3-second cognitive evaluation tick

            double targetThreshold = CurrentThreshold;

            // 1. Execute Sub-5ms RAM Memory Query Pass (0.15ms execution)
            var memoryResult = state.MemoryQuery.QuerySubFiveMs(string.Empty, salienceThreshold: (float)targetThreshold);

            // 2. Multimodal Salience Weighting Pass
            double visionWeight = state.Perception.CameraVisionActive ? 0.25 : 0.0;
            double audioWeight = state.Perception.AudioVadActive ? 0.20 : 0.0;
            double memoryWeight = memoryResult.HighestSalience * 0.55;

            double computedSalienceScore = Math.Min(1.0, memoryWeight + visionWeight + audioWeight);
            state.Perception.SpontaneousInitiationScore = Math.Round(computedSalienceScore, 3);

            // 3. Evaluate Threshold & Refractory Cooldown
            if (computedSalienceScore >= targetThreshold && memoryResult.RelevantEntries.Count > 0)
            {
                var topMemory = memoryResult.RelevantEntries[0];
                string memoryContentKey = topMemory.Content;

                // Check Refractory Cooldown (prevent duplicate alerts within 30 seconds)
                if (_refractoryCooldowns.TryGetValue(memoryContentKey, out var lastAlertTime) && (DateTime.UtcNow - lastAlertTime) < _cooldownDuration)
                {
                    continue; // Skip duplicate alert during refractory period
                }

                _refractoryCooldowns[memoryContentKey] = DateTime.UtcNow;

                string activeContext = $"[Multimodal Salience θ={computedSalienceScore:F2}] Category: {topMemory.Category} | {topMemory.Content}";
                string alertMsg = await _llamaEngine.GenerateSpontaneousAlertAsync(state, activeContext);

                state.RecentSpontaneousAlerts.Add($"{DateTime.Now:HH:mm:ss} - {alertMsg}");
                onSpontaneousInitiate(alertMsg);
            }

            // Cleanup expired refractory cooldown entries
            var expiredKeys = _refractoryCooldowns.Where(kv => (DateTime.UtcNow - kv.Value) > _cooldownDuration).Select(kv => kv.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _refractoryCooldowns.TryRemove(key, out _);
            }
        }
    }
}
