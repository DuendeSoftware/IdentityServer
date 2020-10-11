// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Models the request to validate scopes and resource indicators for a client.
    /// </summary>
    public class ResourceValidationRequest
    {
        /// <summary>
        /// The client.
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// The requested scope values.
        /// </summary>
        public IEnumerable<string> Scopes { get; set; }

        // /// <summary>
        // /// The requested resource indicators.
        // /// </summary>
        //  todo: add back when we support resource indicators
        // public IEnumerable<string> ResourceIndicators { get; set; }
    }
}
