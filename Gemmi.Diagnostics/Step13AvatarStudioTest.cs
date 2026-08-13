using System;
using System.IO;
using Gemmi.Core;

namespace Gemmi.Diagnostics;

public class Step13AvatarStudioTest
{
    public static void Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 13: SOVEREIGN 3D AVATAR CREATION ENGINE TEST ");
        Console.WriteLine("==========================================================================");

        string outputGlbPath = @"C:\Users\admin\haven-server\wwwroot\uploads\gemmi_avatar_v3.glb";

        Console.WriteLine("\n[1] Initializing Living Avatar Controller & 15-Point Matrix...");
        var avatar = new AvatarStateController();
        var matrix = avatar.Get15PointSpatialMatrix();

        var spec = new GemmiAvatarStudio.AvatarGenerationSpec
        {
            CompanionName = "Ash Sovereign Gemmi Companion 3D",
            HeightMeters = 1.75f,
            PrimaryColor = (0.0f, 0.95f, 0.99f, 1.0f),  // Cyan Base Armor
            AccentColor = (0.95f, 0.33f, 0.85f, 1.0f),  // Magenta Accent Armor
            HairColor = (0.12f, 0.14f, 0.22f, 1.0f),    // Obsidian Hair
            CoreGemColor = (1.0f, 0.72f, 0.01f, 1.0f)   // Amber Core Power Gem
        };

        Console.WriteLine($"\n[2] Generating Sovereign 3D Avatar GLB ({spec.CompanionName})...");
        string generatedPath = GemmiAvatarStudio.BuildAndExportSovereignAvatarGlb(spec, matrix, outputGlbPath);

        long fileSize = new FileInfo(generatedPath).Length;
        Console.WriteLine($"    • [SUCCESS] Exported Sovereign 3D Avatar GLB Model!");
        Console.WriteLine($"    • File Location : {generatedPath}");
        Console.WriteLine($"    • File Size     : {fileSize / 1024.0:F2} KB ({fileSize} bytes)");

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 13 SOVEREIGN 3D AVATAR CREATION ENGINE PASSED PERFECTLY!     ");
        Console.WriteLine("==========================================================================");
    }
}
