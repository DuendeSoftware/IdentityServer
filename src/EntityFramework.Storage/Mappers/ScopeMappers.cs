// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using Duende.IdentityServer.EntityFramework.Entities;

namespace Duende.IdentityServer.EntityFramework.Mappers;

/// <summary>
/// Extension methods to map to/from entity/model for scopes.
/// </summary>
public static class ScopeMappers
{
    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public static Models.ApiScope ToModel(this ApiScope entity)
    {
        return entity == null ? null : 
            new Models.ApiScope
            {
                Enabled = entity.Enabled,
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument,
                UserClaims = entity.UserClaims?.Select(c => c.Type).ToList() ?? new List<string>(),
                Properties = entity.Properties?.ToDictionary(p => p.Key, p => p.Value) ?? new Dictionary<string, string>(),

                Required = entity.Required,
                Emphasize = entity.Emphasize
            };
    }

    /// <summary>
    /// Maps a model to an entity.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns></returns>
    public static Entities.ApiScope ToEntity(this Models.ApiScope model)
    {
        return model == null ? null : 
            new Entities.ApiScope
            {
                Enabled = model.Enabled,
                Name = model.Name,
                DisplayName = model.DisplayName,
                Description = model.Description,
                ShowInDiscoveryDocument = model.ShowInDiscoveryDocument,
                UserClaims = model.UserClaims?.Select(c => new Entities.ApiScopeClaim
                {
                    Type = c,
                }).ToList() ?? new List<ApiScopeClaim>(),
                Properties = model.Properties?.Select(p => new Entities.ApiScopeProperty
                {
                    Key = p.Key, Value = p.Value
                }).ToList() ?? new List<ApiScopeProperty>(),
                
                Required = model.Required,
                Emphasize = model.Emphasize
            };
    }
}
