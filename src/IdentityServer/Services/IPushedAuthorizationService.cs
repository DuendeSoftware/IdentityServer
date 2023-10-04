using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Interface for the pushed authorization service. This is distinct from the
/// <see cref="IPushedAuthorizationResponseGenerator"/> in that the response generator
/// contains logic specific to the PAR endpoint, while this service contains logic about
/// how pushed requests are handled in general.
/// </summary>
public interface IPushedAuthorizationService
{
    /// <summary>
    /// Unprotects and deserializes the pushed authorization parameters
    /// </summary>
    /// <param name="parameters">The data protected, serialized raw parameters.</param>
    /// <returns>The unprotected parameters, parsed into a NameValueCollection.</returns>
    NameValueCollection DeserializePushedParameters(string parameters);

    /// <summary>
    /// Protects and serializes pushed authorization parameters.
    /// </summary>
    /// <param name="raw">The raw parameter collection.</param>
    /// <returns>The parameters, serialized and data protected.</returns>
    string Serialize(NameValueCollection raw);

    /// <summary>
    /// Gets the pushed authorization request specified in an authorize request.
    /// </summary>
    /// <param name="request">The authorize request.</param>
    /// <returns>The pushed authorization request.</returns>
    Task<PushedAuthorizationRequest> GetPushedAuthorizationRequest(ValidatedAuthorizeRequest request);
}