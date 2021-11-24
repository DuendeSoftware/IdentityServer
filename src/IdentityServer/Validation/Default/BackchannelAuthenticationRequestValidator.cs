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
using Duende.IdentityServer.Services;

namespace Duende.IdentityServer.Validation
{
    internal class BackchannelAuthenticationRequestValidator : IBackchannelAuthenticationRequestValidator
    {
        private readonly IdentityServerOptions _options;
        private readonly IResourceValidator _resourceValidator;
        private readonly ITokenValidator _tokenValidator;
        private readonly IBackchannelAuthenticationUserValidator _backchannelAuthenticationUserValidator;
        private readonly IJwtRequestValidator _jwtRequestValidator;
        private readonly IJwtRequestUriHttpClient _jwtRequestUriHttpClient;
        private readonly ILogger<BackchannelAuthenticationRequestValidator> _logger;

        private ValidatedBackchannelAuthenticationRequest _validatedRequest;

        public BackchannelAuthenticationRequestValidator(
            IdentityServerOptions options,
            IResourceValidator resourceValidator,
            ITokenValidator tokenValidator,
            IBackchannelAuthenticationUserValidator backchannelAuthenticationUserValidator,
            IJwtRequestValidator jwtRequestValidator,
            IJwtRequestUriHttpClient jwtRequestUriHttpClient,
            ILogger<BackchannelAuthenticationRequestValidator> logger)
        {
            _options = options;
            _resourceValidator = resourceValidator;
            _tokenValidator = tokenValidator;
            _backchannelAuthenticationUserValidator = backchannelAuthenticationUserValidator;
            _jwtRequestValidator = jwtRequestValidator;
            _jwtRequestUriHttpClient = jwtRequestUriHttpClient;
            _logger = logger;
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
            if (!clientValidationResult.Client.AllowedGrantTypes.Contains(OidcConstants.GrantTypes.Ciba))
            {
                LogError("Client {clientId} not configured with the CIBA grant type.", clientValidationResult.Client.ClientId);
                return Invalid(BackchannelAuthenticationErrors.UnauthorizedClient, "Unauthorized client");
            }

            _validatedRequest.SetClient(clientValidationResult.Client, clientValidationResult.Secret, clientValidationResult.Confirmation);

            //////////////////////////////////////////////////////////
            // load request object
            //////////////////////////////////////////////////////////
            var roLoadResult = await TryLoadRequestObjectAsync();
            if (!roLoadResult.Success)
            {
                return roLoadResult.ErrorResult;
            }

            //////////////////////////////////////////////////////////
            // validate request object
            //////////////////////////////////////////////////////////
            var roValidationResult = await TryValidateRequestObjectAsync();
            if (!roValidationResult.Success)
            {
                return roValidationResult.ErrorResult;
            }

            if (_validatedRequest.Client.RequireRequestObject &&
                !_validatedRequest.RequestObjectValues.Any())
            {
                LogError("Client is configured for RequireRequestObject but none present");
                return Invalid(BackchannelAuthenticationErrors.InvalidRequest);
            }

            //////////////////////////////////////////////////////////
            // scope must be present
            //////////////////////////////////////////////////////////
            var scope = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.Scope);
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
            var resourceIndicators = _validatedRequest.Raw.GetValues("resource") ?? Enumerable.Empty<string>();

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
            // check requested_expiry
            //////////////////////////////////////////////////////////
            var requestLifetime = _validatedRequest.Client.CibaLifetime ?? _options.Ciba.DefaultLifetime;
            var requestedExpiry = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.RequestedExpiry);
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
            var acrValues = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.AcrValues);
            if (acrValues.IsPresent())
            {
                if (acrValues.Length > _options.InputLengthRestrictions.AcrValues)
                {
                    LogError("Acr values too long");
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid acr_values");
                }

                _validatedRequest.AuthenticationContextReferenceClasses = acrValues.FromSpaceSeparatedString().Distinct().ToList();

                //////////////////////////////////////////////////////////
                // check custom acr_values: idp and tenant
                //////////////////////////////////////////////////////////
                var tenant = _validatedRequest.AuthenticationContextReferenceClasses.FirstOrDefault(x => x.StartsWith(KnownAcrValues.Tenant));
                if (tenant != null)
                {
                    _validatedRequest.AuthenticationContextReferenceClasses.Remove(tenant);
                    tenant = tenant.Substring(KnownAcrValues.Tenant.Length);
                    _validatedRequest.Tenant = tenant;
                }

                var idp = _validatedRequest.AuthenticationContextReferenceClasses.FirstOrDefault(x => x.StartsWith(KnownAcrValues.HomeRealm));
                if (idp != null)
                {
                    _validatedRequest.AuthenticationContextReferenceClasses.Remove(idp);
                    idp = idp.Substring(KnownAcrValues.HomeRealm.Length);

                    // check if idp is present but client does not allow it, and then ignore it
                    if (_validatedRequest.Client.IdentityProviderRestrictions != null && 
                        _validatedRequest.Client.IdentityProviderRestrictions.Any())
                    {
                        if (!_validatedRequest.Client.IdentityProviderRestrictions.Contains(idp))
                        {
                            _logger.LogWarning("idp requested ({idp}) is not in client restriction list.", idp);
                            idp = null;
                        }
                    }

                    _validatedRequest.IdP = idp;
                }
            }


            //////////////////////////////////////////////////////////
            // login hints
            //////////////////////////////////////////////////////////
            var loginHint = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.LoginHint);
            var loginHintToken = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.LoginHintToken);
            var idTokenHint = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.IdTokenHint);

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

                var idTokenHintValidationResult = await _tokenValidator.ValidateIdentityTokenAsync(idTokenHint, _validatedRequest.ClientId, false);
                if (idTokenHintValidationResult.IsError)
                {
                    LogError("id token hint failed to validate: " + idTokenHintValidationResult.Error, idTokenHintValidationResult.ErrorDescription);
                    return Invalid(BackchannelAuthenticationErrors.InvalidRequest, "Invalid id_token_hint");
                }

                _validatedRequest.IdTokenHint = idTokenHint;
                _validatedRequest.IdTokenHintClaims = idTokenHintValidationResult.Claims;
            }

            //////////////////////////////////////////////////////////
            // check user_code
            //////////////////////////////////////////////////////////
            var userCode = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.UserCode);
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
            var bindingMessage = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.BindingMessage);
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
                IdTokenHintClaims = _validatedRequest.IdTokenHintClaims,
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

            // todo: ciba do we call into the profile service at this point for IsActive?

            LogSuccess();
            return new BackchannelAuthenticationRequestValidationResult(_validatedRequest);
        }

        private async Task<(bool Success, BackchannelAuthenticationRequestValidationResult ErrorResult)> TryLoadRequestObjectAsync()
        {
            var jwtRequest = _validatedRequest.Raw.Get(OidcConstants.BackchannelAuthenticationRequest.Request);
            var jwtRequestUri = _validatedRequest.Raw.Get("request_uri"); // todo: ciba constant

            if (jwtRequest.IsPresent() && jwtRequestUri.IsPresent())
            {
                LogError("Both request and request_uri are present");
                return (false, Invalid("Only one request parameter is allowed"));
            }

            if (_options.Endpoints.EnableJwtRequestUri)
            {
                if (jwtRequestUri.IsPresent())
                {
                    // 512 is from the spec
                    if (jwtRequestUri.Length > 512)
                    {
                        LogError("request_uri is too long");
                        return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequestUri, "request_uri is too long"));
                    }

                    var jwt = await _jwtRequestUriHttpClient.GetJwtAsync(jwtRequestUri, _validatedRequest.Client);
                    if (jwt.IsMissing())
                    {
                        LogError("no value returned from request_uri");
                        return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequestUri, "no value returned from request_uri"));
                    }

                    jwtRequest = jwt;
                }
            }
            else if (jwtRequestUri.IsPresent())
            {
                LogError("request_uri present but config prohibits");
                return (false, Invalid(OidcConstants.AuthorizeErrors.RequestUriNotSupported));
            }

            // check length restrictions
            if (jwtRequest.IsPresent())
            {
                if (jwtRequest.Length >= _options.InputLengthRestrictions.Jwt)
                {
                    LogError("request value is too long");
                    return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequestObject, "Invalid request value"));
                }
            }

            _validatedRequest.RequestObject = jwtRequest;
            return (true, null);
        }

        private async Task<(bool Success, BackchannelAuthenticationRequestValidationResult ErrorResult)> TryValidateRequestObjectAsync()
        {
            //////////////////////////////////////////////////////////
            // validate request object
            /////////////////////////////////////////////////////////
            if (_validatedRequest.RequestObject.IsPresent())
            {
                // TODO: ciba client_id not required in JWT it seems
                // TODO: ciba check if the typ claim is diff than authz EP: JwtClaimTypes.JwtTypes.AuthorizationRequest
                
                // validate the request JWT for this client
                var jwtRequestValidationResult = await _jwtRequestValidator.ValidateAsync(_validatedRequest.Client, _validatedRequest.RequestObject);
                if (jwtRequestValidationResult.IsError)
                {
                    LogError("request JWT validation failure");
                    return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequestObject, "Invalid JWT request"));
                }

                // TODO: ciba validate jti in request token

                // validate response_type match
                //var responseType = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.ResponseType);
                //if (responseType != null)
                //{
                //    var payloadResponseType =
                //        jwtRequestValidationResult.Payload.SingleOrDefault(c =>
                //            c.Type == OidcConstants.AuthorizeRequest.ResponseType)?.Value;

                //    if (!string.IsNullOrEmpty(payloadResponseType))
                //    {
                //        if (payloadResponseType != responseType)
                //        {
                //            LogError("response_type in JWT payload does not match response_type in request");
                //            return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequest, "Invalid JWT request"));
                //        }
                //    }
                //}

                // validate client_id mismatch
                var payloadClientId =
                    jwtRequestValidationResult.Payload.SingleOrDefault(c =>
                        c.Type == OidcConstants.AuthorizeRequest.ClientId)?.Value;

                if (!string.IsNullOrEmpty(payloadClientId))
                {
                    if (!string.Equals(_validatedRequest.Client.ClientId, payloadClientId, StringComparison.Ordinal))
                    {
                        LogError("client_id in JWT payload does not match client_id in request");
                        return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequest, "Invalid JWT request"));
                    }
                }
                else
                {
                    LogError("client_id is missing in JWT payload");
                    return (false, Invalid(OidcConstants.AuthorizeErrors.InvalidRequestObject, "Invalid JWT request"));
                }

                // TODO: ciba validate that no request params are in body


                // merge jwt payload values into original request parameters
                // 1. clear the keys in the raw collection for every key found in the request object
                foreach (var claimType in jwtRequestValidationResult.Payload.Select(c => c.Type).Distinct())
                {
                    var qsValue = _validatedRequest.Raw.Get(claimType);
                    if (qsValue != null)
                    {
                        _validatedRequest.Raw.Remove(claimType);
                    }
                }

                // 2. copy over the value
                foreach (var claim in jwtRequestValidationResult.Payload)
                {
                    _validatedRequest.Raw.Add(claim.Type, claim.Value);
                }

                var ruri = _validatedRequest.Raw.Get(OidcConstants.AuthorizeRequest.RequestUri);
                if (ruri != null)
                {
                    _validatedRequest.Raw.Remove(OidcConstants.AuthorizeRequest.RequestUri);
                    _validatedRequest.Raw.Add(OidcConstants.AuthorizeRequest.Request, _validatedRequest.RequestObject);
                }


                _validatedRequest.RequestObjectValues = jwtRequestValidationResult.Payload;
            }

            return (true, null);
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
