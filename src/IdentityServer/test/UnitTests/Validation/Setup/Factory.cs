// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IdentityServer.UnitTests.Common;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Services.Default;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace IdentityServer.UnitTests.Validation.Setup
{
    internal static class Factory
    {
        public static IClientStore CreateClientStore()
        {
            return new InMemoryClientStore(TestClients.Get());
        }

        public static TokenRequestValidator CreateTokenRequestValidator(
            IdentityServerOptions options = null,
            IResourceStore resourceStore = null,
            IAuthorizationCodeStore authorizationCodeStore = null,
            IRefreshTokenStore refreshTokenStore = null,
            IResourceOwnerPasswordValidator resourceOwnerValidator = null,
            IProfileService profile = null,
            IDeviceCodeValidator deviceCodeValidator = null,
            IEnumerable<IExtensionGrantValidator> extensionGrantValidators = null,
            ICustomTokenRequestValidator customRequestValidator = null,
            ITokenValidator tokenValidator = null,
            IRefreshTokenService refreshTokenService = null,
            IResourceValidator resourceValidator = null)
        {
            if (options == null)
            {
                options = TestIdentityServerOptions.Create();
            }

            if (resourceStore == null)
            {
                resourceStore = new InMemoryResourcesStore(TestScopes.GetIdentity(), TestScopes.GetApis(), TestScopes.GetScopes());
            }

            if (resourceOwnerValidator == null)
            {
                resourceOwnerValidator = new TestResourceOwnerPasswordValidator();
            }

            if (profile == null)
            {
                profile = new TestProfileService();
            }
            
            if (deviceCodeValidator == null)
            {
                deviceCodeValidator = new TestDeviceCodeValidator();
            }

            if (customRequestValidator == null)
            {
                customRequestValidator = new DefaultCustomTokenRequestValidator();
            }

            ExtensionGrantValidator aggregateExtensionGrantValidator;
            if (extensionGrantValidators == null)
            {
                aggregateExtensionGrantValidator = new ExtensionGrantValidator(new[] { new TestGrantValidator() }, TestLogger.Create<ExtensionGrantValidator>());
            }
            else
            {
                aggregateExtensionGrantValidator = new ExtensionGrantValidator(extensionGrantValidators, TestLogger.Create<ExtensionGrantValidator>());
            }

            if (authorizationCodeStore == null)
            {
                authorizationCodeStore = CreateAuthorizationCodeStore();
            }

            if (refreshTokenStore == null)
            {
                refreshTokenStore = CreateRefreshTokenStore();
            }

            if (resourceValidator == null)
            {
                resourceValidator = CreateResourceValidator(resourceStore);
            }
            
            if (tokenValidator == null)
            {
                tokenValidator = CreateTokenValidator(refreshTokenStore: refreshTokenStore, profile: profile);
            }

            if (refreshTokenService == null)
            {
                refreshTokenService = CreateRefreshTokenService(
                    refreshTokenStore,
                    profile);
            }

            return new TokenRequestValidator(
                options,
                authorizationCodeStore,
                resourceOwnerValidator,
                profile,
                deviceCodeValidator,
                aggregateExtensionGrantValidator,
                customRequestValidator,
                resourceValidator,
                resourceStore,
                tokenValidator,
                refreshTokenService,
                new TestEventService(), 
                new StubClock(), 
                TestLogger.Create<TokenRequestValidator>());
        }

        private static IRefreshTokenService CreateRefreshTokenService(IRefreshTokenStore store, IProfileService profile)
        {
            var service = new DefaultRefreshTokenService(
                store,
                profile,
                new StubClock(),
                TestLogger.Create<DefaultRefreshTokenService>());

            return service;
        }

        internal static IResourceValidator CreateResourceValidator(IResourceStore store = null)
        {
            store = store ?? new InMemoryResourcesStore(TestScopes.GetIdentity(), TestScopes.GetApis(), TestScopes.GetScopes());
            return new DefaultResourceValidator(store, new DefaultScopeParser(TestLogger.Create<DefaultScopeParser>()), TestLogger.Create<DefaultResourceValidator>());
        }

        internal static ITokenCreationService CreateDefaultTokenCreator(IdentityServerOptions options = null)
        {
            return new DefaultTokenCreationService(
                new StubClock(),
                new DefaultKeyMaterialService(new IValidationKeysStore[] { },
                    new ISigningCredentialStore[] { new InMemorySigningCredentialsStore(TestCert.LoadSigningCredentials()) }),
                options ?? TestIdentityServerOptions.Create(),
                TestLogger.Create<DefaultTokenCreationService>());
        }

        public static DeviceAuthorizationRequestValidator CreateDeviceAuthorizationRequestValidator(
            IdentityServerOptions options = null,
            IResourceStore resourceStore = null,
            IResourceValidator resourceValidator = null)
        {
            if (options == null)
            {
                options = TestIdentityServerOptions.Create();
            }
            
            if (resourceStore == null)
            {
                resourceStore = new InMemoryResourcesStore(TestScopes.GetIdentity(), TestScopes.GetApis(), TestScopes.GetScopes());
            }

            if (resourceValidator == null)
            {
                resourceValidator = CreateResourceValidator(resourceStore);
            }


            return new DeviceAuthorizationRequestValidator(
                options,
                resourceValidator,
                TestLogger.Create<DeviceAuthorizationRequestValidator>());
        }

        public static AuthorizeRequestValidator CreateAuthorizeRequestValidator(
            IdentityServerOptions options = null,
            IResourceStore resourceStore = null,
            IClientStore clients = null,
            IProfileService profile = null,
            ICustomAuthorizeRequestValidator customValidator = null,
            IRedirectUriValidator uriValidator = null,
            IResourceValidator resourceValidator = null,
            JwtRequestValidator jwtRequestValidator = null,
            IJwtRequestUriHttpClient jwtRequestUriHttpClient = null)
        {
            if (options == null)
            {
                options = TestIdentityServerOptions.Create();
            }

            if (resourceStore == null)
            {
                resourceStore = new InMemoryResourcesStore(TestScopes.GetIdentity(), TestScopes.GetApis(), TestScopes.GetScopes());
            }

            if (clients == null)
            {
                clients = new InMemoryClientStore(TestClients.Get());
            }

            if (customValidator == null)
            {
                customValidator = new DefaultCustomAuthorizeRequestValidator();
            }

            if (uriValidator == null)
            {
                uriValidator = new StrictRedirectUriValidator();
            }

            if (resourceValidator == null)
            {
                resourceValidator = CreateResourceValidator(resourceStore);
            }

            if (jwtRequestValidator == null)
            {
                jwtRequestValidator = new JwtRequestValidator("https://identityserver", new LoggerFactory().CreateLogger<JwtRequestValidator>());
            }

            if (jwtRequestUriHttpClient == null)
            {
                jwtRequestUriHttpClient = new DefaultJwtRequestUriHttpClient(new HttpClient(new NetworkHandler(new Exception("no jwt request uri response configured"))), options, new LoggerFactory());
            }


            var userSession = new MockUserSession();

            return new AuthorizeRequestValidator(
                options,
                clients,
                customValidator,
                uriValidator,
                resourceValidator,
                userSession,
                jwtRequestValidator,
                jwtRequestUriHttpClient,
                TestLogger.Create<AuthorizeRequestValidator>());
        }

        public static TokenValidator CreateTokenValidator(
            IReferenceTokenStore store = null, 
            IRefreshTokenStore refreshTokenStore = null,
            IProfileService profile = null, 
            IdentityServerOptions options = null, ISystemClock clock = null)
        {
            if (options == null)
            {
                options = TestIdentityServerOptions.Create();
            }

            if (profile == null)
            {
                profile = new TestProfileService();
            }

            if (store == null)
            {
                store = CreateReferenceTokenStore();
            }

            clock = clock ?? new StubClock();

            if (refreshTokenStore == null)
            {
                refreshTokenStore = CreateRefreshTokenStore();
            }

            var clients = CreateClientStore();
            var context = new MockHttpContextAccessor(options);
            var logger = TestLogger.Create<TokenValidator>();

            var keyInfo = new SecurityKeyInfo
            {
                Key = TestCert.LoadSigningCredentials().Key,
                SigningAlgorithm = "RS256"
            };

            var validator = new TokenValidator(
                clients: clients,
                clock: clock,
                profile: profile,
                referenceTokenStore: store,
                refreshTokenStore: refreshTokenStore,
                customValidator: new DefaultCustomTokenValidator(),
                    keys: new DefaultKeyMaterialService(new[] { new InMemoryValidationKeysStore(new[] { keyInfo }) }, Enumerable.Empty<ISigningCredentialStore>()),
                logger: logger,
                options: options,
                context: context);

            return validator;
        }

        public static IDeviceCodeValidator CreateDeviceCodeValidator(
            IDeviceFlowCodeService service,
            IProfileService profile = null,
            IDeviceFlowThrottlingService throttlingService = null,
            ISystemClock clock = null)
        {
            profile = profile ?? new TestProfileService();
            throttlingService = throttlingService ?? new TestDeviceFlowThrottlingService();
            clock = clock ?? new StubClock();
            
            var validator = new DeviceCodeValidator(service, profile, throttlingService, clock, TestLogger.Create<DeviceCodeValidator>());

            return validator;
        }

        public static IClientSecretValidator CreateClientSecretValidator(IClientStore clients = null, SecretParser parser = null, SecretValidator validator = null, IdentityServerOptions options = null)
        {
            options = options ?? TestIdentityServerOptions.Create();

            if (clients == null) clients = new InMemoryClientStore(TestClients.Get());

            if (parser == null)
            {
                var parsers = new List<ISecretParser>
                {
                    new BasicAuthenticationSecretParser(options, TestLogger.Create<BasicAuthenticationSecretParser>()),
                    new PostBodySecretParser(options, TestLogger.Create<PostBodySecretParser>())
                };

                parser = new SecretParser(parsers, TestLogger.Create<SecretParser>());
            }

            if (validator == null)
            {
                var validators = new List<ISecretValidator>
                {
                    new HashedSharedSecretValidator(TestLogger.Create<HashedSharedSecretValidator>()),
                    new PlainTextSharedSecretValidator(TestLogger.Create<PlainTextSharedSecretValidator>())
                };

                validator = new SecretValidator(new StubClock(), validators, TestLogger.Create<SecretValidator>());
            }

            return new ClientSecretValidator(clients, parser, validator, new TestEventService(), TestLogger.Create<ClientSecretValidator>());
        }

        public static IAuthorizationCodeStore CreateAuthorizationCodeStore()
        {
            return new DefaultAuthorizationCodeStore(new InMemoryPersistedGrantStore(),
                new PersistentGrantSerializer(),
                new DefaultHandleGenerationService(),
                TestLogger.Create<DefaultAuthorizationCodeStore>());
        }
        
        public static IRefreshTokenStore CreateRefreshTokenStore()
        {
            return new DefaultRefreshTokenStore(new InMemoryPersistedGrantStore(),
                new PersistentGrantSerializer(),
                new DefaultHandleGenerationService(),
                TestLogger.Create<DefaultRefreshTokenStore>());
        }
        
        public static IReferenceTokenStore CreateReferenceTokenStore()
        {
            return new DefaultReferenceTokenStore(new InMemoryPersistedGrantStore(),
                new PersistentGrantSerializer(),
                new DefaultHandleGenerationService(),
                TestLogger.Create<DefaultReferenceTokenStore>());
        }

        public static IDeviceFlowCodeService CreateDeviceCodeService()
        {
            return new DefaultDeviceFlowCodeService(new InMemoryDeviceFlowStore(), new DefaultHandleGenerationService());
        }
        
        public static IUserConsentStore CreateUserConsentStore()
        {
            return new DefaultUserConsentStore(new InMemoryPersistedGrantStore(),
                new PersistentGrantSerializer(),
                new DefaultHandleGenerationService(),
                TestLogger.Create<DefaultUserConsentStore>());
        }
    }
}