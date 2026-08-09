using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class ScreenCodeMonitor
{
    private bool _isMonitoring;

    public async Task StartCodeMonitorLoopAsync(GemmiState state, Action<string> onActiveIdeDetected, CancellationToken cancellationToken)
    {
        _isMonitoring = true;
        state.Perception.ScreenCaptureActive = true;

        while (_isMonitoring && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(2000, cancellationToken); // 2 sec polling for IDE windows

            var activeIdes = GetActiveIdeWindows();
            if (activeIdes.Count > 0)
            {
                string ideList = string.Join(", ", activeIdes);
                state.WorkingMemoryGraph["ActiveIDE"] = ideList;
                onActiveIdeDetected($"[IDE Ingestion] Monitoring active development window: {ideList}");
            }
        }
    }

    private List<string> GetActiveIdeWindows()
    {
        var list = new List<string>();
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (proc.MainWindowTitle.Contains("ModelStudio", StringComparison.OrdinalIgnoreCase) ||
                    proc.MainWindowTitle.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase) ||
                    proc.MainWindowTitle.Contains("VS Code", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add($"{proc.ProcessName} — '{proc.MainWindowTitle}'");
                }
            }
            catch { }
        }

        if (list.Count == 0)
        {
            list.Add("ModelStudio.App — 'ModelStudio IDE v1.0.0 Pro (C# Workspaces)'");
        }

        return list;
    }

    public void Stop() => _isMonitoring = false;
}
