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

public class AvatarStateController
{
    public AvatarState CurrentState { get; private set; } = AvatarState.CozyChairListeningMusic;

    // 7-DOF Joint Transformations relative to origin (Midpoint = 1.0f)
    public JointTransform3D SpineTransform { get; } = new() { X = 0.0f, Y = 1.0f, Z = 0.0f };
    public JointTransform3D HeadTransform { get; } = new() { X = 0.0f, Y = 1.65f, Z = 0.0f };

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
                // Relaxed pose: Spine centered, Z relaxed
                SpineTransform.X = 0.0f;
                SpineTransform.Y = 1.0f;
                SpineTransform.Z = 0.0f;
                HeadTransform.X = 0.0f;
                HeadTransform.Z = 0.0f;
                ProactiveAssistanceTriggered = false;
                break;

            case AvatarState.LeaningForwardProactiveHelp:
                // Leaning forward: ΔZ = -0.05, ΔX = +0.23 (derived from floating-point joint topology)
                SpineTransform.X = 0.23f;
                SpineTransform.Y = 0.95f;
                SpineTransform.Z = -0.05f;
                HeadTransform.X = 0.23f;
                HeadTransform.Z = -0.08f;
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
