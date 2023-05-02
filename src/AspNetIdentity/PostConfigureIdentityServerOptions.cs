using Duende.IdentityServer.Configuration;
using Microsoft.Extensions.Options;

namespace Duende.IdentityServer.AspNetIdentity;

/// <summary>
/// Identity server options configuration
/// </summary>
public class PostConfigureIdentityServerOptions : IPostConfigureOptions<IdentityServerOptions>
{
    private readonly IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> _authOptions;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="authOptions"></param>
    public PostConfigureIdentityServerOptions(IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> authOptions)
    {
        _authOptions = authOptions;
    }

    /// <inheritdoc />
    public void PostConfigure(string name, IdentityServerOptions options)
    {
        if (_authOptions.Value.DefaultAuthenticateScheme != null
            && _authOptions.Value.DefaultAuthenticateScheme != options.DynamicProviders.SignOutScheme)
        {
            options.DynamicProviders.SignOutScheme = _authOptions.Value.DefaultAuthenticateScheme;
        }
    }
}
