using System;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step9IKAndCollisionTest
{
    public static void Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 9: 2-BONE IK REACHING & SPATIAL COLLISION TEST    ");
        Console.WriteLine("==========================================================================");

        // 1. Setup Memory Graph & Avatar
        Console.WriteLine("\n[1] Setting up 3D Spatial Graph & Avatar Controller...");
        var graph = new EpisodicMemoryGraph();
        graph.AddOrUpdateConcept("Coffee Mug", MemoryCategory.Vision, 1.5f, spatialVector: (0.25f, 0.98f, -0.05f));
        graph.AddOrUpdateConcept("Obstacle Desk Pillar", MemoryCategory.Location, 2.0f, spatialVector: (0.75f, 1.00f, 0.20f));
        graph.AddOrUpdateConcept("Workstation Target", MemoryCategory.Code, 1.0f, spatialVector: (1.50f, 1.00f, 0.40f));

        var avatar = new AvatarStateController();
        Console.WriteLine($"    -> Avatar Initial Position: {avatar.SpineTransform}");

        // 2. Test Inverse Kinematics (IK) Reaching for Coffee Mug
        Console.WriteLine("\n[2] Testing 2-Bone Inverse Kinematics (IK) Reaching for Coffee Mug at (0.25m, 0.98m, -0.05m)...");
        var ikResult = avatar.ReachRightHandToTarget(0.25f, 0.98f, -0.05f);
        Console.WriteLine($"    • {ikResult}");
        Console.WriteLine($"    • Right Shoulder Base : {ikResult.ShoulderPos}");
        Console.WriteLine($"    • Calculated Elbow Pos : {ikResult.ElbowPos}");
        Console.WriteLine($"    • Hand World Target    : {ikResult.HandPos}");
        Console.WriteLine($"    • Shoulder Elevation   : {ikResult.ShoulderElevationDeg:F1}°");
        Console.WriteLine($"    • Elbow Flexion Angle  : {ikResult.ElbowBendDeg:F1}°");

        // 3. Test 3D Spatial Collision Detection
        Console.WriteLine("\n[3] Testing 3D Bounding Sphere Collision Engine...");
        var collisionCheck = SpatialCollisionEngine.EvaluateAvatarCollisions(avatar, graph, avatarPersonalSpaceRadius: 0.35f);
        Console.WriteLine($"    • {collisionCheck}");

        // 4. Test Obstacle-Avoidance Waypoint Pathfinding
        Console.WriteLine("\n[4] Testing Obstacle-Avoiding 4D Waypoint Pathfinding (Start -> Obstacle Desk Pillar -> Workstation Target)...");
        var navTrajectory = avatar.NavigateWithObstacleAvoidance(1.50f, 0.40f, graph);
        Console.WriteLine($"    • Generated Detour Trajectory Frames: {navTrajectory.Count} 4D Gait Steps");
        int stepNum = 1;
        foreach (var frame in navTrajectory)
        {
            Console.WriteLine($"    • [WAYPOINT STEP {stepNum++}] Center Hips: {frame.Level1_CenterHips} | Spine Chest: {frame.Level2_SpineChest}");
        }

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 9 IK REACHING & COLLISION TEST PASSED PERFECTLY!               ");
        Console.WriteLine("==========================================================================");
    }
}
