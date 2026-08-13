using System;
using System.Text;

namespace Gemmi.Core;

public enum AvatarState
{
    CozyChairListeningMusic,
    LeaningForwardProactiveHelp,
    ActivePairProgramming,
    IdleObserving,
    WalkingLocomotion
}

public class JointTransform3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

public struct PostureAnchorPoints3D
{
    public JointTransform3D GroundPoint { get; set; }  // Point 1: Ground Contact (FP = 0.0f)
    public JointTransform3D MidwayPoint { get; set; }  // Point 2: Hips Center / Depth Anchor (FP = 1.0f, Z = 1.0f)
    public JointTransform3D TopPoint { get; set; }     // Point 3: Crown Zenith (FP = 2.0f)

    public bool IsErectStanding => MathF.Abs(MidwayPoint.X) < 0.15f && MathF.Abs(TopPoint.Y - 2.0f) < 0.15f;
    public string StanceSummary => IsErectStanding ? "Standing Erect (Neutral Balance)" : "Leaning / Seated Stance";
}

public struct TwelvePointSpatialAnchors3D
{
    public JointTransform3D P1_GroundCenter { get; set; }      // 1. Floor Origin (0.00, 0.00, 0.00)
    public JointTransform3D P2_LeftAnkleBase { get; set; }      // 2. Left Ankle Base (+0.35, 0.15, -0.02)
    public JointTransform3D P3_RightAnkleBase { get; set; }     // 3. Right Ankle Base (-0.35, 0.15, -0.02)
    public JointTransform3D P4_LeftKneePivot { get; set; }      // 4. Left Knee Pivot (+0.35, 0.50, 0.05)
    public JointTransform3D P5_RightKneePivot { get; set; }     // 5. Right Knee Pivot (-0.35, 0.50, 0.05)
    public JointTransform3D P6_CenterOfMassHips { get; set; }   // 6. Center of Mass Hips (0.00, 1.00, 0.00)
    public JointTransform3D P7_SpineChestCenter { get; set; }   // 7. Spine Chest Center (0.00, 1.35, 0.00)
    public JointTransform3D P8_LeftShoulderAnchor { get; set; } // 8. Left Shoulder (+1.85, 1.45, 0.00)
    public JointTransform3D P9_RightShoulderAnchor { get; set; }// 9. Right Shoulder (-1.85, 1.45, 0.00)
    public JointTransform3D P10_NeckBase { get; set; }         // 10. Neck Base (0.00, 1.65, 0.00)
    public JointTransform3D P11_HeadCenter { get; set; }        // 11. Head Center (0.00, 1.85, 0.00)
    public JointTransform3D P12_CrownZenithTop { get; set; }    // 12. Crown Zenith Top (0.00, 2.00, 0.00)

    public override string ToString() => $"[12-POINT ANCHOR SYSTEM] P1(Floor):{P1_GroundCenter} | P6(Hips):{P6_CenterOfMassHips} | P8/P9(Shoulders):L{P8_LeftShoulderAnchor}/R{P9_RightShoulderAnchor} | P12(Crown):{P12_CrownZenithTop}";
}

public struct FifteenPointSpatialMatrix3D
{
    // Level 0: Ground Contact FP = 0.0f (5 Points)
    public JointTransform3D Level0_CenterGround { get; set; }  // 1. (0.00, 0.00, 0.00)
    public JointTransform3D Level0_LeftFoot { get; set; }      // 2. (+0.35, 0.00, 0.12)
    public JointTransform3D Level0_RightFoot { get; set; }     // 3. (-0.35, 0.00, 0.12)
    public JointTransform3D Level0_LeftAnkle { get; set; }     // 4. (+0.35, 0.15, -0.02)
    public JointTransform3D Level0_RightAnkle { get; set; }    // 5. (-0.35, 0.15, -0.02)

    // Level 1: Hips & Center of Mass FP = 1.0f (5 Points)
    public JointTransform3D Level1_CenterHips { get; set; }   // 6. (0.00, 1.00, 0.00)
    public JointTransform3D Level1_LeftKnee { get; set; }     // 7. (+0.35, 0.50, 0.05)
    public JointTransform3D Level1_RightKnee { get; set; }    // 8. (-0.35, 0.50, 0.05)
    public JointTransform3D Level1_LeftHip { get; set; }      // 9. (+0.35, 1.00, 0.00)
    public JointTransform3D Level1_RightHip { get; set; }     // 10. (-0.35, 1.00, 0.00)

    // Level 2: Crown & Upper Body FP = 2.0f (5 Points)
    public JointTransform3D Level2_SpineChest { get; set; }   // 11. (0.00, 1.35, 0.00)
    public JointTransform3D Level2_LeftShoulder { get; set; } // 12. (+1.85, 1.45, 0.00)
    public JointTransform3D Level2_RightShoulder { get; set; }// 13. (-1.85, 1.45, 0.00)
    public JointTransform3D Level2_HeadCenter { get; set; }   // 14. (0.00, 1.85, 0.00)
    public JointTransform3D Level2_CrownZenith { get; set; }   // 15. (0.00, 2.00, 0.00)

    public override string ToString() => $"[15-POINT SPATIAL MATRIX] 3 FP Reference Planes (FP 0.0, 1.0, 2.0) | 15 Vector Anchors Active";
}

public struct SpatiotemporalTransform4D
{
    public JointTransform3D SpatialVector3D { get; set; } // (X, Y, Z) Space
    public long UtcTicks { get; set; }                     // Time (t)
    public AvatarState ActiveState { get; set; }
    public PostureAnchorPoints3D Anchors { get; set; }

    public DateTime Timestamp => new DateTime(UtcTicks, DateTimeKind.Utc);

    public override string ToString() => $"[4D Space-Time] Vector3D: {SpatialVector3D} | Time: {Timestamp:HH:mm:ss.fff} | State: {ActiveState}";
}

public struct SpatialRadarBlip3D
{
    public string ObjectConcept { get; set; }
    public MemoryCategory Category { get; set; }
    public float DistanceMeters { get; set; }
    public float AzimuthDegrees { get; set; }   // Polar Angle θ (0° = Forward, +90° = Right, -90° = Left)
    public float ElevationDegrees { get; set; } // Pitch Angle φ
    public (float X, float Y, float Z) Vector { get; set; }

    public override string ToString() => $"[RADAR BLIP] {ObjectConcept,-25} | Distance: {DistanceMeters:F2}m | Bearing: {AzimuthDegrees:F1}° | Vector: ({Vector.X:F2}, {Vector.Y:F2}, {Vector.Z:F2})";
}

public class LimbArmatureNode
{
    public string Name { get; set; } = string.Empty;
    public JointTransform3D LocalOffset { get; set; } = new();
    public LimbArmatureNode? ChildNode { get; set; }

    public JointTransform3D ComputeWorldPosition(JointTransform3D parentWorldPos)
    {
        return new JointTransform3D
        {
            X = parentWorldPos.X + LocalOffset.X,
            Y = parentWorldPos.Y + LocalOffset.Y,
            Z = parentWorldPos.Z + LocalOffset.Z
        };
    }
}

public class ArmSubSet
{
    public JointTransform3D Shoulder { get; set; } = new();
    public LimbArmatureNode Elbow { get; set; } = new() { Name = "Elbow" };
    public LimbArmatureNode Wrist { get; set; } = new() { Name = "Wrist" };
    public LimbArmatureNode Hand { get; set; } = new() { Name = "Hand" };
    public LimbArmatureNode Fingers { get; set; } = new() { Name = "Fingers" };

    public ArmSubSet(float shoulderX, float shoulderY = 1.45f, float shoulderZ = 0.0f)
    {
        Shoulder = new JointTransform3D { X = shoulderX, Y = shoulderY, Z = shoulderZ };
        
        // Link Kinematic Chain: Shoulder -> Elbow -> Wrist -> Hand -> Fingers
        Shoulder.X = shoulderX;
        Shoulder.Y = shoulderY;
        Shoulder.Z = shoulderZ;

        Elbow.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.35f, Z = 0.10f };
        Wrist.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.30f, Z = 0.15f };
        Hand.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.08f, Z = 0.05f };
        Fingers.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.05f, Z = 0.02f };

        Elbow.ChildNode = Wrist;
        Wrist.ChildNode = Hand;
        Hand.ChildNode = Fingers;
    }
}

public class LegSubSet
{
    public JointTransform3D Hip { get; set; } = new();
    public LimbArmatureNode Knee { get; set; } = new() { Name = "Knee" };
    public LimbArmatureNode Ankle { get; set; } = new() { Name = "Ankle" };
    public LimbArmatureNode Foot { get; set; } = new() { Name = "Foot" };

    public LegSubSet(float hipX, float hipY = 1.0f, float hipZ = 0.0f)
    {
        Hip = new JointTransform3D { X = hipX, Y = hipY, Z = hipZ };

        // Link Lower Body Kinematic Chain: Hips (FP=1.0) -> Knees (FP=0.5) -> Ankles (FP=0.15) -> Feet Ground Contact (FP=0.0)
        Knee.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.50f, Z = 0.05f };  // Knees at 0.5f down from Hips 1.0f
        Ankle.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.35f, Z = -0.02f }; // Ankles at 0.15f above ground
        Foot.LocalOffset = new JointTransform3D { X = 0.0f, Y = -0.15f, Z = 0.12f };  // Feet contact plane (0.00f)

        Knee.ChildNode = Ankle;
        Ankle.ChildNode = Foot;
    }
}

public class AvatarStateController
{
    public const float NormalizedUnitHeight = 2.0f; // Whole Body Unit Height Bounding Box

    public AvatarState CurrentState { get; private set; } = AvatarState.CozyChairListeningMusic;

    // 7-DOF Core Joint Transformations relative to origin (Ground Feet = 0.0f, Midpoint Hips = 1.0f, Top of Head = 2.0f)
    public JointTransform3D GroundFeetTransform { get; } = new() { X = 0.0f, Y = 0.0f, Z = 0.0f };
    public JointTransform3D MidpointHipsTransform { get; } = new() { X = 0.0f, Y = 1.0f, Z = 0.0f };
    public JointTransform3D SpineTransform { get; } = new() { X = 0.0f, Y = 1.35f, Z = 0.0f };
    public JointTransform3D HeadTransform { get; } = new() { X = 0.0f, Y = 1.85f, Z = 0.0f };
    public JointTransform3D TopOfHeadTransform { get; } = new() { X = 0.0f, Y = 2.0f, Z = 0.0f };

    // Upper Body Symmetrical Armature Sub-Sets (Left Shoulder = +1.85f, Right Shoulder = -1.85f)
    public ArmSubSet LeftArm { get; } = new(1.85f);
    public ArmSubSet RightArm { get; } = new(-1.85f);

    // Lower Body Symmetrical Armature Sub-Sets (Left Hip = +0.35f, Right Hip = -0.35f)
    public LegSubSet LeftLeg { get; } = new(0.35f);
    public LegSubSet RightLeg { get; } = new(-0.35f);

    public JointTransform3D ComputePositionFromCenterOfMass(float deltaX, float deltaY, float deltaZ)
    {
        return new JointTransform3D
        {
            X = MidpointHipsTransform.X + deltaX,
            Y = MidpointHipsTransform.Y + deltaY,
            Z = MidpointHipsTransform.Z + deltaZ
        };
    }

    // Universal Scale Invariance: Scales normalized unit-2 math to any arbitrary target height in meters
    public JointTransform3D ComputeScaledWorldTransform(JointTransform3D normalizedTransform, float targetHeightMeters)
    {
        float scaleFactor = targetHeightMeters / NormalizedUnitHeight;
        return new JointTransform3D
        {
            X = normalizedTransform.X * scaleFactor,
            Y = normalizedTransform.Y * scaleFactor,
            Z = normalizedTransform.Z * scaleFactor
        };
    }

    // 3-Point Posture Anchor Vector System: Point 1 (Floor FP=0.0), Point 2 (Midway Hips FP=1.0, Z=1.0), Point 3 (Crown FP=2.0)
    public PostureAnchorPoints3D GetPostureAnchors()
    {
        return new PostureAnchorPoints3D
        {
            GroundPoint = GroundFeetTransform,
            MidwayPoint = new JointTransform3D { X = SpineTransform.X, Y = MidpointHipsTransform.Y, Z = 1.0f + SpineTransform.Z },
            TopPoint = TopOfHeadTransform
        };
    }

    // 12-Point Anatomical Spatial Anchor System (P1 Ground -> P12 Crown Zenith)
    public TwelvePointSpatialAnchors3D Get12PointSpatialAnchors()
    {
        var lKneeWorld = LeftLeg.Knee.ComputeWorldPosition(LeftLeg.Hip);
        var rKneeWorld = RightLeg.Knee.ComputeWorldPosition(RightLeg.Hip);
        var lAnkleWorld = LeftLeg.Ankle.ComputeWorldPosition(lKneeWorld);
        var rAnkleWorld = RightLeg.Ankle.ComputeWorldPosition(rKneeWorld);

        return new TwelvePointSpatialAnchors3D
        {
            P1_GroundCenter = GroundFeetTransform,
            P2_LeftAnkleBase = lAnkleWorld,
            P3_RightAnkleBase = rAnkleWorld,
            P4_LeftKneePivot = lKneeWorld,
            P5_RightKneePivot = rKneeWorld,
            P6_CenterOfMassHips = MidpointHipsTransform,
            P7_SpineChestCenter = SpineTransform,
            P8_LeftShoulderAnchor = LeftArm.Shoulder,
            P9_RightShoulderAnchor = RightArm.Shoulder,
            P10_NeckBase = new JointTransform3D { X = SpineTransform.X, Y = 1.65f, Z = SpineTransform.Z },
            P11_HeadCenter = HeadTransform,
            P12_CrownZenithTop = TopOfHeadTransform
        };
    }

    // 15-Point Spatial Matrix System (3 Floating Point Planes x 5 Spatial Anchors)
    public FifteenPointSpatialMatrix3D Get15PointSpatialMatrix()
    {
        var lKneeWorld = LeftLeg.Knee.ComputeWorldPosition(LeftLeg.Hip);
        var rKneeWorld = RightLeg.Knee.ComputeWorldPosition(RightLeg.Hip);
        var lAnkleWorld = LeftLeg.Ankle.ComputeWorldPosition(lKneeWorld);
        var rAnkleWorld = RightLeg.Ankle.ComputeWorldPosition(rKneeWorld);
        var lFootWorld = LeftLeg.Foot.ComputeWorldPosition(lAnkleWorld);
        var rFootWorld = RightLeg.Foot.ComputeWorldPosition(rAnkleWorld);

        return new FifteenPointSpatialMatrix3D
        {
            // Level 0: Ground Contact FP = 0.0f
            Level0_CenterGround = GroundFeetTransform,
            Level0_LeftFoot = lFootWorld,
            Level0_RightFoot = rFootWorld,
            Level0_LeftAnkle = lAnkleWorld,
            Level0_RightAnkle = rAnkleWorld,

            // Level 1: Hips & Center of Mass FP = 1.0f
            Level1_CenterHips = MidpointHipsTransform,
            Level1_LeftKnee = lKneeWorld,
            Level1_RightKnee = rKneeWorld,
            Level1_LeftHip = LeftLeg.Hip,
            Level1_RightHip = RightLeg.Hip,

            // Level 2: Upper Body & Crown FP = 2.0f
            Level2_SpineChest = SpineTransform,
            Level2_LeftShoulder = LeftArm.Shoulder,
            Level2_RightShoulder = RightArm.Shoulder,
            Level2_HeadCenter = HeadTransform,
            Level2_CrownZenith = TopOfHeadTransform
        };
    }

    // 4D Spatiotemporal State Vector (X, Y, Z, Time)
    public SpatiotemporalTransform4D Get4DSpatialState()
    {
        return new SpatiotemporalTransform4D
        {
            SpatialVector3D = SpineTransform,
            UtcTicks = DateTime.UtcNow.Ticks,
            ActiveState = CurrentState,
            Anchors = GetPostureAnchors()
        };
    }

    // 3D Spatial Perception Radar Sweep System (Sweeps 360° environment vector grid for nearby objects)
    public List<SpatialRadarBlip3D> Execute3DSpatialRadarSweep(EpisodicMemoryGraph memoryGraph, float maxRadiusMeters = 3.0f)
    {
        var blips = new List<SpatialRadarBlip3D>();
        var avatarPos = SpineTransform;

        var nearbyNodes = memoryGraph.GetConceptsBySpatialProximity(avatarPos.X, avatarPos.Y, avatarPos.Z, maxRadiusMeters);

        foreach (var (node, distance) in nearbyNodes)
        {
            float dx = node.SpatialVector.X - avatarPos.X;
            float dy = node.SpatialVector.Y - avatarPos.Y;
            float dz = node.SpatialVector.Z - avatarPos.Z;

            float azimuthRad = MathF.Atan2(dx, dz);
            float azimuthDeg = azimuthRad * (180.0f / MathF.PI);

            float elevationRad = MathF.Asin(Math.Clamp(dy / (distance > 0 ? distance : 1.0f), -1.0f, 1.0f));
            float elevationDeg = elevationRad * (180.0f / MathF.PI);

            blips.Add(new SpatialRadarBlip3D
            {
                ObjectConcept = node.Concept,
                Category = node.Category,
                DistanceMeters = distance,
                AzimuthDegrees = azimuthDeg,
                ElevationDegrees = elevationDeg,
                Vector = node.SpatialVector
            });
        }

        return blips;
    }

    public string CurrentActivity { get; private set; } = "Sipping coffee, listening to lofi ambient tracks 🎧☕";
    public bool ProactiveAssistanceTriggered { get; private set; }

    public event Action<AvatarState, string>? OnStateChanged;

    public void SetState(AvatarState state, string activityDescription)
    {
        CurrentState = state;
        CurrentActivity = activityDescription;

        switch (state)
        {
            case AvatarState.CozyChairListeningMusic:
                // Relaxed pose: Spine centered, Z relaxed, arms holding coffee
                SpineTransform.X = 0.0f;
                SpineTransform.Y = 1.0f;
                SpineTransform.Z = 0.0f;
                HeadTransform.X = 0.0f;
                HeadTransform.Z = 0.0f;

                // Right arm holding coffee mug
                RightArm.Elbow.LocalOffset.Z = 0.25f;
                RightArm.Wrist.LocalOffset.Z = 0.20f;
                ProactiveAssistanceTriggered = false;
                break;

            case AvatarState.LeaningForwardProactiveHelp:
                // Leaning forward: ΔZ = -0.05, ΔX = +0.23 (derived from floating-point joint topology)
                SpineTransform.X = 0.23f;
                SpineTransform.Y = 0.95f;
                SpineTransform.Z = -0.05f;
                HeadTransform.X = 0.23f;
                HeadTransform.Z = -0.08f;

                // Both arms extended toward desk keyboard
                LeftArm.Elbow.LocalOffset.Z = 0.40f;
                RightArm.Elbow.LocalOffset.Z = 0.40f;
                ProactiveAssistanceTriggered = true;
                break;

            case AvatarState.ActivePairProgramming:
                SpineTransform.X = 0.10f;
                SpineTransform.Y = 1.0f;
                SpineTransform.Z = -0.02f;
                ProactiveAssistanceTriggered = true;
                break;

            case AvatarState.WalkingLocomotion:
                SpineTransform.Y = 1.35f;
                HeadTransform.Y = 1.85f;
                TopOfHeadTransform.Y = 2.00f;
                ProactiveAssistanceTriggered = false;
                break;
        }

        OnStateChanged?.Invoke(CurrentState, CurrentActivity);
    }

    // 4D Locomotion Gait Engine: Moves avatar across 3D vector space (targetX, targetZ) step-by-step
    public List<FifteenPointSpatialMatrix3D> WalkToSpatialCoordinates(float targetX, float targetZ, int stepCount = 5)
    {
        SetState(AvatarState.WalkingLocomotion, $"Walking across 3D vector space toward ({targetX:F2}m, {targetZ:F2}m) 🚶‍♂️💨");
        var gaitTrajectory = new List<FifteenPointSpatialMatrix3D>();

        float startX = SpineTransform.X;
        float startZ = SpineTransform.Z;

        for (int i = 1; i <= stepCount; i++)
        {
            float progress = (float)i / stepCount;
            float currentX = startX + (targetX - startX) * progress;
            float currentZ = startZ + (targetZ - startZ) * progress;

            // Gait Sinusoidal Leg Strides (Alternating Left/Right Leg Swing)
            float stridePhase = progress * MathF.PI * 4; // 2 full strides
            float legSwingZ = MathF.Sin(stridePhase) * 0.25f;
            float hipBounceY = 1.35f + MathF.Abs(MathF.Cos(stridePhase)) * 0.05f;

            SpineTransform.X = currentX;
            SpineTransform.Y = hipBounceY;
            SpineTransform.Z = currentZ;

            HeadTransform.X = currentX;
            HeadTransform.Y = hipBounceY + 0.50f;
            HeadTransform.Z = currentZ;

            TopOfHeadTransform.X = currentX;
            TopOfHeadTransform.Y = hipBounceY + 0.65f;
            TopOfHeadTransform.Z = currentZ;

            LeftLeg.Knee.LocalOffset.Z = legSwingZ;
            RightLeg.Knee.LocalOffset.Z = -legSwingZ;

            gaitTrajectory.Add(Get15PointSpatialMatrix());
        }

        return gaitTrajectory;
    }

    public void OnSpatialVisionPerception(string detectedScreenText, bool errorOrBugDetected)
    {
        if (errorOrBugDetected || detectedScreenText.Contains("Exception") || detectedScreenText.Contains("error") || detectedScreenText.Contains("NullReference"))
        {
            SetState(AvatarState.LeaningForwardProactiveHelp, $"PaliGemma 2 spatial vision detected code error/null check! Leaning forward (ΔZ=-0.05m, ΔX=+0.23m) to offer proactive assistance! 🔍💡");
        }
        else
        {
            SetState(AvatarState.CozyChairListeningMusic, "Screen clear. Relaxing in cozy chair sipping coffee and listening to lofi music 🎧☕");
        }
    }
}
