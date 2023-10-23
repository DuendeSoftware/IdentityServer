// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System;
using System.Collections.Specialized;

namespace Duende.IdentityServer.Services;

public class DeserializedPushedAuthorizationRequest
{
    public string ReferenceValue { get; set; }
    public NameValueCollection PushedParameters { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
