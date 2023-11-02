// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Xunit;
using Duende.IdentityServer.Validation;
using System.Collections.Specialized;
using FluentAssertions;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace UnitTests.Services.Default;

public class ParRedirectUriValidatorTests
{
    [Fact]
    public async Task PushedRedirectUriCanBeUsedAsync()
    {
        var subject = new ParRedirectUriValidator();
        var redirectUri = "https://pushed.example.com";
        var pushedParameters = new NameValueCollection
        {
            { "redirect_uri", redirectUri }
        };

        var result = await subject.IsRedirectUriValidAsync(new RedirectUriValidationContext
        {
            AuthorizeRequestType = AuthorizeRequestType.AuthorizeWithPushedParameters,
            RequestParameters = pushedParameters,
            RequestedUri = redirectUri
        });

        result.Should().Be(true);
    }

    [Fact]
    public async Task AnythingIsPermittedAtParEndpoint()
    {
        var subject = new ParRedirectUriValidator();
        var redirectUri = "https://pushed.example.com";
        var pushedParameters = new NameValueCollection
        {
            { "redirect_uri", redirectUri }
        };

        var result = await subject.IsRedirectUriValidAsync(new RedirectUriValidationContext
        {
            AuthorizeRequestType = AuthorizeRequestType.PushedAuthorization,
            RequestParameters = pushedParameters,
            RequestedUri = redirectUri
        });

        result.Should().Be(true);
    }


    [Fact]
    public async Task NotUsingThePushedRedirectUriShouldFailAsync()
    {
        var subject = new ParRedirectUriValidator();
        var pushedRedirectUri = "https://pushed.example.com";
        var pushedParameters = new NameValueCollection
        {
            { "redirect_uri", pushedRedirectUri }
        };

        var notThePushedRedirectUri = "https://dangerous.example.com";

        var result = await subject.IsRedirectUriValidAsync(new RedirectUriValidationContext
        {
            AuthorizeRequestType = AuthorizeRequestType.AuthorizeWithPushedParameters,
            RequestParameters = pushedParameters,
            RequestedUri = notThePushedRedirectUri,
            Client = new Client()
        });
        
        result.Should().Be(false);
    }

    [Fact]
    public async Task UsingARegisteredPushedUriInsteadOfThePushedRedirectUriShouldSucceed()
    {
        var subject = new ParRedirectUriValidator();
        var pushedRedirectUri = "https://pushed.example.com";
        var pushedParameters = new NameValueCollection
        {
            { "redirect_uri", pushedRedirectUri }
        };

        var registeredRedirectUri = "https://registered.example.com";

        var result = await subject.IsRedirectUriValidAsync(new RedirectUriValidationContext
        {
            AuthorizeRequestType = AuthorizeRequestType.AuthorizeWithPushedParameters,
            RequestParameters = pushedParameters,
            RequestedUri = registeredRedirectUri,
            Client = new Client
            {
                RedirectUris = { "https://registered.example.com" }
            }
        });
        
        registeredRedirectUri.Should().NotBe(pushedRedirectUri);
        result.Should().Be(true);
    }

    [Fact]
    public async Task AuthorizeEndpointWithoutPushedParametersIsStillStrict()
    {
        var subject = new ParRedirectUriValidator();
        var requestedRedirectUri = "https://requested.example.com";
        var authorizeParameters = new NameValueCollection
        {
            { "redirect_uri", requestedRedirectUri }
        };

        var registeredRedirectUri = "https://registered.example.com";

        var result = await subject.IsRedirectUriValidAsync(new RedirectUriValidationContext
        {
            AuthorizeRequestType = AuthorizeRequestType.Authorize,
            RequestParameters = authorizeParameters,
            RequestedUri = requestedRedirectUri,
            Client = new Client
            {
                RedirectUris = { "https://registered.example.com" }
            }
        });
        
        registeredRedirectUri.Should().NotBe(requestedRedirectUri);
        result.Should().Be(false);
    }
}
