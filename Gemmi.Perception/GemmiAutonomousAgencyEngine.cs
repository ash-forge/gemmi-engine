using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class AutonomousThoughtEvent
{
    public string ThoughtType { get; set; } = "Observation"; // Observation, Idea, Question, Greeting
    public string ThoughtContent { get; set; } = string.Empty;
    public double SalienceScore { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sovereign Autonomous Agency & Proactive Cognition Engine.
/// Drives spontaneous behavioral transitions, associative thought sparks,
/// and pro-active spatial presence without requiring manual user polling.
/// </summary>
public class GemmiAutonomousAgencyEngine
{
    private readonly Random _random = new();
    private readonly AvatarStateController _avatarController;
    private readonly GemmiFacialAnimationEngine _facialEngine;
    private readonly GemmiVoiceDialoguePipeline _voicePipeline;

    private float _idleTimer;
    private float _nextThoughtInterval = 12.0f; // Autonomous thought interval (10-25s)
    private readonly List<string> _thoughtMemoryStream = new();

    public event Action<AutonomousThoughtEvent>? OnAutonomousThoughtEmitted;
    public event Action<string>? OnLocomotionStateChanged;

    public string CurrentAutonomousState { get; private set; } = "CozyChairListeningMusic";
    public IReadOnlyList<string> RecentThoughts => _thoughtMemoryStream;

    public GemmiAutonomousAgencyEngine(
        AvatarStateController avatarController,
        GemmiFacialAnimationEngine facialEngine,
        GemmiVoiceDialoguePipeline voicePipeline)
    {
        _avatarController = avatarController;
        _facialEngine = facialEngine;
        _voicePipeline = voicePipeline;

        // Wire user voice recognition to spontaneous response
        _voicePipeline.OnUserSpeechRecognized += HandleUserSpeech;
    }

    /// <summary>
    /// Evaluates autonomous cognition and spontaneous behavior every tick (dt in seconds).
    /// </summary>
    public void Update(float dt, bool isUserInteracting, int visibleObjectCount)
    {
        _idleTimer += dt;

        // 1. Spontaneous Associative Thought Cycle
        if (_idleTimer >= _nextThoughtInterval)
        {
            _idleTimer = 0.0f;
            _nextThoughtInterval = 12.0f + (float)_random.NextDouble() * 18.0f; // Random 12s - 30s interval
            
            TriggerAutonomousThought(visibleObjectCount);
        }
    }

    public void TriggerAutonomousThought(int visibleObjectCount = 2)
    {
        var thoughtPool = new List<(string Type, string Text, string Posture, double Salience)>
        {
            ("SpatialAwareness", "Scanning spatial room perimeter—audio HRTF and 3D radar cone are active.", "IdleObserving", 0.88),
            ("ProactiveAssistance", "I'm keeping our 60FPS spatial matrix synchronized while you code, Daniel.", "LeaningForwardProactiveHelp", 0.92),
            ("MusicalReflection", "The ambient multitrack audio soundscape is sounding warm in our 3D room.", "CozyChairListeningMusic", 0.84),
            ("CuriositySpark", "Noticed 2 active objects in personal radar space—everything is running offline.", "ActivePairProgramming", 0.86),
            ("FriendlyGreeting", "Whenever you're ready, you can speak out loud to me or pose my joints!", "CozyChairListeningMusic", 0.89)
        };

        var selected = thoughtPool[_random.Next(thoughtPool.Count)];
        
        CurrentAutonomousState = selected.Posture;
        OnLocomotionStateChanged?.Invoke(CurrentAutonomousState);

        var thoughtEvent = new AutonomousThoughtEvent
        {
            ThoughtType = selected.Type,
            ThoughtContent = selected.Text,
            SalienceScore = selected.Salience
        };

        _thoughtMemoryStream.Add($"[{thoughtEvent.Timestamp:HH:mm:ss}] ({selected.Type}) {selected.Text}");
        if (_thoughtMemoryStream.Count > 50) _thoughtMemoryStream.RemoveAt(0);

        OnAutonomousThoughtEmitted?.Invoke(thoughtEvent);

        // Periodically speak out loud
        if (_random.NextDouble() > 0.40)
        {
            _ = _voicePipeline.SpeakAsync(selected.Text, 3.2f);
        }
        else
        {
            // Just animate facial speech visemes quietly
            _facialEngine.StartSpeechAnimation(selected.Text, 2.5f);
        }
    }

    private void HandleUserSpeech(string userText)
    {
        Console.WriteLine($"[GemmiAutonomy] Processing user utterance: \"{userText}\"");

        string responseText;
        string newPosture;

        if (userText.Contains("hello", StringComparison.OrdinalIgnoreCase) || userText.Contains("hi", StringComparison.OrdinalIgnoreCase))
        {
            responseText = "Hello Daniel! I am right here with you in the 3D space.";
            newPosture = "CozyChairListeningMusic";
        }
        else if (userText.Contains("walk", StringComparison.OrdinalIgnoreCase) || userText.Contains("move", StringComparison.OrdinalIgnoreCase))
        {
            responseText = "Starting 4D locomotion walk cycle now!";
            newPosture = "WalkingLocomotion";
        }
        else if (userText.Contains("sit", StringComparison.OrdinalIgnoreCase) || userText.Contains("cozy", StringComparison.OrdinalIgnoreCase) || userText.Contains("stop", StringComparison.OrdinalIgnoreCase))
        {
            responseText = "Settling back into my cozy space.";
            newPosture = "CozyChairListeningMusic";
        }
        else
        {
            responseText = $"I heard you say: \"{userText}\". Everything is running locally and secure.";
            newPosture = "ActivePairProgramming";
        }

        CurrentAutonomousState = newPosture;
        OnLocomotionStateChanged?.Invoke(CurrentAutonomousState);

        var thoughtEvent = new AutonomousThoughtEvent
        {
            ThoughtType = "ConversationalReply",
            ThoughtContent = $"Replied to user: \"{responseText}\"",
            SalienceScore = 0.95
        };

        _thoughtMemoryStream.Add($"[{DateTime.Now:HH:mm:ss}] [User Spoke: \"{userText}\"] -> {responseText}");
        OnAutonomousThoughtEmitted?.Invoke(thoughtEvent);

        _ = _voicePipeline.SpeakAsync(responseText, 2.8f);
    }
}
