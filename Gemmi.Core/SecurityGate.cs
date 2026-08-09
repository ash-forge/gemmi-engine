using System;
using System.Collections.Generic;

namespace Gemmi.Core;

public class SecurityToken
{
    public string ProjectName { get; set; } = "";
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
    public bool IsActive => DateTime.UtcNow < ExpiresAt;
}

public class SecurityGate
{
    private readonly Dictionary<string, SecurityToken> _activeTokens = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAirGappedByDefault { get; set; } = true;

    public SecurityToken GrantScopedAccess(string projectName, int hours = 1)
    {
        var token = new SecurityToken
        {
            ProjectName = projectName,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(hours)
        };
        _activeTokens[projectName] = token;
        return token;
    }

    public bool CheckProjectAccess(string projectName)
    {
        if (_activeTokens.TryGetValue(projectName, out var token))
        {
            return token.IsActive;
        }
        return false;
    }
}
