using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using IdentityModel;

namespace UnitTests.Validation.Setup;

/// <summary>
/// Test implementation of the pushed authorization service. Always returns a setup
/// pushed authorization request.
/// </summary>
internal class TestPushedAuthorizationService : IPushedAuthorizationService
{
    public NameValueCollection DeserializePushedParameters(string parameters)
    {
        return Raw;
    }

    public string Serialize(NameValueCollection raw)
    {
        return string.Empty;
    }

    public Task<PushedAuthorizationRequest> GetPushedAuthorizationRequest(ValidatedAuthorizeRequest requestUri)
    {
        return Task.FromResult(PushedAuthorizationRequest);
    }

    public NameValueCollection Raw { get; private set; }

    public PushedAuthorizationRequest PushedAuthorizationRequest { get; private set; }

    public void PushRequest(NameValueCollection raw)
    {
        Raw = raw;
        PushedAuthorizationRequest = new PushedAuthorizationRequest();
    }

    public void PushRequest(NameValueCollection raw, PushedAuthorizationRequest request)
    {
        Raw = raw;
        PushedAuthorizationRequest = request;
    }

    public void PushRequest(string clientId)
    {
        Raw = new NameValueCollection
        {
            { OidcConstants.AuthorizeRequest.ClientId, clientId }
        };
        PushedAuthorizationRequest = new PushedAuthorizationRequest();
    }

    public void PushRequest(string clientId, DateTime expiresUtc)
    {
        Raw = new NameValueCollection
        {
            { OidcConstants.AuthorizeRequest.ClientId, clientId }
        };
        PushedAuthorizationRequest = new PushedAuthorizationRequest
        {
            ExpiresAtUtc = expiresUtc
        };
    }
}