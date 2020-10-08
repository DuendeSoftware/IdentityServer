using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.Extensions.Logging;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    class CustomProfileService : TestUserProfileService
    {
        public CustomProfileService(TestUserStore users, ILogger<TestUserProfileService> logger) : base(users, logger)
        { }

        public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            await base.GetProfileDataAsync(context);

            if (context.Subject.Identity.AuthenticationType == "custom")
            {
                var extraClaim = context.Subject.FindFirst("extra_claim");
                if (extraClaim != null)
                {
                    context.IssuedClaims.Add(extraClaim);
                }
            }
        }
    }
}