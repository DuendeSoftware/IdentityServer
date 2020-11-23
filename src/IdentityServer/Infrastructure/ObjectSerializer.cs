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
        
        public static string ToString(object o)
        {
            return JsonSerializer.Serialize(o, Options);
        }

        public static T FromString<T>(string value)
        {
            return JsonSerializer.Deserialize<T>(value, Options);
        }
    }
}