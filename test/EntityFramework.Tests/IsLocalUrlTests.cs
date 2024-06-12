using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UnitTests.Common;
using UnitTests.Endpoints.Authorize;
using Xunit;

public class IsLocalUrlTests
{
    private const string queryParameters = "?client_id=mvc.code" +
        "&redirect_uri=https%3A%2F%2Flocalhost%3A44302%2Fsignin-oidc" +
        "&response_type=code" +
        "&scope=openid%20profile%20email%20custom.profile%20resource1.scope1%20resource2.scope1%20offline_access" +
        "&code_challenge=LcJN1shWmezC0J5EU7QOi7N_amBuvMDb6PcTY0sB2YY" +
        "&code_challenge_method=S256" +
        "&response_mode=form_post" +
        "&nonce=nonce" +
        "&state=state";
    private const string ExternalWithControlCharacters =
        "/  /evil.com/connect/authorize/callback" + // Note tab character between slashes
        queryParameters;
    private const string ExternalWithoutControlCharacters = 
        "//evil.com/"
        + queryParameters;
    private const string Local =
        "/connect/authorize/callback"
        + queryParameters;

    [Fact]
    public void IsLocalUrl()
    {
        Local.IsLocalUrl().Should().BeTrue();
        ExternalWithoutControlCharacters.IsLocalUrl().Should().BeFalse();
        ExternalWithControlCharacters.IsLocalUrl().Should().BeFalse();
    }
}