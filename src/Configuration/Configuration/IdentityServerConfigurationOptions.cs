namespace Duende.IdentityServer.Configuration.Configuration;

public class IdentityServerConfigurationOptions
{
    public DynamicClientRegistrationOptions DynamicClientRegistration { get; set; } = new DynamicClientRegistrationOptions();
}
