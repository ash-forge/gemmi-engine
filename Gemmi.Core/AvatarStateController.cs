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

public class AvatarStateController
{
    public AvatarState CurrentState { get; private set; } = AvatarState.CozyChairListeningMusic;

    // 7-DOF Core Joint Transformations relative to origin (Midpoint = 1.0f)
    public JointTransform3D SpineTransform { get; } = new() { X = 0.0f, Y = 1.0f, Z = 0.0f };
    public JointTransform3D HeadTransform { get; } = new() { X = 0.0f, Y = 1.65f, Z = 0.0f };

    // Symmetrical Armature Sub-Sets (Left Shoulder = +1.85f, Right Shoulder = -1.85f)
    public ArmSubSet LeftArm { get; } = new(1.85f);
    public ArmSubSet RightArm { get; } = new(-1.85f);

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
