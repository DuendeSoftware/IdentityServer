// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Models the data to update a refresh token.
    /// </summary>
    public class RefreshTokenUpdateRequest
    {
        /// <summary>
        /// The handle of the refresh token.
        /// </summary>
        public string Handle { get; set; }

        /// <summary>
        /// The client.
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// The refresh token to update.
        /// </summary>
        public RefreshToken RefreshToken { get; set; }

        /// <summary>
        /// Flag to indicate that the refreth token was modified, and requires an update to the database.
        /// </summary>
        public bool MustUpdate { get; set; }
    }
}