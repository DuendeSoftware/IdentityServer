// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Configuration.WebApi.v1;

/// <summary>
/// Models a client secret with identifier and expiration
/// </summary>
public class Secret
{
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the expiration.
    /// </summary>
    /// <value>
    /// The expiration.
    /// </value>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the type of the client secret.
    /// </summary>
    /// <value>
    /// The type of the client secret.
    /// </value>
    public string Type { get; set; }
}