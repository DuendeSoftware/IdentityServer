// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IntegrationTests.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests.Hosting;

public class ServerSideSessionTests
{
    private const string Category = "Server Side Sessions";

    private IdentityServerPipeline _pipeline = new IdentityServerPipeline();
    private IServerSideSessionStore _sessionStore;
    private IServerSideTicketStore _ticketStore;

    public ServerSideSessionTests()
    {
        _pipeline.OnPostConfigureServices += s => 
        {
            s.AddIdentityServerBuilder().AddServerSideSessions();
        };
        _pipeline.OnPostConfigure += app =>
        {
            app.Map("/user", ep => {
                ep.Run(ctx => 
                { 
                    if (ctx.User.Identity.IsAuthenticated)
                    {
                        ctx.Response.StatusCode = 200;
                    }
                    else
                    {
                        ctx.Response.StatusCode = 401;
                    }
                    return Task.CompletedTask;
                });
            });
        };


        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
        });
        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "alice",
            Username = "alice",
        });

        _pipeline.Initialize();

        _sessionStore = _pipeline.Resolve<IServerSideSessionStore>();
        _ticketStore = _pipeline.Resolve<IServerSideTicketStore>();
    }

    async Task<bool> IsLoggedIn()
    {
        var response = await _pipeline.BrowserClient.GetAsync(IdentityServerPipeline.BaseUrl + "/user");
        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task login_should_create_server_side_session()
    {
        (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Should().BeEmpty();
        await _pipeline.LoginAsync("bob");
        (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Should().NotBeEmpty();
        (await IsLoggedIn()).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task remove_server_side_session_should_logout_user()
    {
        await _pipeline.LoginAsync("bob");

        await _sessionStore.DeleteSessionsAsync(new SessionFilter { SubjectId = "bob" });
        (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Should().BeEmpty();

        (await IsLoggedIn()).Should().BeFalse();
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task logout_should_remove_server_side_session()
    {
        await _pipeline.LoginAsync("bob");
        await _pipeline.LogoutAsync();

        (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Should().BeEmpty();

        (await IsLoggedIn()).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task corrupted_server_side_session_should_logout_user()
    {
        await _pipeline.LoginAsync("bob");

        var sessions = await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" });
        var session = await _sessionStore.GetSessionAsync(sessions.Single().Key);
        session.Ticket = "invalid";
        await _sessionStore.UpdateSessionAsync(session);

        (await IsLoggedIn()).Should().BeFalse();
        (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task subsequent_logins_should_update_server_side_session()
    {
        await _pipeline.LoginAsync("bob");

        var key = (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Single().Key;

        await _pipeline.LoginAsync("bob");

        (await IsLoggedIn()).Should().BeTrue();
        var sessions = await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" });
        sessions.First().Key.Should().Be(key);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task changing_users_should_create_new_server_side_session()
    {
        await _pipeline.LoginAsync("bob");

        var bob_session = (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "bob" })).Single();

        await Task.Delay(1000);
        await _pipeline.LoginAsync("alice");

        (await IsLoggedIn()).Should().BeTrue();
        var alice_session = (await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "alice" })).Single();

        alice_session.Key.Should().Be(bob_session.Key);
        (alice_session.Created > bob_session.Created).Should().BeTrue();
        alice_session.SessionId.Should().NotBe(bob_session.SessionId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task getsessions_on_ticket_store_should_use_session_store()
    {
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();

        var tickets = await _ticketStore.GetSessionsAsync(new SessionFilter { SubjectId = "alice" });
        var sessions = await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = "alice" });

        tickets.Select(x => x.SessionId).Should().BeEquivalentTo(sessions.Select(x => x.SessionId));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task querysessions_on_ticket_store_should_use_session_store()
    {
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();
        await _pipeline.LoginAsync("alice");
        _pipeline.RemoveLoginCookie();

        var tickets = await _ticketStore.QuerySessionsAsync(new SessionQuery { SubjectId = "alice" });
        var sessions = await _sessionStore.QuerySessionsAsync(new SessionQuery { SubjectId = "alice" });

        tickets.ResultsToken.Should().Be(sessions.ResultsToken);
        tickets.HasPrevResults.Should().Be(sessions.HasPrevResults);
        tickets.HasNextResults.Should().Be(sessions.HasNextResults);
        tickets.TotalCount.Should().Be(sessions.TotalCount);
        tickets.TotalPages.Should().Be(sessions.TotalPages);
        tickets.CurrentPage.Should().Be(sessions.CurrentPage);

        tickets.Results.Select(x => x.SessionId).Should().BeEquivalentTo(sessions.Results.Select(x => x.SessionId));
    }
}
