// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Xunit;

namespace IdentityServer.UnitTests.Services.InMemory
{
    public class InMemoryCorsPolicyServiceTests
    {
        private const string Category = "InMemoryCorsPolicyService";

        private InMemoryCorsPolicyService _subject;
        private List<Client> _clients = new List<Client>();

        public InMemoryCorsPolicyServiceTests()
        {
            _subject = new InMemoryCorsPolicyService(TestLogger.Create<InMemoryCorsPolicyService>(), _clients);
        }

        [Fact]
        [Trait("Category", Category)]
        public void client_has_origin_should_allow_origin()
        {
            _clients.Add(new Client
            {
                AllowedCorsOrigins = new List<string>
                {
                    "http://foo"
                }
            });

            _subject.IsOriginAllowedAsync("http://foo").Result.Should().BeTrue();
        }

        [Theory]
        [InlineData("http://foo")]
        [InlineData("https://bar")]
        [InlineData("http://bar-baz")]
        [Trait("Category", Category)]
        public void client_does_not_has_origin_should_not_allow_origin(string clientOrigin)
        {
            _clients.Add(new Client
            {
                AllowedCorsOrigins = new List<string>
                {
                    clientOrigin
                }
            });
            _subject.IsOriginAllowedAsync("http://bar").Result.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public void client_has_many_origins_and_origin_is_in_list_should_allow_origin()
        {
            _clients.Add(new Client
            {
                AllowedCorsOrigins = new List<string>
                {
                    "http://foo",
                    "http://bar",
                    "http://baz"
                }
            });
            _subject.IsOriginAllowedAsync("http://bar").Result.Should().Be(true);
        }

        [Fact]
        [Trait("Category", Category)]
        public void client_has_many_origins_and_origin_is_in_not_list_should_not_allow_origin()
        {
            _clients.Add(new Client
            {
                AllowedCorsOrigins = new List<string>
                {
                    "http://foo",
                    "http://bar",
                    "http://baz"
                }
            });
            _subject.IsOriginAllowedAsync("http://quux").Result.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public void many_clients_have_same_origins_should_allow_origin()
        {
            _clients.AddRange(new Client[] {
                new Client
                {
                    AllowedCorsOrigins = new List<string>
                    {
                        "http://foo"
                    }
                },
                new Client
                {
                    AllowedCorsOrigins = new List<string>
                    {
                        "http://foo"
                    }
                }
            });
            _subject.IsOriginAllowedAsync("http://foo").Result.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public void handle_invalid_cors_origin_format_exception()
        {
            _clients.AddRange(new Client[] {
                new Client
                {
                    AllowedCorsOrigins = new List<string>
                    {
                        "http://foo",
                        "http://ba z"
                    }
                },
                new Client
                {
                    AllowedCorsOrigins = new List<string>
                    {
                        "http://foo",
                        "http://bar"
                    }
                }
            });
            _subject.IsOriginAllowedAsync("http://bar").Result.Should().BeTrue();
        }
    }
}
