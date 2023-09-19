using System;

namespace Duende.IdentityServer.Models;

/// <summary>
/// Represents a <em>persisted</em> Pushed Authorization Request.
/// </summary>
public class PushedAuthorizationRequest
{
    public string RequestUri { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Parameters { get; set; }
}
