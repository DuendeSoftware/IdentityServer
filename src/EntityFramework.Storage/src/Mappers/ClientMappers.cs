// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using AutoMapper;

namespace Duende.IdentityServer.EntityFramework.Mappers
{
    /// <summary>
    /// Extension methods to map to/from entity/model for clients.
    /// </summary>
    public static class ClientMappers
    {
        static ClientMappers()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClientMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static Models.Client ToModel(this Duende.IdentityServer.EntityFramework.Entities.Client entity)
        {
            return Mapper.Map<Models.Client>(entity);
        }

        /// <summary>
        /// Maps a model to an entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static Duende.IdentityServer.EntityFramework.Entities.Client ToEntity(this Models.Client model)
        {
            return Mapper.Map<Duende.IdentityServer.EntityFramework.Entities.Client>(model);
        }
    }
}