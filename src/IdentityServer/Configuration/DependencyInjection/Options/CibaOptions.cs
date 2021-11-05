// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// Configures client initiated backchannel authentication
    /// </summary>
    public class CibaOptions
    {
        // TODO: have these as default and add nullable on client

        // TODO: CIBA do we need this here or on the client? 
        ///// <summary>
        ///// Gets or sets the default lifetime of the request.
        ///// </summary>
        //public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(15);
        
        /// <summary>
        /// Gets or sets the polling interval in seconds.
        /// </summary>
        public int PollingInterval { get; set; } = 5;
    }
}