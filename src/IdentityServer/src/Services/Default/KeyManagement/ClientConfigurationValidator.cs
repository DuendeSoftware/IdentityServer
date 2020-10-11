// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Validation;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    public class ClientConfigurationValidator : DefaultClientConfigurationValidator
    {
        private readonly KeyManagementOptions _keyManagerOptions;

        public ClientConfigurationValidator(IdentityServerOptions isOptions, KeyManagementOptions keyManagerOptions = null)
            : base(isOptions)
        {
            _keyManagerOptions = keyManagerOptions;
        }

        protected override async Task ValidateLifetimesAsync(ClientConfigurationValidationContext context)
        {
            await base.ValidateLifetimesAsync(context);
            
            if (context.IsValid)
            {
                if (_keyManagerOptions == null) throw new System.Exception("KeyManagerOptions not configured.");

                var keyMaxAge = (int)_keyManagerOptions.KeyRetirement.TotalSeconds;
                var accessTokenAge = context.Client.AccessTokenLifetime;
                if (keyMaxAge < accessTokenAge)
                {
                    context.SetError("AccessTokenLifetime is greater than the IdentityServer signing key KeyRetirement value.");
                }
            }
        }
    }
}
