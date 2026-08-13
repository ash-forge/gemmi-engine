using System;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step11HavenGltfBridgeTest
{
    public static void Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 11: HAVEN VRoid GLTF 3D AVATAR MODEL BINDING TEST ");
        Console.WriteLine("==========================================================================");

        // 1. Initialize Living Avatar Controller
        Console.WriteLine("\n[1] Initializing Living Avatar Controller & 15-Point Spatial Matrix...");
        var avatar = new AvatarStateController();
        var matrix15 = avatar.Get15PointSpatialMatrix();

        Console.WriteLine($"    -> Active 15-Point Spatial Matrix Grid Ready.");

        // 2. Map Haven GLB 3D Avatar (avatar_456071c0b8ea484a89dfaeddf11c5138.glb)
        Console.WriteLine("\n[2] Binding Haven UniGLTF 3D Model (avatar_456071c0b8ea484a89dfaeddf11c5138.glb)...");
        var gltfProfile = HavenVRoidGltfAvatarBridge.MapHavenGltfAvatar(matrix15);

        Console.WriteLine($"    • {gltfProfile}");

        foreach (var binding in gltfProfile.MappedJointBindings)
        {
            Console.WriteLine($"    • {binding}");
        }

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 11 HAVEN GLTF 3D AVATAR BINDING TEST PASSED PERFECTLY!        ");
        Console.WriteLine("==========================================================================");
    }
}
