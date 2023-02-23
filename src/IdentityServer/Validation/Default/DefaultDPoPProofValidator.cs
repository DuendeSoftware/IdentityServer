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
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.DataProtection;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Default implementation of IDPoPProofValidator
/// </summary>
public class DefaultDPoPProofValidator : IDPoPProofValidator
{
    const string ReplayCachePurpose = "DPoPReplay-jti";
    const string DataProtectorPurpose = "DPoPProofValidation-nonce";

    /// <summary>
    /// The options
    /// </summary>
    protected readonly IdentityServerOptions Options;

    /// <summary>
    /// The clock
    /// </summary>
    protected readonly ISystemClock Clock;

    /// <summary>
    /// The replay cache
    /// </summary>
    protected IReplayCache ReplayCache;

    /// <summary>
    /// The server urls service
    /// </summary>
    protected readonly IServerUrls ServerUrls;

    /// <summary>
    /// The data protection provider
    /// </summary>
    protected IDataProtector DataProtector { get; }

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultDPoPProofValidator(
        IdentityServerOptions options,
        IServerUrls server,
        IReplayCache replayCache,
        ISystemClock clock,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DefaultDPoPProofValidator> logger)
    {
        Options = options;
        Clock = clock;
        ReplayCache = replayCache;
        ServerUrls = server;
        DataProtector = dataProtectionProvider.CreateProtector(DataProtectorPurpose);
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

        result.Payload = tokenValidationResult.Claims;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the payload.
    /// </summary>
    protected virtual async Task ValidatePayloadAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        if (result.Payload.TryGetValue(JwtClaimTypes.JwtId, out var jti))
        {
            result.TokenId = jti as string;
        }

        if (String.IsNullOrEmpty(result.TokenId))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'jti' value.";
            return;
        }

        if (!result.Payload.TryGetValue("htm", out var htm) || !"POST".Equals(htm))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htm' value.";
            return;
        }

        var tokenUrl = ServerUrls.BaseUrl.EnsureTrailingSlash() + ProtocolRoutePaths.Token;
        if (!result.Payload.TryGetValue("htu", out var htu) || !tokenUrl.Equals(htu))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htu' value.";
            return;
        }

        if (result.Payload.TryGetValue("iat", out var iat))
        {
            if (iat is int)
            {
                result.IssuedAt = (int)iat;
            }
            if (iat is long)
            {
                result.IssuedAt = (long)iat;
            }
        }

        if (!result.IssuedAt.HasValue)
        {
            result.IsError = true;
            result.ErrorDescription = "Missing 'iat' value.";
            return;
        }

        if (result.Payload.TryGetValue("nonce", out var nonce))
        {
            result.Nonce = nonce as string;
        }

        if (!result.IssuedAt.HasValue && String.IsNullOrEmpty(result.Nonce))
        {
            result.IsError = true;
            result.ErrorDescription = "Must provide either 'nonce' or 'iat' value.";
            return;
        }

        await ValidateReplayAsync(context, result);
        if (result.IsError)
        {
            Logger.LogDebug("Detected replay of DPoP token");
            return;
        }

        await ValidateFreshnessAsync(context, result);
        if (result.IsError)
        {
            Logger.LogDebug("Failed to validate DPoP token freshness");
            return;
        }
    }

    /// <summary>
    /// Validates is the token has been replayed.
    /// </summary>
    protected virtual async Task ValidateReplayAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        if (await ReplayCache.ExistsAsync(ReplayCachePurpose, result.TokenId))
        {
            result.IsError = true;
            result.ErrorDescription = "Detected DPoP proof token replay.";
        }

        await ReplayCache.AddAsync(ReplayCachePurpose, result.TokenId, Clock.UtcNow.Add(Options.DPoP.DPoPTokenValidityDuration));
    }

    /// <summary>
    /// Validates the freshness.
    /// </summary>
    protected virtual async Task ValidateFreshnessAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        var validateIat = (context.Client.DPoPValidationMode & DPoPTokenExpirationValidationMode.Iat) == DPoPTokenExpirationValidationMode.Iat;
        if (validateIat)
        {
            await ValidateIatAsync(context, result);
            if (result.IsError)
            {
                return;
            }
        }

        var validateNonce = (context.Client.DPoPValidationMode & DPoPTokenExpirationValidationMode.Nonce) == DPoPTokenExpirationValidationMode.Nonce;
        if (validateNonce)
        {
            await ValidateNonceAsync(context, result);
            if (result.IsError)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Validates the freshness of the iat value.
    /// </summary>
    protected virtual Task ValidateIatAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        if (IsExpired(context, result, result.IssuedAt.Value))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'iat' value.";
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the freshness of the nonce value.
    /// </summary>
    protected virtual async Task ValidateNonceAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        if (result.Nonce.IsMissing())
        {
            result.IsError = true;
            result.ErrorDescription = "Missing 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce();
            return;
        }

        var time = await GetUnixTimeFromNonceAsync(result.Nonce);
        if (time <= 0)
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce();
            return;
        }

        if (IsExpired(context, result, time))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce();
            return;
        }
    }

    /// <summary>
    /// Creates a nonce value to return to the client.
    /// </summary>
    /// <returns></returns>
    protected virtual string CreateNonce()
    {
        var now = Clock.UtcNow.ToUnixTimeSeconds();
        return DataProtector.Protect(now.ToString());
    }

    /// <summary>
    /// Reads the time the nonce was created.
    /// </summary>
    /// <returns></returns>
    protected virtual ValueTask<long> GetUnixTimeFromNonceAsync(string nonce)
    {
        try
        {
            var value = DataProtector.Unprotect(nonce);
            if (Int64.TryParse(value, out long iat))
            {
                return ValueTask.FromResult(iat);
            }
        }
        catch(Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP 'nonce' value: {error}", ex.ToString());
        }
        
        return ValueTask.FromResult<long>(0);
    }

    /// <summary>
    /// Validates the expiration of the DPoP proof.
    /// Returns true if the time is beyond the allowed limits, false otherwise.
    /// </summary>
    protected virtual bool IsExpired(DPoPProofValidatonContext context, DPoPProofValidatonResult result, long unixTime)
    {
        var now = Clock.UtcNow;
        var skew = Options.DPoP.ClockSkew;
        var start = now.Subtract(skew).ToUnixTimeSeconds();
        
        var validityWindow = Options.DPoP.DPoPTokenValidityDuration;
        var end = now.Add(validityWindow + skew).ToUnixTimeSeconds();
        
        if (unixTime < start || unixTime > end)
        {
            return true;
        }

        return false;
    }
}