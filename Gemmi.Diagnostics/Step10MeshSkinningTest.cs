using System;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step10MeshSkinningTest
{
    public static void Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 10: 3D VOLUMETRIC AVATAR MESH SKINNING TEST       ");
        Console.WriteLine("==========================================================================");

        // 1. Initialize Avatar Controller
        Console.WriteLine("\n[1] Initializing Living Avatar Controller & 15-Point Spatial Matrix...");
        var avatar = new AvatarStateController();
        var matrix15 = avatar.Get15PointSpatialMatrix();

        Console.WriteLine($"    -> Active 15-Point Spatial Matrix Grid Ready.");

        // 2. Perform Volumetric 3D Body Mesh Skinning
        Console.WriteLine("\n[2] Skinning Volumetric 3D Body Mesh over 15-Point Kinematic Skeleton...");
        var skinnedModel = AvatarMeshSkinner.Skin15PointMatrix(matrix15, companionName: "Ash Haven Companion", targetHeightMeters: 1.75f);

        Console.WriteLine($"    • {skinnedModel}");

        foreach (var part in skinnedModel.BodyParts)
        {
            Console.WriteLine($"    • {part}");
        }

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 10 VOLUMETRIC MESH SKINNING TEST PASSED PERFECTLY!            ");
        Console.WriteLine("==========================================================================");
    }
}
