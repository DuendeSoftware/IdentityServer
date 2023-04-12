// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#nullable enable

using Duende.IdentityServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// Interface to model storage of serialized keys.
/// </summary>
public interface ISigningKeyStore
{
    /// <summary>
    /// Returns all the keys in storage.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SerializedKey>> LoadKeysAsync();

    /// <summary>
    /// Persists new key in storage.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task StoreKeyAsync(SerializedKey key);

    /// <summary>
    /// Deletes key from storage.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteKeyAsync(string id);
}