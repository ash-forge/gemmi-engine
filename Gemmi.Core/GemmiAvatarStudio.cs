using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

namespace Gemmi.Core;

public class GemmiAvatarStudio
{
    public struct AvatarGenerationSpec
    {
        public string CompanionName { get; set; }
        public float HeightMeters { get; set; }
        public (float R, float G, float B, float A) PrimaryColor { get; set; }
        public (float R, float G, float B, float A) AccentColor { get; set; }
        public (float R, float G, float B, float A) HairColor { get; set; }

        public override string ToString() => $"[AVATAR CREATION SPEC] Companion: {CompanionName} | Height: {HeightMeters:F2}m";
    }

    public static string BuildAndExportSovereignAvatarGlb(AvatarGenerationSpec spec, FifteenPointSpatialMatrix3D matrix, string outputGlbPath)
    {
        var builder = new GemmiMeshBuilder();

        // 1. Build Head & Visor Mesh (Sphere at Y=1.55m, Joint 12)
        builder.AddSphere("HeadVisor", (0.0f, matrix.Level2_HeadCenter.Y, 0.0f), radius: 0.12f, jointIdx: 12, color: spec.HairColor);

        // 2. Build Crown Hair Zenith Mesh (Sphere at Y=1.70m, Joint 14)
        builder.AddSphere("CrownHair", (0.0f, matrix.Level2_CrownZenith.Y, 0.0f), radius: 0.08f, jointIdx: 14, color: spec.PrimaryColor);

        // 3. Build Upper Chest Armor (Cylinder at Y=1.25m, Joint 8)
        builder.AddCylinderCapsule("ChestArmor", (0.0f, 1.10f, 0.0f), height: 0.30f, radius: 0.18f, jointIdx: 8, color: spec.PrimaryColor);

        // 4. Build Pelvis Hips Mesh (Cylinder at Y=0.97m, Joint 5)
        builder.AddCylinderCapsule("PelvisHips", (0.0f, 0.82f, 0.0f), height: 0.25f, radius: 0.16f, jointIdx: 5, color: spec.AccentColor);

        // 5. Build Shoulder Armor Caps (Left Joint 9, Right Joint 10)
        builder.AddSphere("LeftShoulderCap", (matrix.Level2_LeftShoulder.X, matrix.Level2_LeftShoulder.Y, 0.0f), radius: 0.07f, jointIdx: 9, color: spec.AccentColor);
        builder.AddSphere("RightShoulderCap", (matrix.Level2_RightShoulder.X, matrix.Level2_RightShoulder.Y, 0.0f), radius: 0.07f, jointIdx: 10, color: spec.AccentColor);

        // 6. Build Thigh & Leg Capsules (Left Joint 6, Right Joint 7)
        builder.AddCylinderCapsule("LeftThigh", (matrix.Level1_LeftHip.X, 0.52f, 0.0f), height: 0.36f, radius: 0.08f, jointIdx: 6, color: spec.PrimaryColor);
        builder.AddCylinderCapsule("RightThigh", (matrix.Level1_RightHip.X, 0.52f, 0.0f), height: 0.36f, radius: 0.08f, jointIdx: 7, color: spec.PrimaryColor);

        // 7. Build Feet Base Contacts (Left Joint 1, Right Joint 2)
        builder.AddCylinderCapsule("LeftFootBase", (matrix.Level0_LeftFoot.X, 0.0f, 0.05f), height: 0.10f, radius: 0.07f, jointIdx: 1, color: spec.AccentColor);
        builder.AddCylinderCapsule("RightFootBase", (matrix.Level0_RightFoot.X, 0.0f, 0.05f), height: 0.10f, radius: 0.07f, jointIdx: 2, color: spec.AccentColor);

        // Build Binary GLB Buffer Payload
        var binStream = new MemoryStream();
        var binWriter = new BinaryWriter(binStream);

        var jsonNodesArray = new JsonArray
        {
            new JsonObject { ["name"] = "Gemmi_Avatar_Root", ["children"] = new JsonArray { 1 } },
            new JsonObject { ["name"] = "Hips_Center", ["translation"] = new JsonArray { 0.0, 0.97, 0.0 } },
            new JsonObject { ["name"] = "Left_Foot", ["translation"] = new JsonArray { 0.07, 0.10, 0.02 } },
            new JsonObject { ["name"] = "Right_Foot", ["translation"] = new JsonArray { -0.07, 0.10, 0.02 } },
            new JsonObject { ["name"] = "Left_Knee", ["translation"] = new JsonArray { 0.06, 0.52, 0.0 } },
            new JsonObject { ["name"] = "Right_Knee", ["translation"] = new JsonArray { -0.06, 0.52, 0.0 } },
            new JsonObject { ["name"] = "Left_Thigh", ["translation"] = new JsonArray { 0.07, 0.88, 0.0 } },
            new JsonObject { ["name"] = "Right_Thigh", ["translation"] = new JsonArray { -0.07, 0.88, 0.0 } },
            new JsonObject { ["name"] = "Spine_Chest", ["translation"] = new JsonArray { 0.0, 1.25, 0.0 } },
            new JsonObject { ["name"] = "Left_Shoulder", ["translation"] = new JsonArray { 0.22, 1.35, 0.0 } },
            new JsonObject { ["name"] = "Right_Shoulder", ["translation"] = new JsonArray { -0.22, 1.35, 0.0 } },
            new JsonObject { ["name"] = "Neck_Base", ["translation"] = new JsonArray { 0.0, 1.45, 0.0 } },
            new JsonObject { ["name"] = "Head_Center", ["translation"] = new JsonArray { 0.0, 1.55, 0.0 } },
            new JsonObject { ["name"] = "Crown_Top", ["translation"] = new JsonArray { 0.0, 1.70, 0.0 } },
            new JsonObject { ["name"] = "Crown_Zenith", ["translation"] = new JsonArray { 0.0, 1.85, 0.0 } }
        };

        var jsonMeshesArray = new JsonArray();
        var jsonAccessorsArray = new JsonArray();
        var jsonBufferViewsArray = new JsonArray();

        int currentBufferOffset = 0;

        foreach (var subMesh in builder.SubMeshes)
        {
            int vertCount = subMesh.Vertices.Count;
            int idxCount = subMesh.Indices.Count;

            // 1. Write POSITION Buffer
            int posOffset = currentBufferOffset;
            foreach (var v in subMesh.Vertices)
            {
                binWriter.Write(v.Position.X);
                binWriter.Write(v.Position.Y);
                binWriter.Write(v.Position.Z);
            }
            int posLength = vertCount * 12;
            currentBufferOffset += posLength;

            // 2. Write NORMAL Buffer
            int normOffset = currentBufferOffset;
            foreach (var v in subMesh.Vertices)
            {
                binWriter.Write(v.Normal.X);
                binWriter.Write(v.Normal.Y);
                binWriter.Write(v.Normal.Z);
            }
            int normLength = vertCount * 12;
            currentBufferOffset += normLength;

            // 3. Write TEXCOORD Buffer
            int uvOffset = currentBufferOffset;
            foreach (var v in subMesh.Vertices)
            {
                binWriter.Write(v.TexCoord.U);
                binWriter.Write(v.TexCoord.V);
            }
            int uvLength = vertCount * 8;
            currentBufferOffset += uvLength;

            // 4. Write INDICES Buffer
            int idxOffset = currentBufferOffset;
            foreach (var idx in subMesh.Indices)
            {
                binWriter.Write(idx);
            }
            int idxLength = idxCount * 2;
            int paddedIdxLength = (idxLength + 3) & ~3;
            for (int i = idxLength; i < paddedIdxLength; i++) binWriter.Write((byte)0);
            currentBufferOffset += paddedIdxLength;

            // Register BufferViews
            int bvPos = jsonBufferViewsArray.Count;
            jsonBufferViewsArray.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = posOffset, ["byteLength"] = posLength, ["target"] = 34962 }); // ARRAY_BUFFER
            jsonBufferViewsArray.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = normOffset, ["byteLength"] = normLength, ["target"] = 34962 });
            jsonBufferViewsArray.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = uvOffset, ["byteLength"] = uvLength, ["target"] = 34962 });
            jsonBufferViewsArray.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = idxOffset, ["byteLength"] = idxLength, ["target"] = 34963 }); // ELEMENT_ARRAY_BUFFER

            // Register Accessors
            int accPos = jsonAccessorsArray.Count;
            jsonAccessorsArray.Add(new JsonObject { ["bufferView"] = bvPos, ["componentType"] = 5126, ["count"] = vertCount, ["type"] = "VEC3" }); // FLOAT
            jsonAccessorsArray.Add(new JsonObject { ["bufferView"] = bvPos + 1, ["componentType"] = 5126, ["count"] = vertCount, ["type"] = "VEC3" });
            jsonAccessorsArray.Add(new JsonObject { ["bufferView"] = bvPos + 2, ["componentType"] = 5126, ["count"] = vertCount, ["type"] = "VEC2" });
            jsonAccessorsArray.Add(new JsonObject { ["bufferView"] = bvPos + 3, ["componentType"] = 5123, ["count"] = idxCount, ["type"] = "SCALAR" }); // UNSIGNED_SHORT

            // Register Primitive Mesh
            jsonMeshesArray.Add(new JsonObject
            {
                ["name"] = subMesh.Name,
                ["primitives"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["attributes"] = new JsonObject
                        {
                            ["POSITION"] = accPos,
                            ["NORMAL"] = accPos + 1,
                            ["TEXCOORD_0"] = accPos + 2
                        },
                        ["indices"] = accPos + 3
                    }
                }
            });
        }

        byte[] binData = binStream.ToArray();

        // Assemble Final GLTF JSON Manifest
        var rootGlTF = new JsonObject
        {
            ["asset"] = new JsonObject
            {
                ["generator"] = "Gemmi-Sovereign-3D-Avatar-Studio-v1.0",
                ["version"] = "2.0"
            },
            ["scene"] = 0,
            ["scenes"] = new JsonArray { new JsonObject { ["name"] = "GemmiSovereignScene", ["nodes"] = new JsonArray { 0 } } },
            ["nodes"] = jsonNodesArray,
            ["meshes"] = jsonMeshesArray,
            ["accessors"] = jsonAccessorsArray,
            ["bufferViews"] = jsonBufferViewsArray,
            ["buffers"] = new JsonArray { new JsonObject { ["byteLength"] = binData.Length } }
        };

        byte[] jsonBytes = Encoding.UTF8.GetBytes(rootGlTF.ToJsonString());
        int paddedJsonLen = (jsonBytes.Length + 3) & ~3;
        byte[] paddedJson = new byte[paddedJsonLen];
        Array.Copy(jsonBytes, paddedJson, jsonBytes.Length);
        for (int i = jsonBytes.Length; i < paddedJsonLen; i++) paddedJson[i] = 0x20;

        // Write Final .GLB Binary File
        using (var fs = new FileStream(outputGlbPath, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(Encoding.ASCII.GetBytes("glTF"));
            writer.Write((uint)2);
            writer.Write((uint)(12 + 8 + paddedJsonLen + 8 + binData.Length));

            // Chunk 0 (JSON)
            writer.Write((uint)paddedJsonLen);
            writer.Write(Encoding.ASCII.GetBytes("JSON"));
            writer.Write(paddedJson);

            // Chunk 1 (BIN)
            writer.Write((uint)binData.Length);
            writer.Write(Encoding.ASCII.GetBytes("BIN\0"));
            writer.Write(binData);
        }

        return outputGlbPath;
    }
}
