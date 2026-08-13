using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Hardware;

namespace Gemmi.Diagnostics;

public class GemmiLiveSystemHost
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine(" 🌐 GEMMI ENGINE LIVE SYSTEM HOST SERVER (60FPS TELEMETRY & HARDWARE)     ");
        Console.WriteLine("==========================================================================");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        // 1. Initialize Network Telemetry Server
        Console.WriteLine("\n[1] Starting Gemmi 60FPS WebSocket Telemetry Server (Port 8088)...");
        var networkServer = new GemmiNetworkServer(8088);
        networkServer.Start();

        // 2. Initialize Hardware Sensors
        Console.WriteLine("\n[2] Initializing Microphone & Camera Hardware Sensors...");
        using var micSensor = OperatingSystem.IsWindows() ? new MicrophoneAudioSensor() : null;
        using var camSensor = new CameraVisionSensor(targetFps: 15);

        if (micSensor != null && OperatingSystem.IsWindows() && MicrophoneAudioSensor.GetAvailableMicrophones().Count > 0)
        {
            micSensor.StartRecording(0, 16000, 1);
        }

        camSensor.StartCapture(0, 640, 480);

        // 3. Initialize Core Engines
        Console.WriteLine("\n[3] Initializing Avatar Locomotion, 3D Audio & Vision Engines...");
        var avatar = new AvatarStateController();
        var audioEngine = new GemmiSpatialAudioEngine();
        var collisionEngine = new SpatialCollisionEngine();
        var visionEngine = new GemmiSpatialVisionEngine(collisionEngine);

        var targets = new List<PerceptionTarget>
        {
            new PerceptionTarget { Id = "t1", Label = "Ash Companion", Position = new Vector3(0, 1.5f, -2.0f) },
            new PerceptionTarget { Id = "t2", Label = "Workstation Console", Position = new Vector3(3.0f, 1.0f, -1.0f) }
        };

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine(" 🚀 GEMMI LIVE SYSTEM HOST RUNNING (Press Ctrl+C to Stop)");
        Console.WriteLine(" 🌐 WebGL Visualizer Sync : file:///C:/Users/admin/source/gemmi-engine/gemmi_4d_avatar_visualizer.html");
        Console.WriteLine(" ⚡ WebSocket Telemetry   : ws://localhost:8088/");
        Console.WriteLine("==========================================================================");

        int frameIdx = 0;
        float walkProgress = 0.0f;

        while (!cts.IsCancellationRequested)
        {
            frameIdx++;
            walkProgress += 0.03f;
            float currentX = MathF.Sin(walkProgress) * 1.5f;
            float currentZ = MathF.Cos(walkProgress) * 0.5f;

            string stateStr = MathF.Abs(MathF.Sin(walkProgress)) > 0.3f ? "WalkingLocomotion" : "CozyChairListeningMusic";

            // Get 15-Point Spatial Matrix
            avatar.SpineTransform.X = currentX;
            avatar.SpineTransform.Z = currentZ;
            var matrix = avatar.Get15PointSpatialMatrix();

            // Perform Positional Audio & Vision Raycast Sweep
            Vector3 listenerPos = new Vector3(currentX, 1.6f, currentZ);
            Vector3 listenerFwd = new Vector3(0, 0, -1.0f);
            var visionResults = visionEngine.ScanEnvironment(listenerPos, listenerFwd, targets);

            // Construct 60FPS Spatial Telemetry Frame
            var telemetryFrame = new SpatialTelemetryFrame
            {
                FrameIndex = frameIdx,
                CurrentLocomotionState = stateStr,
                CurrentSpeed = 1.2f,
                CameraPosition = new float[] { currentX, 1.6f, currentZ + 3.0f },
                CameraRotation = new float[] { 0, 0, 0 }
            };

            telemetryFrame.Joints["Level0_CenterGround"] = new float[] { matrix.Level0_CenterGround.X, matrix.Level0_CenterGround.Y, matrix.Level0_CenterGround.Z };
            telemetryFrame.Joints["Level1_CenterHips"] = new float[] { matrix.Level1_CenterHips.X, matrix.Level1_CenterHips.Y, matrix.Level1_CenterHips.Z };
            telemetryFrame.Joints["Level2_SpineChest"] = new float[] { matrix.Level2_SpineChest.X, matrix.Level2_SpineChest.Y, matrix.Level2_SpineChest.Z };
            telemetryFrame.Joints["Level2_HeadCenter"] = new float[] { matrix.Level2_HeadCenter.X, matrix.Level2_HeadCenter.Y, matrix.Level2_HeadCenter.Z };

            foreach (var vr in visionResults)
            {
                telemetryFrame.VisibleObjects.Add(new SpatialObservedObject
                {
                    Id = vr.Target.Id,
                    Label = vr.Target.Label,
                    Position = new float[] { vr.Target.Position.X, vr.Target.Position.Y, vr.Target.Position.Z },
                    Distance = vr.Distance,
                    IsInLineOfSight = vr.IsInLineOfSight,
                    ZoneClassification = vr.ZoneClassification
                });
            }

            await networkServer.BroadcastTelemetryAsync(telemetryFrame);

            if (frameIdx % 60 == 0)
            {
                Console.WriteLine($"[GemmiHost 60FPS] Frame #{frameIdx} | Pos: ({currentX:F2}m, {currentZ:F2}m) | State: {stateStr} | Clients: {networkServer.ConnectedClientCount}");
            }

            await Task.Delay(16, CancellationToken.None); // ~60 FPS
        }

        Console.WriteLine("\n[Stopping Gemmi Live Host...]");
        networkServer.Stop();
        camSensor.StopCapture();
        if (micSensor != null && OperatingSystem.IsWindows()) micSensor.StopRecording();
    }
}
