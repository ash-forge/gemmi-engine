using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Hardware;
using Gemmi.Perception;

namespace Gemmi.Diagnostics;

public class GemmiLiveSystemHost
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine(" 🌐 GEMMI ENGINE LIVE HOST: VOICE DIALOGUE & AUTONOMOUS AGENCY ACTIVE     ");
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

        // 2. State & User Command Tracking
        string activeState = "CozyChairListeningMusic";
        string? activeAction = null;
        float currentX = 0.0f;
        float currentZ = 0.0f;
        float walkProgress = 0.0f;

        // 3. Initialize Hardware Sensors
        Console.WriteLine("\n[2] Initializing Microphone & Camera Hardware Sensors...");
        using var micSensor = OperatingSystem.IsWindows() ? new MicrophoneAudioSensor() : null;
        using var camSensor = new CameraVisionSensor(targetFps: 15);

        if (micSensor != null && OperatingSystem.IsWindows() && MicrophoneAudioSensor.GetAvailableMicrophones().Count > 0)
        {
            micSensor.StartRecording(0, 16000, 1);
        }

        camSensor.StartCapture(0, 640, 480);

        // 4. Initialize Core & Perception Engines
        Console.WriteLine("\n[3] Initializing Avatar Locomotion, 3D Audio, Vision, Voice & Autonomy Engines...");
        var avatar = new AvatarStateController();
        var facialEngine = new GemmiFacialAnimationEngine();
        var audioEngine = new GemmiSpatialAudioEngine();
        var collisionEngine = new SpatialCollisionEngine();
        var visionEngine = new GemmiSpatialVisionEngine(collisionEngine);

        using var voicePipeline = new GemmiVoiceDialoguePipeline(micSensor, facialEngine);
        var autonomyEngine = new GemmiAutonomousAgencyEngine(avatar, facialEngine, voicePipeline);

        float[] currentAudioBands = new float[16];
        voicePipeline.OnAudioWaveformSampled += bands => currentAudioBands = bands;

        autonomyEngine.OnLocomotionStateChanged += state =>
        {
            activeState = state;
            Console.WriteLine($"[GemmiAutonomy] Shifted posture to: {activeState}");
        };

        // Listen for client commands
        networkServer.OnClientCommandReceived += (clientId, commandStr) =>
        {
            try
            {
                using var doc = JsonDocument.Parse(commandStr);
                var root = doc.RootElement;
                if (root.TryGetProperty("state", out var stateProp))
                {
                    string newState = stateProp.GetString() ?? "CozyChairListeningMusic";
                    activeState = newState.Equals("walk", StringComparison.OrdinalIgnoreCase) || newState.Contains("walk", StringComparison.OrdinalIgnoreCase)
                        ? "WalkingLocomotion"
                        : "CozyChairListeningMusic";
                    Console.WriteLine($"[GemmiHost] 👤 User commanded locomotion state: {activeState}");
                }

                if (root.TryGetProperty("action", out var actionProp))
                {
                    activeAction = actionProp.GetString();
                    Console.WriteLine($"[GemmiHost] 🎭 User triggered action: {activeAction}");
                    if (activeAction == "wave")
                    {
                        _ = voicePipeline.SpeakAsync("Hello Daniel! Great to see you!", 2.2f);
                    }
                }

                if (root.TryGetProperty("chat", out var chatProp))
                {
                    string userMsg = chatProp.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(userMsg))
                    {
                        _ = autonomyEngine.ProcessUserMessageAsync(userMsg);
                    }
                }

                if (root.TryGetProperty("speak", out var speakProp))
                {
                    string textToSpeak = speakProp.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(textToSpeak))
                    {
                        _ = autonomyEngine.ProcessUserMessageAsync(textToSpeak);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GemmiHost] Command parse error: {ex.Message}");
            }
        };

        var targets = new List<PerceptionTarget>
        {
            new PerceptionTarget { Id = "t1", Label = "Ash Companion", Position = new Vector3(0, 1.5f, -2.0f) },
            new PerceptionTarget { Id = "t2", Label = "Workstation Console", Position = new Vector3(3.0f, 1.0f, -1.0f) }
        };

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine(" 🚀 GEMMI LIVE SYSTEM HOST RUNNING (Voice & Autonomy Mode)");
        Console.WriteLine(" 🌐 WebGL Visualizer Sync : http://localhost:8088/");
        Console.WriteLine(" ⚡ WebSocket Telemetry   : ws://localhost:8088/");
        Console.WriteLine("==========================================================================");

        int frameIdx = 0;

        while (!cts.IsCancellationRequested)
        {
            frameIdx++;

            // Handle user-selected locomotion vs cozy idle
            if (activeState == "WalkingLocomotion")
            {
                walkProgress += 0.03f;
                currentX = MathF.Sin(walkProgress) * 1.5f;
                currentZ = MathF.Cos(walkProgress) * 0.5f;
            }
            else
            {
                // Smoothly return to center (0, 0)
                currentX += (0.0f - currentX) * 0.08f;
                currentZ += (0.0f - currentZ) * 0.08f;
            }

            // Update Autonomy Engine (~every 100ms)
            if (frameIdx % 6 == 0)
            {
                autonomyEngine.Update(0.1f, false, targets.Count);
            }

            // Update Facial Animation & Blinking
            var morphs = facialEngine.Update(0.016f, activeState);

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
                CurrentLocomotionState = activeState,
                CurrentSpeed = activeState == "WalkingLocomotion" ? 1.2f : 0.0f,
                MorphWeights = morphs.ToDictionary(),
                AudioWaveformBands = currentAudioBands,
                RecentThought = autonomyEngine.RecentThoughts.Count > 0 ? autonomyEngine.RecentThoughts[^1] : null,
                CameraPosition = new float[] { currentX, 1.6f, currentZ + 2.8f },
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

            if (frameIdx % 120 == 0)
            {
                Console.WriteLine($"[GemmiHost 60FPS] Frame #{frameIdx} | Pos: ({currentX:F2}m, {currentZ:F2}m) | State: {activeState} | Clients: {networkServer.ConnectedClientCount}");
            }

            await Task.Delay(16, CancellationToken.None); // ~60 FPS
        }

        Console.WriteLine("\n[Stopping Gemmi Live Host...]");
        networkServer.Stop();
        camSensor.StopCapture();
        if (micSensor != null && OperatingSystem.IsWindows()) micSensor.StopRecording();
    }
}
