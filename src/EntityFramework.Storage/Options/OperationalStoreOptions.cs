// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Microsoft.EntityFrameworkCore;

namespace Duende.IdentityServer.EntityFramework.Options
{
    /// <summary>
    /// Options for configuring the operational context.
    /// </summary>
    public class OperationalStoreOptions
    {
        /// <summary>
        /// Callback to configure the EF DbContext.
        /// </summary>
        /// <value>
        /// The configure database context.
        /// </value>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }

        /// <summary>
        /// Callback in DI resolve the EF DbContextOptions. If set, ConfigureDbContext will not be used.
        /// </summary>
        /// <value>
        /// The configure database context.
        /// </value>
        public Action<IServiceProvider, DbContextOptionsBuilder> ResolveDbContextOptions { get; set; }

        /// <summary>
        /// Gets or sets the default schema.
        /// </summary>
        /// <value>
        /// The default schema.
        /// </value>
        public string DefaultSchema { get; set; } = null;

        /// <summary>
        /// Gets or sets the persisted grants table configuration.
        /// </summary>
        /// <value>
        /// The persisted grants.
        /// </value>
        public TableConfiguration PersistedGrants { get; set; } = new TableConfiguration("PersistedGrants");

        /// <summary>
        /// Gets or sets the device flow codes table configuration.
        /// </summary>
        /// <value>
        /// The device flow codes.
        /// </value>
        public TableConfiguration DeviceFlowCodes { get; set; } = new TableConfiguration("DeviceCodes");

        /// <summary>
        /// Gets or sets a value indicating whether stale entries will be automatically cleaned up from the database.
        /// This is implemented by periodically connecting to the database (according to the TokenCleanupInterval) from the hosting application.
        /// Defaults to false.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable token cleanup]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableTokenCleanup { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether consumed tokens will included in the automatic clean up.
        /// </summary>
        /// <value>
        ///   <c>true</c> if consumed tokens are to be included in cleanup; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveConsumedTokens { get; set; } = false;

        /// <summary>
        /// Gets or sets the token cleanup interval (in seconds). The default is 3600 (1 hour).
        /// </summary>
        /// <value>
        /// The token cleanup interval.
        /// </value>
        public int TokenCleanupInterval { get; set; } = 3600;

        /// <summary>
        /// Gets or sets the number of records to remove at a time. Defaults to 100.
        /// </summary>
        /// <value>
        /// The size of the token cleanup batch.
        /// </value>
        public int TokenCleanupBatchSize { get; set; } = 100;
    }
}