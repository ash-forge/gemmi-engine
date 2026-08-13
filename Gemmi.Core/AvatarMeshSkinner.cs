using System;
using System.Collections.Generic;

namespace Gemmi.Core;

public class AvatarMeshSkinner
{
    public struct VolumetricBodyPartMesh
    {
        public string PartName { get; set; }
        public JointTransform3D CenterPos { get; set; }
        public float RadiusMeters { get; set; }
        public float HeightMeters { get; set; }
        public (byte R, byte G, byte B) ColorRgb { get; set; }

        public override string ToString() =>
            $"[BODY MESH PART] {PartName,-15} | Pos: {CenterPos} | Radius: {RadiusMeters:F2}m | Height: {HeightMeters:F2}m";
    }

    public struct SkinnedAvatarModel
    {
        public string CompanionName { get; set; }
        public float ModelScaleHeight { get; set; }
        public List<VolumetricBodyPartMesh> BodyParts { get; set; }

        public override string ToString() =>
            $"[SKINNED AVATAR MODEL] Companion: {CompanionName} | Height: {ModelScaleHeight:F2}m | Total Mesh Parts: {BodyParts.Count}";
    }

    // Skins a volumetric 3D body mesh over the 15-Point Spatial Matrix
    public static SkinnedAvatarModel Skin15PointMatrix(FifteenPointSpatialMatrix3D matrix, string companionName = "Ash / Haven", float targetHeightMeters = 1.75f)
    {
        var parts = new List<VolumetricBodyPartMesh>();

        // 1. Head & Crown Zenith Mesh (Sphere/Visor)
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Head & Face Visor",
            CenterPos = matrix.Level2_HeadCenter,
            RadiusMeters = 0.12f,
            HeightMeters = 0.25f,
            ColorRgb = (243, 85, 218) // Glowing Magenta
        });

        // 2. Spine & Chest Upper Torso (Volumetric Box)
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Chest Upper Torso",
            CenterPos = matrix.Level2_SpineChest,
            RadiusMeters = 0.20f,
            HeightMeters = 0.35f,
            ColorRgb = (0, 242, 254) // Cyan Armor
        });

        // 3. Pelvis & Hips Center of Mass (Lower Torso)
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Hips Pelvis Mesh",
            CenterPos = matrix.Level1_CenterHips,
            RadiusMeters = 0.18f,
            HeightMeters = 0.25f,
            ColorRgb = (112, 0, 255) // Deep Purple
        });

        // 4. Left/Right Shoulder Armor Caps
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Left Shoulder Cap",
            CenterPos = matrix.Level2_LeftShoulder,
            RadiusMeters = 0.08f,
            HeightMeters = 0.12f,
            ColorRgb = (79, 172, 254)
        });

        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Right Shoulder Cap",
            CenterPos = matrix.Level2_RightShoulder,
            RadiusMeters = 0.08f,
            HeightMeters = 0.12f,
            ColorRgb = (79, 172, 254)
        });

        // 5. Left/Right Thigh & Leg Cylinders
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Left Thigh Mesh",
            CenterPos = matrix.Level1_LeftKnee,
            RadiusMeters = 0.09f,
            HeightMeters = 0.40f,
            ColorRgb = (0, 245, 212)
        });

        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Right Thigh Mesh",
            CenterPos = matrix.Level1_RightKnee,
            RadiusMeters = 0.09f,
            HeightMeters = 0.40f,
            ColorRgb = (0, 245, 212)
        });

        // 6. Left/Right Feet Contact Ground Bases
        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Left Foot Base",
            CenterPos = matrix.Level0_LeftFoot,
            RadiusMeters = 0.07f,
            HeightMeters = 0.08f,
            ColorRgb = (255, 183, 3)
        });

        parts.Add(new VolumetricBodyPartMesh
        {
            PartName = "Right Foot Base",
            CenterPos = matrix.Level0_RightFoot,
            RadiusMeters = 0.07f,
            HeightMeters = 0.08f,
            ColorRgb = (255, 183, 3)
        });

        return new SkinnedAvatarModel
        {
            CompanionName = companionName,
            ModelScaleHeight = targetHeightMeters,
            BodyParts = parts
        };
    }
}
