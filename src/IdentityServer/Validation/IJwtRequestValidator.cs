using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for request object validator
    /// </summary>
    public interface IJwtRequestValidator
    {
        /// <summary>
        /// Validates a JWT request object
        /// </summary>
        /// <param name="client">The client</param>
        /// <param name="jwtTokenString">The JWT</param>
        /// <returns></returns>
        Task<JwtRequestValidationResult> ValidateAsync(Client client, string jwtTokenString);
    }
}