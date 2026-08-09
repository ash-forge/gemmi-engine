using System;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class VisionFrameInfo
{
    public int FrameWidth { get; set; } = 1920;
    public int FrameHeight { get; set; } = 1080;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public string FrameSource { get; set; } = "Desktop Monitor 01";
}

public class VisionStreamIngest
{
    private bool _isCapturing;

    public async Task StartVisionLoopAsync(GemmiState state, CancellationToken cancellationToken)
    {
        _isCapturing = true;
        state.Perception.CameraVisionActive = true;

        while (_isCapturing && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(500, cancellationToken); // 2 FPS vision frame sampling
            var frame = new VisionFrameInfo();
            state.WorkingMemoryGraph["LastVisionFrame"] = $"{frame.FrameSource} ({frame.FrameWidth}x{frame.FrameHeight}) at {frame.CapturedAt:HH:mm:ss}";
        }
    }

    public void Stop() => _isCapturing = false;
}
