// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#pragma warning disable 1591

namespace Duende.IdentityServer.Validation;

public enum BearerTokenUsageType
{
    AuthorizationHeader = 0,
    PostBody = 1,
    QueryString = 2
}
