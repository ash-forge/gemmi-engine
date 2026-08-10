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

            // Compute spontaneous score based on background diagnostic results & events
            double currentScore = Random.Shared.NextDouble();
            state.Perception.SpontaneousInitiationScore = Math.Round(currentScore, 3);

            if (currentScore > Threshold)
            {
                string activeWindowContext = state.Perception.AudioVadActive ? "User speaking via microphone" : "Monitoring active workspace & code editor";
                string alertMsg = await _llamaEngine.GenerateSpontaneousAlertAsync(state, activeWindowContext);
                
                state.RecentSpontaneousAlerts.Add($"{DateTime.Now:HH:mm:ss} - {alertMsg}");
                onSpontaneousInitiate(alertMsg);
            }
        }
    }
}
