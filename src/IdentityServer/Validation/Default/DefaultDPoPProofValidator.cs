// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using Duende.IdentityServer.Extensions;
using System.Text.Json;
using IdentityModel;
using System.Linq;
using Duende.IdentityServer.Services;
using static Duende.IdentityServer.Constants;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Default implementation of IDPoPProofValidator
/// </summary>
public class DefaultDPoPProofValidator : IDPoPProofValidator
{
    /// <summary>
    /// The clock
    /// </summary>
    protected readonly ISystemClock Clock;

    /// <summary>
    /// The server urls service
    /// </summary>
    protected readonly IServerUrls ServerUrls;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultDPoPProofValidator(
        IServerUrls server,
        ISystemClock clock,
        ILogger<DefaultDPoPProofValidator> logger)
    {
        Clock = clock;
        ServerUrls = server;
        Logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DPoPProofValidatonResult> ValidateAsync(DPoPProofValidatonContext context)
    {
        var result = new DPoPProofValidatonResult() { IsError = false };

        try
        {
            if (String.IsNullOrEmpty(context?.ProofToken))
            {
                result.IsError = true;
                result.ErrorDescription = "Missing DPoP proof value.";
                return result;
            }

            await ValidateHeaderAsync(context, result);
            if (result.IsError)
            {
                Logger.LogDebug("Failed to validate DPoP header");
                return result;
            }

            await ValidateSignatureAsync(context, result);
            if (result.IsError)
            {
                Logger.LogDebug("Failed to validate DPoP signature");
                return result;
            }

            await ValidatePayloadAsync(context, result);
            if (result.IsError)
            {
                Logger.LogDebug("Failed to validate DPoP payload");
                return result;
            }

            await ValidateFreshnessAsync(context, result);
            if (result.IsError)
            {
                Logger.LogDebug("Failed to validate DPoP token freshness");
                return result;

            }

            Logger.LogDebug("Successfully validated DPoP proof token with thumbprint: {jkt}", result.JsonWebKeyThumbprint);
            result.IsError = false;
        }
        finally
        {
            if (result.IsError && result.Error.IsMissing())
            {
                // TODO: IdentityModel
                result.Error = "invalid_dpop_proof";
            }
        }

        return result;
    }

    /// <summary>
    /// Validates the header.
    /// </summary>
    protected virtual Task ValidateHeaderAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        JsonWebToken token;

        try
        {
            var handler = new JsonWebTokenHandler();
            token = handler.ReadJsonWebToken(context.ProofToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP token: {error}", ex.Message);
            result.IsError = true;
            result.ErrorDescription = "Malformed DPoP token.";
            return Task.CompletedTask;
        }

        if (!token.TryGetHeaderValue<string>("typ", out var typ) || typ != "dpop+jwk")
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'typ' value.";
            return Task.CompletedTask;
        }

        if (!token.TryGetHeaderValue<string>("alg", out var alg) || !IdentityServerConstants.SupportedDPoPSigningAlgorithms.Contains(alg))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'alg' value.";
            return Task.CompletedTask;
        }

        if (!token.TryGetHeaderValue<IDictionary<string, object>>("jwk", out var jwkValues))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'jwk' value.";
            return Task.CompletedTask;
        }

        result.JsonWebKey = JsonSerializer.Serialize(jwkValues);

        Microsoft.IdentityModel.Tokens.JsonWebKey jwt;
        try
        {
            jwt = new Microsoft.IdentityModel.Tokens.JsonWebKey(result.JsonWebKey);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP jwk value: {error}", ex.Message);
            result.IsError = true;
            result.ErrorDescription = "Invalid 'jwk' value.";
            return Task.CompletedTask;
        }

        if (jwt.HasPrivateKey)
        {
            result.IsError = true;
            result.ErrorDescription = "'jwk' value contains a private key.";
            return Task.CompletedTask;
        }

        result.JsonWebKeyThumbprint = Base64Url.Encode(jwt.ComputeJwkThumbprint());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the signature.
    /// </summary>
    protected virtual Task ValidateSignatureAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        Microsoft.IdentityModel.Tokens.TokenValidationResult tokenValidationResult;

        try
        {
            var key = new Microsoft.IdentityModel.Tokens.JsonWebKey(result.JsonWebKey);
            var tvp = new Microsoft.IdentityModel.Tokens.TokenValidationParameters 
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                IssuerSigningKey = key,
            };
            
            var handler = new JsonWebTokenHandler();
            tokenValidationResult = handler.ValidateToken(context.ProofToken, tvp);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP token: {error}", ex.Message);
            result.IsError = true;
            result.ErrorDescription = "Invalid signature on DPoP token.";
            return Task.CompletedTask;
        }

        if (tokenValidationResult.Exception != null)
        {
            Logger.LogDebug("Error parsing DPoP token: {error}", tokenValidationResult.Exception.Message);
            result.IsError = true;
            result.ErrorDescription = "Invalid signature on DPoP token.";
            return Task.CompletedTask;
        }

        result.ProofPayload = tokenValidationResult.Claims;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the payload.
    /// </summary>
    protected virtual Task ValidatePayloadAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        if (!result.ProofPayload.TryGetValue(JwtClaimTypes.JwtId, out var jti) || jti is not string)
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'jti' value.";
        }

        // TODO: validate jti against replay cache

        if (!result.ProofPayload.TryGetValue("htm", out var htm) || !"POST".Equals(htm))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htm' value.";
        }

        var tokenUrl = ServerUrls.BaseUrl.EnsureTrailingSlash() + ProtocolRoutePaths.Token;
        if (!result.ProofPayload.TryGetValue("htu", out var htu) || !tokenUrl.Equals(htu))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htu' value.";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the freshness.
    /// </summary>
    protected virtual Task ValidateFreshnessAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        return Task.CompletedTask;
    }
}