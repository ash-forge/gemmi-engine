using System;
using System.Collections.Generic;

namespace Gemmi.Core;

public enum HardwareSkuType
{
    ConsumerDesktopNode,
    EnterpriseGoogleInternalNode
}

public class NodeTelemetry
{
    public string NodeId { get; set; } = "DeepHorizon-Node-01";
    public HardwareSkuType SkuType { get; set; } = HardwareSkuType.ConsumerDesktopNode;
    public double CpuTemperatureC { get; set; } = 42.5;
    public double TpuUsagePercent { get; set; } = 18.2;
    public double MemoryUsedGb { get; set; } = 4.2;
    public double TotalMemoryGb { get; set; } = 32.0;
    public bool NfcReaderActive { get; set; } = true;
    public string ActiveNfcBadgeUser { get; set; } = "Unauthenticated";
    public bool NetBirdMeshConnected { get; set; } = true;
}

public class PerceptionStreamState
{
    public bool AudioVadActive { get; set; } = true;
    public bool CameraVisionActive { get; set; } = true;
    public bool ScreenCaptureActive { get; set; } = true;
    public double SpontaneousInitiationScore { get; set; } = 0.0;
    public string LastObservedContext { get; set; } = "Idle - Monitoring local environment";
}

public class GemmiState
{
    public string ActiveSessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;
    public NodeTelemetry Telemetry { get; set; } = new();
    public PerceptionStreamState Perception { get; set; } = new();
    public List<string> RecentSpontaneousAlerts { get; set; } = new();
    public Dictionary<string, string> WorkingMemoryGraph { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // High-Speed Memory Engine Components
    public WorkingMemoryBuffer MemoryBuffer { get; } = new(1000);
    public EpisodicMemoryGraph MemoryGraph { get; } = new();
    public MemoryQueryEngine MemoryQuery { get; }
    public AsyncMemoryStore MemoryStore { get; } = new();

    public GemmiState()
    {
        MemoryQuery = new MemoryQueryEngine(MemoryBuffer, MemoryGraph);
    }
}
