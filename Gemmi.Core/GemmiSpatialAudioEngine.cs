using System;
using System.Numerics;

namespace Gemmi.Core;

public struct SpatialAudioParameters
{
    public float VolumeL;
    public float VolumeR;
    public float DistanceAttenuation;
    public float Pan; // -1.0 (Full Left) to +1.0 (Full Right)
    public float LowPassCutoffHz; // High frequency attenuation for walls/occlusion
    public float ReverbSendLevel;
}

public class GemmiSpatialAudioEngine
{
    public float MinDistance { get; set; } = 1.0f; // Distance below which volume is 100%
    public float MaxDistance { get; set; } = 25.0f; // Distance beyond which sound is silent
    public float RollOffFactor { get; set; } = 1.0f; // Inverse square law intensity

    public SpatialAudioParameters CalculatePositionalAudio(
        Vector3 soundEmitterPos,
        Vector3 listenerPos,
        Vector3 listenerForwardVector,
        bool isOccludedByWall = false)
    {
        Vector3 relativeDir = soundEmitterPos - listenerPos;
        float distance = relativeDir.Length();

        // 1. Distance Attenuation (Inverse Square Law with clamped min/max bounds)
        float attenuation = 1.0f;
        if (distance > MinDistance)
        {
            float clampedDist = Math.Min(distance, MaxDistance);
            attenuation = MinDistance / (MinDistance + RollOffFactor * (clampedDist - MinDistance));
            attenuation = Math.Max(0.0f, attenuation * (1.0f - (clampedDist - MinDistance) / (MaxDistance - MinDistance)));
        }

        // 2. Stereo HRTF Panning Calculation (-1.0 Left to +1.0 Right)
        Vector3 normalizedDir = distance > 0.001f ? Vector3.Normalize(relativeDir) : Vector3.UnitZ;
        Vector3 normalizedForward = Vector3.Normalize(listenerForwardVector);
        Vector3 listenerRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, normalizedForward));

        // Dot product with right vector gives stereo pan
        float pan = Vector3.Dot(normalizedDir, listenerRight);
        pan = Math.Clamp(pan, -1.0f, 1.0f);

        // Constant power stereo volume panning (sine/cosine law)
        float panAngleRad = (pan + 1.0f) * (MathF.PI / 4.0f); // 0 to PI/2
        float volumeL = MathF.Cos(panAngleRad) * attenuation;
        float volumeR = MathF.Sin(panAngleRad) * attenuation;

        // 3. Occlusion Low-Pass Filter Cutoff
        float cutoffHz = 20000.0f; // Unoccluded full spectrum
        if (isOccludedByWall)
        {
            cutoffHz = 1200.0f; // Low-pass muffling through physical obstacle
            volumeL *= 0.65f;
            volumeR *= 0.65f;
        }

        // 4. Reverberation Send Level (Increases with distance)
        float reverbSend = Math.Clamp((distance / MaxDistance) * 0.45f, 0.05f, 0.5f);

        return new SpatialAudioParameters
        {
            VolumeL = Math.Clamp(volumeL, 0.0f, 1.0f),
            VolumeR = Math.Clamp(volumeR, 0.0f, 1.0f),
            DistanceAttenuation = attenuation,
            Pan = pan,
            LowPassCutoffHz = cutoffHz,
            ReverbSendLevel = reverbSend
        };
    }
}
