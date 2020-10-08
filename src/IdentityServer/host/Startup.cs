// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using IdentityServerHost.Configuration;
using IdentityModel;
using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IdentityServerHost.Extensions;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.HttpOverrides;
using IdentityServerHost.Quickstart.UI;

namespace IdentityServerHost
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;

            IdentityModelEventSource.ShowPII = true;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            
            // cookie policy to deal with temporary browser incompatibilities
            services.AddSameSiteCookiePolicy();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;

                    options.EmitScopesAsSpaceDelimitedStringInJwt = true;

                    options.MutualTls.Enabled = true;
                    options.MutualTls.DomainName = "mtls";
                    //options.MutualTls.AlwaysEmitConfirmationClaim = true;
                })
                .AddInMemoryClients(Clients.Get())
                .AddInMemoryIdentityResources(Resources.IdentityResources)
                .AddInMemoryApiScopes(Resources.ApiScopes)
                .AddInMemoryApiResources(Resources.ApiResources)
                .AddSigningCredential()
                .AddExtensionGrantValidator<Extensions.ExtensionGrantValidator>()
                .AddExtensionGrantValidator<Extensions.NoSubjectExtensionGrantValidator>()
                .AddJwtBearerClientAuthentication()
                .AddAppAuthRedirectUriValidator()
                .AddTestUsers(TestUsers.Users)
                .AddProfileService<HostProfileService>()
                .AddCustomTokenRequestValidator<ParameterizedScopeTokenRequestValidator>()
                .AddScopeParser<ParameterizedScopeParser>()
                .AddMutualTlsSecretValidators();

            // use this for persisted grants store
            // var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            // const string connectionString = "DataSource=identityserver.db";
            // builder.AddOperationalStore(options =>
            // {
            //     options.ConfigureDbContext = b => b.UseSqlite(connectionString,
            //         sql => sql.MigrationsAssembly(migrationsAssembly));
            // });
                

            services.AddExternalIdentityProviders();

            services.AddAuthentication()
                .AddCertificate(options =>
                {
                    options.AllowedCertificateTypes = CertificateTypes.All;
                    options.RevocationMode = X509RevocationMode.NoCheck;
                });
            
            services.AddCertificateForwardingForNginx();
            
            services.AddLocalApiAuthentication(principal =>
            {
                principal.Identities.First().AddClaim(new Claim("additional_claim", "additional_value"));

                return Task.FromResult(principal);
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            // use this for persisted grants store
            // app.InitializePersistedGrantsStore();
            
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseCertificateForwarding();
            app.UseCookiePolicy();
            
            app.UseSerilogRequestLogging();

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }

    public static class BuilderExtensions
    {
        public static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder)
        {
            // create random RS256 key
            //builder.AddDeveloperSigningCredential();

            // use an RSA-based certificate with RS256
            var rsaCert = new X509Certificate2("./keys/identityserver.test.rsa.p12", "changeit");
            builder.AddSigningCredential(rsaCert, "RS256");

            // ...and PS256
            builder.AddSigningCredential(rsaCert, "PS256");

            // or manually extract ECDSA key from certificate (directly using the certificate is not support by Microsoft right now)
            var ecCert = new X509Certificate2("./keys/identityserver.test.ecdsa.p12", "changeit");
            var key = new ECDsaSecurityKey(ecCert.GetECDsaPrivateKey())
            {
                KeyId = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex)
            };

            return builder.AddSigningCredential(
                key,
                IdentityServerConstants.ECDsaSigningAlgorithm.ES256);
        }

        // use this for persisted grants store
        // public static void InitializePersistedGrantsStore(this IApplicationBuilder app)
        // {
        //     using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
        //     {
        //         serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
        //     }
        // }
    }

    public static class ServiceExtensions
    {
        public static IServiceCollection AddExternalIdentityProviders(this IServiceCollection services)
        {
            // configures the OpenIdConnect handlers to persist the state parameter into the server-side IDistributedCache.
            services.AddOidcStateDataFormatterCache("aad", "demoidsrv");

            services.AddAuthentication()
                .AddOpenIdConnect("Google", "Google", options =>
                 {
                     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                     options.ForwardSignOut = IdentityServerConstants.DefaultCookieAuthenticationScheme;

                     options.Authority = "https://accounts.google.com/";
                     options.ClientId = "708996912208-9m4dkjb5hscn7cjrn5u0r4tbgkbj1fko.apps.googleusercontent.com";

                     options.CallbackPath = "/signin-google";
                     options.Scope.Add("email");
                 })
                .AddOpenIdConnect("demoidsrv", "IdentityServer", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    options.Authority = "https://demo.identityserver.io/";
                    options.ClientId = "login";
                    options.ResponseType = "id_token";
                    options.SaveTokens = true;
                    options.CallbackPath = "/signin-idsrv";
                    options.SignedOutCallbackPath = "/signout-callback-idsrv";
                    options.RemoteSignOutPath = "/signout-idsrv";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                })
                .AddOpenIdConnect("aad", "Azure AD", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    options.Authority = "https://login.windows.net/4ca9cb4c-5e5f-4be9-b700-c532992a3705";
                    options.ClientId = "96e3c53e-01cb-4244-b658-a42164cb67a9";
                    options.ResponseType = "id_token";
                    options.CallbackPath = "/signin-aad";
                    options.SignedOutCallbackPath = "/signout-callback-aad";
                    options.RemoteSignOutPath = "/signout-aad";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                })
                .AddOpenIdConnect("adfs", "ADFS", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    options.Authority = "https://adfs.leastprivilege.vm/adfs";
                    options.ClientId = "c0ea8d99-f1e7-43b0-a100-7dee3f2e5c3c";
                    options.ResponseType = "id_token";

                    options.CallbackPath = "/signin-adfs";
                    options.SignedOutCallbackPath = "/signout-callback-adfs";
                    options.RemoteSignOutPath = "/signout-adfs";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });

            return services;
        }

        public static void AddCertificateForwardingForNginx(this IServiceCollection services)
        {
            services.AddCertificateForwarding(options =>
            {
                options.CertificateHeader = "X-SSL-CERT";

                options.HeaderConverter = (headerValue) =>
                {
                    X509Certificate2 clientCertificate = null;

                    if(!string.IsNullOrWhiteSpace(headerValue))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(headerValue));
                        clientCertificate = new X509Certificate2(bytes);
                    }

                    return clientCertificate;
                };
            });
        }
    }
}