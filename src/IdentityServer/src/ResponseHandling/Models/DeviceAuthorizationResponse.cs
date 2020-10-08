// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


#pragma warning disable 1591

namespace Duende.IdentityServer.ResponseHandling
{
    public class DeviceAuthorizationResponse
    {
        public string DeviceCode { get; set; }
        public string UserCode { get; set; }
        public string VerificationUri { get; set; }

        public string VerificationUriComplete { get; set; }
        public int DeviceCodeLifetime { get; set; }
        public int Interval { get; set; }
    }
}