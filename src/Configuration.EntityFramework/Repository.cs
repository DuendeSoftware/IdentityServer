using Duende.IdentityServer.Configuration.EntityFramework.DbContexts;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.EntityFramework;

public abstract class Repository
{
    protected Repository(ConfigurationDbContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    /// <summary>
    /// The configuration database context.
    /// </summary>
    protected ConfigurationDbContext Context { get; }

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;
}