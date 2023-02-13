// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Interface for the token request validator
/// </summary>
public interface ITokenRequestValidator
{
    // TODO: can we remove? this was not designed to be replaced. can mark with obsolete and remove in v7.0?
    /// <summary>
    /// Validates the request.
    /// </summary>
    Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);

    /// <summary>
    /// Validates the request.
    /// </summary>
    Task<TokenRequestValidationResult> ValidateRequestAsync(TokenRequestValidationContext context) => ValidateRequestAsync(context.RequestParameters, context.ClientValidationResult);
}