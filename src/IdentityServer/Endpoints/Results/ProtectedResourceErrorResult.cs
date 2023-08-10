// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using IdentityModel;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Models result of a protected resource
/// </summary>
public class ProtectedResourceErrorResult : EndpointResult<ProtectedResourceErrorResult>
{
    /// <summary>
    /// The error
    /// </summary>
    public string Error { get; }
    /// <summary>
    /// The error description
    /// </summary>
    public string ErrorDescription { get; }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="error"></param>
    /// <param name="errorDescription"></param>
    public ProtectedResourceErrorResult(string error, string errorDescription = null)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }
}

internal class ProtectedResourceErrorResultGenerator : IEndpointResultGenerator<ProtectedResourceErrorResult>
{
    public Task ExecuteAsync(ProtectedResourceErrorResult result, HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.SetNoCache();

        var error = result.Error;
        var errorDescription = result.ErrorDescription;

        if (Constants.ProtectedResourceErrorStatusCodes.ContainsKey(error))
        {
            context.Response.StatusCode = Constants.ProtectedResourceErrorStatusCodes[error];
        }

        if (error == OidcConstants.ProtectedResourceErrors.ExpiredToken)
        {
            error = OidcConstants.ProtectedResourceErrors.InvalidToken;
            errorDescription = "The access token expired";
        }

        var errorString = string.Format($"error=\"{error}\"");
        if (errorDescription.IsMissing())
        {
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"IdentityServer\"", errorString }).ToString());
        }
        else
        {
            var errorDescriptionString = string.Format($"error_description=\"{errorDescription}\"");
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"IdentityServer\"", errorString, errorDescriptionString }).ToString());
        }

        return Task.CompletedTask;
    }
}