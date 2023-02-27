namespace Duende.IdentityServer.Configuration.Configuration;

public class IdentityServerConfigurationOptions
{
    public DynamicClientRegistrationOptions DynamicClientRegistration { get; set; } = new DynamicClientRegistrationOptions();
    public string Authority { get; set; } = string.Empty;
}
