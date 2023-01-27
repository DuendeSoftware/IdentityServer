// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Duende.IdentityServer.Validation;

internal class LicenseValidator
{
    static readonly string[] LicenseFileNames = new[] 
    {
        "Duende_License.key",
        "Duende_IdentityServer_License.key",
    };

    static ILogger _logger;
    static Action<string, object[]> _errorLog;
    static Action<string, object[]> _informationLog;
    static Action<string, object[]> _debugLog;

    static IdentityServerOptions _options;
    static License _license;

    static ConcurrentDictionary<string, byte> _clientIds = new ConcurrentDictionary<string, byte>();
    static ConcurrentDictionary<string, byte> _issuers = new ConcurrentDictionary<string, byte>();

    public static void Initalize(ILoggerFactory loggerFactory, IdentityServerOptions options, bool isDevelopment = false)
    {
        _logger = loggerFactory.CreateLogger("Duende.License");
        _options = options;

        var key = options.LicenseKey ?? LoadFromFile();
        _license = ValidateKey(key);

        if (_license?.RedistributionFeature == true && !isDevelopment)
        {
            // for redistribution/prod scenarios, we want most of these to be at trace level
            _errorLog = _informationLog = _debugLog = LogToTrace;
        }
        else
        {
            _errorLog = LogToError;
            _informationLog = LogToInformation;
            _debugLog = LogToDebug;
        }
    }

    private static string LoadFromFile()
    {
        foreach (var name in LicenseFileNames)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), name);
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }
        }

        return null;
    }

    public static void ValidateLicense()
    {
        var errors = new List<string>();

        if (_license == null)
        {
            var message = "You do not have a valid license key for the Duende software. " +
                          "This is allowed for development and testing scenarios. " +
                          "If you are running in production you are required to have a licensed version. " +
                          "Please start a conversation with us: https://duendesoftware.com/contact";

            _logger.LogWarning(message);
            return;
        }
        else if (_license.IsBffEdition)
        {
            errors.Add($"Your Duende software license is not valid for IdentityServer.");
        }
        else
        {
            _debugLog.Invoke("The validated licence key details: {@license}", new[] { _license });

            if (_license.Expiration.HasValue)
            {
                var diff = DateTime.UtcNow.Date.Subtract(_license.Expiration.Value.Date).TotalDays;
                if (diff > 0 && !_license.RedistributionFeature)
                {
                    errors.Add($"Your license for the Duende software expired {diff} days ago.");
                }
            }

            if (_options.KeyManagement.Enabled && !_license.KeyManagementFeature)
            {
                errors.Add("You have automatic key management enabled, but you do not have a valid license for that feature of Duende IdentityServer. Either upgrade your license or disable automatic key management by setting the KeyManagement.Enabled property to false on the IdentityServerOptions.");
            }
        }

        if (errors.Count > 0)
        {
            foreach (var err in errors)
            {
                _errorLog.Invoke(err, Array.Empty<object>());
            }

            _errorLog.Invoke(
                "Please contact {licenceContact} from {licenseCompany} to obtain a valid license for the Duende software.",
                new[] { _license.ContactInfo, _license.CompanyName });
        }
        else
        {
            if (_license.Expiration.HasValue)
            {
                _informationLog.Invoke("You have a valid license key for the Duende software {edition} edition for use at {licenseCompany}. The license expires on {licenseExpiration}.",
                    new object[] { _license.Edition, _license.CompanyName, _license.Expiration.Value.ToLongDateString() });
            }
            else
            {
                _informationLog.Invoke(
                    "You have a valid license key for the Duende software {edition} edition for use at {licenseCompany}.",
                    new object[] { _license.Edition, _license.CompanyName });
            }
        }
    }

    public static void ValidateClient(string clientId)
    {
        if (_license != null)
        {
            if (_license.ClientLimit.HasValue)
            {
                _clientIds.TryAdd(clientId, 1);
                if (_clientIds.Count > _license.ClientLimit)
                {
                    _errorLog.Invoke(
                        "Your license for Duende IdentityServer only permits {clientLimit} number of clients. You have processed requests for {clientCount}. The clients used were: {clients}.",
                        new object[] { _license.ClientLimit, _clientIds.Count, _clientIds.Keys.ToArray() });
                }
            }
        }
    }

    public static void ValidateIssuer(string iss)
    {
        if (_license != null)
        {
            if (_license.IssuerLimit.HasValue)
            {
                _issuers.TryAdd(iss, 1);
                if (_issuers.Count > _license.IssuerLimit)
                {
                    _errorLog.Invoke(
                        "Your license for Duende IdentityServer only permits {issuerLimit} number of issuers. You have processed requests for {issuerCount}. The issuers used were: {issuers}. This might be due to your server being accessed via different URLs or a direct IP and/or you have reverse proxy or a gateway involved. This suggests a network infrastructure configuration problem, or you are deliberately hosting multiple URLs and require an upgraded license.",
                        new object[] { _license.IssuerLimit, _issuers.Count, _issuers.Keys.ToArray() });
                }
            }
        }
    }
    
    public static void ValidateServerSideSessions()
    {
        if (_license != null && !_license.ServerSideSessionsFeature)
        {
            _errorLog.Invoke("You have configured server-side sessions. Your license for Duende IdentityServer does not include that feature.", Array.Empty<object>());
        }
    }

    public static void ValidateResourceIndicators(string resourceIndicator)
    {
        if (_license != null && !String.IsNullOrWhiteSpace(resourceIndicator) && !_license.ResourceIsolationFeature)
        {
            _errorLog.Invoke("A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation.", Array.Empty<object>());
        }
    }

    public static void ValidateResourceIndicators(IEnumerable<string> resourceIndicators)
    {
        if (_license != null && resourceIndicators?.Any() == true && !_license.ResourceIsolationFeature)
        {
            _errorLog.Invoke("A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation.", Array.Empty<object>());
        }
    }

    public static void ValidateDynamicProviders()
    {
        if (_license != null && !_license.DynamicProvidersFeature)
        {
            _errorLog.Invoke("A request was made invoking a dynamic provider. Your license for Duende IdentityServer does not permit dynamic providers.", Array.Empty<object>());
        }
    }

    public static void ValidateCiba()
    {
        if (_license != null && !_license.CibaFeature)
        {
            _errorLog.Invoke("A CIBA (client initiated backchannel authentication) request was made. Your license for Duende IdentityServer does not permit the CIBA feature.", Array.Empty<object>());
        }
    }

    internal static License ValidateKey(string licenseKey)
    {
        if (!String.IsNullOrWhiteSpace(licenseKey))
        {
            var handler = new JsonWebTokenHandler();

            var rsa = new RSAParameters
            {
                Exponent = Convert.FromBase64String("AQAB"),
                Modulus = Convert.FromBase64String(
                    "tAHAfvtmGBng322TqUXF/Aar7726jFELj73lywuCvpGsh3JTpImuoSYsJxy5GZCRF7ppIIbsJBmWwSiesYfxWxBsfnpOmAHU3OTMDt383mf0USdqq/F0yFxBL9IQuDdvhlPfFcTrWEL0U2JsAzUjt9AfsPHNQbiEkOXlIwtNkqMP2naynW8y4WbaGG1n2NohyN6nfNb42KoNSR83nlbBJSwcc3heE3muTt3ZvbpguanyfFXeoP6yyqatnymWp/C0aQBEI5kDahOU641aDiSagG7zX1WaF9+hwfWCbkMDKYxeSWUkQOUOdfUQ89CQS5wrLpcU0D0xf7/SrRdY2TRHvQ=="),
            };

            var key = new RsaSecurityKey(rsa)
            {
                KeyId = "IdentityServerLicensekey/7ceadbb78130469e8806891025414f16"
            };

            var parms = new TokenValidationParameters
            {
                ValidIssuer = "https://duendesoftware.com",
                ValidAudience = "IdentityServer",
                IssuerSigningKey = key,
                ValidateLifetime = false
            };

            var validateResult = handler.ValidateToken(licenseKey, parms);
            if (validateResult.IsValid)
            {
                return new License(new ClaimsPrincipal(validateResult.ClaimsIdentity));
            }
            else
            {
                _logger.LogCritical(validateResult.Exception, "Error validating the Duende software license key");
            }
        }

        return null;
    }
    
    private static void LogToTrace(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            LoggerExtensions.LogTrace(_logger, message, args);
        }
    }
    
    private static void LogToDebug(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LoggerExtensions.LogDebug(_logger, message, args);
        }
    }
    
    private static void LogToInformation(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            LoggerExtensions.LogInformation(_logger, message, args);
        }
    }
    
    private static void LogToError(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            LoggerExtensions.LogError(_logger, message, args);
        }
    }
}