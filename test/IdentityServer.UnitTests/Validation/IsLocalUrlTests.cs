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
        "/	/evil.com/connect/authorize/callback" + // Note tab character between slashes
        queryParameters;
    private const string ExternalWithoutControlCharacters = 
        "//evil.com/"
        + queryParameters;
    private const string Local =
        "/connect/authorize/callback"
        + queryParameters;

    [Fact]
    public async void GetAuthorizationContextAsync()
    {
        var interactionService = new DefaultIdentityServerInteractionService(null, null, null, null, null, null, null, 
            GetReturnUrlParser(), new LoggerFactory().CreateLogger<DefaultIdentityServerInteractionService>());

        (await interactionService.GetAuthorizationContextAsync(Local)).Should().NotBeNull();
        (await interactionService.GetAuthorizationContextAsync(ExternalWithoutControlCharacters)).Should().BeNull();
        (await interactionService.GetAuthorizationContextAsync(ExternalWithControlCharacters)).Should().BeNull();
    }

    [Fact]
    // TODO - Test the duplicated method in the EF package in later release branches
    public void IsLocalUrl()
    {
        Local.IsLocalUrl().Should().BeTrue();
        ExternalWithoutControlCharacters.IsLocalUrl().Should().BeFalse();
        ExternalWithControlCharacters.IsLocalUrl().Should().BeFalse();
    }

    [Fact]
    public void GetIdentityServerRelativeUrl()
    {
        var serverUrls = new MockServerUrls
        {
            Origin = "https://localhost:5001",
            BasePath = "/"
        };

        serverUrls.GetIdentityServerRelativeUrl(Local).Should().NotBeNull();
        serverUrls.GetIdentityServerRelativeUrl(ExternalWithoutControlCharacters).Should().BeNull();
        serverUrls.GetIdentityServerRelativeUrl(ExternalWithControlCharacters).Should().BeNull();
    }

    [Fact]
    public async void OidcReturnUrlParser()
    {
        var oidcParser = GetOidcReturnUrlParser();

        (await oidcParser.ParseAsync(Local)).Should().NotBeNull();
        oidcParser.IsValidReturnUrl(Local).Should().BeTrue();
        (await oidcParser.ParseAsync(ExternalWithoutControlCharacters)).Should().BeNull();
        oidcParser.IsValidReturnUrl(ExternalWithoutControlCharacters).Should().BeFalse();
        (await oidcParser.ParseAsync(ExternalWithControlCharacters)).Should().BeNull();
        oidcParser.IsValidReturnUrl(ExternalWithControlCharacters).Should().BeFalse();
    }

    [Fact]
    public async void ReturnUrlParser()
    {
        var parser = GetReturnUrlParser();

        (await parser.ParseAsync(Local)).Should().NotBeNull();
        parser.IsValidReturnUrl(Local).Should().BeTrue();
        (await parser.ParseAsync(ExternalWithoutControlCharacters)).Should().BeNull();
        parser.IsValidReturnUrl(ExternalWithoutControlCharacters).Should().BeFalse();
        (await parser.ParseAsync(ExternalWithControlCharacters)).Should().BeNull();
        parser.IsValidReturnUrl(ExternalWithControlCharacters).Should().BeFalse();
    }

    private static ReturnUrlParser GetReturnUrlParser()
    {
        var oidcParser = GetOidcReturnUrlParser();
        var parser = new ReturnUrlParser(new IReturnUrlParser[] { oidcParser });
        return parser;
    }

    private static OidcReturnUrlParser GetOidcReturnUrlParser()
    {
        return new OidcReturnUrlParser(
            new IdentityServerOptions(),
            new StubAuthorizeRequestValidator
            {
                Result = new AuthorizeRequestValidationResult
                (
                    new ValidatedAuthorizeRequest()
                )
            },
            new MockUserSession(),
            new MockServerUrls(),
            new LoggerFactory().CreateLogger<OidcReturnUrlParser>());
    }


}