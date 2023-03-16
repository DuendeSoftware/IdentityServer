// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <summary>
/// Represents the context of validation for dynamic client registration,
/// including the original DCR request, the client model that is built up
/// through validation, the caller who made the DCR request, and other
/// contextual information.
/// </summary>
public class DynamicClientRegistrationValidationContext
{
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="DynamicClientRegistrationValidationContext"/> class.
    /// </summary>
    /// <param name="request">The original dynamic client registration request.</param>
    /// <param name="caller">The <see cref="ClaimsPrincipal"/> that made the DCR request.</param>
    public DynamicClientRegistrationValidationContext(DynamicClientRegistrationRequest request, ClaimsPrincipal caller)
    {
        Request = request;
        Caller = caller;
    }

    /// <summary>
    /// The client model that is built up through validation.
    /// </summary>
    public Client Client { get; set; } = new();

    /// <summary>
    /// The original dynamic client registration request.
    /// </summary>
    public DynamicClientRegistrationRequest Request { get; set; }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> that made the DCR request.
    /// </summary>
    public ClaimsPrincipal Caller { get; set; }

    /// <summary>
    /// A collection where additional contextual information may be stored. This
    /// is intended as a place to pass additional custom state between
    /// validation steps.
    /// </summary>
    public Dictionary<string, object> Items { get; set; } = new();
}
