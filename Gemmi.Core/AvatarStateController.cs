using System;
using System.Text;

namespace Gemmi.Core;

public enum AvatarState
{
    CozyChairListeningMusic,
    LeaningForwardProactiveHelp,
    ActivePairProgramming,
    IdleObserving
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

public struct SpatiotemporalTransform4D
{
    public JointTransform3D SpatialVector3D { get; set; } // (X, Y, Z) Space
    public long UtcTicks { get; set; }                     // Time (t)
    public AvatarState ActiveState { get; set; }
    public PostureAnchorPoints3D Anchors { get; set; }

    public DateTime Timestamp => new DateTime(UtcTicks, DateTimeKind.Utc);

    public override string ToString() => $"[4D Space-Time] Vector3D: {SpatialVector3D} | Time: {Timestamp:HH:mm:ss.fff} | State: {ActiveState}";
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
        }

        OnStateChanged?.Invoke(CurrentState, CurrentActivity);
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
