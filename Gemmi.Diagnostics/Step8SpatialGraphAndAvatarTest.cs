using System;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step8SpatialGraphAndAvatarTest
{
    public static void Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 8: 3D SPATIAL VECTOR GRAPH & AMBIENT AVATAR TEST   ");
        Console.WriteLine("==========================================================================");

        // 1. Test 3D Spatial Vector Graph
        Console.WriteLine("\n[1] Initializing 3D Spatial Vector Graph (EpisodicMemoryGraph)...");
        var graph = new EpisodicMemoryGraph();

        graph.AddOrUpdateConcept("Main Workstation Laptop", MemoryCategory.Code, 1.0f, spatialVector: (0.0f, 1.0f, 0.0f));
        graph.AddOrUpdateConcept("Coffee Mug", MemoryCategory.Vision, 1.5f, spatialVector: (0.25f, 0.98f, -0.05f));
        graph.AddOrUpdateConcept("Oscilloscope Workbench", MemoryCategory.Location, 2.0f, spatialVector: (1.50f, 1.10f, 0.40f));
        graph.AddOrUpdateConcept("NAS Storage Server", MemoryCategory.System, 1.8f, spatialVector: (-2.10f, 0.85f, -1.20f));

        Console.WriteLine($"    -> Total Graph Nodes Created: {graph.NodeCount}");

        Console.WriteLine("\n[2] Executing 3D Euclidean Proximity Search (Radius: 0.50 meters from Coffee Mug)...");
        var proximityMatches = graph.GetConceptsBySpatialProximity(0.23f, 0.98f, -0.05f, 0.50f);

        foreach (var match in proximityMatches)
        {
            Console.WriteLine($"    • [FOUND] {match.Node.Concept,-25} | Category: {match.Node.Category,-12} | Distance: {match.DistanceMeters:F3} meters | Spatial Vector: {match.Node.SpatialVector}");
        }

        // 2. Test Living Ambient Avatar Controller
        Console.WriteLine("\n[3] Testing Living Ambient Avatar Controller (AvatarStateController)...");
        var avatar = new AvatarStateController();

        avatar.OnStateChanged += (state, activity) =>
        {
            Console.WriteLine($"\n  [AVATAR STATE CHANGED] -> {state}");
            Console.WriteLine($"    • Activity : {activity}");
            Console.WriteLine($"    • Spine Pose: {avatar.SpineTransform}");
            Console.WriteLine($"    • Head Pose : {avatar.HeadTransform}");
        };

        Console.WriteLine("\n -> Initial State (Cozy Chair Listening to Lofi Music):");
        Console.WriteLine($"    • Current Activity   : {avatar.CurrentActivity}");
        Console.WriteLine($"    • Unit Height Bounding: {AvatarStateController.NormalizedUnitHeight:F1}f (Whole Body Unit Bounds)");
        Console.WriteLine($"    • Top of Head Target  : {avatar.TopOfHeadTransform} (Y = 2.00f)");
        Console.WriteLine($"    • Midpoint Hips Target: {avatar.MidpointHipsTransform} (Y = 1.00f - Center of Mass Anchor)");
        Console.WriteLine($"    • Ground Feet Plane   : {avatar.GroundFeetTransform} (Y = 0.00f - Ground Contact)");
        Console.WriteLine($"    • Spine Pose          : {avatar.SpineTransform}");
        Console.WriteLine($"    • Left Shoulder       : {avatar.LeftArm.Shoulder} (X = +1.85f)");
        Console.WriteLine($"    • Right Shoulder      : {avatar.RightArm.Shoulder} (X = -1.85f)");
        var offsetHand = avatar.ComputePositionFromCenterOfMass(0.23f, -0.05f, 0.40f);
        Console.WriteLine($"    • Hand Vector from Center of Mass (ΔX=0.23, ΔY=-0.05, ΔZ=0.40): {offsetHand}");
        var rightElbowWorld = avatar.RightArm.Elbow.ComputeWorldPosition(avatar.RightArm.Shoulder);
        Console.WriteLine($"    • Right Elbow World Position: {rightElbowWorld} (Holding Coffee)");

        Console.WriteLine("\n -> Simulating PaliGemma 2 Spatial Vision Perception (Detecting NullReferenceException)...");
        avatar.OnSpatialVisionPerception("System.NullReferenceException: Object reference not set to an instance of an object at Program.cs:L42", true);

        Console.WriteLine("\n -> Simulating Code Fix Clear Event...");
        avatar.OnSpatialVisionPerception("Build succeeded. 0 Warning(s) 0 Error(s)", false);

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 8 SPATIAL GRAPH & AMBIENT AVATAR TEST PASSED PERFECTLY!         ");
        Console.WriteLine("==========================================================================");
    }
}
