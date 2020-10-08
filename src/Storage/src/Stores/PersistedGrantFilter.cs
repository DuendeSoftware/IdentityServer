// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Represents a filter used when accessing the persisted grants store. 
    /// Setting multiple properties is interpreted as a logical 'AND' to further filter the query.
    /// At least one value must be supplied.
    /// </summary>
    public class PersistedGrantFilter
    {
        /// <summary>
        /// Subject id of the user.
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// Session id used for the grant.
        /// </summary>
        public string SessionId { get; set; }
        
        /// <summary>
        /// Client id the grant was issued to.
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// The type of grant.
        /// </summary>
        public string Type { get; set; }
    }
}