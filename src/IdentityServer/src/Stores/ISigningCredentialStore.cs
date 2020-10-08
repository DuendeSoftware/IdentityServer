// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for a signing credential store
    /// </summary>
    public interface ISigningCredentialStore
    {
        /// <summary>
        /// Gets the signing credentials.
        /// </summary>
        /// <returns></returns>
        Task<SigningCredentials> GetSigningCredentialsAsync();
    }
}