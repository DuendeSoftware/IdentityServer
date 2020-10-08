// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Common;
using IdentityServer.UnitTests.Services.Default;
using IdentityServer.UnitTests.Validation.Setup;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IdentityServer.UnitTests.Validation.Secrets
{
    public class PrivateKeyJwtSecretValidation
    {
        private readonly ISecretValidator _validator;
        private readonly IClientStore _clients;

        public PrivateKeyJwtSecretValidation()
        {
            _validator = new PrivateKeyJwtSecretValidator(
                new MockHttpContextAccessor(
                    new IdentityServerOptions()
                        {
                            IssuerUri = "https://idsrv3.com"
                        }
                    ),
                    new DefaultReplayCache(new TestCache()), 
                    new LoggerFactory().CreateLogger<PrivateKeyJwtSecretValidator>()
                );
            _clients = new InMemoryClientStore(ClientValidationTestClients.Get());
        }

        private JwtSecurityToken CreateToken(string clientId, DateTime? nowOverride = null)
        {
            var certificate = TestCert.Load();
            var now = nowOverride ?? DateTime.UtcNow;

            var token = new JwtSecurityToken(
                    clientId,
                    "https://idsrv3.com/connect/token",
                    new List<Claim>()
                    {
                        new Claim("jti", Guid.NewGuid().ToString()),
                        new Claim(JwtClaimTypes.Subject, clientId),
                        new Claim(JwtClaimTypes.IssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                    },
                    now,
                    now.AddMinutes(1),
                    new SigningCredentials(
                        new X509SecurityKey(certificate),
                        SecurityAlgorithms.RsaSha256
                    )
                );
            return token;
        }

        [Fact]
        public async Task Invalid_Certificate_X5t_Only_Requires_Full_Certificate()
        {
            var clientId = "certificate_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var token = CreateToken(clientId);
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(token),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Certificate_Thumbprint()
        {
            var clientId = "certificate_invalid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(CreateToken(clientId)),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Valid_Certificate_Base64()
        {
            var clientId = "certificate_base64_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(CreateToken(clientId)),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeTrue();
        }
        
        [Fact]
        public async Task Invalid_Replay()
        {
            var clientId = "certificate_base64_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);
            var token = new JwtSecurityTokenHandler().WriteToken(CreateToken(clientId));
            
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = token,
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);
            result.Success.Should().BeTrue();
            
            result = await _validator.ValidateAsync(client.ClientSecrets, secret);
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Certificate_Base64()
        {
            var clientId = "certificate_base64_invalid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(CreateToken(clientId)),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Issuer()
        {
            var clientId = "certificate_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var token = CreateToken(clientId);
            token.Payload.Remove(JwtClaimTypes.Issuer);
            token.Payload.Add(JwtClaimTypes.Issuer, "invalid");
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(token),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Subject()
        {
            var clientId = "certificate_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var token = CreateToken(clientId);
            token.Payload.Remove(JwtClaimTypes.Subject);
            token.Payload.Add(JwtClaimTypes.Subject, "invalid");
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(token),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Expired_Token()
        {
            var clientId = "certificate_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var token = CreateToken(clientId, DateTime.UtcNow.AddHours(-1));
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(token),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Invalid_Unsigned_Token()
        {
            var clientId = "certificate_valid";
            var client = await _clients.FindEnabledClientByIdAsync(clientId);

            var token = CreateToken(clientId);
            token.Header.Remove("alg");
            token.Header.Add("alg", "none");
            var secret = new ParsedSecret
            {
                Id = clientId,
                Credential = new JwtSecurityTokenHandler().WriteToken(token),
                Type = IdentityServerConstants.ParsedSecretTypes.JwtBearer
            };

            var result = await _validator.ValidateAsync(client.ClientSecrets, secret);

            result.Success.Should().BeFalse();
        }
    }
}