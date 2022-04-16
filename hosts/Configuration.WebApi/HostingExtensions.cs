namespace IdentityServerHost;

internal static class HostingExtensions
{
    internal static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        return builder.Build();
    }
}