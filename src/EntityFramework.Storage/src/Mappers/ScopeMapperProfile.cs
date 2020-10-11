// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using AutoMapper;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Defines entity/model mapping for scopes.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class ScopeMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="ScopeMapperProfile"/>
        /// </summary>
        public ScopeMapperProfile()
        {
            CreateMap<Duende.IdentityServer.EntityFramework.Entities.ApiScopeProperty, KeyValuePair<string, string>>()
                .ReverseMap();

            CreateMap<Duende.IdentityServer.EntityFramework.Entities.ApiScopeClaim, string>()
               .ConstructUsing(x => x.Type)
               .ReverseMap()
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src));

            CreateMap<Duende.IdentityServer.EntityFramework.Entities.ApiScope, Models.ApiScope>(MemberList.Destination)
                .ConstructUsing(src => new Models.ApiScope())
                .ForMember(x => x.Properties, opts => opts.MapFrom(x => x.Properties))
                .ForMember(x => x.UserClaims, opts => opts.MapFrom(x => x.UserClaims))
                .ReverseMap();
        }
    }
}
