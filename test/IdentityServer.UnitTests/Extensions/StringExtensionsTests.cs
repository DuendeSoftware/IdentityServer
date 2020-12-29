// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Xunit;
using Duende.IdentityServer.Extensions;
using FluentAssertions;

namespace UnitTests.Extensions
{
    public class StringExtensionsTests
    {
        private const string Category = "StringExtensions Tests";

        private void CheckOrigin(string inputUrl, string expectedOrigin)
        {
            var actualOrigin = inputUrl.GetOrigin();
            Assert.Equal(expectedOrigin, actualOrigin);
        }

        [Fact]
        public void TestGetOrigin()
        {
            CheckOrigin("http://idsvr.com", "http://idsvr.com");
            CheckOrigin("http://idsvr.com/", "http://idsvr.com");
            CheckOrigin("http://idsvr.com/test", "http://idsvr.com");
            CheckOrigin("http://idsvr.com/test/resource", "http://idsvr.com");
            CheckOrigin("http://idsvr.com:8080", "http://idsvr.com:8080");
            CheckOrigin("http://idsvr.com:8080/", "http://idsvr.com:8080");
            CheckOrigin("http://idsvr.com:8080/test", "http://idsvr.com:8080");
            CheckOrigin("http://idsvr.com:8080/test/resource", "http://idsvr.com:8080");
            CheckOrigin("http://127.0.0.1", "http://127.0.0.1");
            CheckOrigin("http://127.0.0.1/", "http://127.0.0.1");
            CheckOrigin("http://127.0.0.1/test", "http://127.0.0.1");
            CheckOrigin("http://127.0.0.1/test/resource", "http://127.0.0.1");
            CheckOrigin("http://127.0.0.1:8080", "http://127.0.0.1:8080");
            CheckOrigin("http://127.0.0.1:8080/", "http://127.0.0.1:8080");
            CheckOrigin("http://127.0.0.1:8080/test", "http://127.0.0.1:8080");
            CheckOrigin("http://127.0.0.1:8080/test/resource", "http://127.0.0.1:8080");
            CheckOrigin("http://localhost", "http://localhost");
            CheckOrigin("http://localhost/", "http://localhost");
            CheckOrigin("http://localhost/test", "http://localhost");
            CheckOrigin("http://localhost/test/resource", "http://localhost");
            CheckOrigin("http://localhost:8080", "http://localhost:8080");
            CheckOrigin("http://localhost:8080/", "http://localhost:8080");
            CheckOrigin("http://localhost:8080/test", "http://localhost:8080");
            CheckOrigin("http://localhost:8080/test/resource", "http://localhost:8080");
            CheckOrigin("https://idsvr.com", "https://idsvr.com");
            CheckOrigin("https://idsvr.com/", "https://idsvr.com");
            CheckOrigin("https://idsvr.com/test", "https://idsvr.com");
            CheckOrigin("https://idsvr.com/test/resource", "https://idsvr.com");
            CheckOrigin("https://idsvr.com:8080", "https://idsvr.com:8080");
            CheckOrigin("https://idsvr.com:8080/", "https://idsvr.com:8080");
            CheckOrigin("https://idsvr.com:8080/test", "https://idsvr.com:8080");
            CheckOrigin("https://idsvr.com:8080/test/resource", "https://idsvr.com:8080");
            CheckOrigin("https://127.0.0.1", "https://127.0.0.1");
            CheckOrigin("https://127.0.0.1/", "https://127.0.0.1");
            CheckOrigin("https://127.0.0.1/test", "https://127.0.0.1");
            CheckOrigin("https://127.0.0.1/test/resource", "https://127.0.0.1");
            CheckOrigin("https://127.0.0.1:8080", "https://127.0.0.1:8080");
            CheckOrigin("https://127.0.0.1:8080/", "https://127.0.0.1:8080");
            CheckOrigin("https://127.0.0.1:8080/test", "https://127.0.0.1:8080");
            CheckOrigin("https://127.0.0.1:8080/test/resource", "https://127.0.0.1:8080");
            CheckOrigin("https://localhost", "https://localhost");
            CheckOrigin("https://localhost/", "https://localhost");
            CheckOrigin("https://localhost/test", "https://localhost");
            CheckOrigin("https://localhost/test/resource", "https://localhost");
            CheckOrigin("https://localhost:8080", "https://localhost:8080");
            CheckOrigin("https://localhost:8080/", "https://localhost:8080");
            CheckOrigin("https://localhost:8080/test", "https://localhost:8080");
            CheckOrigin("https://localhost:8080/test/resource", "https://localhost:8080");
            CheckOrigin("test://idsvr.com", null);
            CheckOrigin("test://idsvr.com/", null);
            CheckOrigin("test://idsvr.com/test", null);
            CheckOrigin("test://idsvr.com/test/resource", null);
            CheckOrigin("test://idsvr.com:8080", null);
            CheckOrigin("test://idsvr.com:8080/", null);
            CheckOrigin("test://idsvr.com:8080/test", null);
            CheckOrigin("test://idsvr.com:8080/test/resource", null);
            CheckOrigin("test://127.0.0.1", null);
            CheckOrigin("test://127.0.0.1/", null);
            CheckOrigin("test://127.0.0.1/test", null);
            CheckOrigin("test://127.0.0.1/test/resource", null);
            CheckOrigin("test://127.0.0.1:8080", null);
            CheckOrigin("test://127.0.0.1:8080/", null);
            CheckOrigin("test://127.0.0.1:8080/test", null);
            CheckOrigin("test://127.0.0.1:8080/test/resource", null);
            CheckOrigin("test://localhost", null);
            CheckOrigin("test://localhost/", null);
            CheckOrigin("test://localhost/test", null);
            CheckOrigin("test://localhost/test/resource", null);
            CheckOrigin("test://localhost:8080", null);
            CheckOrigin("test://localhost:8080/", null);
            CheckOrigin("test://localhost:8080/test", null);
            CheckOrigin("test://localhost:8080/test/resource", null);
        }


        // scope parsing

        [Fact]
        [Trait("Category", Category)]
        public void Parse_Scopes_with_Empty_Scope_List()
        {
            var scopes = string.Empty.ParseScopesString();

            scopes.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public void Parse_Scopes_with_Sorting()
        {
            var scopes = "scope3 scope2 scope1".ParseScopesString();

            scopes.Count.Should().Be(3);

            scopes[0].Should().Be("scope1");
            scopes[1].Should().Be("scope2");
            scopes[2].Should().Be("scope3");
        }

        [Fact]
        [Trait("Category", Category)]
        public void Parse_Scopes_with_Extra_Spaces()
        {
            var scopes = "   scope3     scope2     scope1   ".ParseScopesString();

            scopes.Count.Should().Be(3);

            scopes[0].Should().Be("scope1");
            scopes[1].Should().Be("scope2");
            scopes[2].Should().Be("scope3");
        }

        [Fact]
        [Trait("Category", Category)]
        public void Parse_Scopes_with_Duplicate_Scope()
        {
            var scopes = "scope2 scope1 scope2".ParseScopesString();

            scopes.Count.Should().Be(2);

            scopes[0].Should().Be("scope1");
            scopes[1].Should().Be("scope2");
        }
    }
}
