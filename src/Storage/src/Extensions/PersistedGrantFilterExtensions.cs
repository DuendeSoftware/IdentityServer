// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Extensions
{
    /// <summary>
    /// Extensions for PersistedGrantFilter.
    /// </summary>
    public static class PersistedGrantFilterExtensions
    {
        /// <summary>
        /// Validates the PersistedGrantFilter and throws if invalid.
        /// </summary>
        /// <param name="filter"></param>
        public static void Validate(this PersistedGrantFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (String.IsNullOrWhiteSpace(filter.ClientId) &&
                String.IsNullOrWhiteSpace(filter.SessionId) &&
                String.IsNullOrWhiteSpace(filter.SubjectId) &&
                String.IsNullOrWhiteSpace(filter.Type))
            {
                throw new ArgumentException("No filter values set.", nameof(filter));
            }
        }
    }
}