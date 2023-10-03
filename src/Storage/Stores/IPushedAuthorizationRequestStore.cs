// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable


using Duende.IdentityServer.Models;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// The interface for a service that stores pushed authorization requests.
/// </summary>
public interface IPushedAuthorizationRequestStore
{
    /// <summary>
    /// Stores the pushed authorization request.
    /// </summary>
    /// <param name="pushedAuthorizationRequest">The request.</param>
    /// <returns></returns>
    /// TODO - When this fails, do we need to send that failure to the caller?
    Task StoreAsync(PushedAuthorizationRequest pushedAuthorizationRequest);

    /// <summary>
    /// Consumes the pushed authorization request, indicating that it should not be used again. Repeated use
    /// could indicate some form of replay attack, but also could indicate that an end user refreshed their
    /// browser or otherwise retried a request that consumed the pushed authorization request.
    /// </summary>
    /// <param name="referenceValue">The reference value of the pushed authorization request. This is the identifier
    /// within the request_uri parameter.</param>
    /// <returns></returns>
    Task ConsumeAsync(string referenceValue);

    /// <summary>
    /// Gets the pushed authorization request.
    /// </summary>
    /// <param name="referenceValue">The reference value of the pushed authorization request. This is the identifier
    /// within the request_uri parameter.</param>
    /// <returns>The pushed authorization request, or null if the request does not exist or was previously consumed.
    /// </returns>
    Task<PushedAuthorizationRequest?> GetAsync(string referenceValue);
}
