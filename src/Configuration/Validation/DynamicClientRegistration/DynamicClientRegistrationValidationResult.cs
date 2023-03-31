// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <summary>
/// Represents the result of validating a dynamic client registration request.
/// </summary>
public abstract class DynamicClientRegistrationValidationResult { }

/// <summary>
/// Represents an error that occurred during validation of a dynamic client
/// registration request.
/// </summary>
public class DynamicClientRegistrationValidationError : DynamicClientRegistrationValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="DynamicClientRegistrationValidationError"/> class.
    /// </summary>
    /// <param name="error">The error code for the error that occurred during
    /// validation. Error codes defined by RFC 7591 are defined as constants in
    /// the <see cref="DynamicClientRegistrationErrors" /> class.</param>
    /// <param name="errorDescription">A human-readable description of the error
    /// that occurred during validation.</param>
    public DynamicClientRegistrationValidationError(string error, string errorDescription)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    /// <summary>
    /// Gets or sets the error code for the error that occurred during
    /// validation. Error codes defined by RFC 7591 are defined as constants in
    /// the <see cref="DynamicClientRegistrationErrors" /> class.
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the error that occurred
    /// during validation.
    /// </summary>
    public string ErrorDescription { get; set; }
}

/// <summary>
/// Represents a successfully validated dynamic client registration request.
/// </summary>
public class DynamicClientRegistrationValidatedRequest : DynamicClientRegistrationValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="DynamicClientRegistrationValidatedRequest"/> class with the
    /// specified client and original request.
    /// </summary>
    /// <param name="client">The validated client.</param>
    /// <param name="originalRequest">The original dynamic client registration
    /// request.</param>
    public DynamicClientRegistrationValidatedRequest(Client client, DynamicClientRegistrationRequest originalRequest)
    {
        Client = client;
        OriginalRequest = originalRequest;
    }

    /// <summary>
    /// Gets or sets the validated client.
    /// </summary>
    public Client Client { get; set; }

    /// <summary>
    /// Gets or sets the original dynamic client registration request.
    /// </summary>
    public DynamicClientRegistrationRequest OriginalRequest { get; set; }
}
