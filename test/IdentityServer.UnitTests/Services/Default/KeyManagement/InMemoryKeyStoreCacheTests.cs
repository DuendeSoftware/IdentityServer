
using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Services.KeyManagement;
using FluentAssertions;
using Xunit;

namespace UnitTests.Services.Default.KeyManagement
{
    public class InMemoryKeyStoreCacheTests
    {
        InMemoryKeyStoreCache _subject;
        MockClock _mockClock = new MockClock(new DateTime(2018, 3, 1, 9, 0, 0));

        public InMemoryKeyStoreCacheTests()
        {
            _subject = new InMemoryKeyStoreCache(_mockClock);
        }

        [Fact]
        public async Task GetKeysAsync_within_expiration_should_return_keys()
        {
            var now = _mockClock.UtcNow;

            var keys = new RsaKeyContainer[] {
                new RsaKeyContainer() { Created = _mockClock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromMinutes(1)) },
                new RsaKeyContainer() { Created = _mockClock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromMinutes(2)) },
            };
            await _subject.StoreKeysAsync(keys, TimeSpan.FromMinutes(1));

            var result = await _subject.GetKeysAsync();
            result.Should().BeSameAs(keys);

            _mockClock.UtcNow = now.Subtract(TimeSpan.FromDays(1));
            result = await _subject.GetKeysAsync();
            result.Should().BeSameAs(keys);

            _mockClock.UtcNow = now.Add(TimeSpan.FromSeconds(59));
            result = await _subject.GetKeysAsync();
            result.Should().BeSameAs(keys);

            _mockClock.UtcNow = now.Add(TimeSpan.FromMinutes(1));
            result = await _subject.GetKeysAsync();
            result.Should().BeSameAs(keys);
        }

        [Fact]
        public async Task GetKeysAsync_past_expiration_should_return_no_keys()
        {
            var now = _mockClock.UtcNow;

            var keys = new RsaKeyContainer[] {
                new RsaKeyContainer() { Created = _mockClock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromMinutes(1)) },
                new RsaKeyContainer() { Created = _mockClock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromMinutes(2)) },
            };
            await _subject.StoreKeysAsync(keys, TimeSpan.FromMinutes(1));

            _mockClock.UtcNow = now.Add(TimeSpan.FromSeconds(61));
            var result = await _subject.GetKeysAsync();
            result.Should().BeNullOrEmpty();
        }
    }
}
