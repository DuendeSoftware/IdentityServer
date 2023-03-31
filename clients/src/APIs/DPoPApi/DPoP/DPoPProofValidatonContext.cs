using System.Collections.Generic;
using System.Security.Claims;

namespace DPoPApi;

public class DPoPProofValidatonContext
{
    public string Url { get; set; }
    public string Method { get; set; }

    /// <summary>
    /// The DPoP proof token to validate
    /// </summary>
    public string ProofToken { get; set; }

    /// <summary>
    /// The validated claims from the access token
    /// </summary>
    public IEnumerable<Claim> AccessTokenClaims { get; set; }
}
