// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;
using Duende.IdentityServer.Hosting.DynamicProviders;

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
            CreateMap<Duende.IdentityServer.EntityFramework.Entities.IdentityProvider, OidcProvider>(MemberList.Destination)
                .ReverseMap();
        }
    }
}
