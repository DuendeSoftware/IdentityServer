// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Configuration;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    public class StartupWithCustomTokenResponses
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication();

            var builder = services.AddIdentityServer(options =>
            {
                options.IssuerUri = "https://idsvr4";

                options.Events = new EventsOptions
                {
                    RaiseErrorEvents = true,
                    RaiseFailureEvents = true,
                    RaiseInformationEvents = true,
                    RaiseSuccessEvents = true
                };
            });

            builder.AddInMemoryClients(Clients.Get());
            builder.AddInMemoryIdentityResources(Scopes.GetIdentityScopes());
            builder.AddInMemoryApiResources(Scopes.GetApiResources());
            builder.AddInMemoryApiScopes(Scopes.GetApiScopes());

            builder.AddDeveloperSigningCredential(persistKey: false);

            services.AddTransient<IResourceOwnerPasswordValidator, CustomResponseResourceOwnerValidator>();
            builder.AddExtensionGrantValidator<CustomResponseExtensionGrantValidator>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseIdentityServer();
        }
    }
}