// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#nullable disable

using Duende.IdentityServer.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Duende;

// APIs needed for IdentityServer specific license validation
internal partial class LicenseValidator
{
    static IdentityServerOptions _options;
    static ConcurrentDictionary<string, byte> _clientIds = new ConcurrentDictionary<string, byte>();
    static ConcurrentDictionary<string, byte> _issuers = new ConcurrentDictionary<string, byte>();

    public static void Initalize(ILoggerFactory loggerFactory, IdentityServerOptions options, bool isDevelopment = false)
    {
        _options = options;

        Initalize(loggerFactory.CreateLogger("Duende.License"), options.LicenseKey, isDevelopment);
    }

    // this should just add to the error list
    static void ValidateLicenseForProduct(IList<string> errors)
    {
        if (_license.IsBffEdition)
        {
            errors.Add($"Your Duende software license is not valid for IdentityServer.");
            return;
        }

        if (_options.KeyManagement.Enabled && !_license.KeyManagementFeature)
        {
            errors.Add("You have automatic key management enabled, but you do not have a valid license for that feature of Duende IdentityServer. Either upgrade your license or disable automatic key management by setting the KeyManagement.Enabled property to false on the IdentityServerOptions.");
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

    public static bool CanUseDPoP()
    {
        if (_license != null)
        {
            return _license.DPoPFeature;
        }

        _informationLog.Invoke("A request was made using DPoP, but you do not have a license. This feature requires the Enterprise Edition tier of license.", null);
        return true;
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
}