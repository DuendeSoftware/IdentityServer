// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Configuration.WebApi.v1;

public class PutClientRequest: Client
{
    /// <summary>
    /// The expected version of the existing client. Used for concurrency control.
    /// </summary>
    public int? ExpectedVersion { get; set; }
}