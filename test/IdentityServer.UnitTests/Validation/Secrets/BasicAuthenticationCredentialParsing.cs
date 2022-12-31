// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Net.Http;
using System.Text;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using UnitTests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace UnitTests.Validation.Secrets;

public class BasicAuthenticationSecretParsing
{
    private const string Category = "Secrets - Basic Authentication Secret Parsing";

    private IdentityServerOptions _options;
    private BasicAuthenticationSecretParser _parser;

    public BasicAuthenticationSecretParsing()
    {
        _options = new IdentityServerOptions();
        _parser = new BasicAuthenticationSecretParser(_options, TestLogger.Create<BasicAuthenticationSecretParser>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async void EmptyContext()
    {
        var context = new DefaultHttpContext();

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void Valid_BasicAuthentication_Request()
    {
        var context = new DefaultHttpContext();

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client:secret")));
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));


        var secret = await _parser.ParseAsync(context);

        secret.Type.Should().Be(IdentityServerConstants.ParsedSecretTypes.SharedSecret);
        secret.Id.Should().Be("client");
        secret.Credential.Should().Be("secret");
    }
        
    [Theory]
    [Trait("Category", Category)]
    [InlineData("client", "secret")]
    [InlineData("cl ient", "secret")]
    [InlineData("cl ient", "se cret")]
    [InlineData("client", "se+cret")]
    [InlineData("cl+ ient", "se+cret")]
    [InlineData("cl+ ient", "se+ cret")]
    [InlineData("client:urn", "secret")]
    public async void Valid_BasicAuthentication_Request_in_various_Formats_Manual(string userName, string password)
    {
        Encoding encoding = Encoding.UTF8;
        var context = new DefaultHttpContext();
            
        if (password == null) password = "";
        string credential = $"{Uri.EscapeDataString(userName)}:{Uri.EscapeDataString(password)}";

        var headerValue = $"Basic {Convert.ToBase64String(encoding.GetBytes(credential))}";
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));
            
        var secret = await _parser.ParseAsync(context);

        secret.Type.Should().Be(IdentityServerConstants.ParsedSecretTypes.SharedSecret);
        secret.Id.Should().Be(userName);
        secret.Credential.Should().Be(password);
    }
        
    [Theory]
    [Trait("Category", Category)]
    [InlineData("client", "secret")]
    [InlineData("cl ient", "secret")]
    [InlineData("cl ient", "se cret")]
    [InlineData("client", "se+cret")]
    [InlineData("cl+ ient", "se+cret")]
    [InlineData("cl+ ient", "se+ cret")]
    [InlineData("client:urn", "secret")]
    public async void Valid_BasicAuthentication_Request_in_various_Formats_IdentityModel(string userName, string password)
    {
        Encoding encoding = Encoding.UTF8;
        var context = new DefaultHttpContext();

        var credential = BasicAuthenticationOAuthHeaderValue.EncodeCredential(userName, password);
        var headerValue = $"Basic {credential}";
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));
            
        var secret = await _parser.ParseAsync(context);

        secret.Type.Should().Be(IdentityServerConstants.ParsedSecretTypes.SharedSecret);
        secret.Id.Should().Be(userName);
        secret.Credential.Should().Be(password);
    }

    [Fact]
    [Trait("Category", Category)]
    public async void Valid_BasicAuthentication_Request_With_UserName_Only_And_Colon_For_Optional_ClientSecret()
    {
        var context = new DefaultHttpContext();
            
        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client:")));
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);

        secret.Type.Should().Be(IdentityServerConstants.ParsedSecretTypes.NoSecret);
        secret.Id.Should().Be("client");
        secret.Credential.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void BasicAuthentication_Request_With_Empty_Basic_Header()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Add("Authorization", new StringValues(string.Empty));

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void Valid_BasicAuthentication_Request_ClientId_Too_Long()
    {
        var context = new DefaultHttpContext();

        var longClientId = "x".Repeat(_options.InputLengthRestrictions.ClientId + 1);
        var credential = string.Format("{0}:secret", longClientId);

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)));
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);
        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void Valid_BasicAuthentication_Request_ClientSecret_Too_Long()
    {
        var context = new DefaultHttpContext();

        var longClientSecret = "x".Repeat(_options.InputLengthRestrictions.ClientSecret + 1);
        var credential = string.Format("client:{0}", longClientSecret);

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)));
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);
        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void BasicAuthentication_Request_With_Empty_Basic_Header_Variation()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Add("Authorization", new StringValues("Basic "));

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void BasicAuthentication_Request_With_Unknown_Scheme()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Add("Authorization", new StringValues("Unknown"));

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void BasicAuthentication_Request_With_Malformed_Credentials_NoBase64_Encoding()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Add("Authorization", new StringValues("Basic somerandomdata"));

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", Category)]
    public async void BasicAuthentication_Request_With_Malformed_Credentials_Base64_Encoding_UserName_Only()
    {
        var context = new DefaultHttpContext();

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client")));
        context.Request.Headers.Add("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);

        secret.Should().BeNull();
    }
}