using System.Text.Json.Serialization;

namespace Duende.IdentityServer.ResponseHandling;

public abstract class PushedAuthorizationResponse
{ }

public class PushedAuthorizationFailure : PushedAuthorizationResponse
{
    public required string Error { get; set; }
    public required string ErrorDescription { get; set; }
}

public class PushedAuthorizationSuccess : PushedAuthorizationResponse
{
    public required string RequestUri { get; set; }
    public required int ExpiresIn { get; set; }
}
