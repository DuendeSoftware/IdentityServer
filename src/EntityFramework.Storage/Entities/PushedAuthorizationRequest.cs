// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#pragma warning disable 1591

using System;

namespace Duende.IdentityServer.EntityFramework.Entities;

public class PushedAuthorizationRequest
{
    public int Id { get; set; }
    public string RequestUri { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Parameters { get; set; }
    // REVIEW - Should we include the creation timestamp?
    // public DateTime CreatedAtUtc { get; set; }
}
