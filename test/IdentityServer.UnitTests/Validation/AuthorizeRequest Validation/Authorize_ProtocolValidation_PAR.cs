// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using IdentityModel;
using UnitTests.Common;
using UnitTests.Validation.Setup;
using Xunit;

namespace UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_ProtocolValidation_Valid_PAR
{
    private const string Category = "AuthorizeRequest Protocol Validation - PAR";

    [Fact]
    [Trait("Category", Category)]
    public void par_should_bind_client_to_pushed_request()
    {
        var initiallyPushedClientId = "clientId1";
        var par = new DeserializedPushedAuthorizationRequest
        {
            PushedParameters = new NameValueCollection
            {
                { OidcConstants.AuthorizeRequest.ClientId, initiallyPushedClientId }
            }
        };
        var differentClientInAuthorizeRequest = "notClientId1";
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = differentClientInAuthorizeRequest
        };

        var validator = Factory.CreateRequestObjectValidator();
        var result = validator.ValidatePushedAuthorizationBindingToClient(par, request);

        result.Should().NotBeNull();
        result.IsError.Should().Be(true);
        result.ErrorDescription.Should().Be("invalid client for pushed authorization request");
    }
    
    [Fact]
    [Trait("Category", Category)]
    public void expired_par_requests_should_fail()
    {
        var authorizeRequest = new ValidatedAuthorizeRequest();
        var par = new DeserializedPushedAuthorizationRequest
        {
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1)
        };

        var validator = Factory.CreateRequestObjectValidator();
        var result = validator.ValidatePushedAuthorizationExpiration(par, authorizeRequest);

        result.Should().NotBeNull();
        result.IsError.Should().Be(true);
        result.ErrorDescription.Should().Be("expired pushed authorization request");
    }
}