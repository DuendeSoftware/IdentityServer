// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Specialized;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation;

public class PushedAuthorizationRequestValidationContext
{
    public PushedAuthorizationRequestValidationContext(NameValueCollection requestParameters, Client client)
    {
        RequestParameters = requestParameters;
        Client = client;
    }
    /// <summary>
    /// The request form parameters
    /// </summary>
    public NameValueCollection RequestParameters { get; set; }

    /// <summary>
    /// The validation result of client authentication
    /// </summary>
    public Client Client { get; set; }
}