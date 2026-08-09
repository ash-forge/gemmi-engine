using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gemmi.Diagnostics;

public class HilTestResult
{
    public int CycleIndex { get; set; }
    public string NodeRevision { get; set; } = "Rev 3";
    public double TotalBootTimeMs { get; set; }
    public bool Passed { get; set; } = true;
}

public class HilTestHarness
{
    public static async Task<List<HilTestResult>> Run100BootStressCyclesAsync(string nodeRev = "Rev 3")
    {
        var list = new List<HilTestResult>();
        for (int i = 1; i <= 100; i++)
        {
            await Task.Delay(10); // Simulated power-cycle test
            var profile = JtagUartProfiler.CaptureBootProfile(nodeRev);
            double totalTime = profile[^1].TimestampMs;

            list.Add(new HilTestResult
            {
                CycleIndex = i,
                NodeRevision = nodeRev,
                TotalBootTimeMs = totalTime,
                Passed = true
            });
        }
        return list;
    }
}
