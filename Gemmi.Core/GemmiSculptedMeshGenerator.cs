using System;
using System.Collections.Generic;

namespace Gemmi.Core;

public class GemmiSculptedMeshGenerator
{
    // Generates a contoured, tapered 3D body part (waist-tapered torso, contoured thighs/calves, sculpted arm limbs)
    public static GemmiMeshBuilder.SubMeshPrimitive GenerateContouredMesh(string name, (float X, float Y, float Z) center, float[] radiusProfile, float height, int radialSegments, ushort jointIdx, (float R, float G, float B, float A) color)
    {
        var subMesh = new GemmiMeshBuilder.SubMeshPrimitive { Name = name, BaseColor = color };
        ushort baseIndex = (ushort)subMesh.Vertices.Count;

        int heightSteps = radiusProfile.Length - 1;
        float stepHeight = height / heightSteps;

        for (int h = 0; h <= heightSteps; h++)
        {
            float currentY = center.Y + (h * stepHeight);
            float radius = radiusProfile[h];

            for (int r = 0; r <= radialSegments; r++)
            {
                float angle = r * 2 * MathF.PI / radialSegments;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                float nx = cos;
                float ny = 0.0f;
                float nz = sin;

                float vx = center.X + radius * cos;
                float vy = currentY;
                float vz = center.Z + radius * sin;

                float u = (float)r / radialSegments;
                float v = (float)h / heightSteps;

                subMesh.Vertices.Add(new GemmiMeshBuilder.Vertex3D
                {
                    Position = (vx, vy, vz),
                    Normal = (nx, ny, nz),
                    TexCoord = (u, v),
                    Joints = (jointIdx, 0, 0, 0),
                    Weights = (1.0f, 0.0f, 0.0f, 0.0f)
                });
            }
        }

        for (int h = 0; h < heightSteps; h++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                ushort first = (ushort)(baseIndex + (h * (radialSegments + 1)) + r);
                ushort second = (ushort)(first + radialSegments + 1);

                subMesh.Indices.Add(first);
                subMesh.Indices.Add((ushort)(first + 1));
                subMesh.Indices.Add(second);

                subMesh.Indices.Add(second);
                subMesh.Indices.Add((ushort)(first + 1));
                subMesh.Indices.Add((ushort)(second + 1));
            }
        }

        return subMesh;
    }
}
