// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
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
    public async Task par_should_bind_client_to_pushed_request()
    {
        // Enable PAR
        var options = TestIdentityServerOptions.Create();
        options.Endpoints.EnablePushedAuthorizationEndpoint = true;
        
        // Stub the service that retrieves and deserializes pushed requests
        var service = new TestPushedAuthorizationService();
        // Tell the stub that we initially pushed one client id 
        var initiallyPushedClientId = "clientId1";
        service.PushRequest(initiallyPushedClientId);
      
        // But now call the service with a different client id
        var differentClientInAuthorizeRequest = "notClientId1";
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = differentClientInAuthorizeRequest
        };

        var validator = Factory.CreateRequestObjectValidator(options: options, pushedAuthorizationService: service);
        var result = await validator.ValidatePushedAuthorizationRequest(request);

        result.IsError.Should().Be(true);
        result.ErrorDescription.Should().Be("invalid client for pushed authorization request");
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task expired_par_requests_should_fail()
    {
        // Enable PAR
        var options = TestIdentityServerOptions.Create();
        options.Endpoints.EnablePushedAuthorizationEndpoint = true;
        
        // Stub the service that retrieves and deserializes pushed requests
        var service = new TestPushedAuthorizationService();
        // Tell the stub that we pushed a request that is expired
        var clientId = "clientId1";
        service.PushRequest(clientId, DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
      
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = clientId
        };

        var validator = Factory.CreateRequestObjectValidator(options: options, pushedAuthorizationService: service);
        var result = await validator.ValidatePushedAuthorizationRequest(request);

        result.IsError.Should().Be(true);
        result.ErrorDescription.Should().Be("expired pushed authorization request");
    }
}