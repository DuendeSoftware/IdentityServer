// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml.Schema;

namespace Duende.IdentityServer;

/// <summary>
/// Constants for Telemetry
/// </summary>
public static class Telemetry
{
    private readonly static string ServiceVersion = typeof(Telemetry).Assembly.GetName().Version.ToString();
    
    /// <summary>
    /// Service name used for Duende IdentityServer.
    /// </summary>
    public const string ServiceName = "Duende.IdentityServer";

    /// <summary>
    /// Metrics configuration.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Meter for IdentityServer
        /// </summary>
        public static readonly Meter Meter = new Meter(ServiceName, ServiceVersion);

        /// <summary>
        /// Counter for active requests.
        /// </summary>
        public static readonly UpDownCounter<long> ActiveRequestCounter = Meter.CreateUpDownCounter<long>("active_requests");

        /// <summary>
        /// Number of successful operations. Probably most useful together with <see cref="FailureCounter"/>.
        /// </summary>
        public static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>("success");

        /// <summary>
        /// Helper method to increase the <see cref="SuccessCounter"/>
        /// </summary>
        /// <param name="clientId">Client involved in event</param>
        public static void Success(string clientId = null)
        {
            if(clientId != null)
            {
                SuccessCounter.Add(1, tag: new("client", clientId));
            }
            else
            {
                SuccessCounter.Add(1);
            }
        }
        
        /// <summary>
        /// Number of failed operations. Probably most useful together with <see cref="SuccessCounter"/>.
        /// </summary>
        public static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>("failure");

        /// <summary>
        /// Helper method to increase the <see cref="FailureCounter"/>
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="clientId">Client involved in event</param>
        public static void Failure(string error, string clientId = null)
        {
            if(clientId != null)
            {
                FailureCounter.Add(1, new("client", clientId), new("error", error));
            }
            else
            {
                FailureCounter.Add(1, tag: new("error", error));
            }
        }
        
        /// <summary>
        /// Token issuance failed counter.
        /// </summary>
        public static readonly Counter<long> TokenIssuedFailureCounter = Meter.CreateCounter<long>("token_issued_failure");

        /// <summary>
        /// Helper method to increase <see cref="TokenIssuedFailureCounter"/> with labels
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="grantType">Grant Type</param>
        /// <param name="error">Error</param>
        public static void TokenIssuedFailure(string clientId, string grantType, string error)
        {
            Failure(error, clientId);
            TokenIssuedFailureCounter.Add(1, new("client", clientId), new ("grant_type", grantType), new("error", error));
        }

        /// <summary>
        /// Successful token issuance counter.
        /// </summary>
        public static readonly Counter<long> TokenIssuedCounter = Meter.CreateCounter<long>("token_issued");

        /// <summary>
        /// Helper method to increase <see cref="TokenIssuedCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="grantType">Grant Type</param>
        public static void TokenIssued(string clientId, string grantType)
        {
            Success(clientId);
            TokenIssuedCounter.Add(1, new("client", clientId), new("grant_type", grantType));
        }

        /// <summary>
        /// Failed back channel (CIBA) authentications counter
        /// </summary>
        public static readonly Counter<long> BackChannelAuthenticationFailureCounter =
            Meter.CreateCounter<long>("backchannel_authentication_failure");

        /// <summary>
        /// Helper method to increase <see cref="BackChannelAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="error"></param>
        public static void BackChannelAuthenticationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            BackChannelAuthenticationFailureCounter
                .Add(1, new("client", clientId), new("error", error));
        }

        /// <summary>
        /// Successful back channel (CIBA) authentications counter
        /// </summary>
        public static readonly Counter<long> BackChannelAuthenticationCounter =
            Meter.CreateCounter<long>("backchannel_authentication");

        /// <summary>
        /// Helper method to increase <see cref="BackChannelAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        public static void BackChannelAuthentication(string clientId)
        {
            Success(clientId);
            BackChannelAuthenticationCounter.Add(1, tag: new("client", clientId));
        }

        /// <summary>
        /// Failed device code authentication counter
        /// </summary>
        public static readonly Counter<long> DeviceAuthenticationFailureCounter =
            Meter.CreateCounter<long>("device_authentication_failure");

        /// <summary>
        /// Helper method to increase <see cref="DeviceAuthenticationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="error">Error</param>
        public static void DeviceAuthenticationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            DeviceAuthenticationFailureCounter.Add(1, new("client", clientId), new("error", error));
        }

        /// <summary>
        /// Successful device code authentication counter
        /// </summary>
        public static readonly Counter<long> DeviceAuthenticationCounter =
            Meter.CreateCounter<long>("device_authentication");

        /// <summary>
        /// Helper method to increase <see cref="DeviceAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId">Client ID</param>
        public static void DeviceAuthentication(string clientId)
        {
            Success(clientId);
            DeviceAuthenticationCounter.Add(1, tag: new("client", clientId));
        }

        /// <summary>
        /// Introspection failure counter
        /// </summary>
        public static readonly Counter<long> IntrospectionFailureCounter =
            Meter.CreateCounter<long>("introspection_failure");

        /// <summary>
        /// Helper method to increase <see cref="IntrospectionFailureCounter"/>
        /// </summary>
        /// <param name="callerId">Api resource or client Id</param>
        /// <param name="error">Error message</param>
        public static void IntrospectionFailure(string callerId, string error)
        {
            Failure(error, callerId);
            IntrospectionFailureCounter.Add(1, new("caller", callerId), new("error", error));
        }

        /// <summary>
        /// Introspection success counter
        /// </summary>
        public static readonly Counter<long> IntrospectionCounter =
            Meter.CreateCounter<long>("introspection");

        /// <summary>
        /// Helper method to increase <see cref="IntrospectionCounter"/>
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="active">Is the token valid/active?</param>
        public static void Introspection(string callerId, bool active)
        {
            Success(callerId);
            IntrospectionCounter.Add(1, new("caller", callerId), new("active", active));
        }

        /// <summary>
        /// Revocation failure counter
        /// </summary>
        public static readonly Counter<long> RevocationFailureCounter =
            Meter.CreateCounter<long>("revocation_failure");

        /// <summary>
        /// Helper method to increase <see cref="RevocationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="error">Error</param>
        public static void RevocationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            RevocationFailureCounter.Add(1, new("client", clientId), new("error", error));
        }

        /// <summary>
        /// Revocation success counter.
        /// </summary>
        public static readonly Counter<long> RevocationCounter =
            Meter.CreateCounter<long>("revocation");

        /// <summary>
        /// Helper method to increase <see cref="RevocationCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        public static void Revocation(string clientId)
        {
            Success(clientId);
            RevocationCounter.Add(1, tag: new("client", clientId));
        }

        /// <summary>
        /// Dynamic identity provider validation failures.
        /// </summary>
        public static Counter<long> DynamicIdentityProviderValidationFailureCounter
            = Meter.CreateCounter<long>("dynamic_identityprovider_validation_failure");

        /// <summary>
        /// Helper method to increase <see cref="DynamicIdentityProviderValidationFailureCounter"/>
        /// </summary>
        /// <param name="scheme">Scheme name</param>
        /// <param name="error">Error message</param>
        public static void DynamicIdentityProviderValidationFailure(string scheme, string error)
        {
            Failure(error);
            DynamicIdentityProviderValidationFailureCounter
                .Add(1, new("scheme", scheme), new("error", error));
        }

        /// <summary>
        /// Dynamic identityprovider validations
        /// </summary>
        public static Counter<long> DynamicIdentityProviderValidationCounter
            = Meter.CreateCounter<long>("dynamic_identityprovider_validation");

        /// <summary>
        /// Helper method to increase <see cref="DynamicIdentityProviderValidationCounter"/>
        /// </summary>
        /// <param name="scheme"></param>
        public static void DynamicIdentityProviderValidation(string scheme)
        {
            Success();
            DynamicIdentityProviderValidationCounter.Add(1, tag: new("scheme", scheme));
        }

        /// <summary>
        /// Unhandled exceptions bubbling up to the middleware.
        /// </summary>
        public static Counter<long> UnHandledExceptionCounter
            = Meter.CreateCounter<long>("unhandled_exception");

        /// <summary>
        /// Helper method to increase <see cref="UnHandledExceptionCounter"/>
        /// </summary>
        /// <param name="ex">Exception</param>
        public static void UnHandledException(Exception ex)
            => UnHandledExceptionCounter.Add(1, new("type", ex.GetType().Name), new("method", ex.TargetSite.Name));

        /// <summary>
        /// Client configuration validation failure.
        /// </summary>
        public static Counter<long> ClientValidationFailureCounter
            = Meter.CreateCounter<long>("client_validation_failure");

        /// <summary>
        /// Helper method to increase <see cref="ClientValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="error">Error</param>
        public static void ClientValidationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            ClientValidationFailureCounter.Add(1, new("client", clientId), new("error", error));
        }

        /// <summary>
        /// Client configuration validation success
        /// </summary>
        public static Counter<long> ClientValidationCounter
            = Meter.CreateCounter<long>("client_validation");

        /// <summary>
        /// Helper method to increase <see cref="ClientValidationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        public static void ClientValidation(string clientId)
        {
            Success(clientId);
            ClientValidationCounter.Add(1, tag: new("client", clientId));
        }

        /// <summary>
        /// Failed Api Secret validations
        /// </summary>
        public static Counter<long> ApiSecretValidationFailureCounter
            = Meter.CreateCounter<long>("apisecret_validation_failure");

        /// <summary>
        /// Helper method to increase <see cref="ApiSecretValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ApiSecretValidationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ApiSecretValidationFailureCounter.Add(1, new("client", clientId), new("error", message));
        }

        /// <summary>
        /// Successful Api Secret validations
        /// </summary>
        public static Counter<long> ApiSecretValidationCounter
            = Meter.CreateCounter<long>("apisecret_validation");

        /// <summary>
        /// Helper method to increase <see cref="ApiSecretValidationCounter"/>
        /// </summary>
        /// <param name="apiId">Api Id</param>
        /// <param name="authMethod">Authentication Method</param>
        public static void ApiSecretValidation(string apiId, string authMethod)
        {
            Success(apiId);
            ApiSecretValidationCounter.Add(1, new("api", apiId),new("auth_method", authMethod));
        }

        /// <summary>
        /// Failed Client Secret validations
        /// </summary>
        public static Counter<long> ClientSecretValidationFailureCounter
            = Meter.CreateCounter<long>("clientsecret_validation_failure");

        /// <summary>
        /// Helper method to increase <see cref="ClientSecretValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ClientSecretValidationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ClientSecretValidationFailureCounter.Add(1, new("client", clientId), new("error", message));
        }

        /// <summary>
        /// Successful Client Secret validations
        /// </summary>
        public static Counter<long> ClientSecretValidationCounter
            = Meter.CreateCounter<long>("clientsecret_validation");

        /// <summary>
        /// Helper method to increase <see cref="ClientSecretValidationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="authMethod"></param>
        public static void ClientSecretValidation(string clientId, string authMethod)
        {
            Success(clientId);
            ClientSecretValidationCounter.Add(1, new("client", clientId), new("auth_method", authMethod));
        }

        /// <summary>
        /// Failed Resource Owner Authentication
        /// </summary>
        public static Counter<long> ResourceOwnerAuthenticationFailureCounter
            = Meter.CreateCounter<long>("resourceowner_authentication_failure");

        /// <summary>
        /// Helper method to increase <see cref="ResourceOwnerAuthenticationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ResourceOwnerAuthenticationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ResourceOwnerAuthenticationFailureCounter.Add(1, new("client", clientId), new("error", message));
        }

        /// <summary>
        /// Successful Api Secret validations
        /// </summary>
        public static Counter<long> ResourceOwnerAuthenticationCounter
            = Meter.CreateCounter<long>("resourceowner_authentication");

        /// <summary>
        /// Helper method to increase <see cref="ResourceOwnerAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="authMethod"></param>
        public static void ResourceOwnerAuthentication(string clientId)
        {
            Success(clientId);
            ResourceOwnerAuthenticationCounter.Add(1, tag: new("client", clientId));
        }

    }
}