// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Represents contextual information about a device flow authorization request.
    /// </summary>
    public class DeviceFlowAuthorizationRequest
    {
        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public Client Client { get; set; }

        /// <summary>
        /// Gets or sets the validated resources.
        /// </summary>
        /// <value>
        /// The scopes requested.
        /// </value>
        public ResourceValidationResult ValidatedResources { get; set; }
    }
}