// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityServer.UnitTests.Validation.Secrets
{
    public class ClientAssertionSecretParsing
    {
        private IdentityServerOptions _options;
        private JwtBearerClientAssertionSecretParser _parser;

        public ClientAssertionSecretParsing()
        {
            _options = new IdentityServerOptions();
            _parser = new JwtBearerClientAssertionSecretParser(_options, new LoggerFactory().CreateLogger<JwtBearerClientAssertionSecretParser>());
        }

        [Fact]
        public async void EmptyContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream();
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }

        [Fact]
        public async void Valid_ClientAssertion()
        {
            var context = new DefaultHttpContext();

            var token = new JwtSecurityToken(issuer: "issuer", claims: new[] { new Claim("sub", "client") });
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var body = "client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion=" + tokenString;

            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().NotBeNull();
            secret.Type.Should().Be(IdentityServerConstants.ParsedSecretTypes.JwtBearer);
            secret.Id.Should().Be("client");
            secret.Credential.Should().Be(tokenString);
        }

        [Fact]
        public async void Missing_ClientAssertionType()
        {
            var context = new DefaultHttpContext();

            var body = "client_id=client&client_assertion=token";

            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }

        [Fact]
        public async void Missing_ClientAssertion()
        {
            var context = new DefaultHttpContext();

            var body = "client_id=client&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }

        [Fact]
        public async void Malformed_PostBody()
        {
            var context = new DefaultHttpContext();
            var body = "malformed";
            
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }

        [Fact]
        public async void ClientId_TooLong()
        {
            var context = new DefaultHttpContext();

            var longClientId = "x".Repeat(_options.InputLengthRestrictions.ClientId + 1);
            var body = $"client_id={longClientId}&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion=token";

            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }

        [Fact]
        public async void ClientAssertion_TooLong()
        {
            var context = new DefaultHttpContext();

            var longToken = "x".Repeat(_options.InputLengthRestrictions.Jwt + 1);
            var body = $"client_id=client&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={longToken}";

            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/x-www-form-urlencoded";

            var secret = await _parser.ParseAsync(context);

            secret.Should().BeNull();
        }
    }
}