// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Reflection;
using System.Text;
using IdentityModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Duende.IdentityServer.Services.KeyManagement
{
    internal static class KeySerializer
    {
        class InclusiveContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                property.Ignored = false;

                return property;
            }
        }

        static JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                ContractResolver = new InclusiveContractResolver()
            };

        public static string Serialize<T>(T item, bool encode = true)
        {
            var json = JsonConvert.SerializeObject(item, _settings);
            var result = json;
            if (encode)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                result = Base64Url.Encode(bytes);
            }
            return result;
        }

        public static T Deserialize<T>(string data, bool decode = true)
        {
            var json = data;
            if (decode)
            {
                var bytes = Base64Url.Decode(data);
                json = Encoding.UTF8.GetString(bytes);
            }
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}
