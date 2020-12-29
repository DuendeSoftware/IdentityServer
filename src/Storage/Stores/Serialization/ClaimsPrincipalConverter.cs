// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable 1591

namespace Duende.IdentityServer.Stores.Serialization
{
    public class ClaimsPrincipalConverter : JsonConverter<ClaimsPrincipal>
    {
        public override ClaimsPrincipal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var source = JsonSerializer.Deserialize<ClaimsPrincipalLite>(ref reader, options);
            if (source == null) return null;

            var claims = source.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType));
            var id = new ClaimsIdentity(claims, source.AuthenticationType, JwtClaimTypes.Name, JwtClaimTypes.Role);
            var target = new ClaimsPrincipal(id);
            return target;
        }

        public override void Write(Utf8JsonWriter writer, ClaimsPrincipal value, JsonSerializerOptions options)
        {
            var target = new ClaimsPrincipalLite
            {
                AuthenticationType = value.Identity.AuthenticationType,
                Claims = value.Claims.Select(x =>
                {
                    var cl = new ClaimLite { Type = x.Type, Value = x.Value, ValueType = x.ValueType };
                    if (cl.ValueType == ClaimValueTypes.String)
                    {
                        cl.ValueType = null;
                    }
                    return cl;
                }).ToArray()
            };
            JsonSerializer.Serialize(writer, target, options);
        }
    }
}
