// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Defines entity/model mapping for persisted grants.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class PersistedGrantMapperProfile:Profile
    {
        /// <summary>
        /// <see cref="PersistedGrantMapperProfile">
        /// </see>
        /// </summary>
        public PersistedGrantMapperProfile()
        {
            CreateMap<Duende.IdentityServer.EntityFramework.Entities.PersistedGrant, Models.PersistedGrant>(MemberList.Destination)
                .ReverseMap();
        }
    }
}
