// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Text.Json;

namespace Duende.IdentityServer
{
    internal static class ObjectSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IgnoreNullValues = true
        };

        private static readonly JsonSerializerOptions OptionsIndented = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            WriteIndented = true
        };
        public static string ToString(object o, bool indented = false)
        {
            return JsonSerializer.Serialize(o, indented? OptionsIndented : Options);
        }

        public static T FromString<T>(string value)
        {
            return JsonSerializer.Deserialize<T>(value, Options);
        }
    }
}