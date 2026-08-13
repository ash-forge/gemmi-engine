using System;
using System.Collections.Generic;
using System.Numerics;

namespace Gemmi.Core;

public class PerceptionTarget
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public Vector3 BoundingBoxSize { get; set; } = new Vector3(0.5f, 1.8f, 0.5f);
}

public class PerceptionResult
{
    public PerceptionTarget Target { get; set; } = new();
    public float Distance { get; set; }
    public float AngleDegrees { get; set; }
    public bool IsInFieldOfView { get; set; }
    public bool IsInLineOfSight { get; set; }
    public string ZoneClassification { get; set; } = "Social"; // Intimate, Personal, Social, Far
}

public class GemmiSpatialVisionEngine
{
    public float FieldOfViewDegrees { get; set; } = 90.0f;
    public float MaxVisionDistance { get; set; } = 30.0f;
    private readonly SpatialCollisionEngine _collisionEngine;

    public GemmiSpatialVisionEngine(SpatialCollisionEngine collisionEngine)
    {
        _collisionEngine = collisionEngine;
    }

    public List<PerceptionResult> ScanEnvironment(
        Vector3 eyePosition,
        Vector3 eyeForwardVector,
        IEnumerable<PerceptionTarget> candidates)
    {
        var results = new List<PerceptionResult>();
        Vector3 normalizedForward = Vector3.Normalize(eyeForwardVector);

        foreach (var candidate in candidates)
        {
            Vector3 targetDir = candidate.Position - eyePosition;
            float distance = targetDir.Length();

            if (distance > MaxVisionDistance) continue;

            Vector3 normalizedTargetDir = distance > 0.001f ? Vector3.Normalize(targetDir) : normalizedForward;

            // 1. Field of View Cone Angle Check
            float dot = Vector3.Dot(normalizedForward, normalizedTargetDir);
            float angleRad = MathF.Acos(Math.Clamp(dot, -1.0f, 1.0f));
            float angleDeg = angleRad * (180.0f / MathF.PI);

            bool inFov = angleDeg <= (FieldOfViewDegrees * 0.5f);

            // 2. Line of Sight Raycast against SpatialCollisionEngine
            bool raycastHit = _collisionEngine.Raycast(
                new SpatialCollisionEngine.Ray3D { Origin = eyePosition, Direction = normalizedTargetDir },
                out float hitDistance,
                out _);

            bool inLineOfSight = !raycastHit || (hitDistance >= distance - 0.1f);

            // 3. Spatial Zone Classification
            string zone = ClassifySpatialZone(distance);

            results.Add(new PerceptionResult
            {
                Target = candidate,
                Distance = MathF.Round(distance, 2),
                AngleDegrees = MathF.Round(angleDeg, 1),
                IsInFieldOfView = inFov,
                IsInLineOfSight = inLineOfSight && inFov,
                ZoneClassification = zone
            });
        }

        return results;
    }

    private static string ClassifySpatialZone(float distanceMeters)
    {
        if (distanceMeters < 1.0f) return "Intimate";
        if (distanceMeters < 3.0f) return "Personal";
        if (distanceMeters < 7.0f) return "Social";
        return "Far";
    }
}
