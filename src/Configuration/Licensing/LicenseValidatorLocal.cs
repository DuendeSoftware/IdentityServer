// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#nullable disable

using Duende.IdentityServer.Configuration.Configuration;
using Microsoft.Extensions.Logging;

namespace Duende;

// APIs needed for IdentityServer specific license validation
internal partial class LicenseValidator
{
    public static void Initalize(ILoggerFactory loggerFactory, IdentityServerConfigurationOptions options, bool isDevelopment = false)
    {
        Initalize(loggerFactory, "IdentityServer.Configuration", options.LicenseKey, isDevelopment);
    }

    // this should just add to the error list
    static void ValidateProductFeaturesForLicense(IList<string> errors)
    {
        if (!_license.ConfigApiFeature)
        {
            errors.Add($"Your Duende software license does not include the Configuration API feature.");
        }
    }
    static void WarnForProductFeaturesWhenMissingLicense()
    { 
        // none
    }
}