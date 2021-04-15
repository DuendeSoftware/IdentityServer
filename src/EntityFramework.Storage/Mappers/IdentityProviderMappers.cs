// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Hosting.DynamicProviders;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Extension methods to map to/from entity/model for identity providers.
    /// </summary>
    public static class IdentityProviderMappers
    {
        static IdentityProviderMappers()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<IdentityProviderMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static OidcProvider ToOidcModel(this OidcIdentityProvider entity)
        {
            return entity == null ? null : Mapper.Map<OidcProvider>(entity);
        }

        /// <summary>
        /// Maps a model to an entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static OidcIdentityProvider ToEntity(this OidcProvider model)
        {
            return model == null ? null : Mapper.Map<OidcIdentityProvider>(model);
        }
    }
}