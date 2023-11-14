// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Validation;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

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
        /// Name of counters.
        /// </summary>
        public static class Counters
        {
            /// <summary>
            /// failure
            /// </summary>
            public const string Failure = "failure";

            /// <summary>
            /// success
            /// </summary>
            public const string Success = "success";

            /// <summary>
            /// active_requests
            /// </summary>
            public const string ActiveRequests = "active_requests";

            /// <summary>
            /// apisecret_validation
            /// </summary>
            public const string ApiSecretValidation = "apisecret_validation";

            /// <summary>
            /// pisecret_validation_failure
            /// </summary>
            public const string ApiSecretValidationFailure = "apisecret_validation_failure";

            /// <summary>
            /// backchannel_authentication
            /// </summary>
            public const string BackchannelAuthentication = "backchannel_authentication";

            /// <summary>
            /// backchannel_authentication_failure
            /// </summary>
            public const string BackchannelAuthenticationFailure = "backchannel_authentication_failure";

            /// <summary>
            /// client_validation_failure
            /// </summary>
            public const string ClientValidationFailure = "client_validation_failure";

            /// <summary>
            /// client_validation
            /// </summary>
            public const string ClientValidation = "client_validation";

            /// <summary>
            /// clientsecret_validation
            /// </summary>
            public const string ClientSecretValidation = "clientsecret_validation";

            /// <summary>
            /// clientsecret_validation_failure
            /// </summary>
            public const string ClientSecretValidationFailure = "clientsecret_validation_failure";

            /// <summary>
            /// device_authentication_failure
            /// </summary>
            public const string DeviceAuthentication = "device_authentication";

            /// <summary>
            /// device_authentication_failure
            /// </summary>
            public const string DeviceAuthenticationFailure = "device_authentication_failure";

            /// <summary>
            /// dynamic_identityprovider_validation
            /// </summary>
            public const string DynamicIdentityProviderValidation = "dynamic_identityprovider_validation";

            /// <summary>
            /// dynamic_identityprovider_validation_failure
            /// </summary>
            public const string DynamicIdentityProviderValidationFailure = "dynamic_identityprovider_validation_failure";

            /// <summary>
            /// introspection
            /// </summary>
            public const string Introspection = "introspection";

            /// <summary>
            /// introspection_failure
            /// </summary>
            public const string IntrospectionFailure = "introspection_failure";

            /// <summary>
            /// pushed_authorization_request
            /// </summary>
            public const string PushedAuthorizationRequest = "pushed_authorization_request";

            /// <summary>
            /// pushed_authorization_request_failure
            /// </summary>
            public const string PushedAuthorizationRequestFailure = "pushed_authorization_request_failure";

            /// <summary>
            /// resourceowner_authentication
            /// </summary>
            public const string ResourceOwnerAuthentication = "resourceowner_authentication";

            /// <summary>
            /// resourceowner_authentication_failure
            /// </summary>
            public const string ResourceOwnerAuthenticationFailure = "resourceowner_authentication_failure";

            /// <summary>
            /// revocation
            /// </summary>
            public const string Revocation = "revocation";

            /// <summary>
            /// revocation_failure
            /// </summary>
            public const string RevocationFailure = "revocation_failure";

            /// <summary>
            /// token_issued
            /// </summary>
            public const string TokenIssued = "token_issued";

            /// <summary>
            /// token_issued_failure
            /// </summary>
            public const string TokenIssuedFailure = "token_issued_failure";

            /// <summary>
            /// unhandled_exception
            /// </summary>
            public const string UnHandledException = "unhandled_exception";
        }

        /// <summary>
        /// Name of tags
        /// </summary>
        public static class Tags
        {
            /// <summary>
            /// api
            /// </summary>
            public const string Api = "api";

            /// <summary>
            /// active
            /// </summary>
            public const string Active = "active";

            /// <summary>
            /// auth_method
            /// </summary>
            public const string AuthMethod = "auth_method";

            /// <summary>
            /// caller
            /// </summary>
            public const string Caller = "caller";

            /// <summary>
            /// client
            /// </summary>
            public const string Client = "client";

            /// <summary>
            /// endpoint
            /// </summary>
            public const string Endpoint = "endpoint";

            /// <summary>
            /// error
            /// </summary>
            public const string Error = "error";

            /// <summary>
            /// grant_type
            /// </summary>
            public const string GrantType = "grant_type";

            /// <summary>
            /// method
            /// </summary>
            public const string Method = "method";

            /// <summary>
            /// path
            /// </summary>
            public const string Path = "path";

            /// <summary>
            /// authorize_request_type
            /// </summary>
            public const string AuthorizeRequestType = "authorize_request_type";

            /// <summary>
            /// scheme
            /// </summary>
            public const string Scheme = "scheme";

            /// <summary>
            /// type
            /// </summary>
            public const string Type = "type";
        }

        /// <summary>
        /// Meter for IdentityServer
        /// </summary>
        public static readonly Meter Meter = new Meter(ServiceName, ServiceVersion);

        /// <summary>
        /// Counter for active requests.
        /// </summary>
        public static readonly UpDownCounter<long> ActiveRequestCounter = Meter.CreateUpDownCounter<long>(Counters.ActiveRequests);

        /// <summary>
        /// Increase <see cref="ActiveRequestCounter"/>
        /// </summary>
        /// <param name="endpointType">Type name for endpoint</param>
        /// <param name="path">Path</param>
        public static void IncreaseActiveRequests(string endpointType, string path) =>
            ActiveRequestCounter.Add(1, new(Tags.Endpoint, endpointType), new(Tags.Path, path));

        /// <summary>
        /// Decrease <see cref="ActiveRequestCounter"/>
        /// </summary>
        /// <param name="endpointType">Type name for endpoint</param>
        /// <param name="path">Path</param>
        public static void DecreaseActiveRequests(string endpointType, string path) =>
            ActiveRequestCounter.Add(-1, new(Tags.Endpoint, endpointType), new(Tags.Path, path));

        /// <summary>
        /// High level number of successful operations. Probably most useful together with <see cref="FailureCounter"/>.
        /// </summary>
        public static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>(Counters.Success);

        /// <summary>
        /// Helper method to increase the <see cref="SuccessCounter"/>
        /// </summary>
        /// <param name="clientId">Client involved in event</param>
        public static void Success(string clientId = null)
        {
            if (clientId != null)
            {
                SuccessCounter.Add(1, tag: new("client", clientId));
            }
            else
            {
                SuccessCounter.Add(1);
            }
        }

        /// <summary>
        /// High level number of failed operations. Probably most useful together with <see cref="SuccessCounter"/>.
        /// </summary>
        public static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(Counters.Failure);

        /// <summary>
        /// Helper method to increase the <see cref="FailureCounter"/>
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="clientId">Client involved in event</param>
        public static void Failure(string error, string clientId = null)
        {
            if (clientId != null)
            {
                FailureCounter.Add(1, new("client", clientId), new("error", error));
            }
            else
            {
                FailureCounter.Add(1, tag: new("error", error));
            }
        }

        /// <summary>
        /// Successful Api Secret validations
        /// </summary>
        public static Counter<long> ApiSecretValidationCounter
            = Meter.CreateCounter<long>(Counters.ApiSecretValidation);

        /// <summary>
        /// Helper method to increase <see cref="ApiSecretValidationCounter"/>
        /// </summary>
        /// <param name="apiId">Api Id</param>
        /// <param name="authMethod">Authentication Method</param>
        public static void ApiSecretValidation(string apiId, string authMethod)
        {
            Success(apiId);
            ApiSecretValidationCounter.Add(1, new(Tags.Api, apiId), new(Tags.AuthMethod, authMethod));
        }

        /// <summary>
        /// Failed Api Secret validations
        /// </summary>
        public static Counter<long> ApiSecretValidationFailureCounter
            = Meter.CreateCounter<long>(Counters.ApiSecretValidationFailure);

        /// <summary>
        /// Helper method to increase <see cref="ApiSecretValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ApiSecretValidationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ApiSecretValidationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, message));
        }

        /// <summary>
        /// Successful back channel (CIBA) authentications counter
        /// </summary>
        public static readonly Counter<long> BackChannelAuthenticationCounter =
            Meter.CreateCounter<long>(Counters.BackchannelAuthentication);

        /// <summary>
        /// Helper method to increase <see cref="BackChannelAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        public static void BackChannelAuthentication(string clientId)
        {
            Success(clientId);
            BackChannelAuthenticationCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Failed back channel (CIBA) authentications counter
        /// </summary>
        public static readonly Counter<long> BackChannelAuthenticationFailureCounter =
            Meter.CreateCounter<long>(Counters.BackchannelAuthenticationFailure);

        /// <summary>
        /// Helper method to increase <see cref="BackChannelAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="error"></param>
        public static void BackChannelAuthenticationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            BackChannelAuthenticationFailureCounter
                .Add(1, new(Tags.Client, clientId), new(Tags.Error, error));
        }

        /// <summary>
        /// Client configuration validation success
        /// </summary>
        public static Counter<long> ClientValidationCounter
            = Meter.CreateCounter<long>(Counters.ClientValidation);

        /// <summary>
        /// Helper method to increase <see cref="ClientValidationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        public static void ClientValidation(string clientId)
        {
            Success(clientId);
            ClientValidationCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Client configuration validation failure.
        /// </summary>
        public static Counter<long> ClientValidationFailureCounter
            = Meter.CreateCounter<long>(Counters.ClientValidationFailure);

        /// <summary>
        /// Helper method to increase <see cref="ClientValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="error">Error</param>
        public static void ClientValidationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            ClientValidationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, error));
        }

        /// <summary>
        /// Successful Client Secret validations
        /// </summary>
        public static Counter<long> ClientSecretValidationCounter
            = Meter.CreateCounter<long>(Counters.ClientSecretValidation);

        /// <summary>
        /// Helper method to increase <see cref="ClientSecretValidationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="authMethod"></param>
        public static void ClientSecretValidation(string clientId, string authMethod)
        {
            Success(clientId);
            ClientSecretValidationCounter.Add(1, new(Tags.Client, clientId), new(Tags.AuthMethod, authMethod));
        }

        /// <summary>
        /// Failed Client Secret validations
        /// </summary>
        public static Counter<long> ClientSecretValidationFailureCounter
            = Meter.CreateCounter<long>(Counters.ClientSecretValidationFailure);

        /// <summary>
        /// Helper method to increase <see cref="ClientSecretValidationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ClientSecretValidationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ClientSecretValidationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, message));
        }

        /// <summary>
        /// Successful device code authentication counter
        /// </summary>
        public static readonly Counter<long> DeviceAuthenticationCounter =
            Meter.CreateCounter<long>(Counters.DeviceAuthentication);

        /// <summary>
        /// Helper method to increase <see cref="DeviceAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId">Client ID</param>
        public static void DeviceAuthentication(string clientId)
        {
            Success(clientId);
            DeviceAuthenticationCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Failed device code authentication counter
        /// </summary>
        public static readonly Counter<long> DeviceAuthenticationFailureCounter =
            Meter.CreateCounter<long>(Counters.DeviceAuthenticationFailure);

        /// <summary>
        /// Helper method to increase <see cref="DeviceAuthenticationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="error">Error</param>
        public static void DeviceAuthenticationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            DeviceAuthenticationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, error));
        }

        /// <summary>
        /// Dynamic identityprovider validations
        /// </summary>
        public static Counter<long> DynamicIdentityProviderValidationCounter
            = Meter.CreateCounter<long>(Counters.DynamicIdentityProviderValidation);

        /// <summary>
        /// Helper method to increase <see cref="DynamicIdentityProviderValidationCounter"/>
        /// </summary>
        /// <param name="scheme"></param>
        public static void DynamicIdentityProviderValidation(string scheme)
        {
            Success();
            DynamicIdentityProviderValidationCounter.Add(1, tag: new(Tags.Scheme, scheme));
        }

        /// <summary>
        /// Dynamic identity provider validation failures.
        /// </summary>
        public static Counter<long> DynamicIdentityProviderValidationFailureCounter
            = Meter.CreateCounter<long>(Counters.DynamicIdentityProviderValidationFailure);

        /// <summary>
        /// Helper method to increase <see cref="DynamicIdentityProviderValidationFailureCounter"/>
        /// </summary>
        /// <param name="scheme">Scheme name</param>
        /// <param name="error">Error message</param>
        public static void DynamicIdentityProviderValidationFailure(string scheme, string error)
        {
            Failure(error);
            DynamicIdentityProviderValidationFailureCounter
                .Add(1, new(Tags.Scheme, scheme), new(Tags.Error, error));
        }

        /// <summary>
        /// Introspection success counter
        /// </summary>
        public static readonly Counter<long> IntrospectionCounter =
            Meter.CreateCounter<long>(Counters.Introspection);

        /// <summary>
        /// Helper method to increase <see cref="IntrospectionCounter"/>
        /// </summary>
        /// <param name="callerId"></param>
        /// <param name="active">Is the token valid/active?</param>
        public static void Introspection(string callerId, bool active)
        {
            Success(callerId);
            IntrospectionCounter.Add(1, new(Tags.Caller, callerId), new(Tags.Active, active));
        }

        /// <summary>
        /// Introspection failure counter
        /// </summary>
        public static readonly Counter<long> IntrospectionFailureCounter =
            Meter.CreateCounter<long>(Counters.IntrospectionFailure);

        /// <summary>
        /// Helper method to increase <see cref="IntrospectionFailureCounter"/>
        /// </summary>
        /// <param name="callerId">Api resource or client Id</param>
        /// <param name="error">Error message</param>
        public static void IntrospectionFailure(string callerId, string error)
        {
            Failure(error, callerId);
            IntrospectionFailureCounter.Add(1, new(Tags.Caller, callerId), new(Tags.Caller, error));
        }

        /// <summary>
        /// Pushed Authorization Request Counter
        /// </summary>
        public static Counter<long> PushedAuthorizationRequestCounter
            = Meter.CreateCounter<long>(Counters.PushedAuthorizationRequest);

        /// <summary>
        /// Helper method to increase <see cref="PushedAuthorizationRequestCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        public static void PushedAuthorizationRequest(string clientId)
        {
            Success(clientId);
            PushedAuthorizationRequestCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Pushed Authorization Failure Request Counter
        /// </summary>
        public static Counter<long> PushedAuthorizationRequestFailureCounter
            = Meter.CreateCounter<long>(Counters.PushedAuthorizationRequestFailure);

        /// <summary>
        /// Helper method to increase <see cref="PushedAuthorizationRequestFailureCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="error">Error reason</param>
        public static void PushedAuthorizationRequestFailure(string clientId, string error)
        {
            Failure(clientId);
            PushedAuthorizationRequestCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, error));
        }

        /// <summary>
        /// Resource Owner Authentication Counter
        /// </summary>
        public static Counter<long> ResourceOwnerAuthenticationCounter
            = Meter.CreateCounter<long>(Counters.ResourceOwnerAuthentication);

        /// <summary>
        /// Helper method to increase <see cref="ResourceOwnerAuthenticationCounter"/>
        /// </summary>
        /// <param name="clientId"></param>
        public static void ResourceOwnerAuthentication(string clientId)
        {
            Success(clientId);
            ResourceOwnerAuthenticationCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Failed Resource Owner Authentication Counter
        /// </summary>
        public static Counter<long> ResourceOwnerAuthenticationFailureCounter
            = Meter.CreateCounter<long>(Counters.ResourceOwnerAuthenticationFailure);

        /// <summary>
        /// Helper method to increase <see cref="ResourceOwnerAuthenticationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="message">Error message</param>
        public static void ResourceOwnerAuthenticationFailure(string clientId, string message)
        {
            Failure(message, clientId);
            ResourceOwnerAuthenticationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, message));
        }

        /// <summary>
        /// Revocation success counter.
        /// </summary>
        public static readonly Counter<long> RevocationCounter =
            Meter.CreateCounter<long>(Counters.Revocation);

        /// <summary>
        /// Helper method to increase <see cref="RevocationCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        public static void Revocation(string clientId)
        {
            Success(clientId);
            RevocationCounter.Add(1, tag: new(Tags.Client, clientId));
        }

        /// <summary>
        /// Revocation failure counter
        /// </summary>
        public static readonly Counter<long> RevocationFailureCounter =
            Meter.CreateCounter<long>(Counters.RevocationFailure);

        /// <summary>
        /// Helper method to increase <see cref="RevocationFailureCounter"/>
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="error">Error</param>
        public static void RevocationFailure(string clientId, string error)
        {
            Failure(error, clientId);
            RevocationFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Error, error));
        }

        /// <summary>
        /// Successful token issuance counter.
        /// </summary>
        public static readonly Counter<long> TokenIssuedCounter = Meter.CreateCounter<long>(Counters.TokenIssued);

        /// <summary>
        /// Helper method to increase <see cref="TokenIssuedCounter"/>
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="grantType">Grant Type</param>
        /// <param name="requestType">Type of authorization request</param>
        public static void TokenIssued(string clientId, string grantType, AuthorizeRequestType? requestType)
        {
            Success(clientId);
            TokenIssuedCounter.Add(1,
                new(Tags.Client, clientId),
                new(Tags.GrantType, grantType),
                new(Tags.AuthorizeRequestType, requestType));
        }

        /// <summary>
        /// Token issuance failed counter.
        /// </summary>
        public static readonly Counter<long> TokenIssuedFailureCounter = Meter.CreateCounter<long>(Counters.TokenIssuedFailure);

        /// <summary>
        /// Helper method to increase <see cref="TokenIssuedFailureCounter"/> with labels
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="grantType">Grant Type</param>
        /// <param name="error">Error</param>
        /// <param name="requestType">Type of authorization request</param>
        public static void TokenIssuedFailure(string clientId, string grantType, AuthorizeRequestType? requestType, string error)
        {
            Failure(error, clientId);
            TokenIssuedFailureCounter.Add(1,
                new(Tags.Client, clientId),
                new(Tags.GrantType, grantType),
                new(Tags.AuthorizeRequestType, requestType),
                new(Tags.Error, error));
        }

        /// <summary>
        /// Unhandled exceptions bubbling up to the middleware.
        /// </summary>
        public static Counter<long> UnHandledExceptionCounter
            = Meter.CreateCounter<long>(Counters.UnHandledException);

        /// <summary>
        /// Helper method to increase <see cref="UnHandledExceptionCounter"/>
        /// </summary>
        /// <param name="ex">Exception</param>
        public static void UnHandledException(Exception ex)
            => UnHandledExceptionCounter.Add(1, new(Tags.Type, ex.GetType().Name), new(Tags.Method, ex.TargetSite.Name));
    }
}