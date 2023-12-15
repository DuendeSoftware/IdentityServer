// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Validation;
using System.Collections.Generic;
using System.Security.Claims;

namespace Duende.IdentityServer.Models;

/// <summary>
/// Models the information to initiate a user login request due to a CIBA request.
/// </summary>
public class BackchannelUserLoginRequest
{
    /// <summary>
    /// Gets or sets the id of the request in the store.
    /// </summary>
    public string InternalId { get; set; } = default!;
        
    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public ClaimsPrincipal Subject { get; set; } = default!;

    /// <summary>
    /// Gets or sets the binding message.
    /// </summary>
    public string? BindingMessage { get; set; }

    /// <summary>
    /// Gets or sets the authentication context reference classes.
    /// </summary>
    public IEnumerable<string>? AuthenticationContextReferenceClasses { get; set; }

    /// <summary>
    /// Gets or sets the tenant passed in the acr_values.
    /// </summary>
    public string? Tenant { get; set; }
        
    /// <summary>
    /// Gets or sets the idp passed in the acr_values.
    /// </summary>
    public string? IdP { get; set; }

    /// <summary>
    /// Gets or sets the resource indicator.
    /// </summary>
    public IEnumerable<string>? RequestedResourceIndicators { get; set; }

    /// <summary>
    /// Gets or sets the client.
    /// </summary>
    public Client Client { get; set; } = default!;

    /// <summary>
    /// Gets or sets the validated resources.
    /// </summary>
    public ResourceValidationResult ValidatedResources { get; set; } = default!;
}
