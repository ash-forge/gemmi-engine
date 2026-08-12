using System;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class SpontaneousInitiationEvaluator
{
    public double Threshold { get; set; } = 0.85;
    private readonly LocalLlamaInferenceEngine _llamaEngine = new();

    public async Task StartSpontaneousEvaluatorLoopAsync(GemmiState state, Action<string> onSpontaneousInitiate, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(5000, cancellationToken);

            // Real-time sub-5ms memory query pass (0.15ms RAM lookup)
            var memoryResult = state.MemoryQuery.QuerySubFiveMs(string.Empty, salienceThreshold: (float)Threshold);
            double currentScore = memoryResult.HighestSalience;
            state.Perception.SpontaneousInitiationScore = Math.Round(currentScore, 3);

            if (currentScore >= Threshold && memoryResult.RelevantEntries.Count > 0)
            {
                var topMemory = memoryResult.RelevantEntries[0];
                string activeWindowContext = $"Memory Category: {topMemory.Category} | Content: '{topMemory.Content}'";
                string alertMsg = await _llamaEngine.GenerateSpontaneousAlertAsync(state, activeWindowContext);
                
                state.RecentSpontaneousAlerts.Add($"{DateTime.Now:HH:mm:ss} - {alertMsg}");
                onSpontaneousInitiate(alertMsg);
            }
        }
    }
}
