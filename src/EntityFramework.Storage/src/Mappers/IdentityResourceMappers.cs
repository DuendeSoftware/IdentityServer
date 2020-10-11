// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;
using Duende.IdentityServer.EntityFramework.Entities;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Extension methods to map to/from entity/model for identity resources.
    /// </summary>
    public static class IdentityResourceMappers
    {
        static IdentityResourceMappers()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<IdentityResourceMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static Models.IdentityResource ToModel(this IdentityResource entity)
        {
            return entity == null ? null : Mapper.Map<Models.IdentityResource>(entity);
        }

        /// <summary>
        /// Maps a model to an entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static IdentityResource ToEntity(this Models.IdentityResource model)
        {
            return model == null ? null : Mapper.Map<IdentityResource>(model);
        }
    }
}