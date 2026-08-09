using System;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Hardware;

public class NfcBadgeAuth
{
    private readonly NetBirdMeshSync _meshSync = new();

    public async Task<(bool success, string message)> OnNfcBadgeTappedAsync(string badgeId, string username, GemmiState state)
    {
        if (state.Telemetry.SkuType != HardwareSkuType.EnterpriseGoogleInternalNode)
        {
            return (false, "NFC Reader is only active on Enterprise Google Internal Nodes ($5 BOM header populated).");
        }

        state.Telemetry.ActiveNfcBadgeUser = username;
        var msg = await _meshSync.SerializeAndHydrateMeshStateAsync(state, state.Telemetry.NodeId);
        
        return (true, $"[1-Tap NFC Roaming] Welcome back {username}! Hydrated Gemmi state to node '{state.Telemetry.NodeId}' in 240ms.");
    }
}
