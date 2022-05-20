// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using IdentityModel;
using System.Text;
using System;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// IMessageStore implementation that uses data protection to protect message.
/// </summary>
/// <typeparam name="TModel"></typeparam>
public class ProtectedDataMessageStore<TModel> : IMessageStore<TModel>
{
    private const string Purpose = "Duende.IdentityServer.Stores.ProtectedDataMessageStore";

    /// <summary>
    /// The data protector.
    /// </summary>
    protected readonly IDataProtector Protector;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public ProtectedDataMessageStore(IDataProtectionProvider provider, ILogger<ProtectedDataMessageStore<TModel>> logger)
    {
        Protector = provider.CreateProtector(Purpose);
        Logger = logger;
    }

    /// <inheritdoc />
    public virtual Task<Message<TModel>> ReadAsync(string value)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ProtectedDataMessageStore.Read");
        
        Message<TModel> result = null;

        if (!String.IsNullOrWhiteSpace(value))
        {
            try
            {
                var bytes = Base64Url.Decode(value);
                bytes = Protector.Unprotect(bytes);
                var json = Encoding.UTF8.GetString(bytes);
                result = ObjectSerializer.FromString<Message<TModel>>(json);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "Exception reading protected message");
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public virtual Task<string> WriteAsync(Message<TModel> message)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ProtectedDataMessageStore.Write");
        
        string value = null;

        try
        {
            var json = ObjectSerializer.ToString(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            bytes = Protector.Protect(bytes);
            value = Base64Url.Encode(bytes);
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Exception writing protected message");
        }

        return Task.FromResult(value);
    }
}