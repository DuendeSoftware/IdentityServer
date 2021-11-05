// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Logging.Models;
using Duende.IdentityServer.Models;
using static Duende.IdentityServer.Constants;

namespace Duende.IdentityServer.Validation
{
    internal class BackchannelAuthenticationRequestValidator : IBackchannelAuthenticationRequestValidator
    {
        private readonly IdentityServerOptions _options;
        private readonly IResourceValidator _resourceValidator;
        private readonly IBackchannelAuthenticationUserValidator _backchannelAuthenticationUserValidator;
        private readonly ILogger _logger;

        private ValidatedBackchannelAuthenticationRequest _validatedRequest;

        public BackchannelAuthenticationRequestValidator(
            IdentityServerOptions options,
            IResourceValidator resourceValidator,
            IBackchannelAuthenticationUserValidator backchannelAuthenticationUserValidator,
            ILogger<TokenRequestValidator> logger)
        {
            _logger = logger;
            _options = options;
            _resourceValidator = resourceValidator;
            _backchannelAuthenticationUserValidator = backchannelAuthenticationUserValidator;
        }

        public async Task<BackchannelAuthenticationRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
        {
            _logger.LogDebug("Start backchannel authentication request validation");

            _validatedRequest = new ValidatedBackchannelAuthenticationRequest
            {
                Raw = parameters ?? throw new ArgumentNullException(nameof(parameters)),
                Options = _options
            };

            if (clientValidationResult == null) throw new ArgumentNullException(nameof(clientValidationResult));

            //////////////////////////////////////////////////////////
            // Client must be configured for CIBA
            //////////////////////////////////////////////////////////
            // TODO: CIBA constant
            if (!clientValidationResult.Client.AllowedGrantTypes.Contains("urn:openid:params:grant-type:ciba"))
            {
                LogError("Client {clientId} not configured with the CIBA grant type.", clientValidationResult.Client.ClientId);
                return Invalid(BackchannelAuthenticationErrors.UnauthorizedClient, "Unauthorized client");
            }

            _validatedRequest.SetClient(clientValidationResult.Client, clientValidationResult.Secret, clientValidationResult.Confirmation);


            //////////////////////////////////////////////////////////
            // scope must be present
            //////////////////////////////////////////////////////////
            var scope = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.Scope);
            if (scope.IsMissing())
            {
                LogError("Missing scope");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Missing scope");
            }

            if (scope.Length > _options.InputLengthRestrictions.Scope)
            {
                LogError("scopes too long.");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid scope");
            }

            _validatedRequest.RequestedScopes = scope.FromSpaceSeparatedString().Distinct().ToList();

            //////////////////////////////////////////////////////////
            // openid scope required
            //////////////////////////////////////////////////////////
            if (!_validatedRequest.RequestedScopes.Contains(IdentityServerConstants.StandardScopes.OpenId))
            {
                LogError("openid scope missing.");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Missing the openid scope");
            }

            //////////////////////////////////////////////////////////
            // check for resource indicators and valid format
            //////////////////////////////////////////////////////////
            var resourceIndicators = _validatedRequest.Raw.GetValues(OidcConstants.TokenRequest.Resource) ?? Enumerable.Empty<string>();

            if (resourceIndicators?.Any(x => x.Length > _options.InputLengthRestrictions.ResourceIndicatorMaxLength) == true)
            {
                return Invalid(BackchannelAuthenticationErrors.InvalidTarget, "Resource indicator maximum length exceeded");
            }

            if (!resourceIndicators.AreValidResourceIndicatorFormat(_logger))
            {
                return Invalid(BackchannelAuthenticationErrors.InvalidTarget, "Invalid resource indicator format");
            }

            _validatedRequest.RequestedResourceIndiators = resourceIndicators?.ToList();

            //////////////////////////////////////////////////////////
            // check if scopes are valid/supported and check for resource scopes
            //////////////////////////////////////////////////////////
            var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = _validatedRequest.Client,
                Scopes = _validatedRequest.RequestedScopes,
                ResourceIndicators = resourceIndicators,
                IncludeNonIsolatedApiResources = _validatedRequest.RequestedScopes.Contains(OidcConstants.StandardScopes.OfflineAccess),
            });

            if (!validatedResources.Succeeded)
            {
                if (validatedResources.InvalidResourceIndicators.Any())
                {
                    return Invalid(BackchannelAuthenticationErrors.InvalidTarget, "Invalid resource indicator");
                }

                if (validatedResources.InvalidScopes.Any())
                {
                    return Invalid(BackchannelAuthenticationErrors.InvalidScope, "Invalid scope");
                }
            }

            LicenseValidator.ValidateResourceIndicators(resourceIndicators);
            _validatedRequest.ValidatedResources = validatedResources;


            //////////////////////////////////////////////////////////
            // login hints
            //////////////////////////////////////////////////////////
            var loginHint = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.LoginHint);
            var loginHintToken = _validatedRequest.Raw.Get("login_hint_token");
            var idTokenHint = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.IdTokenHint);

            var loginHintCount = 0;
            if (loginHint.IsPresent()) loginHintCount++;
            if (loginHintToken.IsPresent()) loginHintCount++;
            if (idTokenHint.IsPresent()) loginHintCount++;

            if (loginHintCount == 0)
            {
                LogError("Missing login_hint_token, id_token_hint, or login_hint");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Missing login_hint_token, id_token_hint, or login_hint");
            }
            else if (loginHintCount > 1)
            {
                LogError("Too many of login_hint_token, id_token_hint, or login_hint");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Too many of login_hint_token, id_token_hint, or login_hint");
            }

            //////////////////////////////////////////////////////////
            // check login_hint
            //////////////////////////////////////////////////////////
            if (loginHint.IsPresent())
            {
                if (loginHint.Length > _options.InputLengthRestrictions.LoginHint)
                {
                    LogError("Login hint too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid login_hint");
                }

                _validatedRequest.LoginHint = loginHint;
            }

            //////////////////////////////////////////////////////////
            // check login_hint_token
            //////////////////////////////////////////////////////////
            if (loginHintToken.IsPresent())
            {
                if (loginHintToken.Length > _options.InputLengthRestrictions.LoginHintToken)
                {
                    LogError("Login hint token too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid login_hint_token");
                }

                _validatedRequest.LoginHintToken = loginHintToken;
            }

            //////////////////////////////////////////////////////////
            // check id_token_hint
            //////////////////////////////////////////////////////////
            if (idTokenHint.IsPresent())
            {
                if (idTokenHint.Length > _options.InputLengthRestrictions.IdTokenHint)
                {
                    LogError("id token hint too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid id_token_hint");
                }

                _validatedRequest.IdTokenHint = idTokenHint;

                // TODO: validate id_token_hint?
            }

            //////////////////////////////////////////////////////////
            // check user_code
            //////////////////////////////////////////////////////////
            var userCode = _validatedRequest.Raw.Get("user_code");
            if (userCode.IsPresent())
            {
                if (userCode.Length > _options.InputLengthRestrictions.UserCode)
                {
                    LogError("user_code too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid user_code");
                }

                _validatedRequest.UserCode = userCode;
            }


            //////////////////////////////////////////////////////////
            // check binding_message
            //////////////////////////////////////////////////////////
            var bindingMessage = _validatedRequest.Raw.Get("binding_message");
            if (bindingMessage.IsPresent())
            {
                if (bindingMessage.Length > _options.InputLengthRestrictions.BindingMessage)
                {
                    LogError("binding_message too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidBindingMessage, "Invalid binding_message");
                }

                _validatedRequest.BindingMessage = bindingMessage;
            }
            
            //////////////////////////////////////////////////////////
            // validate the login hint w/ custom validator
            //////////////////////////////////////////////////////////
            var userResult = await _backchannelAuthenticationUserValidator.ValidateRequestAsync(new BackchannelAuthenticationUserValidatorContext
            {
                IdTokenHint = _validatedRequest.IdTokenHint,
                LoginHint = _validatedRequest.LoginHint,
                LoginHintToken = _validatedRequest.LoginHintToken,
                UserCode = _validatedRequest.UserCode,
                BindingMessage = _validatedRequest.BindingMessage
            });

            if (userResult.IsError)
            {
                if (userResult.Error == BackchannelAuthenticationErrors.AccessDenied)
                {
                    LogError("Request was denied access for that user");
                    return Invalid(BackchannelAuthenticationErrors.AccessDenied, userResult.ErrorDescription);
                }
                if (userResult.Error == BackchannelAuthenticationErrors.ExpiredLoginHintToken)
                {
                    LogError("Expired login_hint_token");
                    return Invalid(BackchannelAuthenticationErrors.ExpiredLoginHintToken, userResult.ErrorDescription ?? "Expired login_hint_token");
                }
                if (userResult.Error == BackchannelAuthenticationErrors.UnknownUserId)
                {
                    LogError("Unknown user id");
                    return Invalid(BackchannelAuthenticationErrors.UnknownUserId, userResult.ErrorDescription);
                }
                if (userResult.Error == BackchannelAuthenticationErrors.MissingUserCode)
                {
                    LogError("Missing user_code");
                    return Invalid(BackchannelAuthenticationErrors.MissingUserCode, userResult.ErrorDescription);
                }
                if (userResult.Error == BackchannelAuthenticationErrors.InvalidUserCode)
                {
                    LogError("Invalid user_code");
                    return Invalid(BackchannelAuthenticationErrors.InvalidUserCode, userResult.ErrorDescription);
                }
                if (userResult.Error == BackchannelAuthenticationErrors.InvalidBindingMessage)
                {
                    LogError("Invalid binding_message");
                    return Invalid(BackchannelAuthenticationErrors.InvalidBindingMessage, userResult.ErrorDescription);
                }

                LogError("Unexpected error from IBackchannelAuthenticationUserValidator: {error}", userResult.Error);
                return Invalid(BackchannelAuthenticationErrors.UnknownUserId);
            }

            if (userResult.IsError || userResult.Subject == null || !userResult.Subject.HasClaim(x => x.Type == JwtClaimTypes.Subject))
            {
                LogError("No subject or subject id returned from IBackchannelAuthenticationUserValidator");
                return Invalid(BackchannelAuthenticationErrors.UnknownUserId);
            }

            if (userResult.Subject == null || !userResult.Subject.HasClaim(x => x.Type == JwtClaimTypes.Subject))
            {
                LogError("No subject or subject id returned from IBackchannelAuthenticationUserValidator");
                return Invalid(BackchannelAuthenticationErrors.UnknownUserId);
            }

            _validatedRequest.Subject = userResult.Subject;


            //////////////////////////////////////////////////////////
            // check user_code
            //////////////////////////////////////////////////////////
            var requestLifetime = _validatedRequest.Client.CibaLifetime ?? _options.Ciba.DefaultLifetime;
            var requestedExpiry = _validatedRequest.Raw.Get("requested_expiry");
            if (requestedExpiry.IsPresent())
            {
                // Int32.MaxValue == 2147483647, which is 10 characters in length
                // so using 9 so we don't overflow below on the Int32.Parse
                if (requestedExpiry.Length > 9)
                {
                    LogError("requested_expiry too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid requested_expiry");
                }

                if (Int32.TryParse(requestedExpiry, out var expiryValue) && 
                    expiryValue > 0 &&
                    expiryValue <= requestLifetime)
                {
                    _validatedRequest.Expiry = expiryValue;
                }
                else
                {
                    LogError("requested_expiry value out of valid range");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid requested_expiry");
                }
            }
            else
            {
                _validatedRequest.Expiry = requestLifetime;
            }

            //////////////////////////////////////////////////////////
            // check acr_values
            //////////////////////////////////////////////////////////
            var acrValues = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.AcrValues);
            if (acrValues.IsPresent())
            {
                if (acrValues.Length > _options.InputLengthRestrictions.AcrValues)
                {
                    LogError("Acr values too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid acr_values");
                }

                _validatedRequest.AuthenticationContextReferenceClasses = acrValues.FromSpaceSeparatedString().Distinct().ToList();
            }

            // todo: support idp and tenant?
            //////////////////////////////////////////////////////////
            // check custom acr_values: idp
            //////////////////////////////////////////////////////////
            //var idp = _validatedRequest.GetIdP();
            //if (idp.IsPresent())
            //{
            //    // if idp is present but client does not allow it, strip it from the request message
            //    if (request.Client.IdentityProviderRestrictions != null && request.Client.IdentityProviderRestrictions.Any())
            //    {
            //        if (!request.Client.IdentityProviderRestrictions.Contains(idp))
            //        {
            //            _logger.LogWarning("idp requested ({idp}) is not in client restriction list.", idp);
            //            request.RemoveIdP();
            //        }
            //    }
            //}

            LogSuccess();
            return new BackchannelAuthenticationRequestValidationResult(_validatedRequest);
        }

        private BackchannelAuthenticationRequestValidationResult Invalid(string error, string errorDescription = null)
        {
            return new BackchannelAuthenticationRequestValidationResult(_validatedRequest, error, errorDescription);
        }

        private void LogError(string message = null, object values = null)
        {
            LogWithRequestDetails(LogLevel.Error, message, values);
        }

        private void LogWithRequestDetails(LogLevel logLevel, string message = null, object values = null)
        {
            var details = new BackchannelAuthenticationRequestValidationLog(_validatedRequest, _options.Logging.TokenRequestSensitiveValuesFilter);

            if (message.IsPresent())
            {
                try
                {
                    if (values == null)
                    {
                        _logger.Log(logLevel, message + ", {@details}", details);
                    }
                    else
                    {
                        _logger.Log(logLevel, message + "{@values}, details: {@details}", values, details);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError("Error logging {exception}, request details: {@details}", ex.Message, details);
                }
            }
            else
            {
                _logger.Log(logLevel, "{@details}", details);
            }
        }

        private void LogSuccess()
        {
            LogWithRequestDetails(LogLevel.Information, "Backchannel authentication request validation success");
        }
    }
}
