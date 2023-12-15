using System.Diagnostics.Metrics;

namespace IdentityServerHost.Pages;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1724 // Type names should not match namespaces

/// <summary>
/// Telemetry helpers for the UI
/// </summary>
public static class Telemetry
{
    private static readonly string ServiceVersion = typeof(Telemetry).Assembly.GetName().Version!.ToString();
    
    /// <summary>
    /// Service name for telemetry.
    /// </summary>
    public static readonly string ServiceName = typeof(Telemetry).Assembly.GetName().Name!;

    /// <summary>
    /// Metrics configuration
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Name of Counters
        /// </summary>
        public static class Counters
        {
            /// <summary>
            /// consent_granted
            /// </summary>
            public const string ConsentGranted = "consent_granted";

            /// <summary>
            /// consent_denied
            /// </summary>
            public const string ConsentDenied = "consent_denied";

            /// <summary>
            /// grants_revoked
            /// </summary>
            public const string GrantsRevoked = "grants_revoked";

            /// <summary>
            /// user_login
            /// </summary>
            public const string UserLogin = "user_login";

            /// <summary>
            /// user_login_failure
            /// </summary>
            public const string UserLoginFailure = "user_login_failure";

            /// <summary>
            /// user_logout
            /// </summary>
            public const string UserLogout = "user_logout";
        }

        /// <summary>
        /// Name of tags
        /// </summary>
        public static class Tags
        {
            /// <summary>
            /// client
            /// </summary>
            public const string Client = "client";

            /// <summary>
            /// error
            /// </summary>
            public const string Error = "error";

            /// <summary>
            /// idp
            /// </summary>
            public const string Idp = "idp";

            /// <summary>
            /// remember
            /// </summary>
            public const string Remember = "remember";

            /// <summary>
            /// scope
            /// </summary>
            public const string Scope = "scope";
        }

        /// <summary>
        /// Meter for the IdentityServer host project
        /// </summary>
        private static readonly Meter Meter = new Meter(ServiceName, ServiceVersion);

        private static Counter<long> ConsentGrantedCounter = Meter.CreateCounter<long>(Counters.ConsentGranted);

        /// <summary>
        /// Helper method to increase <see cref="Counters.ConsentGranted"/> counter. The scopes
        /// are expanded and called one by one to not cause a combinatory explosion of scopes.
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="scopes">Scope names. Each element is added on it's own to the counter</param>
        public static void ConsentGranted(string clientId, IEnumerable<string> scopes, bool remember)
        {
            ArgumentNullException.ThrowIfNull(scopes);
            foreach(var scope in scopes)
            {
                ConsentGrantedCounter.Add(1, new(Tags.Client, clientId), new(Tags.Scope, scope), new(Tags.Remember, remember));
            }
        }

        private static Counter<long> ConsentDeniedCounter = Meter.CreateCounter<long>(Counters.ConsentDenied);

        /// <summary>
        /// Helper method to increase <see cref="Counters.ConsentDenied"/> counter. The scopes
        /// are expanded and called one by one to not cause a combinatory explosion of scopes.
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="scopes">Scope names. Each element is added on it's own to the counter</param>
        public static void ConsentDenied(string clientId, IEnumerable<string> scopes)
        {
            ArgumentNullException.ThrowIfNull(scopes);
            foreach (var scope in scopes)
            {
                ConsentDeniedCounter.Add(1, new(Tags.Client, clientId), new(Tags.Scope, scope));
            }
        }

        private static Counter<long> GrantsRevokedCounter = Meter.CreateCounter<long>(Counters.GrantsRevoked);

        /// <summary>
        /// Helper method to increase the <see cref="Counters.GrantsRevoked"/> counter.
        /// </summary>
        /// <param name="clientId">Client id to revoke for, or null for all.</param>
        public static void GrantsRevoked(string? clientId)
            => GrantsRevokedCounter.Add(1, tag: new(Tags.Client, clientId));

        private static Counter<long> UserLoginCounter = Meter.CreateCounter<long>(Counters.UserLogin);

        /// <summary>
        /// Helper method to increase <see cref="Counters.UserLogin"/> counter.
        /// </summary>
        /// <param name="clientId">Client Id, if available</param>
        public static void UserLogin(string? clientId, string idp)
            => UserLoginCounter.Add(1, new(Tags.Client, clientId), new(Tags.Idp, idp));

        private static Counter<long> UserLoginFailureCounter = Meter.CreateCounter<long>(Counters.UserLoginFailure);

        /// <summary>
        /// Helper method to increase <see cref="Counters.UserLoginFailure" counter.
        /// </summary>
        /// <param name="clientId">Client Id, if available</param>
        /// <param name="error">Error message</param>
        public static void UserLoginFailure(string? clientId, string idp, string error)
            => UserLoginFailureCounter.Add(1, new(Tags.Client, clientId), new(Tags.Idp, idp), new(Tags.Error, error));

        private static Counter<long> UserLogoutCounter = Meter.CreateCounter<long>(Counters.UserLogout);

        /// <summary>
        /// Helper method to increase the <see cref="Counters.UserLogout"/> counter.
        /// </summary>
        /// <param name="idp">Idp/authentication scheme for external authentication, or "local" for built in.</param>
        public static void UserLogout(string? idp)
            => UserLogoutCounter.Add(1, tag: new(Tags.Idp, idp));
    }
}
