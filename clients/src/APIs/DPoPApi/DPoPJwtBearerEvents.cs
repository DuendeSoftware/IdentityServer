using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DPoPApi;

public class DPoPJwtBearerEvents : JwtBearerEvents
{
    static TimeSpan ProofTokenValidityDuration = TimeSpan.FromSeconds(1);
    static TimeSpan ClientClockSkew = TimeSpan.FromMinutes(0);
    static TimeSpan ServerClockSkew = TimeSpan.FromMinutes(5);

    const bool ValidateIat = true;
    const bool ValidateNonce = false;

    const string ReplayCachePurpose = "DPoPJwtBearerEvents-DPoPReplay-jti-";
    const string DataProtectorPurpose = "DPoPJwtBearerEvents-DPoPProofValidation-nonce";

    protected readonly IDataProtector DataProtector;
    protected readonly IReplayCache ReplayCache;
    protected readonly ILogger<DPoPJwtBearerEvents> Logger;

    public DPoPJwtBearerEvents(IDataProtectionProvider dataProtectionProvider, IReplayCache replayCache, ILogger<DPoPJwtBearerEvents> logger)
    {
        DataProtector = dataProtectionProvider.CreateProtector(DataProtectorPurpose);
        ReplayCache = replayCache;
        Logger = logger;
    }

    public override Task MessageReceived(MessageReceivedContext context)
    {
        var authz = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authz.StartsWith(OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP + " "))
        {
            var value = authz.Substring((OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP + " ").Length).Trim();
            context.Token = value;
        }
        else
        {
            // this rejects the attempt for this handler
            context.NoResult();
        }

        return Task.CompletedTask;
    }
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var dpopProofToken = context.HttpContext.Request.Headers[OidcConstants.HttpHeaders.DPoP].FirstOrDefault();

        var result = await ValidateAsync(new DPoPProofValidatonContext
        {
            ProofToken = dpopProofToken,
            AccessTokenClaims = context.Principal.Claims,
            Method = context.HttpContext.Request.Method,
            Url = context.HttpContext.Request.Scheme + "://" + context.HttpContext.Request.Host + context.HttpContext.Request.PathBase + context.HttpContext.Request.Path
        });

        if (result.IsError)
        {
            if (result.ServerIssuedNonce != null)
            {
                context.HttpContext.Items["DPoP-Nonce"] = result.ServerIssuedNonce;
            }
            context.HttpContext.Items["DPoP-Error"] = result.Error;
            // fails the result
            context.Fail(result.ErrorDescription);
        }
    }
    public override Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.HttpContext.Items.ContainsKey("DPoP-Error"))
        {
            var error = context.HttpContext.Items["DPoP-Error"] as string;
            context.Error = error;
        }
        if (context.HttpContext.Items.ContainsKey("DPoP-Nonce"))
        {
            var nonce = context.HttpContext.Items["DPoP-Nonce"] as string;
            context.Response.Headers["DPoP-Nonce"] = nonce;
        }
        return Task.CompletedTask;
    }

    public readonly static IEnumerable<string> SupportedDPoPSigningAlgorithms = new[]
    {
        SecurityAlgorithms.RsaSha256,
        SecurityAlgorithms.RsaSha384,
        SecurityAlgorithms.RsaSha512,

        SecurityAlgorithms.RsaSsaPssSha256,
        SecurityAlgorithms.RsaSsaPssSha384,
        SecurityAlgorithms.RsaSsaPssSha512,

        SecurityAlgorithms.EcdsaSha256,
        SecurityAlgorithms.EcdsaSha384,
        SecurityAlgorithms.EcdsaSha512
    };

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

            Logger.LogDebug("Successfully validated DPoP proof token");
            result.IsError = false;
        }
        finally
        {
            if (result.IsError)
            {
                result.Error = OidcConstants.TokenErrors.InvalidDPoPProof;
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

        if (!token.TryGetHeaderValue<string>("typ", out var typ) || typ != "dpop+jwt")
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'typ' value.";
            return Task.CompletedTask;
        }

        if (!token.TryGetHeaderValue<string>("alg", out var alg) || !SupportedDPoPSigningAlgorithms.Contains(alg))
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

        var jwkJson = JsonSerializer.Serialize(jwkValues);

        Microsoft.IdentityModel.Tokens.JsonWebKey jwk;
        try
        {
            jwk = new Microsoft.IdentityModel.Tokens.JsonWebKey(jwkJson);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP jwk value: {error}", ex.Message);
            result.IsError = true;
            result.ErrorDescription = "Invalid 'jwk' value.";
            return Task.CompletedTask;
        }

        if (jwk.HasPrivateKey)
        {
            result.IsError = true;
            result.ErrorDescription = "'jwk' value contains a private key.";
            return Task.CompletedTask;
        }

        result.JsonWebKey = jwkJson;
        result.JsonWebKeyThumbprint = jwk.CreateThumbprint();
        result.Confirmation = jwk.CreateThumbprintCnf();

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

        if (!result.Payload.TryGetValue("htm", out var htm) || !context.Method.Equals(htm))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htm' value.";
            return;
        }

        if (!result.Payload.TryGetValue("htu", out var htu) || !context.Url.Equals(htu))
        {
            result.IsError = true;
            result.ErrorDescription = "Invalid 'htu' value.";
            return;
        }

        if (result.Payload.TryGetValue("iat", out var iat))
        {
            if (iat is int)
            {
                result.IssuedAt = (int) iat;
            }
            if (iat is long)
            {
                result.IssuedAt = (long) iat;
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

        await ValidateFreshnessAsync(context, result);
        if (result.IsError)
        {
            Logger.LogDebug("Failed to validate DPoP token freshness");
            return;
        }

        // we do replay at the end so we only add to the reply cache if everything else is ok
        await ValidateReplayAsync(context, result);
        if (result.IsError)
        {
            Logger.LogDebug("Detected replay of DPoP token");
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
            return;
        }

        // get largest skew based on how client's freshness is validated
        var validateIat = ValidateIat;
        var validateNonce = ValidateNonce;
        var skew = TimeSpan.Zero;
        if (validateIat && ClientClockSkew > skew)
        {
            skew = ClientClockSkew;
        }
        if (validateNonce && ServerClockSkew > skew)
        {
            skew = ServerClockSkew;
        }

        // we do x2 here because clock might be might be before or after, so we're making cache duration 
        // longer than the likelyhood of proof token expiration, which is done before replay
        skew *= 2;
        var cacheDuration = ProofTokenValidityDuration + skew;
        await ReplayCache.AddAsync(ReplayCachePurpose, result.TokenId, DateTimeOffset.UtcNow.Add(cacheDuration));
    }

    /// <summary>
    /// Validates the freshness.
    /// </summary>
    protected virtual async Task ValidateFreshnessAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        var validateIat = ValidateIat;
        if (validateIat)
        {
            await ValidateIatAsync(context, result);
            if (result.IsError)
            {
                return;
            }
        }

        var validateNonce = ValidateNonce;
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
        if (IsExpired(context, result, ClientClockSkew, result.IssuedAt.Value))
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
        if (String.IsNullOrWhiteSpace(result.Nonce))
        {
            result.IsError = true;
            result.ErrorDescription = "Missing 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce(context, result);
            return;
        }

        var time = await GetUnixTimeFromNonceAsync(context, result);
        if (time <= 0)
        {
            Logger.LogDebug("Invalid time value read from the 'nonce' value");

            result.IsError = true;
            result.ErrorDescription = "Invalid 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce(context, result);
            return;
        }

        if (IsExpired(context, result, ServerClockSkew, time))
        {
            Logger.LogDebug("DPoP 'nonce' expiration failed. It's possible that the server farm clocks might not be closely synchronized, so consider setting the ServerClockSkew on the DPoPOptions on the IdentityServerOptions.");

            result.IsError = true;
            result.ErrorDescription = "Invalid 'nonce' value.";
            result.ServerIssuedNonce = CreateNonce(context, result);
            return;
        }
    }

    /// <summary>
    /// Creates a nonce value to return to the client.
    /// </summary>
    /// <returns></returns>
    protected virtual string CreateNonce(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return DataProtector.Protect(now.ToString());
    }

    /// <summary>
    /// Reads the time the nonce was created.
    /// </summary>
    /// <returns></returns>
    protected virtual ValueTask<long> GetUnixTimeFromNonceAsync(DPoPProofValidatonContext context, DPoPProofValidatonResult result)
    {
        try
        {
            var value = DataProtector.Unprotect(result.Nonce);
            if (Int64.TryParse(value, out long iat))
            {
                return ValueTask.FromResult(iat);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Error parsing DPoP 'nonce' value: {error}", ex.ToString());
        }

        return ValueTask.FromResult<long>(0);
    }

    /// <summary>
    /// Validates the expiration of the DPoP proof.
    /// Returns true if the time is beyond the allowed limits, false otherwise.
    /// </summary>
    protected virtual bool IsExpired(DPoPProofValidatonContext context, DPoPProofValidatonResult result, TimeSpan clockSkew, long issuedAtTime)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var start = now + (int) clockSkew.TotalSeconds;
        if (start < issuedAtTime)
        {
            var diff = issuedAtTime - now;
            Logger.LogDebug("Expiration check failed. Creation time was too far in the future. The time being checked was {iat}, and clock is now {now}. The time difference is {diff}", issuedAtTime, now, diff);
            return true;
        }

        var expiration = issuedAtTime + (int) ProofTokenValidityDuration.TotalSeconds;
        var end = now - (int) clockSkew.TotalSeconds;
        if (expiration < end)
        {
            var diff = now - expiration;
            Logger.LogDebug("Expiration check failed. Expiration has already happened. The expiration was at {exp}, and clock is now {now}. The time difference is {diff}", expiration, now, diff);
            return true;
        }

        return false;
    }
}

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

public class DPoPProofValidatonResult
{
    public static DPoPProofValidatonResult Success = new DPoPProofValidatonResult { IsError = false };

    public bool IsError { get; set; }
    public string Error { get; set; }
    public string ErrorDescription { get; set; }

    /// <summary>
    /// The serialized JWK from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKey { get; set; }

    /// <summary>
    /// The JWK thumbprint from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKeyThumbprint { get; set; }

    /// <summary>
    /// The cnf value for the DPoP proof token 
    /// </summary>
    public string Confirmation { get; set; }

    /// <summary>
    /// The payload value of the DPoP proof token.
    /// </summary>
    public IDictionary<string, object> Payload { get; internal set; }

    /// <summary>
    /// The jti value read from the payload.
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// The nonce value read from the payload.
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// The iat value read from the payload.
    /// </summary>
    public long? IssuedAt { get; set; }

    /// <summary>
    /// The nonce value issued by the server.
    /// </summary>
    public string ServerIssuedNonce { get; set; }
}

/// <summary>
/// Extensions methods for JsonWebKey
/// </summary>
static class JsonWebKeyExtensions
{
    /// <summary>
    /// Create the value of a thumbprint-based cnf claim
    /// </summary>
    public static string CreateThumbprintCnf(this JsonWebKey jwk)
    {
        var jkt = jwk.CreateThumbprint();
        var values = new Dictionary<string, string>
        {
            { JwtClaimTypes.ConfirmationMethods.JwkThumbprint, jkt }
        };
        return JsonSerializer.Serialize(values);
    }
    /// <summary>
    /// Create the value of a thumbprint
    /// </summary>
    public static string CreateThumbprint(this JsonWebKey jwk)
    {
        var jkt = Base64Url.Encode(jwk.ComputeJwkThumbprint());
        return jkt;
    }
}

public interface IReplayCache
{
    /// <summary>
    /// Adds a handle to the cache 
    /// </summary>
    /// <param name="purpose"></param>
    /// <param name="handle"></param>
    /// <param name="expiration"></param>
    /// <returns></returns>
    Task AddAsync(string purpose, string handle, DateTimeOffset expiration);


    /// <summary>
    /// Checks if a cached handle exists 
    /// </summary>
    /// <param name="purpose"></param>
    /// <param name="handle"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(string purpose, string handle);
}
/// <summary>
/// Default implementation of the replay cache using IDistributedCache
/// </summary>
public class DefaultReplayCache : IReplayCache
{
    private const string Prefix = nameof(DefaultReplayCache) + "-";

    private readonly IDistributedCache _cache;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="cache"></param>
    public DefaultReplayCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task AddAsync(string purpose, string handle, DateTimeOffset expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        };

        await _cache.SetAsync(Prefix + purpose + handle, new byte[] { }, options);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string purpose, string handle)
    {
        return (await _cache.GetAsync(Prefix + purpose + handle, default)) != null;
    }
}