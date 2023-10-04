using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Default implementation of <see cref="IPushedAuthorizationService"/>.
/// </summary>
public class PushedAuthorizationService : IPushedAuthorizationService
{
    private readonly IDataProtector _dataProtector;
    private readonly IPushedAuthorizationRequestStore _pushedAuthorizationRequestStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushedAuthorizationService"/>. 
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider.</param>
    /// <param name="pushedAuthorizationRequestStore">The pushed authorization request store.</param>
    public PushedAuthorizationService(
        IDataProtectionProvider dataProtectionProvider, 
        IPushedAuthorizationRequestStore pushedAuthorizationRequestStore)
    {
        _pushedAuthorizationRequestStore = pushedAuthorizationRequestStore;
        _dataProtector = dataProtectionProvider.CreateProtector("PAR");
    }

    /// <inheritdoc />
    public NameValueCollection DeserializePushedParameters(string raw)
    {
        var unprotected = _dataProtector.Unprotect(raw);
        return ObjectSerializer
            .FromString<Dictionary<string, string[]>>(unprotected)
            .FromFullDictionary();
    }
    
    /// <inheritdoc />
    public string Serialize(NameValueCollection raw)
    {
        // Serialize
        var serialized = ObjectSerializer.ToString(raw.ToFullDictionary());

        // Data Protect
        var protectedData = _dataProtector.Protect(serialized);
        return protectedData;
    }
    
    /// <inheritdoc />
    public async Task<PushedAuthorizationRequest> GetPushedAuthorizationRequest(ValidatedAuthorizeRequest request)
    {
        var requestUri = request.Raw.Get(OidcConstants.AuthorizeRequest.RequestUri);
        if (requestUri != null)
        {
            var index = IdentityServerConstants.PushedAuthorizationRequestUri.Length + 1;// +1 for the separator ':'
            var referenceValue = requestUri.Substring(index); 
            return await _pushedAuthorizationRequestStore.GetAsync(referenceValue);
        }

        return null;
    }
}