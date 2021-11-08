// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Test;
using Duende.IdentityServer.Validation;
using IdentityModel;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Implementation of IBackchannelAuthenticationUserValidator using the test user store.
    /// </summary>
    public class TestBackchannelLoginUserValidator : IBackchannelAuthenticationUserValidator
    {
        private readonly TestUserStore _testUserStore;

        /// <summary>
        /// Ctor
        /// </summary>
        public TestBackchannelLoginUserValidator(TestUserStore testUserStore)
        {
            _testUserStore = testUserStore;
        }

        /// <inheritdoc/>
        public Task<BackchannelAuthenticationUserValidatonResult> ValidateRequestAsync(BackchannelAuthenticationUserValidatorContext userValidatorContext)
        {
            var result = new BackchannelAuthenticationUserValidatonResult();

            var user = _testUserStore.FindByUsername(userValidatorContext.LoginHint);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.Subject, user.SubjectId)
                };
                var ci = new ClaimsIdentity(claims, "ciba");
                result.Subject = new ClaimsPrincipal(ci);
            }
 
            return Task.FromResult(result);
        }
    }
}