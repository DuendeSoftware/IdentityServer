// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    // todo: ciba perhaps make a default IBackchannelAuthenticationUserValidator based on the idtokenhint claims?
    // and maybe it calls into the profile service?

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