// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace Duende.IdentityServer.Stores.Serialization
{
    public class CustomContractResolver: DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => p.Writable).ToList();
        }
    }
}