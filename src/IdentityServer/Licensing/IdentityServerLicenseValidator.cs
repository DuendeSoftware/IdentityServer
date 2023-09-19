// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable disable

using Duende.IdentityServer.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Duende.IdentityServer;

// APIs needed for IdentityServer specific license validation
internal class IdentityServerLicenseValidator : LicenseValidator<IdentityServerLicense>
{
    internal readonly static IdentityServerLicenseValidator Instance = new IdentityServerLicenseValidator();

    IdentityServerOptions _options;
    ConcurrentDictionary<string, byte> _clientIds = new ConcurrentDictionary<string, byte>();
    ConcurrentDictionary<string, byte> _issuers = new ConcurrentDictionary<string, byte>();

    public void Initialize(ILoggerFactory loggerFactory, IdentityServerOptions options, bool isDevelopment = false)
    {
        _options = options;

        Initialize(loggerFactory, "IdentityServer", options.LicenseKey);

        if (License?.RedistributionFeature == true && !isDevelopment)
        {
            // for redistribution/prod scenarios, we want most of these to be at trace level
            ErrorLog = WarningLog = InformationLog = DebugLog = LogToTrace;
        }

        ValidateLicense();
    }

    protected override void ValidateExpiration(List<string> errors)
    {
        if (!License.RedistributionFeature)
        {
            base.ValidateExpiration(errors);
        }
    }

    protected override void ValidateProductFeatures(List<string> errors)
    {
        if (License.IsCommunityEdition && License.RedistributionFeature)
        {
            throw new Exception("Invalid License: Redistribution is not valid for the IdentityServer Community Edition.");
        }

        if (License.IsBffEdition)
        {
            throw new Exception("Invalid License: The BFF edition license is not valid for IdentityServer.");
        }
        
        if (_options.KeyManagement.Enabled && !License.KeyManagementFeature)
        {
            errors.Add("You have automatic key management enabled, but your license does not include that feature of Duende IdentityServer. This feature requires the Business or Enterprise Edition tier of license. Either upgrade your license or disable automatic key management by setting the KeyManagement.Enabled property to false on the IdentityServerOptions.");
        }
    }
    protected override void WarnForProductFeaturesWhenMissingLicense()
    {
        if (_options.KeyManagement.Enabled)
        {
            WarningLog?.Invoke("You have automatic key management enabled, but you do not have a license. This feature requires the Business or Enterprise Edition tier of license. Alternatively you can disable automatic key management by setting the KeyManagement.Enabled property to false on the IdentityServerOptions.", null);
        }
    }

    bool ValidateClientWarned = false;
    public void ValidateClient(string clientId)
    {
        _clientIds.TryAdd(clientId, 1);

        if (License != null)
        {
            if (License.ClientLimit.HasValue && _clientIds.Count > License.ClientLimit)
            {
                ErrorLog.Invoke(
                    "Your license for Duende IdentityServer only permits {clientLimit} number of clients. You have processed requests for {clientCount}. The clients used were: {clients}.",
                    new object[] { License.ClientLimit, _clientIds.Count, _clientIds.Keys.ToArray() });
            }
        }
        else
        {
            if (_clientIds.Count > 5 && !ValidateClientWarned)
            {
                ValidateClientWarned = true;
                WarningLog?.Invoke(
                    "You do not have a license, and you have processed requests for {clientCount} clients. This number requires a tier of license higher than Starter Edition. The clients used were: {clients}.",
                    new object[] { _clientIds.Count, _clientIds.Keys.ToArray() });
            }
        }
    }

    bool ValidateIssuerWarned = false;
    public void ValidateIssuer(string iss)
    {
        _issuers.TryAdd(iss, 1);

        if (License != null)
        {
            if (License.IssuerLimit.HasValue && _issuers.Count > License.IssuerLimit)
            {
                ErrorLog.Invoke(
                    "Your license for Duende IdentityServer only permits {issuerLimit} number of issuers. You have processed requests for {issuerCount}. The issuers used were: {issuers}. This might be due to your server being accessed via different URLs or a direct IP and/or you have reverse proxy or a gateway involved. This suggests a network infrastructure configuration problem, or you are deliberately hosting multiple URLs and require an upgraded license.",
                    new object[] { License.IssuerLimit, _issuers.Count, _issuers.Keys.ToArray() });
            }
        }
        else
        {
            if (_issuers.Count > 1 && !ValidateIssuerWarned)
            {
                ValidateIssuerWarned = true;
                WarningLog?.Invoke(
                    "You do not have a license, and you have processed requests for {issuerCount} issuers. If you are deliberately hosting multiple URLs then this number requires a license per issuer, or the Enterprise Edition tier of license. If not then this might be due to your server being accessed via different URLs or a direct IP and/or you have reverse proxy or a gateway involved, and this suggests a network infrastructure configuration problem. The issuers used were: {issuers}.",
                    new object[] { _issuers.Count, _issuers.Keys.ToArray() });
            }
        }
    }

    bool ValidateServerSideSessionsWarned = false;
    public void ValidateServerSideSessions()
    {
        if (License != null)
        {
            if (!License.ServerSideSessionsFeature)
            {
                throw new Exception("You have configured server-side sessions. Your license for Duende IdentityServer does not include that feature. This feature requires the Business or Enterprise Edition tier of license.");
            }
        }
        else if (!ValidateServerSideSessionsWarned)
        {
            ValidateServerSideSessionsWarned = true;
            WarningLog?.Invoke("You have configured server-side sessions, but you do not have a license. This feature requires the Business or Enterprise Edition tier of license.", null);
        }
    }

    bool CanUseDPoPWarned = false;
    public void ValidateDPoP()
    {
        if (License != null)
        {
            if (!License.DPoPFeature)
            {
                throw new Exception("A request was made using DPoP. Your license for Duende IdentityServer does not include the DPoP feature. This feature requires the Enterprise Edition tier of license.");
            }
        }
        else if (!CanUseDPoPWarned)
        {
            CanUseDPoPWarned = true;
            WarningLog?.Invoke("A request was made using DPoP, but you do not have a license. This feature requires the Enterprise Edition tier of license.", null);
        }
    }

    bool ValidateResourceIndicatorsWarned = false;
    public void ValidateResourceIndicators(string resourceIndicator)
    {
        if (!String.IsNullOrWhiteSpace(resourceIndicator))
        {
            if (License != null)
            {
                if (!License.ResourceIsolationFeature)
                {
                    throw new Exception("A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation. This feature requires the Enterprise Edition tier of license.");
                }
            }
            else if (!ValidateResourceIndicatorsWarned)
            {
                ValidateResourceIndicatorsWarned = true;
                WarningLog?.Invoke("A request was made using a resource indicator, but you do not have a license. This feature requires the Enterprise Edition tier of license.", Array.Empty<object>());
            }
        }
    }
    public void ValidateResourceIndicators(IEnumerable<string> resourceIndicators)
    {
        if (resourceIndicators?.Any() == true)
        {
            if (License != null)
            {
                if (!License.ResourceIsolationFeature)
                {
                    throw new Exception("A request was made using a resource indicator. Your license for Duende IdentityServer does not permit resource isolation. This feature requires the Enterprise Edition tier of license.");
                }
            }
            else if (!ValidateResourceIndicatorsWarned)
            {
                ValidateResourceIndicatorsWarned = true;
                WarningLog?.Invoke("A request was made using a resource indicator, but you do not have a license. This feature requires the Enterprise Edition tier of license.", Array.Empty<object>());
            }
        }
    }

    bool ValidateDynamicProvidersWarned = false;
    public void ValidateDynamicProviders()
    {
        if (License != null)
        {
            if (!License.DynamicProvidersFeature)
            {
                throw new Exception("A request was made invoking a dynamic provider. Your license for Duende IdentityServer does not permit dynamic providers. This feature requires the Enterprise Edition tier of license.");
            }
        }
        else if (!ValidateDynamicProvidersWarned)
        {
            ValidateDynamicProvidersWarned = true;
            WarningLog?.Invoke("A request was made invoking a dynamic provider, but you do not have a license. This feature requires the Enterprise Edition tier of license.", null);
        }
    }

    bool ValidateCibaWarned = false;
    public void ValidateCiba()
    {
        if (License != null)
        {
            if (!License.CibaFeature)
            {
                throw new Exception("A CIBA (client initiated backchannel authentication) request was made. Your license for Duende IdentityServer does not permit the CIBA feature. This feature requires the Enterprise Edition tier of license.");
            }
        }
        else if (!ValidateCibaWarned)
        {
            ValidateCibaWarned = true;
            WarningLog?.Invoke("A CIBA (client initiated backchannel authentication) request was made, but you do not have a license. This feature requires the Enterprise Edition tier of license.", null);
        }
    }
}
