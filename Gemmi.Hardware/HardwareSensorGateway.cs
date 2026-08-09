using System;
using System.Collections.Generic;
using System.IO.Ports;
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

public class HardwareSensorGateway
{
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
}
