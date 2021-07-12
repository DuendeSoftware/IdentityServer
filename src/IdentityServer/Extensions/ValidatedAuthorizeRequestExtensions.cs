// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Specialized;

#pragma warning disable 1591

namespace Duende.IdentityServer.Validation
{
    public static class ValidatedAuthorizeRequestExtensions
    {
        public static void RemovePrompt(this ValidatedAuthorizeRequest request)
        {
            var suppress = new StringBuilder();
            if (request.PromptModes.Contains(OidcConstants.PromptModes.Login))
            {
                suppress.Append(OidcConstants.PromptModes.Login);
            }
            if (request.PromptModes.Contains(OidcConstants.PromptModes.SelectAccount))
            {
                if (suppress.Length > 0)
                {
                    suppress.Append(" ");
                }
                suppress.Append(OidcConstants.PromptModes.SelectAccount);
            }

            request.Raw.Add(Constants.SuppressedPrompt, suppress.ToString());
            request.PromptModes = request.PromptModes.Except(new[] { OidcConstants.PromptModes.Login, OidcConstants.PromptModes.SelectAccount }).ToArray();
        }

        public static string GetPrefixedAcrValue(this ValidatedAuthorizeRequest request, string prefix)
        {
            var value = request.AuthenticationContextReferenceClasses
                .FirstOrDefault(x => x.StartsWith(prefix));

            if (value != null)
            {
                value = value.Substring(prefix.Length);
            }

            return value;
        }

        public static void RemovePrefixedAcrValue(this ValidatedAuthorizeRequest request, string prefix)
        {
            request.AuthenticationContextReferenceClasses.RemoveAll(acr => acr.StartsWith(prefix, StringComparison.Ordinal));
            var acr_values = request.AuthenticationContextReferenceClasses.ToSpaceSeparatedString();
            if (acr_values.IsPresent())
            {
                request.Raw[OidcConstants.AuthorizeRequest.AcrValues] = acr_values;
            }
            else
            {
                request.Raw.Remove(OidcConstants.AuthorizeRequest.AcrValues);
            }
        }

        public static string GetIdP(this ValidatedAuthorizeRequest request)
        {
            return request.GetPrefixedAcrValue(Constants.KnownAcrValues.HomeRealm);
        }

        public static void RemoveIdP(this ValidatedAuthorizeRequest request)
        {
            request.RemovePrefixedAcrValue(Constants.KnownAcrValues.HomeRealm);
        }

        public static string GetTenant(this ValidatedAuthorizeRequest request)
        {
            return request.GetPrefixedAcrValue(Constants.KnownAcrValues.Tenant);
        }

        public static IEnumerable<string> GetAcrValues(this ValidatedAuthorizeRequest request)
        {
            return request
                .AuthenticationContextReferenceClasses
                .Where(acr => !Constants.KnownAcrValues.All.Any(well_known => acr.StartsWith(well_known)))
                .Distinct()
                .ToArray();
        }

        public static void RemoveAcrValue(this ValidatedAuthorizeRequest request, string value)
        {
            request.AuthenticationContextReferenceClasses.RemoveAll(x => x.Equals(value, StringComparison.Ordinal));
            var acr_values = request.AuthenticationContextReferenceClasses.ToSpaceSeparatedString();
            if (acr_values.IsPresent())
            {
                request.Raw[OidcConstants.AuthorizeRequest.AcrValues] = acr_values;
            }
            else
            {
                request.Raw.Remove(OidcConstants.AuthorizeRequest.AcrValues);
            }
        }

        public static void AddAcrValue(this ValidatedAuthorizeRequest request, string value)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));

            request.AuthenticationContextReferenceClasses.Add(value);
            var acr_values = request.AuthenticationContextReferenceClasses.ToSpaceSeparatedString();
            request.Raw[OidcConstants.AuthorizeRequest.AcrValues] = acr_values;
        }

        public static string GenerateSessionStateValue(this ValidatedAuthorizeRequest request)
        {
            if (request == null) return null;
            if (!request.IsOpenIdRequest) return null;

            if (request.SessionId == null) return null;

            if (request.ClientId.IsMissing()) return null;
            if (request.RedirectUri.IsMissing()) return null;

            var clientId = request.ClientId;
            var sessionId = request.SessionId;
            var salt = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex);

            var uri = new Uri(request.RedirectUri);
            var origin = uri.Scheme + "://" + uri.Host;
            if (!uri.IsDefaultPort)
            {
                origin += ":" + uri.Port;
            }

            var bytes = Encoding.UTF8.GetBytes(clientId + origin + sessionId + salt);
            byte[] hash;

            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(bytes);
            }

            return Base64Url.Encode(hash) + "." + salt;
        }

        private static NameValueCollection ToOptimizedRawValues(this ValidatedAuthorizeRequest request)
        {
            if (request.Raw.AllKeys.Contains(OidcConstants.AuthorizeRequest.Request))
            {
                // if we already have a request object in the URL, then we can filter out the duplicate entries in the Raw collection
                var collection = new NameValueCollection();
                foreach (var key in request.Raw.AllKeys)
                {
                    // https://openid.net/specs/openid-connect-core-1_0.html#JWTRequests 
                    // requires client id and response type to always be in URL
                    if (key == OidcConstants.AuthorizeRequest.ClientId ||
                        key == OidcConstants.AuthorizeRequest.ResponseType ||
                        !request.RequestObjectValues.Any(x => x.Type == key))
                    {
                        collection.Add(key, request.Raw[key]);
                    }
                }

                return collection;
            }

            return request.Raw;
        }
        
        public static string ToOptimizedQueryString(this ValidatedAuthorizeRequest request)
        {
            return request.ToOptimizedRawValues().ToQueryString();
        }

        public static IDictionary<string, string[]> ToOptimizedFullDictionary(this ValidatedAuthorizeRequest request)
        {
            return request.ToOptimizedRawValues().ToFullDictionary();
        }
    }
}