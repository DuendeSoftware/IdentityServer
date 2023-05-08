// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <summary>
/// Represents the result of validating a dynamic client registration request.
/// </summary>
public interface IDynamicClientRegistrationValidationResult { }

/// <summary>
/// Represents a successfully validated dynamic client registration request.
/// </summary>
public class DynamicClientRegistrationValidatedRequest : IDynamicClientRegistrationValidationResult
{
}
