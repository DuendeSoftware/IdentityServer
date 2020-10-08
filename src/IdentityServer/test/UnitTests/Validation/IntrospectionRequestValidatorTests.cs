// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer.UnitTests.Validation.Setup;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Xunit;

namespace IdentityServer.UnitTests.Validation
{
    public class IntrospectionRequestValidatorTests
    {
        private const string Category = "Introspection request validation";

        private IntrospectionRequestValidator _subject;
        private IReferenceTokenStore _referenceTokenStore;

        public IntrospectionRequestValidatorTests()
        {
            _referenceTokenStore = Factory.CreateReferenceTokenStore();
            var tokenValidator = Factory.CreateTokenValidator(_referenceTokenStore);

            _subject = new IntrospectionRequestValidator(tokenValidator, TestLogger.Create<IntrospectionRequestValidator>());
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_token_should_successfully_validate()
        {
            var token = new Token {
                CreationTime = DateTime.UtcNow,
                Issuer = "http://op",
                ClientId = "codeclient",
                Lifetime = 1000,
                Claims =
                {
                    new System.Security.Claims.Claim("scope", "a"),
                    new System.Security.Claims.Claim("scope", "b")
                }
            };
            var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token);
            
            var param = new NameValueCollection()
            {
                { "token", handle}
            };

            var result = await _subject.ValidateAsync(param, null);

            result.IsError.Should().Be(false);
            result.IsActive.Should().Be(true);
            result.Claims.Count().Should().Be(5);
            result.Token.Should().Be(handle);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_token_should_error()
        {
            var param = new NameValueCollection();
            
            var result = await _subject.ValidateAsync(param, null);

            result.IsError.Should().Be(true);
            result.Error.Should().Be("missing_token");
            result.IsActive.Should().Be(false);
            result.Claims.Should().BeNull();
            result.Token.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_token_should_return_inactive()
        {
            var param = new NameValueCollection()
            {
                { "token", "invalid" }
            };

            var result = await _subject.ValidateAsync(param, null);

            result.IsError.Should().Be(false);
            result.IsActive.Should().Be(false);
            result.Claims.Should().BeNull();
            result.Token.Should().Be("invalid");
        }
    }
}