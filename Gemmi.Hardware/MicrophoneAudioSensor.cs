using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NAudio.Wave;

namespace Gemmi.Hardware;

public class AudioDeviceInfo
{
    public int DeviceNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Channels { get; set; }
}

[SupportedOSPlatform("windows")]
public class MicrophoneAudioSensor : IDisposable
{
    private WaveInEvent? _waveIn;
    private bool _isRecording;
    private float _currentRmsLevel;

    public event Action<byte[], int>? OnAudioBufferCaptured;
    public event Action<float>? OnVolumeRmsChanged;

    public bool IsRecording => _isRecording;
    public float CurrentRmsLevel => _currentRmsLevel;

    public static List<AudioDeviceInfo> GetAvailableMicrophones()
    {
        var devices = new List<AudioDeviceInfo>();
        if (!OperatingSystem.IsWindows()) return devices;

        int count = WaveInEvent.DeviceCount;
        for (int i = 0; i < count; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo
            {
                DeviceNumber = i,
                ProductName = capabilities.ProductName,
                Channels = capabilities.Channels
            });
        }
        return devices;
    }

    public bool StartRecording(int deviceNumber = 0, int sampleRate = 16000, int channels = 1)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("[MicrophoneAudioSensor] NAudio WaveIn is only supported on Windows.");
            return false;
        }

        if (_isRecording) return true;

        try
        {
            if (WaveInEvent.DeviceCount == 0)
            {
                Console.WriteLine("[MicrophoneAudioSensor] No microphone input devices found on system.");
                return false;
            }

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(sampleRate, 16, channels),
                BufferMilliseconds = 50
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
            _isRecording = true;

            Console.WriteLine($"[MicrophoneAudioSensor] Recording started on Mic Device #{deviceNumber} ({sampleRate}Hz, {channels}ch PCM)...");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MicrophoneAudioSensor] Failed to start recording: {ex.Message}");
            return false;
        }
    }

    public void StopRecording()
    {
        if (!_isRecording || _waveIn == null) return;

        try
        {
            _waveIn.StopRecording();
            _isRecording = false;
            Console.WriteLine("[MicrophoneAudioSensor] Recording stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MicrophoneAudioSensor] Stop recording exception: {ex.Message}");
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;

        // Calculate RMS Volume Level for Voice Activity Detection (VAD)
        float sum = 0f;
        int sampleCount = e.BytesRecorded / 2;

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            float sampleFloat = sample / 32768.0f;
            sum += sampleFloat * sampleFloat;
        }

        _currentRmsLevel = MathF.Sqrt(sum / Math.Max(1, sampleCount));
        OnVolumeRmsChanged?.Invoke(_currentRmsLevel);

        // Forward raw PCM audio bytes to speech-to-text listener
        OnAudioBufferCaptured?.Invoke(e.Buffer, e.BytesRecorded);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;
        if (e.Exception != null)
        {
            Console.WriteLine($"[MicrophoneAudioSensor] Recording stopped error: {e.Exception.Message}");
        }
    }

    public void Dispose()
    {
        StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
    }
}
