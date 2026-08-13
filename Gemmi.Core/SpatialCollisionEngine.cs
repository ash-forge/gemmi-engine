using System;
using System.Collections.Generic;
using System.Numerics;

namespace Gemmi.Core;

public class SpatialCollisionEngine
{
    public struct Ray3D
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
    }

    public struct BoundingSphere3D
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public float RadiusMeters { get; set; }

        public bool Intersects(BoundingSphere3D other)
        {
            float dx = CenterX - other.CenterX;
            float dy = CenterY - other.CenterY;
            float dz = CenterZ - other.CenterZ;
            float distanceSq = dx * dx + dy * dy + dz * dz;
            float radiusSum = RadiusMeters + other.RadiusMeters;
            return distanceSq <= (radiusSum * radiusSum);
        }

        public override string ToString() => $"[SPHERE COLLIDER] Center: ({CenterX:F2}, {CenterY:F2}, {CenterZ:F2}) | Radius: {RadiusMeters:F2}m";
    }

    public struct CollisionCheckResult
    {
        public bool HasCollision { get; set; }
        public string CollidedConcept { get; set; }
        public float PenetrationDistanceMeters { get; set; }

        public override string ToString() =>
            HasCollision ? $"[COLLISION DETECTED] Object: {CollidedConcept} | Penetration: {PenetrationDistanceMeters:F3}m" : "[NO COLLISION CLEAR]";
    }

    public bool Raycast(Ray3D ray, out float hitDistance, out Vector3 hitNormal)
    {
        hitDistance = float.MaxValue;
        hitNormal = Vector3.UnitY;
        return false; // No physical obstacles hit in default empty spatial grid
    }

    // Check collision between avatar's current 15-point position and all objects in spatial memory graph
    public static CollisionCheckResult EvaluateAvatarCollisions(AvatarStateController avatar, EpisodicMemoryGraph memoryGraph, float avatarPersonalSpaceRadius = 0.35f)
    {
        var avatarPos = avatar.SpineTransform;
        var avatarSphere = new BoundingSphere3D
        {
            CenterX = avatarPos.X,
            CenterY = avatarPos.Y,
            CenterZ = avatarPos.Z,
            RadiusMeters = avatarPersonalSpaceRadius
        };

        var nearbyNodes = memoryGraph.GetConceptsBySpatialProximity(avatarPos.X, avatarPos.Y, avatarPos.Z, radiusMeters: 2.0f);

        foreach (var (node, dist) in nearbyNodes)
        {
            var objectSphere = new BoundingSphere3D
            {
                CenterX = node.SpatialVector.X,
                CenterY = node.SpatialVector.Y,
                CenterZ = node.SpatialVector.Z,
                RadiusMeters = 0.25f // default object bounding radius
            };

            if (avatarSphere.Intersects(objectSphere))
            {
                float penetration = (avatarSphere.RadiusMeters + objectSphere.RadiusMeters) - dist;
                return new CollisionCheckResult
                {
                    HasCollision = true,
                    CollidedConcept = node.Concept,
                    PenetrationDistanceMeters = MathF.Max(0.001f, penetration)
                };
            }
        }

        return new CollisionCheckResult { HasCollision = false, CollidedConcept = "None", PenetrationDistanceMeters = 0.0f };
    }

    // Waypoint pathfinding: Calculates obstacle-free 3D waypoints avoiding colliding objects
    public static List<(float X, float Z)> CalculateObstacleFreeWaypoints(float startX, float startZ, float targetX, float targetZ, EpisodicMemoryGraph memoryGraph)
    {
        var waypoints = new List<(float X, float Z)>();
        waypoints.Add((startX, startZ));

        var obstacleNodes = memoryGraph.GetConceptsBySpatialProximity(startX, 1.0f, startZ, radiusMeters: 5.0f);
        bool directPathBlocked = false;
        (float X, float Z) detourOffset = (0.0f, 0.0f);

        foreach (var (node, dist) in obstacleNodes)
        {
            float ox = node.SpatialVector.X;
            float oz = node.SpatialVector.Z;

            // Check if obstacle lies on direct path segment
            float distToPath = MathF.Abs((targetZ - startZ) * ox - (targetX - startX) * oz + targetX * startZ - targetZ * startX) /
                               MathF.Sqrt(MathF.Pow(targetZ - startZ, 2) + MathF.Pow(targetX - startX, 2) + 0.001f);

            if (distToPath < 0.40f) // Obstacle in path corridor
            {
                directPathBlocked = true;
                // Add lateral detour waypoint perpendicular to travel direction
                float dx = targetX - startX;
                float dz = targetZ - startZ;
                float len = MathF.Sqrt(dx * dx + dz * dz + 0.001f);
                float perpX = -dz / len;
                float perpZ = dx / len;

                detourOffset = (ox + perpX * 0.60f, oz + perpZ * 0.60f);
                break;
            }
        }

        if (directPathBlocked)
        {
            waypoints.Add(detourOffset); // Detour waypoint around obstacle
        }

        waypoints.Add((targetX, targetZ)); // Destination waypoint
        return waypoints;
    }
}
