// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;
using Duende.IdentityServer.EntityFramework.Entities;

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
        public static Models.IdentityProvider ToModel(this IdentityProvider entity)
        {
            return entity == null ? null : Mapper.Map<Models.IdentityProvider>(entity);
        }

        /// <summary>
        /// Maps a model to an entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static Entities.IdentityProvider ToEntity(this Models.IdentityProvider model)
        {
            return model == null ? null : Mapper.Map<IdentityProvider>(model);
        }
    }
}