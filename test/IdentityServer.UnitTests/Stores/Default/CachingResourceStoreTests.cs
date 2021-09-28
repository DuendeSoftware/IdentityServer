// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using UnitTests.Common;
using Xunit;

namespace UnitTests.Stores.Default
{
    public class CachingResourceStoreTests
    {
        List<IdentityResource> _identityResources = new List<IdentityResource>();
        List<ApiResource> _apiResources = new List<ApiResource>();
        List<ApiScope> _apiScopes = new List<ApiScope>();
        InMemoryResourcesStore _store;
        IdentityServerOptions _options = new IdentityServerOptions();

        MockCache<ApiScope> _scopeCache = new MockCache<ApiScope>();
        MockCache<Resources> _resourceCache = new MockCache<Resources>();

        CachingResourceStore<InMemoryResourcesStore> _subject;

        public CachingResourceStoreTests()
        {
            _apiScopes.Add(new ApiScope("scope1"));
            _apiScopes.Add(new ApiScope("scope2"));
            _apiScopes.Add(new ApiScope("scope3"));
            _apiScopes.Add(new ApiScope("scope4"));

            _store = new InMemoryResourcesStore(_identityResources, _apiResources, _apiScopes);
            _subject = new CachingResourceStore<InMemoryResourcesStore>(
                _options, 
                _store,
                new MockCache<IdentityResource>(),
                new MockCache<ApiResource>(),
                _scopeCache,
                _resourceCache);
        }

        [Fact]
        public async Task FindApiScopesByNameAsync_should_populate_cache()
        {
            _scopeCache.Items.Count.Should().Be(0);

            var items = await _subject.FindApiScopesByNameAsync(new[] { "scope3", "scope1", "scope2", "invalid" });
            items.Count().Should().Be(3);

            _scopeCache.Items.Count.Should().Be(3);
        }
        
        [Fact]
        public async Task FindApiScopesByNameAsync_should_populate_missing_cache_items()
        {
            _scopeCache.Items.Count.Should().Be(0);

            var items = await _subject.FindApiScopesByNameAsync(new[] { "scope1" });
            items.Count().Should().Be(1);
            _scopeCache.Items.Count.Should().Be(1);

            _apiScopes.Remove(_apiScopes.Single(x => x.Name == "scope1"));
            items = await _subject.FindApiScopesByNameAsync(new[] { "scope1", "scope2" });
            items.Count().Should().Be(2);
            _scopeCache.Items.Count.Should().Be(2);

            _apiScopes.Remove(_apiScopes.Single(x => x.Name == "scope2"));
            items = await _subject.FindApiScopesByNameAsync(new[] { "scope3", "scope2", "scope4" });
            items.Count().Should().Be(3);
            _scopeCache.Items.Count.Should().Be(4);

            // this shows we will find it in the cache, even if removed from the DB
            _apiScopes.Remove(_apiScopes.Single(x => x.Name == "scope3"));
            items = await _subject.FindApiScopesByNameAsync(new[] { "scope3", "scope1", "scope2" });
            items.Count().Should().Be(3);
            _scopeCache.Items.Count.Should().Be(4);
        }
    }
}