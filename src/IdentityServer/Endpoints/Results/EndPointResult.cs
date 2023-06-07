// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using Duende.IdentityServer.Hosting;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Provides the base implementation of IEndpointResult.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EndpointResult<T> : IEndpointResult
    where T : class, IEndpointResult
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext context)
    {
        var generator = context.RequestServices.GetService<IEndpointResultGenerator<T>>();
        if (generator != null)
        {
            T target = this as T;
            if (target == null)
            {
                throw new Exception($"Type paramter {typeof(T)} must be the class derived from 'EndPointResult<T>'.");
            }

            await generator.ProcessAsync(target, context);
        }
        else
        {
            throw new Exception($"No IEndpointResultGenerator<T> registered for IEndpointResult type '{typeof(T)}'.");
        }
    }
}
