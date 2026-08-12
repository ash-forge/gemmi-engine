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
    private readonly HashSet<string> _seenObservationHashes = new();
    private readonly TimeSpan _cooldownDuration = TimeSpan.FromSeconds(30);

    public async Task StartSpontaneousEvaluatorLoopAsync(GemmiState state, Action<string> onSpontaneousInitiate, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken); // 1-second cognitive evaluation tick

            double targetThreshold = CurrentThreshold;

            // 1. Execute Sub-5ms RAM Memory Query Pass (0.15ms execution)
            var memoryResult = state.MemoryQuery.QuerySubFiveMs(string.Empty, salienceThreshold: 0.0f);

            // 2. Multimodal Salience Weighting Pass
            double visionWeight = state.Perception.CameraVisionActive ? 0.15 : 0.0;
            double audioWeight = state.Perception.AudioVadActive ? 0.10 : 0.0;
            double memoryWeight = memoryResult.HighestSalience;

            // Dopamine Novelty Spike (+0.30 boost if top memory is brand new)
            double dopamineBoost = 0.0;
            if (memoryResult.RelevantEntries.Count > 0)
            {
                var topEntry = memoryResult.RelevantEntries[0];
                if (_seenObservationHashes.Add(topEntry.Content))
                {
                    dopamineBoost = 0.30; // Brand new observation trigger!
                }
            }

            // 3. Stochastic Associative Graph Leap ("Oh Shiny!" Random Memory Walk)
            bool isOhShinyLeap = false;
            string ohShinyContext = string.Empty;

            if (Random.Shared.NextDouble() < 0.15 && memoryResult.RelevantEntries.Count > 0)
            {
                var seedMemory = memoryResult.RelevantEntries[Random.Shared.Next(memoryResult.RelevantEntries.Count)];
                var relatedNodes = state.MemoryGraph.GetRelatedConcepts(seedMemory.Content);

                if (relatedNodes.Count > 0)
                {
                    var shinyNode = relatedNodes[Random.Shared.Next(relatedNodes.Count)];
                    isOhShinyLeap = true;
                    ohShinyContext = $"[✨ 'OH SHINY!' ASSOCIATIVE LEAP] Connected '{seedMemory.Content}' -> Distant Graph Concept '{shinyNode.Concept}' ({shinyNode.Category})";
                    dopamineBoost += 0.40; // Associative jump boost!
                }
            }

            double computedSalienceScore = Math.Min(1.0, memoryWeight + visionWeight + audioWeight + dopamineBoost);
            state.Perception.SpontaneousInitiationScore = Math.Round(computedSalienceScore, 3);

            // 4. Evaluate Threshold & Refractory Cooldown
            if (computedSalienceScore >= targetThreshold && memoryResult.RelevantEntries.Count > 0)
            {
                var topMemory = memoryResult.RelevantEntries[0];
                string memoryContentKey = isOhShinyLeap ? ohShinyContext : topMemory.Content;

                // Check Refractory Cooldown (prevent duplicate alerts within 30 seconds)
                if (_refractoryCooldowns.TryGetValue(memoryContentKey, out var lastAlertTime) && (DateTime.UtcNow - lastAlertTime) < _cooldownDuration)
                {
                    continue; // Skip duplicate alert during refractory period
                }

                _refractoryCooldowns[memoryContentKey] = DateTime.UtcNow;

                string activeContext = isOhShinyLeap 
                    ? $"{ohShinyContext} (θ={computedSalienceScore:F2})" 
                    : $"[Multimodal Salience θ={computedSalienceScore:F2}] Category: {topMemory.Category} | {topMemory.Content}";

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
