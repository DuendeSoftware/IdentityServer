using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Xunit;
using static IdentityServer4.Constants;

namespace IdentityServer.UnitTests.Extensions
{
    public class EndpointOptionsExtensionsTests
    {
        private readonly EndpointsOptions _options = new EndpointsOptions();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForAuthorizeEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableAuthorizeEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.Authorize)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForCheckSessionEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableCheckSessionEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.CheckSession)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForDeviceAuthorizationEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableDeviceAuthorizationEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.DeviceAuthorization)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForDiscoveryEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableDiscoveryEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.Discovery)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForEndSessionEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableEndSessionEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.EndSession)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForIntrospectionEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableIntrospectionEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.Introspection)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForTokenEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableTokenEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.Token)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForRevocationEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableTokenRevocationEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.Revocation)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEndpointEnabledShouldReturnExpectedForUserInfoEndpoint(bool expectedIsEndpointEnabled)
        {
            _options.EnableUserInfoEndpoint = expectedIsEndpointEnabled;

            Assert.Equal(
                expectedIsEndpointEnabled,
                _options.IsEndpointEnabled(
                    CreateTestEndpoint(EndpointNames.UserInfo)));
        }

        private Endpoint CreateTestEndpoint(string name)
        {
            return new Endpoint(name, "", null);
        }
    }
}
