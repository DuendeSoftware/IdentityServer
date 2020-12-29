// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Logging.Models;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;

namespace Duende.IdentityServer.Validation
{
    internal class TokenRequestValidator : ITokenRequestValidator
    {
        private readonly IdentityServerOptions _options;
        private readonly IIssuerNameService _issuerNameService;
        private readonly IAuthorizationCodeStore _authorizationCodeStore;
        private readonly ExtensionGrantValidator _extensionGrantValidator;
        private readonly ICustomTokenRequestValidator _customRequestValidator;
        private readonly IResourceValidator _resourceValidator;
        private readonly IResourceStore _resourceStore;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEventService _events;
        private readonly IResourceOwnerPasswordValidator _resourceOwnerValidator;
        private readonly IProfileService _profile;
        private readonly IDeviceCodeValidator _deviceCodeValidator;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        private ValidatedTokenRequest _validatedRequest;

        public TokenRequestValidator(
            IdentityServerOptions options,
            IIssuerNameService issuerNameService,
            IAuthorizationCodeStore authorizationCodeStore, 
            IResourceOwnerPasswordValidator resourceOwnerValidator, 
            IProfileService profile, 
            IDeviceCodeValidator deviceCodeValidator, 
            ExtensionGrantValidator extensionGrantValidator, 
            ICustomTokenRequestValidator customRequestValidator,
            IResourceValidator resourceValidator,
            IResourceStore resourceStore,
            IRefreshTokenService refreshTokenService,
            IEventService events, 
            ISystemClock clock, 
            ILogger<TokenRequestValidator> logger)
        {
            _logger = logger;
            _options = options;
            _issuerNameService = issuerNameService;
            _clock = clock;
            _authorizationCodeStore = authorizationCodeStore;
            _resourceOwnerValidator = resourceOwnerValidator;
            _profile = profile;
            _deviceCodeValidator = deviceCodeValidator;
            _extensionGrantValidator = extensionGrantValidator;
            _customRequestValidator = customRequestValidator;
            _resourceValidator = resourceValidator;
            _resourceStore = resourceStore;
            _refreshTokenService = refreshTokenService;
            _events = events;
        }

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="clientValidationResult">The client validation result.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameters
        /// or
        /// client
        /// </exception>
        public async Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
        {
            _logger.LogDebug("Start token request validation");

            _validatedRequest = new ValidatedTokenRequest
            {
                IssuerName = await _issuerNameService.GetCurrentAsync(),
                Raw = parameters ?? throw new ArgumentNullException(nameof(parameters)),
                Options = _options
            };

            if (clientValidationResult == null) throw new ArgumentNullException(nameof(clientValidationResult));

            _validatedRequest.SetClient(clientValidationResult.Client, clientValidationResult.Secret, clientValidationResult.Confirmation);

            /////////////////////////////////////////////
            // check client protocol type
            /////////////////////////////////////////////
            if (_validatedRequest.Client.ProtocolType != IdentityServerConstants.ProtocolTypes.OpenIdConnect)
            {
                LogError("Invalid protocol type for client",
                    new
                    {
                        clientId = _validatedRequest.Client.ClientId,
                        expectedProtocolType = IdentityServerConstants.ProtocolTypes.OpenIdConnect,
                        actualProtocolType = _validatedRequest.Client.ProtocolType
                    });

                return Invalid(OidcConstants.TokenErrors.InvalidClient);
            }

            /////////////////////////////////////////////
            // check grant type
            /////////////////////////////////////////////
            var grantType = parameters.Get(OidcConstants.TokenRequest.GrantType);
            if (grantType.IsMissing())
            {
                LogError("Grant type is missing");
                return Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
            }

            if (grantType.Length > _options.InputLengthRestrictions.GrantType)
            {
                LogError("Grant type is too long");
                return Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
            }

            _validatedRequest.GrantType = grantType;

            //////////////////////////////////////////////////////////
            // check for resource indicator and basic formatting
            //////////////////////////////////////////////////////////
            var resourceIndicators = parameters.GetValues(OidcConstants.TokenRequest.Resource) ?? Enumerable.Empty<string>();

            if (resourceIndicators?.Any(x => x.Length > _options.InputLengthRestrictions.ResourceIndicatorMaxLength) == true)
            {
                return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Resource indicator maximum length exceeded");
            }

            if (!resourceIndicators.AreValidResourceIndicatorFormat(_logger))
            {
                return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Invalid resource indicator format");
            }
            
            if (resourceIndicators.Count() > 1)
            {
                return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Multiple resource indicators not supported on token endpoint.");
            }

            _validatedRequest.RequestedResourceIndicator = resourceIndicators.SingleOrDefault();


            //////////////////////////////////////////////////////////
            // run specific logic for grants
            //////////////////////////////////////////////////////////

            switch (grantType)
            {
                case OidcConstants.GrantTypes.AuthorizationCode:
                    return await RunValidationAsync(ValidateAuthorizationCodeRequestAsync, parameters);
                case OidcConstants.GrantTypes.ClientCredentials:
                    return await RunValidationAsync(ValidateClientCredentialsRequestAsync, parameters);
                case OidcConstants.GrantTypes.Password:
                    return await RunValidationAsync(ValidateResourceOwnerCredentialRequestAsync, parameters);
                case OidcConstants.GrantTypes.RefreshToken:
                    return await RunValidationAsync(ValidateRefreshTokenRequestAsync, parameters);
                case OidcConstants.GrantTypes.DeviceCode:
                    return await RunValidationAsync(ValidateDeviceCodeRequestAsync, parameters);
                default:
                    return await RunValidationAsync(ValidateExtensionGrantRequestAsync, parameters);
            }
        }

        private async Task<TokenRequestValidationResult> RunValidationAsync(Func<NameValueCollection, Task<TokenRequestValidationResult>> validationFunc, NameValueCollection parameters)
        {
            // run standard validation
            var result = await validationFunc(parameters);
            if (result.IsError)
            {
                return result;
            }

            // run custom validation
            _logger.LogTrace("Calling into custom request validator: {type}", _customRequestValidator.GetType().FullName);

            var customValidationContext = new CustomTokenRequestValidationContext { Result = result };
            await _customRequestValidator.ValidateAsync(customValidationContext);

            if (customValidationContext.Result.IsError)
            {
                if (customValidationContext.Result.Error.IsPresent())
                {
                    LogError("Custom token request validator", new { error = customValidationContext.Result.Error });
                }
                else
                {
                    LogError("Custom token request validator error");
                }

                return customValidationContext.Result;
            }

            LogSuccess();

            LicenseValidator.ValidateClient(customValidationContext.Result.ValidatedRequest.ClientId);

            return customValidationContext.Result;
        }

        private async Task<TokenRequestValidationResult> ValidateAuthorizationCodeRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start validation of authorization code token request");

            /////////////////////////////////////////////
            // check if client is authorized for grant type
            /////////////////////////////////////////////
            if (!_validatedRequest.Client.AllowedGrantTypes.ToList().Contains(GrantType.AuthorizationCode) &&
                !_validatedRequest.Client.AllowedGrantTypes.ToList().Contains(GrantType.Hybrid))
            {
                LogError("Client not authorized for code flow");
                return Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
            }

            /////////////////////////////////////////////
            // validate authorization code
            /////////////////////////////////////////////
            var code = parameters.Get(OidcConstants.TokenRequest.Code);
            if (code.IsMissing())
            {
                LogError("Authorization code is missing");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (code.Length > _options.InputLengthRestrictions.AuthorizationCode)
            {
                LogError("Authorization code is too long");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _validatedRequest.AuthorizationCodeHandle = code;

            var authZcode = await _authorizationCodeStore.GetAuthorizationCodeAsync(code);
            if (authZcode == null)
            {
                LogError("Invalid authorization code", new { code });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }
            
            /////////////////////////////////////////////
            // validate client binding
            /////////////////////////////////////////////
            if (authZcode.ClientId != _validatedRequest.Client.ClientId)
            {
                LogError("Client is trying to use a code from a different client", new { clientId = _validatedRequest.Client.ClientId, codeClient = authZcode.ClientId });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            // remove code from store
            // todo: set to consumed in the future?
            await _authorizationCodeStore.RemoveAuthorizationCodeAsync(code);

            if (authZcode.CreationTime.HasExceeded(authZcode.Lifetime, _clock.UtcNow.UtcDateTime))
            {
                LogError("Authorization code expired", new { code });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            /////////////////////////////////////////////
            // populate session id
            /////////////////////////////////////////////
            if (authZcode.SessionId.IsPresent())
            {
                _validatedRequest.SessionId = authZcode.SessionId;
            }

            /////////////////////////////////////////////
            // validate code expiration
            /////////////////////////////////////////////
            if (authZcode.CreationTime.HasExceeded(_validatedRequest.Client.AuthorizationCodeLifetime, _clock.UtcNow.UtcDateTime))
            {
                LogError("Authorization code is expired");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _validatedRequest.AuthorizationCode = authZcode;
            _validatedRequest.Subject = authZcode.Subject;

            /////////////////////////////////////////////
            // validate redirect_uri
            /////////////////////////////////////////////
            var redirectUri = parameters.Get(OidcConstants.TokenRequest.RedirectUri);
            if (redirectUri.IsMissing())
            {
                LogError("Redirect URI is missing");
                return Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
            }

            if (redirectUri.Equals(_validatedRequest.AuthorizationCode.RedirectUri, StringComparison.Ordinal) == false)
            {
                LogError("Invalid redirect_uri", new { redirectUri, expectedRedirectUri = _validatedRequest.AuthorizationCode.RedirectUri });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            /////////////////////////////////////////////
            // validate scopes are present
            /////////////////////////////////////////////
            if (_validatedRequest.AuthorizationCode.RequestedScopes == null ||
                !_validatedRequest.AuthorizationCode.RequestedScopes.Any())
            {
                LogError("Authorization code has no associated scopes");
                return Invalid(OidcConstants.TokenErrors.InvalidRequest);
            }

            //////////////////////////////////////////////////////////
            // resource indicator
            //////////////////////////////////////////////////////////
            if (_validatedRequest.RequestedResourceIndicator != null &&
                _validatedRequest.AuthorizationCode.RequestedResourceIndicators?.Any() == true &&
                !_validatedRequest.AuthorizationCode.RequestedResourceIndicators.Contains(_validatedRequest.RequestedResourceIndicator))
            {
                return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Resource indicator does not match any resource indicator in the original authorize request.");
            }

            //////////////////////////////////////////////////////////
            // resource and scope validation 
            //////////////////////////////////////////////////////////
            var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest { 
                Client = _validatedRequest.Client,
                Scopes = _validatedRequest.AuthorizationCode.RequestedScopes,
                ResourceIndicators = _validatedRequest.AuthorizationCode.RequestedResourceIndicators,
                // if we are issuing a refresh token, then we need to allow the non-isolated resource
                IncludeNonIsolatedApiResources = _validatedRequest.AuthorizationCode.RequestedScopes.Contains(OidcConstants.StandardScopes.OfflineAccess)
            });

            if (!validatedResources.Succeeded)
            {
                if (validatedResources.InvalidResourceIndicators.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Invalid resource indicator.");
                }
                if (validatedResources.InvalidScopes.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope.");
                }
            }

            LicenseValidator.ValidateResourceIndicators(_validatedRequest.RequestedResourceIndicator);
            _validatedRequest.ValidatedResources = validatedResources.FilterByResourceIndicator(_validatedRequest.RequestedResourceIndicator);

            /////////////////////////////////////////////
            // validate PKCE parameters
            /////////////////////////////////////////////
            var codeVerifier = parameters.Get(OidcConstants.TokenRequest.CodeVerifier);
            if (_validatedRequest.Client.RequirePkce || _validatedRequest.AuthorizationCode.CodeChallenge.IsPresent())
            {
                _logger.LogDebug("Client required a proof key for code exchange. Starting PKCE validation");

                var proofKeyResult = ValidateAuthorizationCodeWithProofKeyParameters(codeVerifier, _validatedRequest.AuthorizationCode);
                if (proofKeyResult.IsError)
                {
                    return proofKeyResult;
                }

                _validatedRequest.CodeVerifier = codeVerifier;
            }
            else
            {
                if (codeVerifier.IsPresent())
                {
                    LogError("Unexpected code_verifier: {codeVerifier}. This happens when the client is trying to use PKCE, but it is not enabled. Set RequirePkce to true.", codeVerifier);
                    return Invalid(OidcConstants.TokenErrors.InvalidGrant);
                }
            }

            /////////////////////////////////////////////
            // make sure user is enabled
            /////////////////////////////////////////////
            var isActiveCtx = new IsActiveContext(_validatedRequest.AuthorizationCode.Subject, _validatedRequest.Client, IdentityServerConstants.ProfileIsActiveCallers.AuthorizationCodeValidation);
            await _profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                LogError("User has been disabled", new { subjectId = _validatedRequest.AuthorizationCode.Subject.GetSubjectId() });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _logger.LogDebug("Validation of authorization code token request success");

            return Valid();
        }

        private async Task<TokenRequestValidationResult> ValidateClientCredentialsRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start client credentials token request validation");

            /////////////////////////////////////////////
            // check if client is authorized for grant type
            /////////////////////////////////////////////
            if (!_validatedRequest.Client.AllowedGrantTypes.ToList().Contains(GrantType.ClientCredentials))
            {
                LogError("Client not authorized for client credentials flow, check the AllowedGrantTypes setting", new { clientId = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
            }

            /////////////////////////////////////////////
            // check if client is allowed to request scopes
            /////////////////////////////////////////////
            var scopeError = await ValidateRequestedScopesAndResourcesAsync(parameters, ignoreImplicitIdentityScopes: true, ignoreImplicitOfflineAccess: true);
            if (scopeError != null)
            {
                return Invalid(scopeError);
            }

            if (_validatedRequest.ValidatedResources.Resources.IdentityResources.Any())
            {
                LogError("Client cannot request OpenID scopes in client credentials flow", new { clientId = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.InvalidScope);
            }

            if (_validatedRequest.ValidatedResources.Resources.OfflineAccess)
            {
                LogError("Client cannot request a refresh token in client credentials flow", new { clientId = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.InvalidScope);
            }

            _logger.LogDebug("{clientId} credentials token request validation success", _validatedRequest.Client.ClientId);
            return Valid();
        }

        private async Task<TokenRequestValidationResult> ValidateResourceOwnerCredentialRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start resource owner password token request validation");

            /////////////////////////////////////////////
            // check if client is authorized for grant type
            /////////////////////////////////////////////
            if (!_validatedRequest.Client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword))
            {
                LogError("Client not authorized for resource owner flow, check the AllowedGrantTypes setting", new { client_id = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
            }

            /////////////////////////////////////////////
            // check if client is allowed to request scopes
            /////////////////////////////////////////////
            var scopeError = await ValidateRequestedScopesAndResourcesAsync(parameters);
            if (scopeError != null)
            {
                return Invalid(scopeError);
            }

            /////////////////////////////////////////////
            // check resource owner credentials
            /////////////////////////////////////////////
            var userName = parameters.Get(OidcConstants.TokenRequest.UserName);
            var password = parameters.Get(OidcConstants.TokenRequest.Password);

            if (userName.IsMissing())
            {
                LogError("Username is missing");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (password.IsMissing())
            {
                password = "";
            }

            if (userName.Length > _options.InputLengthRestrictions.UserName ||
                password.Length > _options.InputLengthRestrictions.Password)
            {
                LogError("Username or password too long");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _validatedRequest.UserName = userName;


            /////////////////////////////////////////////
            // authenticate user
            /////////////////////////////////////////////
            var resourceOwnerContext = new ResourceOwnerPasswordValidationContext
            {
                UserName = userName,
                Password = password,
                Request = _validatedRequest
            };
            await _resourceOwnerValidator.ValidateAsync(resourceOwnerContext);

            if (resourceOwnerContext.Result.IsError)
            {
                // protect against bad validator implementations
                resourceOwnerContext.Result.Error ??= OidcConstants.TokenErrors.InvalidGrant;

                if (resourceOwnerContext.Result.Error == OidcConstants.TokenErrors.UnsupportedGrantType)
                {
                    LogError("Resource owner password credential grant type not supported");
                    await RaiseFailedResourceOwnerAuthenticationEventAsync(userName, "password grant type not supported", resourceOwnerContext.Request.Client.ClientId);

                    return Invalid(OidcConstants.TokenErrors.UnsupportedGrantType, customResponse: resourceOwnerContext.Result.CustomResponse);
                }

                var errorDescription = "invalid_username_or_password";

                if (resourceOwnerContext.Result.ErrorDescription.IsPresent())
                {
                    errorDescription = resourceOwnerContext.Result.ErrorDescription;
                }

                LogInformation("User authentication failed: ", errorDescription ?? resourceOwnerContext.Result.Error);
                await RaiseFailedResourceOwnerAuthenticationEventAsync(userName, errorDescription, resourceOwnerContext.Request.Client.ClientId);

                return Invalid(resourceOwnerContext.Result.Error, errorDescription, resourceOwnerContext.Result.CustomResponse);
            }

            if (resourceOwnerContext.Result.Subject == null)
            {
                var error = "User authentication failed: no principal returned";
                LogError(error);
                await RaiseFailedResourceOwnerAuthenticationEventAsync(userName, error, resourceOwnerContext.Request.Client.ClientId);

                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            /////////////////////////////////////////////
            // make sure user is enabled
            /////////////////////////////////////////////
            var isActiveCtx = new IsActiveContext(resourceOwnerContext.Result.Subject, _validatedRequest.Client, IdentityServerConstants.ProfileIsActiveCallers.ResourceOwnerValidation);
            await _profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                LogError("User has been disabled", new { subjectId = resourceOwnerContext.Result.Subject.GetSubjectId() });
                await RaiseFailedResourceOwnerAuthenticationEventAsync(userName, "user is inactive", resourceOwnerContext.Request.Client.ClientId);

                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _validatedRequest.UserName = userName;
            _validatedRequest.Subject = resourceOwnerContext.Result.Subject;

            await RaiseSuccessfulResourceOwnerAuthenticationEventAsync(userName, resourceOwnerContext.Result.Subject.GetSubjectId(), resourceOwnerContext.Request.Client.ClientId);
            _logger.LogDebug("Resource owner password token request validation success.");
            return Valid(resourceOwnerContext.Result.CustomResponse);
        }

        private async Task<TokenRequestValidationResult> ValidateRefreshTokenRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start validation of refresh token request");

            var refreshTokenHandle = parameters.Get(OidcConstants.TokenRequest.RefreshToken);
            if (refreshTokenHandle.IsMissing())
            {
                LogError("Refresh token is missing");
                return Invalid(OidcConstants.TokenErrors.InvalidRequest);
            }

            if (refreshTokenHandle.Length > _options.InputLengthRestrictions.RefreshToken)
            {
                LogError("Refresh token too long");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            var result = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenHandle, _validatedRequest.Client);

            if (result.IsError)
            {
                LogWarning("Refresh token validation failed. aborting");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _validatedRequest.RefreshToken = result.RefreshToken;
            _validatedRequest.RefreshTokenHandle = refreshTokenHandle;
            _validatedRequest.Subject = result.RefreshToken.Subject;

            //////////////////////////////////////////////////////////
            // resource indicator
            //////////////////////////////////////////////////////////
            var resourceIndicators = _validatedRequest.RefreshToken.AuthorizedResourceIndicators;
            if (_validatedRequest.RefreshToken.AuthorizedResourceIndicators != null)
            {
                // we had an authorization request so check current requested resource against original list
                if (_validatedRequest.RequestedResourceIndicator != null &&
                    !_validatedRequest.RefreshToken.AuthorizedResourceIndicators.Contains(_validatedRequest.RequestedResourceIndicator))
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Resource indicator does not match any resource indicator in the original authorize request.");
                }
            }
            else if (!String.IsNullOrWhiteSpace(_validatedRequest.RequestedResourceIndicator))
            {
                resourceIndicators = new[] { _validatedRequest.RequestedResourceIndicator };
            }

            //////////////////////////////////////////////////////////
            // resource and scope validation 
            //////////////////////////////////////////////////////////
            var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = _validatedRequest.Client,
                Scopes = _validatedRequest.RefreshToken.AuthorizedScopes,
                ResourceIndicators = resourceIndicators,
                // we're issuing refresh token, so we need to allow for non-isolated resource
                IncludeNonIsolatedApiResources = true,
            });

            if (!validatedResources.Succeeded)
            {
                if (validatedResources.InvalidResourceIndicators.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Invalid resource indicator.");
                }
                if (validatedResources.InvalidScopes.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope.");
                }
            }

            LicenseValidator.ValidateResourceIndicators(_validatedRequest.RequestedResourceIndicator);
            _validatedRequest.ValidatedResources = validatedResources.FilterByResourceIndicator(_validatedRequest.RequestedResourceIndicator);

            _logger.LogDebug("Validation of refresh token request success");
            // todo: more logging - similar to TokenValidator before
            
            return Valid();
        }

        private async Task<TokenRequestValidationResult> ValidateDeviceCodeRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start validation of device code request");

            /////////////////////////////////////////////
            // resource indicator not supported for device flow
            /////////////////////////////////////////////
            if (_validatedRequest.RequestedResourceIndicator != null)
            {
                LogError("Resource indicators not supported for device flow");
                return Invalid(OidcConstants.TokenErrors.InvalidTarget);
            }

            /////////////////////////////////////////////
            // check if client is authorized for grant type
            /////////////////////////////////////////////
            if (!_validatedRequest.Client.AllowedGrantTypes.ToList().Contains(GrantType.DeviceFlow))
            {
                LogError("Client not authorized for device flow");
                return Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
            }

            /////////////////////////////////////////////
            // validate device code parameter
            /////////////////////////////////////////////
            var deviceCode = parameters.Get(OidcConstants.TokenRequest.DeviceCode);
            if (deviceCode.IsMissing())
            {
                LogError("Device code is missing");
                return Invalid(OidcConstants.TokenErrors.InvalidRequest);
            }

            if (deviceCode.Length > _options.InputLengthRestrictions.DeviceCode)
            {
                LogError("Device code too long");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            /////////////////////////////////////////////
            // validate device code
            /////////////////////////////////////////////
            var deviceCodeContext = new DeviceCodeValidationContext { DeviceCode = deviceCode, Request = _validatedRequest };
            await _deviceCodeValidator.ValidateAsync(deviceCodeContext);

            if (deviceCodeContext.Result.IsError)
            {
                return deviceCodeContext.Result;
            }

            //////////////////////////////////////////////////////////
            // scope validation 
            //////////////////////////////////////////////////////////
            var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = _validatedRequest.Client,
                Scopes = _validatedRequest.DeviceCode.AuthorizedScopes,
                ResourceIndicators = null // not supported for device grant
            });

            if (!validatedResources.Succeeded)
            {
                if (validatedResources.InvalidResourceIndicators.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidTarget, "Invalid resource indicator.");
                }
                if (validatedResources.InvalidScopes.Any())
                {
                    return Invalid(OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope.");
                }
            }

            LicenseValidator.ValidateResourceIndicators(_validatedRequest.RequestedResourceIndicator);
            _validatedRequest.ValidatedResources = validatedResources;

            _logger.LogDebug("Validation of device code token request success");

            return Valid();
        }

        private async Task<TokenRequestValidationResult> ValidateExtensionGrantRequestAsync(NameValueCollection parameters)
        {
            _logger.LogDebug("Start validation of custom grant token request");

            /////////////////////////////////////////////
            // check if client is allowed to use grant type
            /////////////////////////////////////////////
            if (!_validatedRequest.Client.AllowedGrantTypes.Contains(_validatedRequest.GrantType))
            {
                LogError("Client does not have the custom grant type in the allowed list, therefore requested grant is not allowed", new { clientId = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
            }

            /////////////////////////////////////////////
            // check if a validator is registered for the grant type
            /////////////////////////////////////////////
            if (!_extensionGrantValidator.GetAvailableGrantTypes().Contains(_validatedRequest.GrantType, StringComparer.Ordinal))
            {
                LogError("No validator is registered for the grant type", new { grantType = _validatedRequest.GrantType });
                return Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
            }

            /////////////////////////////////////////////
            // check if client is allowed to request scopes
            /////////////////////////////////////////////
            var scopeError = await ValidateRequestedScopesAndResourcesAsync(parameters);
            if (scopeError != null)
            {
                return Invalid(scopeError);
            }

            /////////////////////////////////////////////
            // validate custom grant type
            /////////////////////////////////////////////
            var result = await _extensionGrantValidator.ValidateAsync(_validatedRequest);

            if (result == null)
            {
                LogError("Invalid extension grant");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (result.IsError)
            {
                if (result.Error.IsPresent())
                {
                    LogError("Invalid extension grant", new { error = result.Error });
                    return Invalid(result.Error, result.ErrorDescription, result.CustomResponse);
                }
                else
                {
                    LogError("Invalid extension grant");
                    return Invalid(OidcConstants.TokenErrors.InvalidGrant, customResponse: result.CustomResponse);
                }
            }

            if (result.Subject != null)
            {
                /////////////////////////////////////////////
                // make sure user is enabled
                /////////////////////////////////////////////
                var isActiveCtx = new IsActiveContext(
                    result.Subject,
                    _validatedRequest.Client,
                    IdentityServerConstants.ProfileIsActiveCallers.ExtensionGrantValidation);

                await _profile.IsActiveAsync(isActiveCtx);

                if (isActiveCtx.IsActive == false)
                {
                    // todo: raise event?

                    LogError("User has been disabled", new { subjectId = result.Subject.GetSubjectId() });
                    return Invalid(OidcConstants.TokenErrors.InvalidGrant);
                }

                _validatedRequest.Subject = result.Subject;
            }

            _logger.LogDebug("Validation of extension grant token request success");
            return Valid(result.CustomResponse);
        }

        // todo: do we want to rework the semantics of these ignore params?
        // also seems like other workflows other than CC clients can omit scopes?
        private async Task<string> ValidateRequestedScopesAndResourcesAsync(NameValueCollection parameters, bool ignoreImplicitIdentityScopes = false, bool ignoreImplicitOfflineAccess = false)
        {
            var scopes = parameters.Get(OidcConstants.TokenRequest.Scope);
            if (scopes.IsMissing())
            {
                _logger.LogTrace("Client provided no scopes - checking allowed scopes list");

                if (!_validatedRequest.Client.AllowedScopes.IsNullOrEmpty())
                {
                    // this finds all the scopes the client is allowed to access
                    var clientAllowedScopes = new List<string>();
                    if (!ignoreImplicitIdentityScopes)
                    {
                        var resources = await _resourceStore.FindResourcesByScopeAsync(_validatedRequest.Client.AllowedScopes);
                        clientAllowedScopes.AddRange(resources.ToScopeNames().Where(x => _validatedRequest.Client.AllowedScopes.Contains(x)));
                    }
                    else
                    {
                        var apiScopes = await _resourceStore.FindApiScopesByNameAsync(_validatedRequest.Client.AllowedScopes);
                        clientAllowedScopes.AddRange(apiScopes.Select(x => x.Name));
                    }

                    if (!ignoreImplicitOfflineAccess)
                    {
                        if (_validatedRequest.Client.AllowOfflineAccess)
                        {
                            clientAllowedScopes.Add(IdentityServerConstants.StandardScopes.OfflineAccess);
                        }
                    }

                    scopes = clientAllowedScopes.Distinct().ToSpaceSeparatedString();
                    _logger.LogTrace("Defaulting to: {scopes}", scopes);
                }
                else
                {
                    LogError("No allowed scopes configured for client", new { clientId = _validatedRequest.Client.ClientId });
                    return OidcConstants.TokenErrors.InvalidScope;
                }
            }

            if (scopes.Length > _options.InputLengthRestrictions.Scope)
            {
                LogError("Scope parameter exceeds max allowed length");
                return OidcConstants.TokenErrors.InvalidScope;
            }

            var requestedScopes = scopes.ParseScopesString();

            if (requestedScopes == null)
            {
                LogError("No scopes found in request");
                return OidcConstants.TokenErrors.InvalidScope;
            }


            var resourceIndicators = _validatedRequest.RequestedResourceIndicator == null ? 
                Enumerable.Empty<string>() : 
                new[] { _validatedRequest.RequestedResourceIndicator };

            var resourceValidationResult = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest { 
                Client = _validatedRequest.Client,
                Scopes = requestedScopes,
                ResourceIndicators = resourceIndicators,
                // if the client is passing explicit scopes, we want to exclude the non-isolated resource scenaio from validation
                IncludeNonIsolatedApiResources = parameters.Get(OidcConstants.TokenRequest.Scope).IsMissing()
            });

            if (!resourceValidationResult.Succeeded)
            {
                if (resourceValidationResult.InvalidResourceIndicators.Any())
                {
                    LogError("Invalid resource indicator");
                    return OidcConstants.TokenErrors.InvalidTarget;
                }

                if (resourceValidationResult.InvalidScopes.Any())
                {
                    LogError("Invalid scopes requested");
                }
                else
                {
                    LogError("Invalid scopes for client requested");
                }

                return OidcConstants.TokenErrors.InvalidScope;
            }
            
            _validatedRequest.RequestedScopes = requestedScopes;

            LicenseValidator.ValidateResourceIndicators(_validatedRequest.RequestedResourceIndicator);
            _validatedRequest.ValidatedResources = resourceValidationResult.FilterByResourceIndicator(_validatedRequest.RequestedResourceIndicator);
            
            return null;
        }

        private TokenRequestValidationResult ValidateAuthorizationCodeWithProofKeyParameters(string codeVerifier, AuthorizationCode authZcode)
        {
            if (authZcode.CodeChallenge.IsMissing() || authZcode.CodeChallengeMethod.IsMissing())
            {
                LogError("Client is missing code challenge or code challenge method", new { clientId = _validatedRequest.Client.ClientId });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (codeVerifier.IsMissing())
            {
                LogError("Missing code_verifier");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (codeVerifier.Length < _options.InputLengthRestrictions.CodeVerifierMinLength ||
                codeVerifier.Length > _options.InputLengthRestrictions.CodeVerifierMaxLength)
            {
                LogError("code_verifier is too short or too long");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (Constants.SupportedCodeChallengeMethods.Contains(authZcode.CodeChallengeMethod) == false)
            {
                LogError("Unsupported code challenge method", new { codeChallengeMethod = authZcode.CodeChallengeMethod });
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (ValidateCodeVerifierAgainstCodeChallenge(codeVerifier, authZcode.CodeChallenge, authZcode.CodeChallengeMethod) == false)
            {
                LogError("Transformed code verifier does not match code challenge");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            return Valid();
        }

        private bool ValidateCodeVerifierAgainstCodeChallenge(string codeVerifier, string codeChallenge, string codeChallengeMethod)
        {
            if (codeChallengeMethod == OidcConstants.CodeChallengeMethods.Plain)
            {
                return TimeConstantComparer.IsEqual(codeVerifier.Sha256(), codeChallenge);
            }

            var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hashedBytes = codeVerifierBytes.Sha256();
            var transformedCodeVerifier = Base64Url.Encode(hashedBytes);

            return TimeConstantComparer.IsEqual(transformedCodeVerifier.Sha256(), codeChallenge);
        }

        private TokenRequestValidationResult Valid(Dictionary<string, object> customResponse = null)
        {
            return new TokenRequestValidationResult(_validatedRequest, customResponse);
        }

        private TokenRequestValidationResult Invalid(string error, string errorDescription = null, Dictionary<string, object> customResponse = null)
        {
            return new TokenRequestValidationResult(_validatedRequest, error, errorDescription, customResponse);
        }

        private void LogError(string message = null, object values = null)
        {
            LogWithRequestDetails(LogLevel.Error, message, values);
        }

        private void LogWarning(string message = null, object values = null)
        {
            LogWithRequestDetails(LogLevel.Warning, message, values);
        }

        private void LogInformation(string message = null, object values = null)
        {
            LogWithRequestDetails(LogLevel.Information, message, values);
        }

        private void LogWithRequestDetails(LogLevel logLevel, string message = null, object values = null)
        {
            var details = new TokenRequestValidationLog(_validatedRequest, _options.Logging.TokenRequestSensitiveValuesFilter);

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
            LogWithRequestDetails(LogLevel.Information, "Token request validation success");
        }

        private Task RaiseSuccessfulResourceOwnerAuthenticationEventAsync(string userName, string subjectId, string clientId)
        {
            return _events.RaiseAsync(new UserLoginSuccessEvent(userName, subjectId, null, interactive: false, clientId));
        }

        private Task RaiseFailedResourceOwnerAuthenticationEventAsync(string userName, string error, string clientId)
        {
            return _events.RaiseAsync(new UserLoginFailureEvent(userName, error, interactive: false, clientId: clientId));
        }
    }
}
