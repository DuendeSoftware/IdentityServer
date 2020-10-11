// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace Duende.IdentityServer.Extensions
{
    internal static class StringExtensions
    {
        [DebuggerStepThrough]
        public static string ToSpaceSeparatedString(this IEnumerable<string> list)
        {
            if (list == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(100);

            foreach (var element in list)
            {
                sb.Append(element + " ");
            }

            return sb.ToString().Trim();
        }

        [DebuggerStepThrough]
        public static IEnumerable<string> FromSpaceSeparatedString(this string input)
        {
            input = input.Trim();
            return input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static List<string> ParseScopesString(this string scopes)
        {
            if (scopes.IsMissing())
            {
                return null;
            }

            scopes = scopes.Trim();
            var parsedScopes = scopes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

            if (parsedScopes.Any())
            {
                parsedScopes.Sort();
                return parsedScopes;
            }

            return null;
        }

        [DebuggerStepThrough]
        public static bool IsMissing(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        [DebuggerStepThrough]
        public static bool IsMissingOrTooLong(this string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
            if (value.Length > maxLength)
            {
                return true;
            }

            return false;
        }

        [DebuggerStepThrough]
        public static bool IsPresent(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        [DebuggerStepThrough]
        public static string EnsureLeadingSlash(this string url)
        {
            if (url != null && !url.StartsWith("/"))
            {
                return "/" + url;
            }

            return url;
        }

        [DebuggerStepThrough]
        public static string EnsureTrailingSlash(this string url)
        {
            if (url != null && !url.EndsWith("/"))
            {
                return url + "/";
            }

            return url;
        }

        [DebuggerStepThrough]
        public static string RemoveLeadingSlash(this string url)
        {
            if (url != null && url.StartsWith("/"))
            {
                url = url.Substring(1);
            }

            return url;
        }

        [DebuggerStepThrough]
        public static string RemoveTrailingSlash(this string url)
        {
            if (url != null && url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return url;
        }

        [DebuggerStepThrough]
        public static string CleanUrlPath(this string url)
        {
            if (String.IsNullOrWhiteSpace(url)) url = "/";

            if (url != "/" && url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return url;
        }

        [DebuggerStepThrough]
        public static bool IsLocalUrl(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Allows "/" or "/foo" but not "//" or "/\".
            if (url[0] == '/')
            {
                // url is exactly "/"
                if (url.Length == 1)
                {
                    return true;
                }

                // url doesn't start with "//" or "/\"
                if (url[1] != '/' && url[1] != '\\')
                {
                    return true;
                }

                return false;
            }

            // Allows "~/" or "~/foo" but not "~//" or "~/\".
            if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            {
                // url is exactly "~/"
                if (url.Length == 2)
                {
                    return true;
                }

                // url doesn't start with "~//" or "~/\"
                if (url[2] != '/' && url[2] != '\\')
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        [DebuggerStepThrough]
        public static string AddQueryString(this string url, string query)
        {
            if (!url.Contains("?"))
            {
                url += "?";
            }
            else if (!url.EndsWith("&"))
            {
                url += "&";
            }

            return url + query;
        }

        [DebuggerStepThrough]
        public static string AddQueryString(this string url, string name, string value)
        {
            return url.AddQueryString(name + "=" + UrlEncoder.Default.Encode(value));
        }

        [DebuggerStepThrough]
        public static string AddHashFragment(this string url, string query)
        {
            if (!url.Contains("#"))
            {
                url += "#";
            }

            return url + query;
        }

        [DebuggerStepThrough]
        public static NameValueCollection ReadQueryStringAsNameValueCollection(this string url)
        {
            if (url != null)
            {
                var idx = url.IndexOf('?');
                if (idx >= 0)
                {
                    url = url.Substring(idx + 1);
                }
                var query = QueryHelpers.ParseNullableQuery(url);
                if (query != null)
                {
                    return query.AsNameValueCollection();
                }
            }

            return new NameValueCollection();           
        }

        public static string GetOrigin(this string url)
        {
            if (url != null)
            {
                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception)
                {
                    return null;
                }

                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    return $"{uri.Scheme}://{uri.Authority}";
                }
            }

            return null;
        }
        
        public static string Obfuscate(this string value)
        {
            var last4Chars = "****";
            if (value.IsPresent() && value.Length > 4)
            {
                last4Chars = value.Substring(value.Length - 4);
            }

            return "****" + last4Chars;
        }
    }
}