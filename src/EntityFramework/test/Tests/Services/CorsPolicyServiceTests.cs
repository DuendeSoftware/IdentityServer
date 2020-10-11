// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.EntityFramework.Services;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Services
{
    public class CorsPolicyServiceTests : IntegrationTest<CorsPolicyServiceTests, ConfigurationDbContext, ConfigurationStoreOptions>
    {
        public CorsPolicyServiceTests(DatabaseProviderFixture<ConfigurationDbContext> fixture) : base(fixture)
        {
            foreach (var options in TestDatabaseProviders.SelectMany(x => x.Select(y => (DbContextOptions<ConfigurationDbContext>)y)).ToList())
            {
                using (var context = new ConfigurationDbContext(options, StoreOptions))
                    context.Database.EnsureCreated();
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void IsOriginAllowedAsync_WhenOriginIsAllowed_ExpectTrue(DbContextOptions<ConfigurationDbContext> options)
        {
            const string testCorsOrigin = "https://identityserver.io/";

            using (var context = new ConfigurationDbContext(options, StoreOptions))
            {
                context.Clients.Add(new Client
                {
                    ClientId = Guid.NewGuid().ToString(),
                    ClientName = Guid.NewGuid().ToString(),
                    AllowedCorsOrigins = new List<string> { "https://www.identityserver.com" }
                }.ToEntity());
                context.Clients.Add(new Client
                {
                    ClientId = "2",
                    ClientName = "2",
                    AllowedCorsOrigins = new List<string> { "https://www.identityserver.com", testCorsOrigin }
                }.ToEntity());
                context.SaveChanges();
            }

            bool result;
            using (var context = new ConfigurationDbContext(options, StoreOptions))
            {
                var ctx = new DefaultHttpContext();
                var svcs = new ServiceCollection();
                svcs.AddSingleton<IConfigurationDbContext>(context);
                ctx.RequestServices = svcs.BuildServiceProvider();
                var ctxAccessor = new HttpContextAccessor();
                ctxAccessor.HttpContext = ctx;

                var service = new CorsPolicyService(ctxAccessor, FakeLogger<CorsPolicyService>.Create());
                result = service.IsOriginAllowedAsync(testCorsOrigin).Result;
            }

            Assert.True(result);
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void IsOriginAllowedAsync_WhenOriginIsNotAllowed_ExpectFalse(DbContextOptions<ConfigurationDbContext> options)
        {
            using (var context = new ConfigurationDbContext(options, StoreOptions))
            {
                context.Clients.Add(new Client
                {
                    ClientId = Guid.NewGuid().ToString(),
                    ClientName = Guid.NewGuid().ToString(),
                    AllowedCorsOrigins = new List<string> { "https://www.identityserver.com" }
                }.ToEntity());
                context.SaveChanges();
            }

            bool result;
            using (var context = new ConfigurationDbContext(options, StoreOptions))
            {
                var ctx = new DefaultHttpContext();
                var svcs = new ServiceCollection();
                svcs.AddSingleton<IConfigurationDbContext>(context);
                ctx.RequestServices = svcs.BuildServiceProvider();
                var ctxAccessor = new HttpContextAccessor();
                ctxAccessor.HttpContext = ctx;

                var service = new CorsPolicyService(ctxAccessor, FakeLogger<CorsPolicyService>.Create());
                result = service.IsOriginAllowedAsync("InvalidOrigin").Result;
            }

            Assert.False(result);
        }
    }
}