using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer.UnitTests.Validation.Setup;
using Xunit;

namespace IdentityServer.UnitTests.Services.Default
{
    public class DefaultRefreshTokenServiceTests
    {
        private DefaultRefreshTokenService _subject;
        private DefaultRefreshTokenStore _store;

        private ClaimsPrincipal _user = new IdentityServerUser("123").CreatePrincipal();
        private StubClock _clock = new StubClock();

        public DefaultRefreshTokenServiceTests()
        {
            _store = new DefaultRefreshTokenStore(
                new InMemoryPersistedGrantStore(),
                new PersistentGrantSerializer(),
                new DefaultHandleGenerationService(),
                TestLogger.Create<DefaultRefreshTokenStore>());

            _subject = new DefaultRefreshTokenService(
                _store, 
                new TestProfileService(),
                _clock, 
                TestLogger.Create<DefaultRefreshTokenService>());
        }

        [Fact]
        public async Task CreateRefreshToken_token_exists_in_store()
        {
            var client = new Client();
            var accessToken = new Token();

            var handle = await _subject.CreateRefreshTokenAsync(_user, accessToken, client);

            (await _store.GetRefreshTokenAsync(handle)).Should().NotBeNull();
        }

        [Fact]
        public async Task CreateRefreshToken_should_match_absolute_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                AbsoluteRefreshTokenLifetime = 10
            };

            var handle = await _subject.CreateRefreshTokenAsync(_user, new Token(), client);

            var refreshToken = (await _store.GetRefreshTokenAsync(handle));

            refreshToken.Should().NotBeNull();
            refreshToken.Lifetime.Should().Be(client.AbsoluteRefreshTokenLifetime);
        }

        [Fact]
        public async Task CreateRefreshToken_should_cap_sliding_lifetime_that_exceeds_absolute_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime  = 100,
                AbsoluteRefreshTokenLifetime = 10
            };

            var handle = await _subject.CreateRefreshTokenAsync(_user, new Token(), client);

            var refreshToken = (await _store.GetRefreshTokenAsync(handle));

            refreshToken.Should().NotBeNull();
            refreshToken.Lifetime.Should().Be(client.AbsoluteRefreshTokenLifetime);
        }

        [Fact]
        public async Task CreateRefreshToken_should_match_sliding_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 10
            };

            var handle = await _subject.CreateRefreshTokenAsync(_user, new Token(), client);

            var refreshToken = (await _store.GetRefreshTokenAsync(handle));

            refreshToken.Should().NotBeNull();
            refreshToken.Lifetime.Should().Be(client.SlidingRefreshTokenLifetime);
        }

        [Fact]
        public async Task UpdateRefreshToken_one_time_use_should_create_new_token()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            (await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client))
                .Should().NotBeNull()
                .And
                .NotBe(handle);
        }

        [Fact]
        public async Task UpdateRefreshToken_sliding_with_non_zero_absolute_should_update_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 10,
                AbsoluteRefreshTokenLifetime = 100
            };

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var handle = await _store.StoreRefreshTokenAsync(new RefreshToken
            {
                CreationTime = now.AddSeconds(-10),
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            });

            var refreshToken = await _store.GetRefreshTokenAsync(handle);
            var newHandle = await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client);

            newHandle.Should().NotBeNull().And.Be(handle);

            var newRefreshToken = await _store.GetRefreshTokenAsync(newHandle);

            newRefreshToken.Should().NotBeNull();
            newRefreshToken.Lifetime.Should().Be((int)(now - newRefreshToken.CreationTime).TotalSeconds + client.SlidingRefreshTokenLifetime);
        }

        [Fact]
        public async Task UpdateRefreshToken_lifetime_exceeds_absolute_should_be_absolute_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 10,
                AbsoluteRefreshTokenLifetime = 1000
            };

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var handle = await _store.StoreRefreshTokenAsync(new RefreshToken
            {
                CreationTime = now.AddSeconds(-1000),
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            });

            var refreshToken = await _store.GetRefreshTokenAsync(handle);
            var newHandle = await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client);

            newHandle.Should().NotBeNull().And.Be(handle);

            var newRefreshToken = await _store.GetRefreshTokenAsync(newHandle);

            newRefreshToken.Should().NotBeNull();
            newRefreshToken.Lifetime.Should().Be(client.AbsoluteRefreshTokenLifetime);
        }

        [Fact]
        public async Task UpdateRefreshToken_sliding_with_zero_absolute_should_update_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 10,
                AbsoluteRefreshTokenLifetime = 0
            };

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var handle = await _store.StoreRefreshTokenAsync(new RefreshToken
            {
                CreationTime = now.AddSeconds(-1000),
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            });

            var refreshToken = await _store.GetRefreshTokenAsync(handle);
            var newHandle = await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client);

            newHandle.Should().NotBeNull().And.Be(handle);

            var newRefreshToken = await _store.GetRefreshTokenAsync(newHandle);

            newRefreshToken.Should().NotBeNull();
            newRefreshToken.Lifetime.Should().Be((int)(now - newRefreshToken.CreationTime).TotalSeconds + client.SlidingRefreshTokenLifetime);
        }

        [Fact]
        public async Task UpdateRefreshToken_for_onetime_and_sliding_with_zero_absolute_should_update_lifetime()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 10,
                AbsoluteRefreshTokenLifetime = 0
            };

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var handle = await _store.StoreRefreshTokenAsync(new RefreshToken
            {
                CreationTime = now.AddSeconds(-1000),
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            });

            var refreshToken = await _store.GetRefreshTokenAsync(handle);
            var newHandle = await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client);

            newHandle.Should().NotBeNull().And.NotBe(handle);

            var newRefreshToken = await _store.GetRefreshTokenAsync(newHandle);

            newRefreshToken.Should().NotBeNull();
            newRefreshToken.Lifetime.Should().Be((int)(now - newRefreshToken.CreationTime).TotalSeconds + client.SlidingRefreshTokenLifetime);
        }

        [Fact]
        public async Task UpdateRefreshToken_one_time_use_should_consume_token_and_create_new_one_with_correct_dates()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var newHandle = await _subject.UpdateRefreshTokenAsync(handle, refreshToken, client);

            var oldToken = await _store.GetRefreshTokenAsync(handle);
            var newToken = await _store.GetRefreshTokenAsync(newHandle);

            oldToken.ConsumedTime.Should().Be(now);
            newToken.ConsumedTime.Should().BeNull();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_invalid_token_should_fail()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var result = await _subject.ValidateRefreshTokenAsync("invalid", client);

            result.IsError.Should().BeTrue();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_client_without_allow_offline_access_should_fail()
        {
            var client = new Client
            {
                ClientId = "client1",
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var result = await _subject.ValidateRefreshTokenAsync(handle, client);

            result.IsError.Should().BeTrue();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_invalid_client_binding_should_fail()
        {
            var client = new Client
            {
                ClientId = "client1",
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client2",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var result = await _subject.ValidateRefreshTokenAsync(handle, client);

            result.IsError.Should().BeTrue();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_expired_token_should_fail()
        {
            var client = new Client
            {
                ClientId = "client1",
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow.AddSeconds(20);
            _clock.UtcNowFunc = () => now;

            var result = await _subject.ValidateRefreshTokenAsync(handle, client);

            result.IsError.Should().BeTrue();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_consumed_token_should_fail()
        {
            var client = new Client
            {
                ClientId = "client1",
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                ConsumedTime = DateTime.UtcNow,
                
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var result = await _subject.ValidateRefreshTokenAsync(handle, client);

            result.IsError.Should().BeTrue();
        }
        
        [Fact]
        public async Task ValidateRefreshToken_valid_token_should_succeed()
        {
            var client = new Client
            {
                ClientId = "client1",
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly
            };

            var refreshToken = new RefreshToken
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = client.ClientId,
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Claims = new List<Claim>()
                    {
                        new Claim("sub", "123")
                    }
                }
            };

            var handle = await _store.StoreRefreshTokenAsync(refreshToken);

            var now = DateTime.UtcNow;
            _clock.UtcNowFunc = () => now;

            var result = await _subject.ValidateRefreshTokenAsync(handle, client);

            result.IsError.Should().BeFalse();
        }
    }
}
