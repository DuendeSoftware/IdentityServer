// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Stores;
using IdentityModel;

namespace Duende.IdentityServer.Services;

// This abstraction is so that we don't have to think about data protection
public interface IPushedAuthorizationService
{
    /// <summary>
    /// Stores the pushed authorization request.
    /// </summary>
    /// <param name="referenceValue"></param>
    /// <param name="expiresAtUtc"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task StoreAsync(DeserializedPushedAuthorizationRequest pushedAuthorizationRequest);

    /// <summary>
    /// Consumes the pushed authorization request, indicating that it should not
    /// be used again. Repeated use could indicate some form of replay attack,
    /// but also could indicate that an end user refreshed their browser or
    /// otherwise retried a request that consumed the pushed authorization
    /// request.
    /// </summary>
    /// <param name="referenceValue">The reference value of the pushed
    /// authorization request. The reference value is the identifier within the
    /// request_uri parameter.</param>
    /// <returns></returns>
    Task ConsumeAsync(string referenceValue);

    /// <summary>
    /// Gets the raw pushed authorization parameters.
    /// </summary>
    /// <param name="referenceValue">The reference value of the pushed
    /// authorization request. The reference value is the identifier within the
    /// request_uri parameter.</param>
    /// <returns>The pushed authorization request, or null if the request does
    /// not exist or was previously consumed.
    /// </returns>
    Task<DeserializedPushedAuthorizationRequest?> GetPushedAuthorizationRequestAsync(string referenceValue);

    // Making this function take the reference value is nice because the API is comprehensible
    // BUT, that means the caller has to extract the reference value from the raw parameters
    // And that is a bit annoying to set up in a test, because you can't replace this implementation with a fake that will just make the test work
}

public class DeserializedPushedAuthorizationRequest
{
    public string ReferenceValue { get; set; }
    public NameValueCollection PushedParameters { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class PushedAuthorizationService : IPushedAuthorizationService
{
    private readonly IPushedAuthorizationSerializer _serializer;
    private readonly IPushedAuthorizationRequestStore _store;

    public PushedAuthorizationService(
        IPushedAuthorizationSerializer serializer,
        IPushedAuthorizationRequestStore store)
    {
        _serializer = serializer;
        _store = store;
    }

    public Task ConsumeAsync(string referenceValue)
    {
        return _store.ConsumeByHashAsync(referenceValue.ToSha256());
    }

    public async Task<DeserializedPushedAuthorizationRequest?> GetPushedAuthorizationRequestAsync(string referenceValue)
    {
        var par = await _store.GetByHashAsync(referenceValue.ToSha256());
        if (par == null)
        {
            return null;
        }
        var deserialized = _serializer.Deserialize(par.Parameters);
        return new DeserializedPushedAuthorizationRequest
        {
            PushedParameters = deserialized,
            ExpiresAtUtc = par.ExpiresAtUtc
        };
    }

    public async Task StoreAsync(DeserializedPushedAuthorizationRequest request)
    {
        var protectedData = _serializer.Serialize(request.PushedParameters);
        await _store.StoreAsync(new Models.PushedAuthorizationRequest
        {
            ReferenceValueHash = request.ReferenceValue.ToSha256(),
            ExpiresAtUtc = request.ExpiresAtUtc,
            Parameters = protectedData
        });
    }
}
