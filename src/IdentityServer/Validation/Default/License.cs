// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Security.Claims;

namespace Duende.IdentityServer.Validation;

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
        ISVFeature = claims.HasClaim("feature", "isv");

        if (IsCommunityEdition && ISVFeature)
        {
            throw new Exception("Invalid License: ISV is not valid for community edition.");
        }

        if (IsBffEdition && ISVFeature)
        {
            throw new Exception("Invalid License: ISV is not valid for BFF edition.");
        }

        if (IsBffEdition)
        {
            // for BFF-only edition we set BFF flag and ignore all other features
            BffFeature = true;
            ClientLimit = 0;
            IssuerLimit = 0;
            return;
        }

        KeyManagementFeature = claims.HasClaim("feature", "key_management");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Business:
            case LicenseEdition.Community:
                KeyManagementFeature = true;
                break;
        }

        ResourceIsolationFeature = claims.HasClaim("feature", "resource_isolation");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Community:
                ResourceIsolationFeature = true;
                break;
        }
            
        DynamicProvidersFeature = claims.HasClaim("feature", "dynamic_providers");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Community:
                DynamicProvidersFeature = true;
                break;
        }

        BffFeature = claims.HasClaim("feature", "bff");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Business:
            case LicenseEdition.Community:
                BffFeature = true;
                break;
        }


        if (!claims.HasClaim("feature", "unlimited_clients"))
        {
            // default values
            if (ISVFeature)
            {
                // default for all ISV editions
                ClientLimit = 5;
            }
            else
            {
                // defaults limits for non-ISV editions
                switch (Edition)
                {
                    case LicenseEdition.Business:
                        ClientLimit = 15;
                        break;
                    case LicenseEdition.Starter:
                        ClientLimit = 5;
                        break;
                }
            }
                    
            if (Int32.TryParse(claims.FindFirst("client_limit")?.Value, out var clientLimit))
            {
                // explicit, so use that value
                ClientLimit = clientLimit;
            }

            if (!ISVFeature)
            {
                // these for the non-ISV editions that always have unlimited, regardless of explicit value
                switch (Edition)
                {
                    case LicenseEdition.Enterprise:
                    case LicenseEdition.Community:
                        // unlimited
                        ClientLimit = null;
                        break;
                }
            }
        }

        if (!claims.HasClaim("feature", "unlimited_issuers"))
        {
            // default 
            IssuerLimit = 1;

            if (Int32.TryParse(claims.FindFirst("issuer_limit")?.Value, out var issuerLimit))
            {
                IssuerLimit = issuerLimit;
            }

            // these for the editions that always have unlimited, regardless of explicit value
            switch (Edition)
            {
                case LicenseEdition.Enterprise:
                case LicenseEdition.Community:
                    // unlimited
                    IssuerLimit = null;
                    break;
            }
        }
    }

    public string CompanyName { get; set; }
    public string ContactInfo { get; set; }

    public DateTime? Expiration { get; set; }

    public LicenseEdition Edition { get; set; }

    internal bool IsEnterpriseEdition => Edition == LicenseEdition.Enterprise;
    internal bool IsBusinessEdition => Edition == LicenseEdition.Business;
    internal bool IsStarterEdition => Edition == LicenseEdition.Starter;
    internal bool IsCommunityEdition => Edition == LicenseEdition.Community;
    internal bool IsBffEdition => Edition == LicenseEdition.Bff;

    public int? ClientLimit { get; set; }
    public int? IssuerLimit { get; set; }

    public bool KeyManagementFeature { get; set; }
    public bool ResourceIsolationFeature { get; set; }
    public bool DynamicProvidersFeature { get; set; }
    public bool ISVFeature { get; set; }
    public bool BffFeature { get; set; }

    public enum LicenseEdition
    {
        Enterprise,
        Business,
        Starter,
        Community,
        Bff
    }

    public override string ToString()
    {
        return ObjectSerializer.ToString(this);
    }
}