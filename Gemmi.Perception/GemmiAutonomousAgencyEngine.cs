using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class AutonomousThoughtEvent
{
    public string ThoughtType { get; set; } = "Observation"; // Observation, Idea, Question, Greeting, ConversationalReply
    public string ThoughtContent { get; set; } = string.Empty;
    public double SalienceScore { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sovereign Autonomous Agency & Proactive Cognition Engine.
/// Drives spontaneous behavioral transitions, associative thought sparks,
/// and full-duplex neural LLM dialogue via LocalLlamaInferenceEngine on port 11436.
/// </summary>
public class GemmiAutonomousAgencyEngine
{
    private readonly Random _random = new();
    private readonly AvatarStateController _avatarController;
    private readonly GemmiFacialAnimationEngine _facialEngine;
    private readonly GemmiVoiceDialoguePipeline _voicePipeline;
    private readonly LocalLlamaInferenceEngine _llamaEngine;
    private readonly GemmiState _state;

    private float _idleTimer;
    private float _nextThoughtInterval = 15.0f; // Autonomous thought interval (15-30s)
    private readonly List<string> _thoughtMemoryStream = new();
    private readonly List<(string Role, string Content)> _conversationHistory = new();

    public event Action<AutonomousThoughtEvent>? OnAutonomousThoughtEmitted;
    public event Action<string>? OnLocomotionStateChanged;

    public string CurrentAutonomousState { get; private set; } = "CozyChairListeningMusic";
    public IReadOnlyList<string> RecentThoughts => _thoughtMemoryStream;
    public IReadOnlyList<(string Role, string Content)> ConversationHistory => _conversationHistory;

    public GemmiAutonomousAgencyEngine(
        AvatarStateController avatarController,
        GemmiFacialAnimationEngine facialEngine,
        GemmiVoiceDialoguePipeline voicePipeline,
        GemmiState? state = null,
        LocalLlamaInferenceEngine? llamaEngine = null)
    {
        _avatarController = avatarController;
        _facialEngine = facialEngine;
        _voicePipeline = voicePipeline;
        _state = state ?? new GemmiState();
        _llamaEngine = llamaEngine ?? new LocalLlamaInferenceEngine("http://127.0.0.1:11436");

        // Wire user voice recognition to neural LLM dialogue handler
        _voicePipeline.OnUserSpeechRecognized += text =>
        {
            _ = ProcessUserMessageAsync(text);
        };
    }

    /// <summary>
    /// Evaluates autonomous cognition and spontaneous behavior every tick (dt in seconds).
    /// </summary>
    public void Update(float dt, bool isUserInteracting, int visibleObjectCount)
    {
        _idleTimer += dt;

        // Spontaneous Associative Thought Cycle
        if (_idleTimer >= _nextThoughtInterval)
        {
            _idleTimer = 0.0f;
            _nextThoughtInterval = 15.0f + (float)_random.NextDouble() * 20.0f;
            
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
            ("CuriositySpark", "Noticed active objects in personal radar space—everything is running locally.", "ActivePairProgramming", 0.86),
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

        // Animate facial speech visemes quietly
        _facialEngine.StartSpeechAnimation(selected.Text, 2.5f);
    }

    /// <summary>
    /// Processes a user chat or spoken message through the local neural LLM (port 11436)
    /// and streams the response back via speech synthesis and 3D facial visemes.
    /// </summary>
    public async Task<string> ProcessUserMessageAsync(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return string.Empty;

        Console.WriteLine($"[GemmiDialogue] 👤 User: \"{userText}\"");

        // 1. Posture heuristic adjustments
        if (userText.Contains("walk", StringComparison.OrdinalIgnoreCase) || userText.Contains("move", StringComparison.OrdinalIgnoreCase))
        {
            CurrentAutonomousState = "WalkingLocomotion";
            OnLocomotionStateChanged?.Invoke(CurrentAutonomousState);
        }
        else if (userText.Contains("sit", StringComparison.OrdinalIgnoreCase) || userText.Contains("cozy", StringComparison.OrdinalIgnoreCase) || userText.Contains("stop", StringComparison.OrdinalIgnoreCase))
        {
            CurrentAutonomousState = "CozyChairListeningMusic";
            OnLocomotionStateChanged?.Invoke(CurrentAutonomousState);
        }

        // 2. Query Local Llama Neural Model on Port 11436
        string reply = await _llamaEngine.GenerateConversationalReplyAsync(userText, _state, _conversationHistory);

        // 3. Update Conversation History (rolling 8 turns)
        _conversationHistory.Add(("user", userText));
        _conversationHistory.Add(("assistant", reply));
        while (_conversationHistory.Count > 8) _conversationHistory.RemoveAt(0);

        // 4. Push to Working Memory Buffer
        _state.MemoryBuffer.AddObservation(
            MemoryCategory.Voice,
            $"User: \"{userText}\" | Gemmi: \"{reply}\"",
            salienceScore: 0.90f
        );

        // 5. Emit Thought & Log
        var thoughtEvent = new AutonomousThoughtEvent
        {
            ThoughtType = "ConversationalReply",
            ThoughtContent = reply,
            SalienceScore = 0.95
        };

        _thoughtMemoryStream.Add($"[{DateTime.Now:HH:mm:ss}] [User] \"{userText}\" -> [Gemmi] \"{reply}\"");
        if (_thoughtMemoryStream.Count > 50) _thoughtMemoryStream.RemoveAt(0);

        OnAutonomousThoughtEmitted?.Invoke(thoughtEvent);

        // 6. Speak Reply out loud with 3D Lip-Sync Visemes
        float estimatedDuration = MathF.Max(2.0f, reply.Length * 0.065f);
        await _voicePipeline.SpeakAsync(reply, estimatedDuration);

        return reply;
    }
}
