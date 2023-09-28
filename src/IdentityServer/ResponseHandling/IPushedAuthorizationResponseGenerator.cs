// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.ResponseHandling;


public interface IPushedAuthorizationResponseGenerator
{
    Task<PushedAuthorizationResponse> CreateResponseAsync(ValidatedPushedAuthorizationRequest request);
}
