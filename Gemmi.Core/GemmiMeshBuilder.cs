using System;
using System.Collections.Generic;
using System.IO;

namespace Gemmi.Core;

public class GemmiMeshBuilder
{
    public struct Vertex3D
    {
        public (float X, float Y, float Z) Position;
        public (float X, float Y, float Z) Normal;
        public (float U, float V) TexCoord;
        public (ushort J0, ushort J1, ushort J2, ushort J3) Joints;
        public (float W0, float W1, float W2, float W3) Weights;
    }

    public class SubMeshPrimitive
    {
        public string Name { get; set; } = "SubMesh";
        public List<Vertex3D> Vertices { get; } = new();
        public List<ushort> Indices { get; } = new();
        public (float R, float G, float B, float A) BaseColor { get; set; } = (0.0f, 0.95f, 0.99f, 1.0f); // Cyan default
    }

    public List<SubMeshPrimitive> SubMeshes { get; } = new();

    // 1. Generate 3D UV Sphere (Head / Visor Mesh)
    public void AddSphere(string name, (float X, float Y, float Z) center, float radius, ushort jointIdx, (float R, float G, float B, float A) color, int latitudeBands = 12, int longitudeBands = 12)
    {
        var subMesh = new SubMeshPrimitive { Name = name, BaseColor = color };
        ushort baseIndex = (ushort)subMesh.Vertices.Count;

        for (int lat = 0; lat <= latitudeBands; lat++)
        {
            float theta = lat * MathF.PI / latitudeBands;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= longitudeBands; lon++)
            {
                float phi = lon * 2 * MathF.PI / longitudeBands;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float nx = cosPhi * sinTheta;
                float ny = cosTheta;
                float nz = sinPhi * sinTheta;

                float u = 1.0f - ((float)lon / longitudeBands);
                float v = 1.0f - ((float)lat / latitudeBands);

                subMesh.Vertices.Add(new Vertex3D
                {
                    Position = (center.X + radius * nx, center.Y + radius * ny, center.Z + radius * nz),
                    Normal = (nx, ny, nz),
                    TexCoord = (u, v),
                    Joints = (jointIdx, 0, 0, 0),
                    Weights = (1.0f, 0.0f, 0.0f, 0.0f)
                });
            }
        }

        for (int lat = 0; lat < latitudeBands; lat++)
        {
            for (int lon = 0; lon < longitudeBands; lon++)
            {
                ushort first = (ushort)(baseIndex + (lat * (longitudeBands + 1)) + lon);
                ushort second = (ushort)(first + longitudeBands + 1);

                subMesh.Indices.Add(first);
                subMesh.Indices.Add((ushort)(first + 1));
                subMesh.Indices.Add(second);

                subMesh.Indices.Add(second);
                subMesh.Indices.Add((ushort)(first + 1));
                subMesh.Indices.Add((ushort)(second + 1));
            }
        }

        SubMeshes.Add(subMesh);
    }

    // 2. Generate 3D Cylinder Capsule (Torso, Arms, Legs Mesh)
    public void AddCylinderCapsule(string name, (float X, float Y, float Z) baseCenter, float height, float radius, ushort jointIdx, (float R, float G, float B, float A) color, int segments = 12)
    {
        var subMesh = new SubMeshPrimitive { Name = name, BaseColor = color };
        ushort baseIndex = (ushort)subMesh.Vertices.Count;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2 * MathF.PI / segments;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            // Bottom Ring
            subMesh.Vertices.Add(new Vertex3D
            {
                Position = (baseCenter.X + radius * cos, baseCenter.Y, baseCenter.Z + radius * sin),
                Normal = (cos, 0, sin),
                TexCoord = ((float)i / segments, 0),
                Joints = (jointIdx, 0, 0, 0),
                Weights = (1.0f, 0.0f, 0.0f, 0.0f)
            });

            // Top Ring
            subMesh.Vertices.Add(new Vertex3D
            {
                Position = (baseCenter.X + radius * cos, baseCenter.Y + height, baseCenter.Z + radius * sin),
                Normal = (cos, 0, sin),
                TexCoord = ((float)i / segments, 1),
                Joints = (jointIdx, 0, 0, 0),
                Weights = (1.0f, 0.0f, 0.0f, 0.0f)
            });
        }

        for (int i = 0; i < segments; i++)
        {
            ushort idx = (ushort)(baseIndex + i * 2);
            subMesh.Indices.Add(idx);
            subMesh.Indices.Add((ushort)(idx + 1));
            subMesh.Indices.Add((ushort)(idx + 2));

            subMesh.Indices.Add((ushort)(idx + 1));
            subMesh.Indices.Add((ushort)(idx + 3));
            subMesh.Indices.Add((ushort)(idx + 2));
        }

        SubMeshes.Add(subMesh);
    }
}
