using System;

namespace Gemmi.Core;

public class InverseKinematicsEngine
{
    public const float UpperArmLength = 0.35f; // L1 (Shoulder to Elbow)
    public const float ForearmLength = 0.30f;  // L2 (Elbow to Wrist)
    public const float MaximumArmReach = UpperArmLength + ForearmLength; // 0.65m total reach

    public struct IkReachSolution
    {
        public bool IsTargetReachable { get; set; }
        public float TargetDistanceMeters { get; set; }
        public JointTransform3D ShoulderPos { get; set; }
        public JointTransform3D ElbowPos { get; set; }
        public JointTransform3D HandPos { get; set; }
        public float ShoulderElevationDeg { get; set; }
        public float ElbowBendDeg { get; set; }

        public override string ToString() =>
            $"[IK REACH SOLUTION] Reachable: {IsTargetReachable} | Distance: {TargetDistanceMeters:F2}m | Elbow Bend: {ElbowBendDeg:F1}° | Hand Pos: {HandPos}";
    }

    // Analytical 2-Bone Inverse Kinematics Solver using Law of Cosines
    public static IkReachSolution SolveArmIk(ArmSubSet arm, JointTransform3D targetWorldPos)
    {
        var shoulder = arm.Shoulder;
        float dx = targetWorldPos.X - shoulder.X;
        float dy = targetWorldPos.Y - shoulder.Y;
        float dz = targetWorldPos.Z - shoulder.Z;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        bool reachable = distance <= MaximumArmReach;

        // Clamp distance to maximum reach minus tiny epsilon to prevent NaN in acos
        float dClamped = Math.Clamp(distance, 0.05f, MaximumArmReach - 0.001f);

        // Law of Cosines: d^2 = L1^2 + L2^2 - 2*L1*L2*cos(beta)
        float cosBeta = Math.Clamp((UpperArmLength * UpperArmLength + ForearmLength * ForearmLength - dClamped * dClamped) / (2.0f * UpperArmLength * ForearmLength), -1.0f, 1.0f);
        float betaRad = MathF.Acos(cosBeta);
        float elbowBendDeg = (MathF.PI - betaRad) * (180.0f / MathF.PI);

        // Law of Cosines for Shoulder Elevation Alpha: L2^2 = L1^2 + d^2 - 2*L1*d*cos(alpha)
        float cosAlpha = Math.Clamp((UpperArmLength * UpperArmLength + dClamped * dClamped - ForearmLength * ForearmLength) / (2.0f * UpperArmLength * dClamped), -1.0f, 1.0f);
        float alphaRad = MathF.Acos(cosAlpha);

        // Base Target Angles
        float baseElevationRad = MathF.Atan2(dy, MathF.Sqrt(dx * dx + dz * dz));
        float totalShoulderElevationRad = baseElevationRad + alphaRad;
        float shoulderElevationDeg = totalShoulderElevationRad * (180.0f / MathF.PI);

        // Compute Elbow Position in 3D Space
        float dirX = dx / (distance > 0 ? distance : 1.0f);
        float dirY = dy / (distance > 0 ? distance : 1.0f);
        float dirZ = dz / (distance > 0 ? distance : 1.0f);

        var elbowPos = new JointTransform3D
        {
            X = shoulder.X + dirX * UpperArmLength * MathF.Cos(alphaRad),
            Y = shoulder.Y + MathF.Sin(totalShoulderElevationRad) * UpperArmLength,
            Z = shoulder.Z + dirZ * UpperArmLength * MathF.Cos(alphaRad)
        };

        var handPos = new JointTransform3D
        {
            X = reachable ? targetWorldPos.X : shoulder.X + dirX * MaximumArmReach,
            Y = reachable ? targetWorldPos.Y : shoulder.Y + dirY * MaximumArmReach,
            Z = reachable ? targetWorldPos.Z : shoulder.Z + dirZ * MaximumArmReach
        };

        return new IkReachSolution
        {
            IsTargetReachable = reachable,
            TargetDistanceMeters = distance,
            ShoulderPos = shoulder,
            ElbowPos = elbowPos,
            HandPos = handPos,
            ShoulderElevationDeg = shoulderElevationDeg,
            ElbowBendDeg = elbowBendDeg
        };
    }
}
