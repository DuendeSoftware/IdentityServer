using Duende.IdentityServer.Hosting.DynamicProviders;
using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// Add extension methods for configuring generic dynamic providers.
/// </summary>
public static class IdentityServerBuilderDynamicSchemesExtensions
{
    /// <summary>
    /// Adds the in memory identity provider store.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="providers"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddInMemoryIdentityProviders(
        this IIdentityServerBuilder builder, IEnumerable<IdentityProvider> providers)
    {
        builder.Services.AddSingleton(providers);
        builder.AddIdentityProviderStore<InMemoryIdentityProviderStore>();

        return builder;
    }
}
