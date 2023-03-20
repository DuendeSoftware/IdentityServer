// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Models;
using System.Linq;
using Duende.IdentityServer.Configuration;
using IdentityModel;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// Service to processes backchannel logout requests for upstream IdPs
/// </summary>
public class BackChannelRequestProcessor
{
    private readonly IBackchannelLogoutRequestValidator _backchannelLogoutRequestValidator;
    private readonly IServerSideTicketStore _serverSideTicketStore;
    private readonly ISessionCoordinationService _sessionCoordinationService;
    private readonly ILogger _logger;

    /// <summary>
    /// ctor
    /// </summary>
    public BackChannelRequestProcessor(IBackchannelLogoutRequestValidator backchannelLogoutRequestValidator, IServerSideTicketStore serverSideTicketStore, ISessionCoordinationService sessionCoordinationService, ILogger<BackChannelRequestProcessor> logger)
    {
        _backchannelLogoutRequestValidator = backchannelLogoutRequestValidator;
        _serverSideTicketStore = serverSideTicketStore;
        _sessionCoordinationService = sessionCoordinationService;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <returns></returns>
    public async Task ProcessAsync(string scheme, HttpContext context)
    {
        _logger.LogDebug("Processing back-channel logout request for scheme {scheme}", scheme);

        context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
        context.Response.Headers.Add("Pragma", "no-cache");

        try
        {
            if (context.Request.HasFormContentType)
            {
                var logoutToken = context.Request.Form[OidcConstants.BackChannelLogoutRequest.LogoutToken].FirstOrDefault();

                if (!String.IsNullOrWhiteSpace(logoutToken))
                {
                    var result = await _backchannelLogoutRequestValidator.ValidateAsync(new BackchannelLogoutRequest
                    {
                        Scheme = scheme,
                        LogoutToken = logoutToken
                    });

                    if (!result.IsError)
                    {

                        //await _sessionCoordinationService.ProcessLogoutAsync(new UserSession
                        //{

                        //}
                        //{ 
                        //    SubjectId = result.SubjectId,
                        //    SessionId = result.SessionId,
                        //});

                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = OidcConstants.TokenErrors.InvalidRequest, error_description = result.ErrorDescription });
                    }
                    
                    return;
                }
                else
                {
                    _logger.LogDebug($"Failed to process backchannel logout request. 'Logout token is missing'");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Failed to process backchannel logout request. '{ex.Message}'");
        }

        _logger.LogDebug($"Failed to process backchannel logout request.");
        context.Response.StatusCode = 400;
    }
}

