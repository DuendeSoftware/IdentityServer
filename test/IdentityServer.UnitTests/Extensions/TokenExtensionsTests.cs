// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using UnitTests.Common;
using Xunit;

namespace UnitTests.Extensions;

public class TokenExtensionsTests
{
    [Theory]
    [InlineData("test_bool", "TRUE", ClaimValueTypes.Boolean, "\"test_bool\":true")]
    [InlineData("test_bool", "False", ClaimValueTypes.Boolean, "\"test_bool\":false")]
    [InlineData("test_int32", "1", ClaimValueTypes.Integer, "\"test_int32\":1")]
    [InlineData("test_int32", "02", ClaimValueTypes.Integer32, "\"test_int32\":2")]
    [InlineData("test_int64", "0123456789012", ClaimValueTypes.Integer64, "\"test_int64\":123456789012")]
    [InlineData("test_json_array", " [ \"value1\" , \"value2\" , \"value3\" ] ", "json", 
        "\"test_json_array\":[\"value1\",\"value2\",\"value3\"]")]
    [InlineData("test_json_obj", " { \"value1\": \"value2\" , \"value3\": [ \"value4\", \"value5\" ] } ", "json", 
        "\"test_json_obj\":{\"value1\":\"value2\",\"value3\":[\"value4\",\"value5\"]}")]
    [InlineData("test_any", "raw\"string\tspecial char", "any", "\"test_any\":\"raw\\u0022string\\tspecial char\"")]
    public void TestClaimValueTypes(string type, string value, string valueType, string expected)
    {
        var token = new Token(OidcConstants.TokenTypes.AccessToken)
        {
            Issuer = "issuer",
            Claims = new List<Claim> { new Claim(type, value, valueType) },
        };

        var payloadDict = token.CreateJwtPayloadDictionary(new IdentityServerOptions(), new DefaultClock(), 
            TestLogger.Create<TokenExtensionsTests>());

        var payloadJson = JsonSerializer.Serialize(payloadDict);

        Assert.Contains(expected, payloadJson);
    }
}