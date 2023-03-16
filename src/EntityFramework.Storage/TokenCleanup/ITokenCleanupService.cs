// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.EntityFramework;

/// <summary>
/// Service that cleans up stale persisted grants and device codes.
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// Removes expired persisted grants, expired device codes, and optionally
    /// consumed persisted grants from the stores.
    /// </summary>
    /// <param name="cancellationToken">A token that propagates notification
    /// that the cleanup operation should be canceled.</param>
    /// <returns></returns>
    Task RemoveExpiredGrantsAsync(CancellationToken cancellationToken = default);
}
