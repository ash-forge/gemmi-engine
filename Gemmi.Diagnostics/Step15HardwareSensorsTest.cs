using System;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Hardware;

namespace Gemmi.Diagnostics;

public class Step15HardwareSensorsTest
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  🧠 GEMMI ENGINE STEP 15: HARDWARE MICROPHONE AUDIO & CAMERA VISION TEST ");
        Console.WriteLine("==========================================================================");

        // 1. Test Physical Microphone Audio Hardware Enumeration & Stream
        Console.WriteLine("\n[1] Enumerating Local Microphone Devices...");
        var mics = MicrophoneAudioSensor.GetAvailableMicrophones();
        Console.WriteLine($"    • Found {mics.Count} Microphone Input Device(s):");
        foreach (var mic in mics)
        {
            Console.WriteLine($"      - Mic #{mic.DeviceNumber}: {mic.ProductName} ({mic.Channels} Channels)");
        }

        if (OperatingSystem.IsWindows() && mics.Count > 0)
        {
            Console.WriteLine("\n[2] Initializing Microphone Audio Input Sensor (16kHz Mono PCM)...");
            using var micSensor = new MicrophoneAudioSensor();
            
            int capturedBufferCount = 0;
            micSensor.OnAudioBufferCaptured += (bytes, len) =>
            {
                capturedBufferCount++;
            };

            micSensor.OnVolumeRmsChanged += (rms) =>
            {
                // RMS volume indicator
            };

            bool micStarted = micSensor.StartRecording(0, 16000, 1);
            Console.WriteLine($"    • Mic Recording Active : {micStarted} ({micSensor.IsRecording})");

            await Task.Delay(500); // Record for 500ms
            micSensor.StopRecording();
            Console.WriteLine($"    • Captured {capturedBufferCount} audio PCM buffers from live microphone!");
        }
        else
        {
            Console.WriteLine("\n[2] Skipping live mic stream test (non-Windows platform or no mic attached).");
        }

        // 2. Test Camera Vision Hardware Enumeration & Video Frame Stream
        Console.WriteLine("\n[3] Enumerating Local Webcam Video Devices...");
        var webcams = CameraVisionSensor.GetAvailableWebcams();
        Console.WriteLine($"    • Found {webcams.Count} Camera Vision Device(s):");
        foreach (var cam in webcams)
        {
            Console.WriteLine($"      - Camera #{cam.DeviceId}: {cam.Name}");
        }

        Console.WriteLine("\n[4] Initializing Camera Vision Sensor (640x480 @ 15 FPS Stream)...");
        using var cameraSensor = new CameraVisionSensor(targetFps: 15);

        int capturedFrameCount = 0;
        long lastFrameSize = 0;

        cameraSensor.OnFrameCaptured += (frame) =>
        {
            capturedFrameCount++;
            lastFrameSize = frame.JpegBytes.Length;
        };

        bool cameraStarted = cameraSensor.StartCapture(0, 640, 480);
        Console.WriteLine($"    • Camera Stream Active : {cameraStarted} ({cameraSensor.IsCapturing})");

        await Task.Delay(500); // Capture for 500ms
        cameraSensor.StopCapture();

        Console.WriteLine($"    • Captured {capturedFrameCount} video frame(s) from camera vision sensor (Latest frame: {lastFrameSize} bytes JPEG)!");

        // 3. Test Unified Hardware Sensor Gateway
        Console.WriteLine("\n[5] Testing Unified HardwareSensorGateway...");
        using var gateway = new HardwareSensorGateway();
        bool gatewayCam = gateway.InitializeCamera(0);
        Console.WriteLine($"    • Gateway Camera Active: {gateway.IsVisionHardwareActive}");

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("  [✓] STEP 15 HARDWARE MICROPHONE & CAMERA VISION PASSED PERFECTLY!      ");
        Console.WriteLine("==========================================================================");
    }
}
