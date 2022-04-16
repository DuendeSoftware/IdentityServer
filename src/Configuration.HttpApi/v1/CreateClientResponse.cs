// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Configuration.WebApi.v1;

public class CreateClientResponse
{
    /// <summary>
    /// Unique ID of the client
    /// </summary>
    public string ClientId { get; set; }

    public int Version { get; set; }
}