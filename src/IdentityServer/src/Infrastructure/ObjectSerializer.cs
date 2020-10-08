// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


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