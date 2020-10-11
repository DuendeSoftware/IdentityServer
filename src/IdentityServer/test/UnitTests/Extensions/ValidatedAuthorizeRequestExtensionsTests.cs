// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Validation;
using Xunit;

namespace UnitTests.Extensions
{
    public class ValidatedAuthorizeRequestExtensionsTests
    {
        [Fact]
        public void GetAcrValues_should_return_snapshot_of_values()
        {
            var request = new ValidatedAuthorizeRequest()
            {
                Raw = new System.Collections.Specialized.NameValueCollection()
            };
            request.AuthenticationContextReferenceClasses.Add("a");
            request.AuthenticationContextReferenceClasses.Add("b");
            request.AuthenticationContextReferenceClasses.Add("c");

            var acrs = request.GetAcrValues();
            foreach(var acr in acrs)
            {
                request.RemoveAcrValue(acr);
            }
        }
    }
}
