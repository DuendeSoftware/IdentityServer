// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Extensions;

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
        if (filter is null) throw new ArgumentNullException(nameof(filter));

        var noFilterValueSet =
            string.IsNullOrWhiteSpace(filter.ClientId) && filter.ClientIds.IsNullOrEmpty() &&
            string.IsNullOrWhiteSpace(filter.SessionId) &&
            string.IsNullOrWhiteSpace(filter.SubjectId) &&
            string.IsNullOrWhiteSpace(filter.Type) && filter.Types.IsNullOrEmpty();
        if (noFilterValueSet)
        {
            throw new ArgumentException("No filter values set.", nameof(filter));
        }
    }
}