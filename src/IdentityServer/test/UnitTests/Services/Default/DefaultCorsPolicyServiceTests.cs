// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4.Services;
using Xunit;

namespace IdentityServer.UnitTests.Services.Default
{
    public class DefaultCorsPolicyServiceTests
    {
        private const string Category = "DefaultCorsPolicyService";

        private DefaultCorsPolicyService subject;

        public DefaultCorsPolicyServiceTests()
        {
            subject = new DefaultCorsPolicyService(TestLogger.Create<DefaultCorsPolicyService>());
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_null_param_ReturnsFalse()
        {
            subject.IsOriginAllowedAsync(null).Result.Should().Be(false);
            subject.IsOriginAllowedAsync(String.Empty).Result.Should().Be(false);
            subject.IsOriginAllowedAsync("    ").Result.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_OriginIsAllowed_ReturnsTrue()
        {
            subject.AllowedOrigins.Add("http://foo");
            subject.IsOriginAllowedAsync("http://foo").Result.Should().Be(true);
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_OriginIsNotAllowed_ReturnsFalse()
        {
            subject.AllowedOrigins.Add("http://foo");
            subject.IsOriginAllowedAsync("http://bar").Result.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_OriginIsInAllowedList_ReturnsTrue()
        {
            subject.AllowedOrigins.Add("http://foo");
            subject.AllowedOrigins.Add("http://bar");
            subject.AllowedOrigins.Add("http://baz");
            subject.IsOriginAllowedAsync("http://bar").Result.Should().Be(true);
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_OriginIsNotInAllowedList_ReturnsFalse()
        {
            subject.AllowedOrigins.Add("http://foo");
            subject.AllowedOrigins.Add("http://bar");
            subject.AllowedOrigins.Add("http://baz");
            subject.IsOriginAllowedAsync("http://quux").Result.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public void IsOriginAllowed_AllowAllTrue_ReturnsTrue()
        {
            subject.AllowAll = true;
            subject.IsOriginAllowedAsync("http://foo").Result.Should().Be(true);
        }
    }
}
