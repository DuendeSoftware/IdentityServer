// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Configuration;

namespace Duende.IdentityServer.Services;

internal class OidcReturnUrlParser : IReturnUrlParser
{
    private readonly IdentityServerOptions _options;
    private readonly IAuthorizeRequestValidator _validator;
    private readonly IUserSession _userSession;
    private readonly IServerUrls _urls;
    private readonly ILogger _logger;
    private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcReturnUrlParser"/> class.
    /// </summary>
    /// <param name="options">Identity Server options.</param>
    /// <param name="validator">The authorized request validator instance.</param>
    /// <param name="userSession">The user session instance.</param>
    /// <param name="urls">The URLs helper.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authorizationParametersMessageStore">The authorization parameters message store.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="validator"/> is null.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="userSession"/> is null.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="urls"/> is null.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="logger"/> is null.</exception>
    public OidcReturnUrlParser(
        IdentityServerOptions options,
        IAuthorizeRequestValidator validator,
        IUserSession userSession,
        IServerUrls urls,
        ILogger<OidcReturnUrlParser> logger,
        IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _urls = urls ?? throw new ArgumentNullException(nameof(urls));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
    {
        using var activity = Tracing.ActivitySource.StartActivity("OidcReturnUrlParser.Parse");

        if (IsValidReturnUrl(returnUrl))
        {
            var parameters = returnUrl.ReadQueryStringAsNameValueCollection();
            if (_authorizationParametersMessageStore != null)
            {
                var messageStoreId = parameters[Constants.AuthorizationParamsStore.MessageStoreIdParameterName];
                var entry = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                parameters = entry?.Data.FromFullDictionary() ?? new NameValueCollection();
            }

            var user = await _userSession.GetUserAsync();
            var result = await _validator.ValidateAsync(parameters, user);
            if (!result.IsError)
            {
                _logger.LogTrace("AuthorizationRequest being returned");
                return new AuthorizationRequest(result.ValidatedRequest);
            }
        }

        _logger.LogTrace("No AuthorizationRequest being returned");
        return null;
    }

    public bool IsValidReturnUrl(string returnUrl)
    {
        using var activity = Tracing.ActivitySource.StartActivity("OidcReturnUrlParser.IsValidReturnUrl");

        if (_options.UserInteraction.AllowOriginInReturnUrl && returnUrl is not null)
        {
            if (!Uri.IsWellFormedUriString(returnUrl, UriKind.RelativeOrAbsolute))
            {
                _logger.LogTrace("returnUrl is not valid");
                return false;
            }

            var host = _urls.Origin;
            if (returnUrl.StartsWith(host, StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = returnUrl.Substring(host.Length);
            }
        }

        if (returnUrl.IsLocalUrl())
        {
            returnUrl = TruncateReturnUrl(
                TruncateReturnUrl(returnUrl, '?'),
                '#');

            if (returnUrl.EndsWith(Constants.ProtocolRoutePaths.Authorize, StringComparison.Ordinal) ||
                returnUrl.EndsWith(Constants.ProtocolRoutePaths.AuthorizeCallback, StringComparison.Ordinal))
            {
                _logger.LogTrace("returnUrl is valid");
                return true;
            }
        }

        _logger.LogTrace("returnUrl is not valid");
        return false;

        static string TruncateReturnUrl(string url, char truncateCharacter)
        {
            var index = url.IndexOf(truncateCharacter);
            return index >= 0 ?
                url.Substring(0, index) :
                url;
        }
    }
}