// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using System.Collections.Generic;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Events
{
    /// <summary>
    /// Event for successful backchannel authentication result
    /// </summary>
    /// <seealso cref="Event" />
    public class BackchannelAuthenticationSuccessEvent : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackchannelAuthenticationSuccessEvent"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public BackchannelAuthenticationSuccessEvent(BackchannelAuthenticationResponse response)
            : this()
        {
            //ClientId = response.Request.ClientId;
            //ClientName = response.Request.Client.ClientName;
            //RedirectUri = response.RedirectUri;
            //Endpoint = Constants.EndpointNames.BackchannelAuthentication;
            //SubjectId = response.Request.Subject.GetSubjectId();
            //Scopes = response.Scope;
            //GrantType = response.Request.GrantType;

            //var tokens = new List<Token>();
            //if (response.IdentityToken != null)
            //{
            //    tokens.Add(new Token(OidcConstants.TokenTypes.IdentityToken, response.IdentityToken));
            //}
            //if (response.Code != null)
            //{
            //    tokens.Add(new Token(OidcConstants.ResponseTypes.Code, response.Code));
            //}
            //if (response.AccessToken != null)
            //{
            //    tokens.Add(new Token(OidcConstants.TokenTypes.AccessToken, response.AccessToken));
            //}
            //Tokens = tokens;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackchannelAuthenticationSuccessEvent"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="request">The request.</param>
        public BackchannelAuthenticationSuccessEvent(BackchannelAuthenticationResponse response, BackchannelAuthenticationRequestValidationResult request)
            : this()
        {
            ClientId = request.ValidatedRequest.Client.ClientId;
            ClientName = request.ValidatedRequest.Client.ClientName;
            Endpoint = Constants.EndpointNames.Token;
            SubjectId = request.ValidatedRequest.Subject?.GetSubjectId();
            //GrantType = request.ValidatedRequest.GrantType;

            //if (GrantType == OidcConstants.GrantTypes.RefreshToken)
            //{
            //    Scopes = request.ValidatedRequest.RefreshToken.AuthorizedScopes.ToSpaceSeparatedString();
            //}
            //else if (GrantType == OidcConstants.GrantTypes.AuthorizationCode)
            //{
            //    Scopes = request.ValidatedRequest.AuthorizationCode.RequestedScopes.ToSpaceSeparatedString();
            //}
            //else
            //{
            //    Scopes = request.ValidatedRequest.ValidatedResources?.RawScopeValues.ToSpaceSeparatedString();
            //}
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackchannelAuthenticationSuccessEvent"/> class.
        /// </summary>
        protected BackchannelAuthenticationSuccessEvent()
            : base(EventCategories.BackchannelAuthentication,
                  "Backchannel Authentication Success",
                  EventTypes.Success,
                  EventIds.BackchannelAuthenticationSuccess)
        {
        }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        /// <value>
        /// The name of the client.
        /// </value>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the subject identifier.
        /// </summary>
        /// <value>
        /// The subject identifier.
        /// </value>
        public string SubjectId { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        /// <value>
        /// The scopes.
        /// </value>
        public string Scopes { get; set; }
    }
}