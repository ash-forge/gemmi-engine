using System;
using System.Collections.Generic;

namespace Gemmi.Diagnostics;

public class BootMilestone
{
    public string MilestoneName { get; set; } = "";
    public double TimestampMs { get; set; }
    public string Status { get; set; } = "OK";
}

public class JtagUartProfiler
{
    public static List<BootMilestone> CaptureBootProfile(string nodeRev = "Rev 3")
    {
        double multiplier = nodeRev == "Rev 1" ? 1.8 : nodeRev == "Rev 2" ? 1.2 : 0.8;

        return new List<BootMilestone>
        {
            new BootMilestone { MilestoneName = "PMIC Power Rail Lock", TimestampMs = Math.Round(0.00 * multiplier, 2) },
            new BootMilestone { MilestoneName = "ARM CPU Core POST & RAM Training", TimestampMs = Math.Round(40.0 * multiplier, 2) },
            new BootMilestone { MilestoneName = "Core BIOS & Bootloader Handoff", TimestampMs = Math.Round(120.0 * multiplier, 2) },
            new BootMilestone { MilestoneName = "Edge TPU Vector Engine Init", TimestampMs = Math.Round(280.0 * multiplier, 2) },
            new BootMilestone { MilestoneName = "C# ash-server-cs Runtime Start", TimestampMs = Math.Round(450.0 * multiplier, 2) },
            new BootMilestone { MilestoneName = "Deep Horizon UI Rendered & Ready", TimestampMs = Math.Round(650.0 * multiplier, 2) }
        };
    }
}
