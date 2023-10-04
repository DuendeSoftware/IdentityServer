using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Services;

// This is for general operations on pushed authorization requests, vs the response generator does 
// business logic specically for the par endpoint

public interface IPushedAuthorizationService
{
    /// <summary>
    /// Unprotects and deserializes the pushed authorization parameters
    /// </summary>
    NameValueCollection DeserializePushedParameters(string parameters);

    string Serialize(NameValueCollection raw);

    
    Task<PushedAuthorizationRequest> GetPushedAuthorizationRequest(ValidatedAuthorizeRequest requestUri);
}