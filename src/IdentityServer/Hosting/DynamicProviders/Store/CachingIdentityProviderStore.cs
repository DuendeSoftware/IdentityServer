// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting.DynamicProviders;

/// <summary>
/// Caching decorator for IIdentityProviderStore
/// </summary>
/// <typeparam name="T"></typeparam>
public class CachingIdentityProviderStore<T> : IIdentityProviderStore
    where T : IIdentityProviderStore
{
    private readonly IIdentityProviderStore _inner;
    private readonly ICache<IdentityProvider> _cache;
    private readonly ICache<IEnumerable<IdentityProviderName>> _allCache;
    private readonly IdentityServerOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="inner"></param>
    /// <param name="cache"></param>
    /// <param name="allCache"></param>
    /// <param name="options"></param>
    /// <param name="httpContextAccessor"></param>
    public CachingIdentityProviderStore(T inner, 
        ICache<IdentityProvider> cache,
        ICache<IEnumerable<IdentityProviderName>> allCache,
        IdentityServerOptions options,
        IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _cache = cache;
        _allCache = allCache;
        _options = options;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IdentityProviderName>> GetAllSchemeNamesAsync()
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("CachingIdentityProviderStore.GetAllSchemeNames");
        
        var result = await _allCache.GetOrAddAsync("__all__", 
            _options.Caching.IdentityProviderCacheDuration, 
            async () => await _inner.GetAllSchemeNamesAsync());
        return result;
    }

    /// <inheritdoc/>
    public async Task<IdentityProvider> GetBySchemeAsync(string scheme)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("CachingIdentityProviderStore.GetByScheme");
        
        var result = await _cache.GetOrAddAsync(scheme,
            _options.Caching.IdentityProviderCacheDuration,
            async () =>
            {
                var item = await _inner.GetBySchemeAsync(scheme);
                RemoveCacheEntry(item);
                return item;
            });
        return result;
    }

    // when items are re-added, we remove the corresponding options from the 
    // options monitor since those instances are cached my the authentication handler plumbing
    // this keeps theirs in sync with ours when we re-load from the DB
    void RemoveCacheEntry(IdentityProvider idp)
    {
        if (idp != null)
        {
            var provider = _options.DynamicProviders.FindProviderType(idp.Type);
            if (provider != null)
            {
                var optionsMonitorType = typeof(IOptionsMonitorCache<>).MakeGenericType(provider.OptionsType);
                // need to resolve the provide type dynamically, thus the need for the http context accessor
                var optionsCache = _httpContextAccessor.HttpContext.RequestServices.GetService(optionsMonitorType);
                if (optionsCache != null)
                {
                    var mi = optionsMonitorType.GetMethod("TryRemove");
                    if (mi != null)
                    {
                        mi.Invoke(optionsCache, new[] { idp.Scheme });
                    }
                }
            }
        }
    }
}