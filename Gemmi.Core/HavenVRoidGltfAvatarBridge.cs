using System;
using System.Collections.Generic;

namespace Gemmi.Core;

public class HavenVRoidGltfAvatarBridge
{
    public struct GltfJointBinding
    {
        public string VRoidBoneName { get; set; }
        public int NodeIndex { get; set; }
        public (float X, float Y, float Z) TargetSpatialMatrixVector { get; set; }

        public override string ToString() => $"[GLTF BONE BINDING] Bone: {VRoidBoneName,-20} | NodeId: {NodeIndex:2d} | Vector: ({TargetSpatialMatrixVector.X:F2}, {TargetSpatialMatrixVector.Y:F2}, {TargetSpatialMatrixVector.Z:F2})";
    }

    public struct HavenGltfSkinningProfile
    {
        public string ModelFilename { get; set; }
        public int TotalNodes { get; set; }
        public int TotalSkins { get; set; }
        public List<GltfJointBinding> MappedJointBindings { get; set; }

        public override string ToString() => $"[HAVEN GLTF PROFILE] File: {ModelFilename} | Total Nodes: {TotalNodes} | Mapped 15-Point Matrix Joints: {MappedJointBindings.Count}";
    }

    // Maps 15-Point Spatial Matrix & 12-Point Spatial Anchors to UniGLTF / VRoid Humanoid Skeleton
    public static HavenGltfSkinningProfile MapHavenGltfAvatar(FifteenPointSpatialMatrix3D spatialMatrix, string glbPath = @"C:\Users\admin\haven-server\wwwroot\uploads\avatar_456071c0b8ea484a89dfaeddf11c5138.glb")
    {
        var bindings = new List<GltfJointBinding>
        {
            // Level 0: Ground Plane FP = 0.0f
            new GltfJointBinding { VRoidBoneName = "Root", NodeIndex = 13, TargetSpatialMatrixVector = (spatialMatrix.Level0_CenterGround.X, spatialMatrix.Level0_CenterGround.Y, spatialMatrix.Level0_CenterGround.Z) },
            new GltfJointBinding { VRoidBoneName = "LeftFoot", NodeIndex = 17, TargetSpatialMatrixVector = (spatialMatrix.Level0_LeftFoot.X, spatialMatrix.Level0_LeftFoot.Y, spatialMatrix.Level0_LeftFoot.Z) },
            new GltfJointBinding { VRoidBoneName = "RightFoot", NodeIndex = 22, TargetSpatialMatrixVector = (spatialMatrix.Level0_RightFoot.X, spatialMatrix.Level0_RightFoot.Y, spatialMatrix.Level0_RightFoot.Z) },

            // Level 1: Hips & Knees Center FP = 1.0f
            new GltfJointBinding { VRoidBoneName = "Hips", NodeIndex = 14, TargetSpatialMatrixVector = (spatialMatrix.Level1_CenterHips.X, spatialMatrix.Level1_CenterHips.Y, spatialMatrix.Level1_CenterHips.Z) },
            new GltfJointBinding { VRoidBoneName = "LeftLeg (Knee)", NodeIndex = 16, TargetSpatialMatrixVector = (spatialMatrix.Level1_LeftKnee.X, spatialMatrix.Level1_LeftKnee.Y, spatialMatrix.Level1_LeftKnee.Z) },
            new GltfJointBinding { VRoidBoneName = "RightLeg (Knee)", NodeIndex = 21, TargetSpatialMatrixVector = (spatialMatrix.Level1_RightKnee.X, spatialMatrix.Level1_RightKnee.Y, spatialMatrix.Level1_RightKnee.Z) },
            new GltfJointBinding { VRoidBoneName = "LeftUpLeg (Hip)", NodeIndex = 15, TargetSpatialMatrixVector = (spatialMatrix.Level1_LeftHip.X, spatialMatrix.Level1_LeftHip.Y, spatialMatrix.Level1_LeftHip.Z) },
            new GltfJointBinding { VRoidBoneName = "RightUpLeg (Hip)", NodeIndex = 20, TargetSpatialMatrixVector = (spatialMatrix.Level1_RightHip.X, spatialMatrix.Level1_RightHip.Y, spatialMatrix.Level1_RightHip.Z) },

            // Level 2: Spine Chest & Head Zenith FP = 2.0f
            new GltfJointBinding { VRoidBoneName = "Spine / Chest", NodeIndex = 10, TargetSpatialMatrixVector = (spatialMatrix.Level2_SpineChest.X, spatialMatrix.Level2_SpineChest.Y, spatialMatrix.Level2_SpineChest.Z) },
            new GltfJointBinding { VRoidBoneName = "LeftShoulder", NodeIndex = 8, TargetSpatialMatrixVector = (spatialMatrix.Level2_LeftShoulder.X, spatialMatrix.Level2_LeftShoulder.Y, spatialMatrix.Level2_LeftShoulder.Z) },
            new GltfJointBinding { VRoidBoneName = "RightShoulder", NodeIndex = 9, TargetSpatialMatrixVector = (spatialMatrix.Level2_RightShoulder.X, spatialMatrix.Level2_RightShoulder.Y, spatialMatrix.Level2_RightShoulder.Z) },
            new GltfJointBinding { VRoidBoneName = "Head Center", NodeIndex = 7, TargetSpatialMatrixVector = (spatialMatrix.Level2_HeadCenter.X, spatialMatrix.Level2_HeadCenter.Y, spatialMatrix.Level2_HeadCenter.Z) },
            new GltfJointBinding { VRoidBoneName = "Crown Zenith", NodeIndex = 9, TargetSpatialMatrixVector = (spatialMatrix.Level2_CrownZenith.X, spatialMatrix.Level2_CrownZenith.Y, spatialMatrix.Level2_CrownZenith.Z) }
        };

        return new HavenGltfSkinningProfile
        {
            ModelFilename = "avatar_456071c0b8ea484a89dfaeddf11c5138.glb",
            TotalNodes = 143,
            TotalSkins = 12,
            MappedJointBindings = bindings
        };
    }
}
