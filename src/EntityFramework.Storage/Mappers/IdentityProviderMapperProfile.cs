// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;
using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Defines entity/model mapping for identity provider.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class IdentityProviderMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="IdentityProviderMapperProfile"/>
        /// </summary>
        public IdentityProviderMapperProfile()
        {
            CreateMap<Duende.IdentityServer.EntityFramework.Entities.IdentityProvider, IdentityProvider>(MemberList.Destination)
                .ForMember(x => x.Properties, opts => opts.ConvertUsing(PropertiesConverter.Converter, x => x.Properties))
                .ReverseMap()
                .ForMember(x => x.Properties, opts => opts.ConvertUsing(PropertiesConverter.Converter, x => x.Properties));
        }
    }

    class PropertiesConverter :
        IValueConverter<Dictionary<string, string>, string>,
        IValueConverter<string, Dictionary<string, string>>
    {
        public static PropertiesConverter Converter = new PropertiesConverter();

        public string Convert(Dictionary<string, string> sourceMember, ResolutionContext context)
        {
            return JsonSerializer.Serialize(sourceMember);
        }

        public Dictionary<string, string> Convert(string sourceMember, ResolutionContext context)
        {
            if (String.IsNullOrWhiteSpace(sourceMember))
            {
                return new Dictionary<string, string>();
            }
            return JsonSerializer.Deserialize<Dictionary<string, string>>(sourceMember);
        }
    }
}
