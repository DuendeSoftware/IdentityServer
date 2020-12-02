// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;

namespace Duende.IdentityServer.Validation
{
    internal class LicenseValidator
    {
        const string LicenseFileName = "Duende_IdentityServer_License.txt";

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
            _license = ParseLicenseKey(key);
        }

        private static string LoadFromFile()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), LicenseFileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return null;
        }

        private static License ParseLicenseKey(string licenseKey)
        {
            if (String.IsNullOrWhiteSpace(licenseKey)) return null;

            return System.Text.Json.JsonSerializer.Deserialize<License>(licenseKey);
            
            //return null;
            //return new License { Edition = "Starter", ClientLimit = 5, IssuerLimit = 1 };
            //return new License { Edition = "Enterprise", ClientLimit = 15 };
            //return new License { Edition = "Enterprise", Expiration = DateTime.UtcNow.AddHours(1) };
        }

        public static void ValidateLicense()
        {
            var errors = new List<string>();

            if (_license == null)
            {
                errors.Add("You do not have a valid license key for Duende IdentityServer.");
            }
            else
            {
                if (_options.KeyManagement.Enabled && !_license.KeyManagement)
                {
                    errors.Add("You have automatic key management enabled, yet you do not have the valid license for that feature of Duende IdentityServer.");
                }
            }

            if (errors.Count > 0)
            {
                foreach(var err in errors)
                {
                    _logger.LogWarning(err);
                }
            }
            else
            {
                if (_license.Expiration.HasValue)
                {
                    _logger.LogInformation("You have a valid license for Duende IdentityServer which expires on {expiration}.", _license.Expiration.Value.ToLongDateString());
                }
                else
                {
                    _logger.LogInformation("You have a valid license for Duende IdentityServer.");
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
                        _logger.LogWarning("Your license for Duende IdentityServer only permits {clientLimit} number of clients. You have processed requests for {clientCount}.", _license.ClientLimit, _clientIds.Count);
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
                        _logger.LogWarning("Your license for Duende IdentityServer only permits {issuerLimit} number of issuers. You have processed requests for {issuerCount}.", _license.IssuerLimit, _issuers.Count);
                    }
                }
            }
        }
    }

    internal class License
    {
        public DateTime? Expiration { get; set; }
        public int? ClientLimit { get; set; }
        public int? IssuerLimit { get; set; }
        public bool KeyManagement { get; set; }
    }
}
