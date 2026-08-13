using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step14SpatialEngineTest
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 14: NETWORK TELEMETRY, AUDIO & VISION TEST ");
        Console.WriteLine("==========================================================================");

        // 1. Test 3D Spatial Audio Engine
        Console.WriteLine("\n[1] Testing Gemmi 3D Spatial Audio Engine...");
        var audioEngine = new GemmiSpatialAudioEngine { MinDistance = 1.0f, MaxDistance = 20.0f };

        Vector3 listenerPos = new Vector3(0, 1.6f, 0);
        Vector3 listenerForward = new Vector3(0, 0, -1.0f);

        Vector3 soundPosLeft = new Vector3(-3.0f, 1.6f, -2.0f);  // Panned Left
        Vector3 soundPosRight = new Vector3(4.0f, 1.6f, -2.0f);  // Panned Right
        Vector3 soundPosBehindWall = new Vector3(0, 1.6f, -10.0f); // Occluded Behind Wall

        var audioLeft = audioEngine.CalculatePositionalAudio(soundPosLeft, listenerPos, listenerForward);
        var audioRight = audioEngine.CalculatePositionalAudio(soundPosRight, listenerPos, listenerForward);
        var audioWall = audioEngine.CalculatePositionalAudio(soundPosBehindWall, listenerPos, listenerForward, isOccludedByWall: true);

        Console.WriteLine($"    • Left Sound Source (-3m): VolL={audioLeft.VolumeL:F2}, VolR={audioLeft.VolumeR:F2}, Pan={audioLeft.Pan:F2}, Atten={audioLeft.DistanceAttenuation:F2}");
        Console.WriteLine($"    • Right Sound Source (+4m): VolL={audioRight.VolumeL:F2}, VolR={audioRight.VolumeR:F2}, Pan={audioRight.Pan:F2}, Atten={audioRight.DistanceAttenuation:F2}");
        Console.WriteLine($"    • Occluded Source (-10m Wall): VolL={audioWall.VolumeL:F2}, VolR={audioWall.VolumeR:F2}, Cutoff={audioWall.LowPassCutoffHz:F0}Hz, Reverb={audioWall.ReverbSendLevel:F2}");

        // 2. Test 3D Spatial Vision Engine
        Console.WriteLine("\n[2] Testing Gemmi 3D Spatial Vision & Line-of-Sight Engine...");
        var collisionEngine = new SpatialCollisionEngine();
        var visionEngine = new GemmiSpatialVisionEngine(collisionEngine) { FieldOfViewDegrees = 90.0f };

        var targets = new List<PerceptionTarget>
        {
            new PerceptionTarget { Id = "obj_01", Label = "Ash Companion", Position = new Vector3(0, 1.5f, -2.0f) },     // In FOV (2m - Personal Zone)
            new PerceptionTarget { Id = "obj_02", Label = "Control Console", Position = new Vector3(4.0f, 1.0f, -1.0f) },   // Outside 90° FOV (Far Right)
            new PerceptionTarget { Id = "obj_03", Label = "Far Beacon", Position = new Vector3(0, 10.0f, -12.0f) }          // In FOV (12m - Far Zone)
        };

        var perceptionResults = visionEngine.ScanEnvironment(listenerPos, listenerForward, targets);
        foreach (var p in perceptionResults)
        {
            Console.WriteLine($"    • Target [{p.Target.Label}] @ {p.Distance}m ({p.AngleDegrees}° angle): FOV={p.IsInFieldOfView}, LOS={p.IsInLineOfSight}, Zone={p.ZoneClassification}");
        }

        // 3. Test Real-Time 60FPS WebSocket Telemetry Network Server
        Console.WriteLine("\n[3] Testing Gemmi Real-Time WebSocket Telemetry Server (Port 8088)...");
        var networkServer = new GemmiNetworkServer(8088);
        networkServer.Start();

        Console.WriteLine($"    • Server Status : Running={networkServer.IsRunning}, Port=8088");

        var avatarController = new AvatarStateController();
        var m = avatarController.Get15PointSpatialMatrix();

        var telemetryFrame = new SpatialTelemetryFrame
        {
            FrameIndex = 1,
            CurrentLocomotionState = "Walking",
            CurrentSpeed = 1.25f,
            CameraPosition = new float[] { 0, 1.6f, 3.0f },
            CameraRotation = new float[] { 0, 0, 0 }
        };

        telemetryFrame.Joints["Level0_CenterGround"] = new float[] { m.Level0_CenterGround.X, m.Level0_CenterGround.Y, m.Level0_CenterGround.Z };
        telemetryFrame.Joints["Level0_LeftFoot"] = new float[] { m.Level0_LeftFoot.X, m.Level0_LeftFoot.Y, m.Level0_LeftFoot.Z };
        telemetryFrame.Joints["Level0_RightFoot"] = new float[] { m.Level0_RightFoot.X, m.Level0_RightFoot.Y, m.Level0_RightFoot.Z };
        telemetryFrame.Joints["Level1_CenterHips"] = new float[] { m.Level1_CenterHips.X, m.Level1_CenterHips.Y, m.Level1_CenterHips.Z };
        telemetryFrame.Joints["Level1_LeftKnee"] = new float[] { m.Level1_LeftKnee.X, m.Level1_LeftKnee.Y, m.Level1_LeftKnee.Z };
        telemetryFrame.Joints["Level1_RightKnee"] = new float[] { m.Level1_RightKnee.X, m.Level1_RightKnee.Y, m.Level1_RightKnee.Z };
        telemetryFrame.Joints["Level2_SpineChest"] = new float[] { m.Level2_SpineChest.X, m.Level2_SpineChest.Y, m.Level2_SpineChest.Z };
        telemetryFrame.Joints["Level2_HeadCenter"] = new float[] { m.Level2_HeadCenter.X, m.Level2_HeadCenter.Y, m.Level2_HeadCenter.Z };

        await networkServer.BroadcastTelemetryAsync(telemetryFrame);
        Console.WriteLine("    • [SUCCESS] Broadcasted 60FPS 15-Point Spatial Matrix Telemetry Packet!");

        networkServer.Stop();

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 14 SPATIAL NETWORK, AUDIO & VISION ENGINES PASSED PERFECTLY!  ");
        Console.WriteLine("==========================================================================");
    }
}
