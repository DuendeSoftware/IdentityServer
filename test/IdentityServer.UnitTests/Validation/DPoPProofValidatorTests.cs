// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using IdentityModel;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using UnitTests.Common;
using UnitTests.Validation.Setup;
using UnitTests.Validation.TokenRequest_Validation;
using Xunit;

namespace UnitTests.Validation;

public class DPoPProofValidatorTests
{
    private const string Category = "DPoP validator";

    private IdentityServerOptions _options = new IdentityServerOptions();
    private StubClock _clock = new StubClock();
    
    private DateTime _now = new DateTime(2020, 3, 10, 9, 0, 0, DateTimeKind.Utc);
    public DateTime UtcNow
    {
        get
        {
            if (_now > DateTime.MinValue) return _now;
            return DateTime.UtcNow;
        }
    }

    DefaultDPoPProofValidator _subject;

    public DPoPProofValidatorTests()
    {
        _clock.UtcNowFunc = () => UtcNow;
        _subject = new DefaultDPoPProofValidator(_options, _clock, new LoggerFactory().CreateLogger<DefaultDPoPProofValidator>());
    }

    string CreateDPoPToken()
    {
        //var key = CryptoHelper.CreateRsaSecurityKey();
        //var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        //jwk.Alg = "RS256";

        //var header = new Dictionary<string, object>()
        //{
        //    { "typ", "dpop+jwt" },
        //    { "alg", "RS256" },
        //    { "jwk", jwk },
        //};

        return null;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task empty_string_should_fail_validation()
    {
        var popToken = "";
        var ctx = new DPoPProofValidatonContext { ProofTooken = popToken };
        var result = await _subject.ValidateAsync(ctx);
        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }
    
    //[Fact]
    //[Trait("Category", Category)]
    //public async Task valid_dpop_jwt_should_pass_validation()
    //{
    //    var popToken = CreateDPoPToken();
    //    var ctx = new DPoPProofValidatonContext { ProofTooken = popToken };
    //    var result = await _subject.ValidateAsync(ctx);
    //    result.IsError.Should().BeTrue();
    //    result.Error.Should().Be("invalid_dpop_proof");
    //}
}