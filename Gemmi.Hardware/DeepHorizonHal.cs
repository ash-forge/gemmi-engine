using System;
using Gemmi.Core;

namespace Gemmi.Hardware;

public class DeepHorizonHal
{
    public string NodeId { get; set; } = "DeepHorizon-Node-01";
    public HardwareSkuType SkuType { get; set; } = HardwareSkuType.ConsumerDesktopNode;

    public void InitializeNodeHardware(GemmiState state)
    {
        state.Telemetry.NodeId = NodeId;
        state.Telemetry.SkuType = SkuType;
        state.Telemetry.MemoryUsedGb = 4.2;
        state.Telemetry.TotalMemoryGb = SkuType == HardwareSkuType.EnterpriseGoogleInternalNode ? 32.0 : 16.0;
        state.Telemetry.NfcReaderActive = SkuType == HardwareSkuType.EnterpriseGoogleInternalNode;

        state.WorkingMemoryGraph["HAL_Init"] = $"Initialized Deep Horizon HAL on {NodeId} ({SkuType}) at {DateTime.Now:HH:mm:ss}";
    }

    public void TogglePowerRelay()
    {
        // Triggers hardware power relay cycle for HIL boot testing
    }
}
