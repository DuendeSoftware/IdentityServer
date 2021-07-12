// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Security.Claims;

namespace Duende.IdentityServer.Validation
{
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
            ISV = claims.HasClaim("feature", "isv");

            if (ISV && IsCommunity)
            {
                throw new Exception("Invalid License: ISV is not valid for community edition.");
            }

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
            
            DynamicProviders = claims.HasClaim("feature", "dynamic_providers");
            switch (Edition)
            {
                case LicenseEdition.Enterprise:
                    DynamicProviders = true;
                    break;
            }

            if (!claims.HasClaim("feature", "unlimited_clients"))
            {
                if (IsEnterprise && !ISV)
                {
                    // non-ISV enterprise always has no client limit
                    ClientLimit = null; // unlimited
                }
                else if (Int32.TryParse(claims.FindFirst("client_limit")?.Value, out var clientLimit))
                {
                    ClientLimit = clientLimit;
                }
                else if (ISV)
                {
                    // default for all ISV editions
                    ClientLimit = 5;
                }
                else
                {
                    // defaults for non-ISV editions
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
        public bool DynamicProviders { get; set; }
        public bool ISV { get; set; }

        public enum LicenseEdition
        {
            Enterprise,
            Business,
            Starter,
            Community
        }

        public override string ToString()
        {
            return ObjectSerializer.ToString(this);
        }
    }
}