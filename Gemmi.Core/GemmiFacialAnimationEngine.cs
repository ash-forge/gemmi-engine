using System;
using System.Collections.Generic;

namespace Gemmi.Core;

/// <summary>
/// Represents standard Facial Action Coding System (FACS) blendshape weights [0.0f - 1.0f].
/// </summary>
public class FacialMorphWeights
{
    // Speech & Lip-Sync Visemes
    public float JawOpen { get; set; }
    public float MouthSmileLeft { get; set; }
    public float MouthSmileRight { get; set; }
    public float MouthFunnel { get; set; }
    public float MouthPucker { get; set; }
    public float LipCornerPuller { get; set; }

    // Eyes & Gaze
    public float EyeBlinkLeft { get; set; }
    public float EyeBlinkRight { get; set; }
    public float EyeSquintLeft { get; set; }
    public float EyeSquintRight { get; set; }

    // Eyebrows & Mood Expressions
    public float BrowInnerUp { get; set; }
    public float BrowDownLeft { get; set; }
    public float BrowDownRight { get; set; }

    public Dictionary<string, float> ToDictionary()
    {
        return new Dictionary<string, float>
        {
            ["jawOpen"] = Math.Clamp(JawOpen, 0.0f, 1.0f),
            ["mouthSmileLeft"] = Math.Clamp(MouthSmileLeft, 0.0f, 1.0f),
            ["mouthSmileRight"] = Math.Clamp(MouthSmileRight, 0.0f, 1.0f),
            ["mouthFunnel"] = Math.Clamp(MouthFunnel, 0.0f, 1.0f),
            ["mouthPucker"] = Math.Clamp(MouthPucker, 0.0f, 1.0f),
            ["lipCornerPuller"] = Math.Clamp(LipCornerPuller, 0.0f, 1.0f),
            ["eyeBlinkLeft"] = Math.Clamp(EyeBlinkLeft, 0.0f, 1.0f),
            ["eyeBlinkRight"] = Math.Clamp(EyeBlinkRight, 0.0f, 1.0f),
            ["eyeSquintLeft"] = Math.Clamp(EyeSquintLeft, 0.0f, 1.0f),
            ["eyeSquintRight"] = Math.Clamp(EyeSquintRight, 0.0f, 1.0f),
            ["browInnerUp"] = Math.Clamp(BrowInnerUp, 0.0f, 1.0f),
            ["browDownLeft"] = Math.Clamp(BrowDownLeft, 0.0f, 1.0f),
            ["browDownRight"] = Math.Clamp(BrowDownRight, 0.0f, 1.0f)
        };
    }
}

/// <summary>
/// Real-Time 4D Facial Animation & Phoneme-to-Viseme Lip Sync Engine.
/// Computes natural stochastic micro-blinking, emotional expressions, and speech viseme weights.
/// </summary>
public class GemmiFacialAnimationEngine
{
    private readonly Random _random = new();
    private float _blinkTimer;
    private float _nextBlinkInterval = 3.2f;
    private bool _isBlinking;
    private float _blinkProgress;

    // Active speech viseme state
    private string? _currentSpeechText;
    private float _speechTimeElapsed;
    private float _speechTotalDuration;

    public FacialMorphWeights CurrentWeights { get; } = new();

    public void StartSpeechAnimation(string text, float durationSeconds = 2.5f)
    {
        _currentSpeechText = text;
        _speechTimeElapsed = 0.0f;
        _speechTotalDuration = Math.Max(0.5f, durationSeconds);
    }

    public void StopSpeechAnimation()
    {
        _currentSpeechText = null;
        _speechTimeElapsed = 0.0f;
        CurrentWeights.JawOpen = 0.0f;
        CurrentWeights.MouthFunnel = 0.0f;
        CurrentWeights.MouthPucker = 0.0f;
    }

    /// <summary>
    /// Updates facial animation state per frame at delta time dt (in seconds).
    /// </summary>
    public FacialMorphWeights Update(float dt, string currentEmotionState = "CozyNeutral")
    {
        // 1. Procedural Natural Eye Blinking Cycle
        UpdateProceduralBlinking(dt);

        // 2. Procedural Speech Viseme Lip-Sync Cycle
        UpdateSpeechVisemes(dt);

        // 3. Emotional Micro-Expressions
        UpdateEmotionalExpressions(currentEmotionState);

        return CurrentWeights;
    }

    private void UpdateProceduralBlinking(float dt)
    {
        if (!_isBlinking)
        {
            _blinkTimer += dt;
            if (_blinkTimer >= _nextBlinkInterval)
            {
                _isBlinking = true;
                _blinkProgress = 0.0f;
                _blinkTimer = 0.0f;
                _nextBlinkInterval = 2.5f + (float)_random.NextDouble() * 2.0f; // 2.5s - 4.5s random interval
            }
            CurrentWeights.EyeBlinkLeft = 0.0f;
            CurrentWeights.EyeBlinkRight = 0.0f;
        }
        else
        {
            _blinkProgress += dt / 0.15f; // Fast 150ms natural blink duration
            if (_blinkProgress >= 1.0f)
            {
                _isBlinking = false;
                CurrentWeights.EyeBlinkLeft = 0.0f;
                CurrentWeights.EyeBlinkRight = 0.0f;
            }
            else
            {
                // Smooth sine-wave blink curve: 0 -> 1 -> 0
                float blinkWeight = MathF.Sin(_blinkProgress * MathF.PI);
                CurrentWeights.EyeBlinkLeft = blinkWeight;
                CurrentWeights.EyeBlinkRight = blinkWeight;
            }
        }
    }

    private void UpdateSpeechVisemes(float dt)
    {
        if (string.IsNullOrEmpty(_currentSpeechText) || _speechTimeElapsed >= _speechTotalDuration)
        {
            // Smoothly decay jaw openness back to zero
            CurrentWeights.JawOpen = Math.Max(0.0f, CurrentWeights.JawOpen - dt * 6.0f);
            CurrentWeights.MouthFunnel = Math.Max(0.0f, CurrentWeights.MouthFunnel - dt * 6.0f);
            CurrentWeights.MouthPucker = Math.Max(0.0f, CurrentWeights.MouthPucker - dt * 6.0f);
            return;
        }

        _speechTimeElapsed += dt;
        float cadence = _speechTimeElapsed * 14.0f; // ~4-5 syllables per second cadence

        // Multi-frequency oscillation modeling natural syllable cadence (A/E/I/O/U mouth shapes)
        float jawSine = (MathF.Sin(cadence) + 1.0f) * 0.5f;
        float vowelMod = (MathF.Sin(cadence * 0.5f) + 1.0f) * 0.5f;

        CurrentWeights.JawOpen = 0.15f + jawSine * 0.55f;
        CurrentWeights.MouthFunnel = vowelMod > 0.6f ? (vowelMod - 0.6f) * 1.5f : 0.0f;
        CurrentWeights.MouthPucker = (1.0f - jawSine) * 0.25f;
    }

    private void UpdateEmotionalExpressions(string emotionState)
    {
        switch (emotionState.ToLowerInvariant())
        {
            case "happy":
            case "cozychairlisteningmusic":
            case "cozyneutral":
                CurrentWeights.MouthSmileLeft = 0.45f;
                CurrentWeights.MouthSmileRight = 0.45f;
                CurrentWeights.BrowInnerUp = 0.15f;
                CurrentWeights.BrowDownLeft = 0.0f;
                CurrentWeights.BrowDownRight = 0.0f;
                break;

            case "curious":
            case "leaningforwardproactivehelp":
            case "activepairprogramming":
                CurrentWeights.MouthSmileLeft = 0.25f;
                CurrentWeights.MouthSmileRight = 0.25f;
                CurrentWeights.BrowInnerUp = 0.55f;
                CurrentWeights.EyeSquintLeft = 0.10f;
                CurrentWeights.EyeSquintRight = 0.10f;
                break;

            case "thoughtful":
            case "idleobserving":
                CurrentWeights.MouthSmileLeft = 0.10f;
                CurrentWeights.MouthSmileRight = 0.10f;
                CurrentWeights.BrowInnerUp = 0.30f;
                CurrentWeights.EyeSquintLeft = 0.20f;
                CurrentWeights.EyeSquintRight = 0.20f;
                break;

            default:
                CurrentWeights.MouthSmileLeft = 0.20f;
                CurrentWeights.MouthSmileRight = 0.20f;
                CurrentWeights.BrowInnerUp = 0.10f;
                break;
        }
    }
}
