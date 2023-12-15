// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Abstract access to the current issuer name
/// </summary>
public interface IIssuerNameService
{
    /// <summary>
    /// Returns the issuer name for the current request
    /// </summary>
    /// <returns></returns>
    Task<string> GetCurrentAsync();
}
