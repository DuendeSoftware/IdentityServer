// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Configuration.Configuration;

public class IdentityServerConfigurationOptions
{
    public DynamicClientRegistrationOptions DynamicClientRegistration { get; set; } = new DynamicClientRegistrationOptions();
}