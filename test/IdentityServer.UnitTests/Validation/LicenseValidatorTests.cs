// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Security.Claims;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using Xunit;

namespace UnitTests.Validation
{
    public class LicenseValidatorTests
    {
        private const string Category = "License validator tests";

        [Fact]
        [Trait("Category", Category)]
        public void license_should_parse_company_data()
        {
            var subject = new License(
                new Claim("edition", "enterprise"),
                new Claim("company_name", "foo"),
                new Claim("contact_info", "bar"));
            subject.CompanyName.Should().Be("foo");
            subject.ContactInfo.Should().Be("bar");
        }

        [Fact]
        [Trait("Category", Category)]
        public void license_should_parse_expiration()
        {
            {
                var subject = new License(new Claim("edition", "enterprise"));
                subject.Expiration.Should().BeNull();
            }

            {
                var exp = new DateTimeOffset(2020, 1, 12, 13, 5, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var subject = new License(
                    new Claim("edition", "enterprise"),
                    new Claim("exp", exp.ToString()));
                subject.Expiration.Should().Be(new DateTime(2020, 1, 12, 13, 5, 0, DateTimeKind.Utc));
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public void license_should_parse_edition_and_use_default_values()
        {
            {
                var subject = new License(new Claim("edition", "enterprise"));
                subject.Edition.Should().Be(License.LicenseEdition.Enterprise);
                subject.IsEnterprise.Should().BeTrue();
                subject.ClientLimit.Should().BeNull();
                subject.IssuerLimit.Should().BeNull();
                subject.KeyManagement.Should().BeTrue();
                subject.ResourceIsolation.Should().BeTrue();
                subject.ISV.Should().BeFalse();
            }
            {
                var subject = new License(new Claim("edition", "business"));
                subject.Edition.Should().Be(License.LicenseEdition.Business);
                subject.IsBusiness.Should().BeTrue();
                subject.ClientLimit.Should().Be(15);
                subject.IssuerLimit.Should().Be(1);
                subject.KeyManagement.Should().BeTrue();
                subject.ResourceIsolation.Should().BeFalse();
                subject.ISV.Should().BeFalse();
            }
            {
                var subject = new License(new Claim("edition", "starter"));
                subject.Edition.Should().Be(License.LicenseEdition.Starter);
                subject.IsStarter.Should().BeTrue();
                subject.ClientLimit.Should().Be(5);
                subject.IssuerLimit.Should().Be(1);
                subject.KeyManagement.Should().BeFalse();
                subject.ResourceIsolation.Should().BeFalse();
                subject.ISV.Should().BeFalse();
            }
            {
                var subject = new License(new Claim("edition", "community"));
                subject.Edition.Should().Be(License.LicenseEdition.Community);
                subject.IsCommunity.Should().BeTrue();
                subject.ClientLimit.Should().Be(5);
                subject.IssuerLimit.Should().Be(1);
                subject.KeyManagement.Should().BeTrue();
                subject.ResourceIsolation.Should().BeFalse();
                subject.ISV.Should().BeFalse();
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public void license_should_handle_overrides_for_default_edition_values()
        {
            {
                var subject = new License(
                    new Claim("edition", "enterprise"),
                    new Claim("client_limit", "20"),
                    new Claim("issuer_limit", "5"));
                subject.ClientLimit.Should().BeNull();
                subject.IssuerLimit.Should().BeNull();
            }

            {
                var subject = new License(
                    new Claim("edition", "business"),
                    new Claim("client_limit", "20"),
                    new Claim("issuer_limit", "5"),
                    new Claim("feature", "resource_isolation"));
                subject.ClientLimit.Should().Be(20);
                subject.IssuerLimit.Should().Be(5);
                subject.ResourceIsolation.Should().BeTrue();
            }
            {
                var subject = new License(
                    new Claim("edition", "business"),
                    new Claim("client_limit", "20"),
                    new Claim("feature", "unlimited_issuers"),
                    new Claim("issuer_limit", "5"),
                    new Claim("feature", "unlimited_clients"));
                subject.ClientLimit.Should().BeNull();
                subject.IssuerLimit.Should().BeNull();
            }

            {
                var subject = new License(
                    new Claim("edition", "starter"),
                    new Claim("client_limit", "20"),
                    new Claim("issuer_limit", "5"),
                    new Claim("feature", "key_management"),
                    new Claim("feature", "isv"),
                    new Claim("feature", "resource_isolation"));
                subject.ClientLimit.Should().Be(20);
                subject.IssuerLimit.Should().Be(5);
                subject.KeyManagement.Should().BeTrue();
                subject.ResourceIsolation.Should().BeTrue();
                subject.ISV.Should().BeTrue();
            }
            {
                var subject = new License(
                    new Claim("edition", "starter"),
                    new Claim("client_limit", "20"),
                    new Claim("feature", "unlimited_issuers"),
                    new Claim("issuer_limit", "5"),
                    new Claim("feature", "unlimited_clients"));
                subject.ClientLimit.Should().BeNull();
                subject.IssuerLimit.Should().BeNull();
            }

            {
                var subject = new License(
                    new Claim("edition", "community"),
                    new Claim("client_limit", "20"));
                subject.ClientLimit.Should().Be(20);
            }
            {
                var subject = new License(
                    new Claim("edition", "community"),
                    new Claim("client_limit", "20"),
                    new Claim("feature", "unlimited_clients"));
                subject.ClientLimit.Should().BeNull();
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public void invalid_edition_should_fail()
        {
            {
                Action func = () => new License(new Claim("edition", "invalid"));
                func.Should().Throw<Exception>();
            }
            {
                Action func = () => new License(new Claim("edition", ""));
                func.Should().Throw<Exception>();
            }
            {
                Action func = () => new License();
                func.Should().Throw<Exception>();
            }
        }
    }
}
