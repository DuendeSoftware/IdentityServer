using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Threading.Tasks;

namespace IdentityServer.UnitTests.Validation.Setup
{
    class TestTokenValidator : ITokenValidator
    {
        private readonly TokenValidationResult _result;

        public TestTokenValidator(TokenValidationResult result)
        {
            _result = result;
        }

        public Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string expectedScope = null)
        {
            return Task.FromResult(_result);
        }

        public Task<TokenValidationResult> ValidateIdentityTokenAsync(string token, string clientId = null, bool validateLifetime = true)
        {
            return Task.FromResult(_result);
        }

        public Task<TokenValidationResult> ValidateRefreshTokenAsync(string token, Client client = null)
        {
            return Task.FromResult(_result);
        }
    }
}