using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Hardware;

public class HardwareSensorTelemetry
{
    public string SensorName { get; set; } = "";
    public string PortName { get; set; } = "COM3";
    public double SensorValue { get; set; }
    public string Unit { get; set; } = "";
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

public class HardwareSensorGateway : IDisposable
{
    private MicrophoneAudioSensor? _micSensor;
    private CameraVisionSensor? _cameraSensor;

    public MicrophoneAudioSensor? Microphone => _micSensor;
    public CameraVisionSensor? Camera => _cameraSensor;

    public bool IsAudioHardwareActive => _micSensor?.IsRecording ?? false;
    public bool IsVisionHardwareActive => _cameraSensor?.IsCapturing ?? false;

    [SupportedOSPlatform("windows")]
    public bool InitializeMicrophone(int deviceNumber = 0, int sampleRate = 16000)
    {
        _micSensor = new MicrophoneAudioSensor();
        return _micSensor.StartRecording(deviceNumber, sampleRate);
    }

    public bool InitializeCamera(int deviceId = 0, int fps = 15)
    {
        _cameraSensor = new CameraVisionSensor(targetFps: fps);
        return _cameraSensor.StartCapture(deviceId);
    }

    public static List<HardwareSensorTelemetry> ScanHardwareSensors()
    {
        return new List<HardwareSensorTelemetry>
        {
            new HardwareSensorTelemetry { SensorName = "Edge TPU Core Temp", PortName = "USB-C (TPU 01)", SensorValue = 48.2, Unit = "°C" },
            new HardwareSensorTelemetry { SensorName = "Chassis Ambient Temp", PortName = "COM3 / I2C", SensorValue = 24.1, Unit = "°C" },
            new HardwareSensorTelemetry { SensorName = "PMIC 12V Power Rail", PortName = "JTAG UART", SensorValue = 12.04, Unit = "V" },
            new HardwareSensorTelemetry { SensorName = "NFC Antenna RSSI", PortName = "SPI 0", SensorValue = -42.0, Unit = "dBm" }
        };
    }

    public static void PushTelemetryToMemory(GemmiState state)
    {
        var sensors = ScanHardwareSensors();
        foreach (var s in sensors)
        {
            state.MemoryBuffer.AddObservation(
                MemoryCategory.System,
                $"Deep Horizon Hardware Sensor ({s.SensorName}): {s.SensorValue} {s.Unit} via {s.PortName}",
                salienceScore: 0.50f
            );
        }
    }

    public void Dispose()
    {
        _micSensor?.Dispose();
        _cameraSensor?.Dispose();
    }
}
