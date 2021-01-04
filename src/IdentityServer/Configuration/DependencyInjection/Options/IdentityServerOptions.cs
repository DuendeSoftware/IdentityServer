// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Stores.Serialization;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// The IdentityServerOptions class is the top level container for all configuration settings of IdentityServer.
    /// </summary>
    public class IdentityServerOptions
    {
        /// <summary>
        /// Gets or sets the unique name of this server instance, e.g. https://myissuer.com.
        /// If not set, the issuer name is inferred from the request
        /// </summary>
        /// <value>
        /// Unique name of this server instance, e.g. https://myissuer.com
        /// </value>
        public string IssuerUri { get; set; }

        /// <summary>
        /// Set to false to preserve the original casing of the IssuerUri. Defaults to true.
        /// </summary>
        public bool LowerCaseIssuerUri { get; set; } = true;

        /// <summary>
        /// Gets or sets the value for the JWT typ header for access tokens.
        /// </summary>
        /// <value>
        /// The JWT typ value.
        /// </value>
        public string AccessTokenJwtType { get; set; } = "at+jwt";

        /// <summary>
        /// Emits an aud claim with the format issuer/resources. That's needed for some older access token validation plumbing. Defaults to false.
        /// </summary>
        public bool EmitStaticAudienceClaim { get; set; } = false;
        
        /// <summary>
        /// Specifies whether scopes in JWTs are emitted as array or string
        /// </summary>
        public bool EmitScopesAsSpaceDelimitedStringInJwt { get; set; } = false;
        
        /// <summary>
        /// Specifies whether the s_hash claim gets emitted in identity tokens. Defaults to false.
        /// </summary>
        public bool EmitStateHash { get; set; } = false;
        
        /// <summary>
        /// Specifies whether the JWT typ and content-type for JWT secured authorization requests is checked according to IETF spec.
        /// This might break older OIDC conformant request objects.
        /// </summary>
        public bool StrictJarValidation { get; set; } = false;

        /// <summary>
        /// Gets or sets the endpoint configuration.
        /// </summary>
        /// <value>
        /// The endpoints configuration.
        /// </value>
        public EndpointsOptions Endpoints { get; set; } = new EndpointsOptions();

        /// <summary>
        /// Gets or sets the discovery endpoint configuration.
        /// </summary>
        /// <value>
        /// The discovery endpoint configuration.
        /// </value>
        public DiscoveryOptions Discovery { get; set; } = new DiscoveryOptions();

        /// <summary>
        /// Gets or sets the authentication options.
        /// </summary>
        /// <value>
        /// The authentication options.
        /// </value>
        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();

        /// <summary>
        /// Gets or sets the events options.
        /// </summary>
        /// <value>
        /// The events options.
        /// </value>
        public EventsOptions Events { get; set; } = new EventsOptions();

        /// <summary>
        /// Gets or sets the max input length restrictions.
        /// </summary>
        /// <value>
        /// The length restrictions.
        /// </value>
        public InputLengthRestrictions InputLengthRestrictions { get; set; } = new InputLengthRestrictions();

        /// <summary>
        /// Gets or sets the options for the user interaction.
        /// </summary>
        /// <value>
        /// The user interaction options.
        /// </value>
        public UserInteractionOptions UserInteraction { get; set; } = new UserInteractionOptions();

        /// <summary>
        /// Gets or sets the caching options.
        /// </summary>
        /// <value>
        /// The caching options.
        /// </value>
        public CachingOptions Caching { get; set; } = new CachingOptions();

        /// <summary>
        /// Gets or sets the cors options.
        /// </summary>
        /// <value>
        /// The cors options.
        /// </value>
        public CorsOptions Cors { get; set; } = new CorsOptions();

        /// <summary>
        /// Gets or sets the Content Security Policy options.
        /// </summary>
        public CspOptions Csp { get; set; } = new CspOptions();

        /// <summary>
        /// Gets or sets the validation options.
        /// </summary>
        public ValidationOptions Validation { get; set; } = new ValidationOptions();

        /// <summary>
        /// Gets or sets the device flow options.
        /// </summary>
        public DeviceFlowOptions DeviceFlow { get; set; } = new DeviceFlowOptions();
        
        /// <summary>
        /// Gets or sets the logging options
        /// </summary>
        public LoggingOptions Logging { get; set; } = new LoggingOptions();

        /// <summary>
        /// Gets or sets the mutual TLS options.
        /// </summary>
        public MutualTlsOptions MutualTls { get; set; } = new MutualTlsOptions();
        
        /// <summary>
        /// Gets or sets the signing key management options.
        /// </summary>
        public KeyManagementOptions KeyManagement { get; set; } = new KeyManagementOptions();

        /// <summary>
        /// Options for persisted grants.
        /// </summary>
        public PersistentGrantOptions PersistentGrants { get; set; } = new PersistentGrantOptions();

        /// <summary>
        /// Gets or sets the license key.
        /// </summary>
        public string LicenseKey { get; set; }
    }
}