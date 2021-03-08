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

namespace Duende.IdentityServer.Validation
{
    internal class LicenseValidator
    {
        const string LicenseFileName = "Duende_IdentityServer_License.key";

        static ILogger _logger;
        static IdentityServerOptions _options;
        static License _license;

        static ConcurrentDictionary<string, byte> _clientIds = new ConcurrentDictionary<string, byte>();
        static ConcurrentDictionary<string, byte> _issuers = new ConcurrentDictionary<string, byte>();

        public static void Initalize(ILoggerFactory loggerFactory, IdentityServerOptions options)
        {
            _logger = loggerFactory.CreateLogger("Duende.IdentityServer");
            _options = options;

            var key = options.LicenseKey ?? LoadFromFile();
            _license = ValidateKey(key);
        }

        private static string LoadFromFile()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), LicenseFileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }

            return null;
        }

        // todo: check this periodcally?
        public static void ValidateLicense()
        {
            var errors = new List<string>();

            if (_license == null)
            {
                var message = "You do not have a valid license key for Duende IdentityServer. " +
                              "This is allowed for development and testing scenarios. " +
                              "If you are running in production you are required to have a licensed version. Please start a conversation with us: https://duendesoftware.com/contact";

                _logger.LogWarning(message);
                return;
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("The validated licence key details: {@license}", _license);
                }

                if (_license.Expiration.HasValue)
                {
                    var diff = DateTime.UtcNow.Date.Subtract(_license.Expiration.Value.Date).TotalDays;
                    if (diff > 0 && !_license.ISV)
                    {
                        errors.Add($"Your license for Duende IdentityServer expired {diff} days ago.");
                    }
                }

                if (_options.KeyManagement.Enabled && !_license.KeyManagement)
                {
                    errors.Add(
                        "You have automatic key management enabled, but you do not have a valid license for that feature of Duende IdentityServer.");
                }
            }

            if (errors.Count > 0)
            {
                foreach (var err in errors)
                {
                    _logger.LogError(err);
                }

                if (_license != null)
                {
                    _logger.LogError(
                        "Please contact {licenceContact} from {licenseCompany} to obtain a valid license for Duende IdentityServer.",
                        _license.ContactInfo, _license.CompanyName);
                }
            }
            else
            {
                if (_license.Expiration.HasValue)
                {
                    _logger.LogInformation(
                        "You have a valid license key for Duende IdentityServer {edition} edition for use at {licenseCompany}. The license expires on {licenseExpiration}.",
                        _license.Edition, _license.CompanyName, _license.Expiration.Value.ToLongDateString());
                }
                else
                {
                    _logger.LogInformation(
                        "You have a valid license key for Duende IdentityServer {edition} edition for use at {licenseCompany}.",
                        _license.Edition, _license.CompanyName);
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
                        _logger.LogError(
                            "Your license for Duende IdentityServer only permits {clientLimit} number of clients. You have processed requests for {clientCount}.",
                            _license.ClientLimit, _clientIds.Count);
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
                        _logger.LogError(
                            "Your license for Duende IdentityServer only permits {issuerLimit} number of issuers. You have processed requests for {issuerCount}.",
                            _license.IssuerLimit, _issuers.Count);
                    }
                }
            }
        }

        public static void ValidateResourceIndicators(string resourceIndicator)
        {
            if (_license != null && !String.IsNullOrWhiteSpace(resourceIndicator) && !_license.ResourceIsolation)
            {
                _logger.LogError(
                    "A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation.");
            }
        }

        public static void ValidateResourceIndicators(IEnumerable<string> resourceIndicators)
        {
            if (_license != null && resourceIndicators?.Any() == true && !_license.ResourceIsolation)
            {
                _logger.LogError(
                    "A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation.");
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
                    _logger.LogCritical(validateResult.Exception, "Error validating Duende IdentityServer license key");
                }
            }

            return null;
        }
    }

    internal class License
    {
        // for testing
        internal License(params Claim[] claims)
            : this(new ClaimsPrincipal(new ClaimsIdentity(claims)))
        {
        }

        public License(ClaimsPrincipal claims)
        {
            CompanyName = claims.FindFirst("company_name")?.Value;
            ContactInfo = claims.FindFirst("contact_info")?.Value;

            if (Int64.TryParse(claims.FindFirst("exp")?.Value, out var exp))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                Expiration = expDate.UtcDateTime;
            }

            var edition = claims.FindFirst("edition")?.Value;
            if (!Enum.TryParse<License.LicenseEdition>(edition, true, out var editionValue))
            {
                throw new Exception($"Invalid edition in licence: '{edition}'");
            }

            Edition = editionValue;

            KeyManagement = claims.HasClaim("feature", "key_management");
            switch (Edition)
            {
                case LicenseEdition.Enterprise:
                case LicenseEdition.Business:
                case LicenseEdition.Community:
                    KeyManagement = true;
                    break;
            }

            ResourceIsolation = claims.HasClaim("feature", "resource_isolation");
            switch (Edition)
            {
                case LicenseEdition.Enterprise:
                    ResourceIsolation = true;
                    break;
            }

            if (!claims.HasClaim("feature", "unlimited_clients"))
            {
                if (IsEnterprise)
                {
                    ClientLimit = null; // unlimited
                }
                else if (Int32.TryParse(claims.FindFirst("client_limit")?.Value, out var clientLimit))
                {
                    ClientLimit = clientLimit;
                }
                else
                {
                    switch (Edition)
                    {
                        case LicenseEdition.Business:
                            ClientLimit = 15;
                            break;
                        case LicenseEdition.Starter:
                        case LicenseEdition.Community:
                            ClientLimit = 5;
                            break;
                    }
                }
            }

            if (!claims.HasClaim("feature", "unlimited_issuers"))
            {
                if (IsEnterprise)
                {
                    IssuerLimit = null; // unlimited
                }
                else if (Int32.TryParse(claims.FindFirst("issuer_limit")?.Value, out var issuerLimit))
                {
                    IssuerLimit = issuerLimit;
                }
                else
                {
                    IssuerLimit = 1;
                }
            }

            ISV = claims.HasClaim("feature", "isv");
        }

        public string CompanyName { get; set; }
        public string ContactInfo { get; set; }

        public DateTime? Expiration { get; set; }

        public LicenseEdition Edition { get; set; }

        internal bool IsEnterprise => Edition == LicenseEdition.Enterprise;
        internal bool IsBusiness => Edition == LicenseEdition.Business;
        internal bool IsStarter => Edition == LicenseEdition.Starter;
        internal bool IsCommunity => Edition == LicenseEdition.Community;

        public int? ClientLimit { get; set; }
        public int? IssuerLimit { get; set; }

        public bool KeyManagement { get; set; }
        public bool ResourceIsolation { get; set; }
        public bool ISV { get; set; }

        public enum LicenseEdition
        {
            Enterprise,
            Business,
            Starter,
            Community
        }
    }
}