// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Nop implementation of IBackchannelAuthenticationUserValidator.
    /// </summary>
    public class NopBackchannelAuthenticationUserValidator : IBackchannelAuthenticationUserValidator
    {
        /// <inheritdoc/>
        public Task<BackchannelAuthenticationUserValidatonResult> ValidateRequestAsync(BackchannelAuthenticationUserValidatorContext userValidatorContext)
        {
            var result = new BackchannelAuthenticationUserValidatonResult { 
                Error = "not implemented"
            };
            return Task.FromResult(result);
        }
    }
}