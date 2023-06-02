// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#nullable disable

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Duende.IdentityServer;

/// <summary>
/// Models the license for IdentityServer.
/// </summary>
public class License
{
    // for testing
    internal License(params Claim[] claims)
        : this(new ClaimsPrincipal(new ClaimsIdentity(claims)))
    {
    }

    internal License(ClaimsPrincipal claims)
    {
        if (Int32.TryParse(claims.FindFirst("id")?.Value, out var id))
        {
            Id = id;
        }

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
            throw new Exception($"Invalid edition in license: '{edition}'");
        }

        Edition = editionValue;
        RedistributionFeature = claims.HasClaim("feature", "isv") || claims.HasClaim("feature", "redistribution");
        Extras = claims.FindFirst("extras")?.Value;

        if (IsCommunityEdition && RedistributionFeature)
        {
            throw new Exception("Invalid License: Redistribution is not valid for community edition.");
        }

        if (IsBffEdition && RedistributionFeature)
        {
            throw new Exception("Invalid License: Redistribution is not valid for BFF edition.");
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
        
        CibaFeature = claims.HasClaim("feature", "ciba");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Community:
                CibaFeature = true;
                break;
        }

        ServerSideSessionsFeature = claims.HasClaim("feature", "server_side_sessions");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Business:
            case LicenseEdition.Community:
                ServerSideSessionsFeature = true;
                break;
        }

        ConfigApiFeature = claims.HasClaim("feature", "config_api");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Business:
            case LicenseEdition.Community:
                ConfigApiFeature = true;
                break;
        }

        DPoPFeature = claims.HasClaim("feature", "dpop");
        switch (Edition)
        {
            case LicenseEdition.Enterprise:
            case LicenseEdition.Community:
                DPoPFeature = true;
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
            if (RedistributionFeature)
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

            if (!RedistributionFeature)
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

        Claims = claims;
    }

    internal readonly ClaimsPrincipal Claims;

    /// <summary>
    /// The serial number
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The company name
    /// </summary>
    public string CompanyName { get; set; }
    /// <summary>
    /// The company contact info
    /// </summary>
    public string ContactInfo { get; set; }

    /// <summary>
    /// The license expiration
    /// </summary>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// The license edition 
    /// </summary>
    public LicenseEdition Edition { get; set; }

    internal bool IsEnterpriseEdition => Edition == LicenseEdition.Enterprise;
    internal bool IsBusinessEdition => Edition == LicenseEdition.Business;
    internal bool IsStarterEdition => Edition == LicenseEdition.Starter;
    internal bool IsCommunityEdition => Edition == LicenseEdition.Community;
    internal bool IsBffEdition => Edition == LicenseEdition.Bff;

    /// <summary>
    /// The client limit
    /// </summary>
    public int? ClientLimit { get; set; }
    /// <summary>
    /// The issuer limit
    /// </summary>
    public int? IssuerLimit { get; set; }

    /// <summary>
    /// Automatic key management
    /// </summary>
    public bool KeyManagementFeature { get; set; }
    /// <summary>
    /// Resource isolation
    /// </summary>
    public bool ResourceIsolationFeature { get; set; }
    /// <summary>
    /// Dynamic providers
    /// </summary>
    public bool DynamicProvidersFeature { get; set; }
    /// <summary>
    /// Redistribution
    /// </summary>
    public bool RedistributionFeature { get; set; }
    /// <summary>
    /// BFF
    /// </summary>
    public bool BffFeature { get; set; }
    /// <summary>
    /// CIBA
    /// </summary>
    public bool CibaFeature { get; set; }
    /// <summary>
    /// Server-side sessions
    /// </summary>
    public bool ServerSideSessionsFeature { get; set; }
    /// <summary>
    ///  Config API
    /// </summary>
    public bool ConfigApiFeature { get; set; }
    /// <summary>
    /// DPoP
    /// </summary>
    public bool DPoPFeature { get; set; }

    /// <summary>
    /// Extras
    /// </summary>
    public string Extras { get; set; }

    /// <summary>
    /// Models the license tier
    /// </summary>
    public enum LicenseEdition
    {
        /// <summary>
        /// Enterprise
        /// </summary>
        Enterprise,
        /// <summary>
        /// Business
        /// </summary>
        Business,
        /// <summary>
        /// Starter
        /// </summary>
        Starter,
        /// <summary>
        /// Community
        /// </summary>
        Community,
        /// <summary>
        /// Bff
        /// </summary>
        Bff
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ObjectSerializer.ToString(this);
    }

    internal static class ObjectSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string ToString(object o)
        {
            return JsonSerializer.Serialize(o, Options);
        }
    }
}