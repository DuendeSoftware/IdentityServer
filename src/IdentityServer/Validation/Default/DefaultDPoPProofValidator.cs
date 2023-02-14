// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using System;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Default implementation of IDPoPProofValidator
/// </summary>
public class DefaultDPoPProofValidator : IDPoPProofValidator
{
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultDPoPProofValidator(
        IdentityServerOptions options,
        ISystemClock clock, 
        ILogger<DefaultDPoPProofValidator> logger)
    {
        _logger = logger;
        _clock = clock;
    }

    /// <inheritdoc/>
    public async Task<DPoPProofValidatonResult> ValidateAsync(DPoPProofValidatonContext context)
    {
        var result = new DPoPProofValidatonResult();

        if (String.IsNullOrEmpty(context?.ProofTooken))
        {
            // TODO: IdentityModel
            result.Error = "invalid_dpop_proof";
            result.ErrorDescription = "Missing DPoP proof value.";
        }
        else
        {
            await ValidateHeaderAsync(context, result);
            if (!result.IsError)
            {
                await ValidateSignatureAsync(context, result);
                if (!result.IsError)
                {
                    await ValidatePayloadAsync(context, result);
                    if (!result.IsError)
                    {
                        await ValidateFreshnessAsync(context, result);
                        if (result.IsError)
                        {
                            _logger.LogDebug("Failed to validate DPoP token freshness");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Failed to validate DPoP payload");
                    }
                }
                else
                {
                    _logger.LogDebug("Failed to validate DPoP signature");
                }
            }
            else
            {
                _logger.LogDebug("Failed to validate DPoP header");
            }
        }

        return result;
    }

    private Task ValidateHeaderAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(context.ProofTooken);

        var typ = token.GetHeaderValue<string>("typ");
        var alg = token.GetHeaderValue<string>("alg");
        var jwk = token.GetHeaderValue<IDictionary<string, object>>("jwk");
        
        return Task.CompletedTask;
    }

    private Task ValidateSignatureAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        return Task.CompletedTask;
    }

    private Task ValidatePayloadAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        return Task.CompletedTask;
    }

    private Task ValidateFreshnessAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        return Task.CompletedTask;
    }
}