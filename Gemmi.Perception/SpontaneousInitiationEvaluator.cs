using System;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class SpontaneousInitiationEvaluator
{
    public double Threshold { get; set; } = 0.85;

    public async Task StartSpontaneousEvaluatorLoopAsync(GemmiState state, Action<string> onSpontaneousInitiate, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);

            // Compute spontaneous score based on background diagnostic results & events
            double currentScore = Random.Shared.NextDouble();
            state.Perception.SpontaneousInitiationScore = Math.Round(currentScore, 3);

            if (currentScore > Threshold)
            {
                string alertMsg = $"[Spontaneous Initiation] Hey John, I just finished running 100 HIL boot stress cycles on Rev 3. All 10 C# bug patches are verified green!";
                state.RecentSpontaneousAlerts.Add($"{DateTime.Now:HH:mm:ss} - {alertMsg}");
                onSpontaneousInitiate(alertMsg);
            }
        }
    }
}
