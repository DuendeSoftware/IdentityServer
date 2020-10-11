// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting;

namespace Duende.IdentityServer.Extensions
{
    internal static class EndpointOptionsExtensions
    {
        public static bool IsEndpointEnabled(this EndpointsOptions options, Endpoint endpoint)
        {
            return endpoint?.Name switch
            {
                Constants.EndpointNames.Authorize => options.EnableAuthorizeEndpoint,
                Constants.EndpointNames.CheckSession => options.EnableCheckSessionEndpoint,
                Constants.EndpointNames.DeviceAuthorization => options.EnableDeviceAuthorizationEndpoint,
                Constants.EndpointNames.Discovery => options.EnableDiscoveryEndpoint,
                Constants.EndpointNames.EndSession => options.EnableEndSessionEndpoint,
                Constants.EndpointNames.Introspection => options.EnableIntrospectionEndpoint,
                Constants.EndpointNames.Revocation => options.EnableTokenRevocationEndpoint,
                Constants.EndpointNames.Token => options.EnableTokenEndpoint,
                Constants.EndpointNames.UserInfo => options.EnableUserInfoEndpoint,
                _ => true
            };
        }
    }
}