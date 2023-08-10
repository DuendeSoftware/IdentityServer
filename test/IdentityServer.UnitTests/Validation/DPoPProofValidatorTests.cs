// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UnitTests.Common;
using Xunit;

namespace UnitTests.Validation;

public class DPoPProofValidatorTests
{
    private const string Category = "DPoP validator tests";

    private IdentityServerOptions _options = new IdentityServerOptions();
    private StubClock _clock = new StubClock();
    private TestReplayCache _testReplayCache;
    private StubDataProtectionProvider _stubDataProtectionProvider = new StubDataProtectionProvider();

    private DateTime _now = new DateTime(2020, 3, 10, 9, 0, 0, DateTimeKind.Utc);
    public DateTime UtcNow
    {
        get
        {
            if (_now > DateTime.MinValue) return _now;
            return DateTime.UtcNow;
        }
    }

    private DPoPProofValidatonContext _context = new DPoPProofValidatonContext
    { 
        ClientClockSkew = TimeSpan.Zero, 
        Url = "https://identityserver/connect/token", 
        Method = "POST" 
    };

Dictionary<string, object> _header;
    Dictionary<string, object> _payload;
    string _privateJWK = "{\"Crv\":null,\"D\":\"QeBWodq0hSYjfAxxo0VZleXLqwwZZeNWvvFfES4WyItao_-OJv1wKA7zfkZxbWkpK5iRbKrl2AMJ52AtUo5JJ6QZ7IjAQlgM0lBg3ltjb1aA0gBsK5XbiXcsV8DiAnRuy6-XgjAKPR8Lo-wZl_fdPbVoAmpSdmfn_6QXXPBai5i7FiyDbQa16pI6DL-5SCj7F78QDTRiJOqn5ElNvtoJEfJBm13giRdqeriFi3pCWo7H3QBgTEWtDNk509z4w4t64B2HTXnM0xj9zLnS42l7YplJC7MRibD4nVBMtzfwtGRKLj8beuDgtW9pDlQqf7RVWX5pHQgiHAZmUi85TEbYdQ\",\"DP\":\"h2F54OMaC9qq1yqR2b55QNNaChyGtvmTHSdqZJ8lJFqvUorlz-Uocj2BTowWQnaMd8zRKMdKlSeUuSv4Z6WmjSxSsNbonI6_II5XlZLWYqFdmqDS-xCmJY32voT5Wn7OwB9xj1msDqrFPg-PqSBOh5OppjCqXqDFcNvSkQSajXc\",\"DQ\":\"VABdS20Nxkmq6JWLQj7OjRxVJuYsHrfmWJmDA7_SYtlXaPUcg-GiHGQtzdDWEeEi0dlJjv9I3FdjKGC7CGwqtVygW38DzVYJsV2EmRNJc1-j-1dRs_pK9GWR4NYm0mVz_IhS8etIf9cfRJk90xU3AL3_J6p5WNF7I5ctkLpnt8M\",\"E\":\"AQAB\",\"K\":null,\"KeyOps\":[],\"Kty\":\"RSA\",\"N\":\"yWWAOSV3Z_BW9rJEFvbZyeU-q2mJWC0l8WiHNqwVVf7qXYgm9hJC0j1aPHku_Wpl38DpK3Xu3LjWOFG9OrCqga5Pzce3DDJKI903GNqz5wphJFqweoBFKOjj1wegymvySsLoPqqDNVYTKp4nVnECZS4axZJoNt2l1S1bC8JryaNze2stjW60QT-mIAGq9konKKN3URQ12dr478m0Oh-4WWOiY4HrXoSOklFmzK-aQx1JV_SZ04eIGfSw1pZZyqTaB1BwBotiy-QA03IRxwIXQ7BSx5EaxC5uMCMbzmbvJqjt-q8Y1wyl-UQjRucgp7hkfHSE1QT3zEex2Q3NFux7SQ\",\"Oth\":null,\"P\":\"_T7MTkeOh5QyqlYCtLQ2RWf2dAJ9i3wrCx4nEDm1c1biijhtVTL7uJTLxwQIM9O2PvOi5Dq-UiGy6rhHZqf5akWTeHtaNyI-2XslQfaS3ctRgmGtRQL_VihK-R9AQtDx4eWL4h-bDJxPaxby_cVo_j2MX5AeoC1kNmcCdDf_X0M\",\"Q\":\"y5ZSThaGLjaPj8Mk2nuD8TiC-sb4aAZVh9K-W4kwaWKfDNoPcNb_dephBNMnOp9M1br6rDbyG7P-Sy_LOOsKg3Q0wHqv4hnzGaOQFeMJH4HkXYdENC7B5JG9PefbC6zwcgZWiBnsxgKpScNWuzGF8x2CC-MdsQ1bkQeTPbJklIM\",\"QI\":\"i716Vt9II_Rt6qnjsEhfE4bej52QFG9a1hSnx5PDNvRrNqR_RpTA0lO9qeXSZYGHTW_b6ZXdh_0EUwRDEDHmaxjkIcTADq6JLuDltOhZuhLUSc5NCKLAVCZlPcaSzv8-bZm57mVcIpx0KyFHxvk50___Jgx1qyzwLX03mPGUbDQ\",\"Use\":null,\"X\":null,\"X5c\":[],\"X5t\":null,\"X5tS256\":null,\"X5u\":null,\"Y\":null,\"KeySize\":2048,\"HasPrivateKey\":true,\"CryptoProviderFactory\":{\"CryptoProviderCache\":{},\"CustomCryptoProvider\":null,\"CacheSignatureProviders\":true,\"SignatureProviderObjectPoolCacheSize\":80}}";
    string _publicJWK = "{\"kty\":\"RSA\",\"use\":\"sig\",\"x5t\":null,\"e\":\"AQAB\",\"n\":\"yWWAOSV3Z_BW9rJEFvbZyeU-q2mJWC0l8WiHNqwVVf7qXYgm9hJC0j1aPHku_Wpl38DpK3Xu3LjWOFG9OrCqga5Pzce3DDJKI903GNqz5wphJFqweoBFKOjj1wegymvySsLoPqqDNVYTKp4nVnECZS4axZJoNt2l1S1bC8JryaNze2stjW60QT-mIAGq9konKKN3URQ12dr478m0Oh-4WWOiY4HrXoSOklFmzK-aQx1JV_SZ04eIGfSw1pZZyqTaB1BwBotiy-QA03IRxwIXQ7BSx5EaxC5uMCMbzmbvJqjt-q8Y1wyl-UQjRucgp7hkfHSE1QT3zEex2Q3NFux7SQ\",\"x5c\":null,\"x\":null,\"y\":null,\"crv\":null}";
    string _JKT = "JGSVlE73oKtQQI1dypYg8_JNat0xJjsQNyOI5oxaZf4";

    DefaultDPoPProofValidator _subject;

    public DPoPProofValidatorTests()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        _options.DPoP.ServerClockSkew = TimeSpan.Zero;

        _clock.UtcNowFunc = () => UtcNow;
        _testReplayCache = new TestReplayCache(_clock);

        _subject = new DefaultDPoPProofValidator(
            _options, 
            _testReplayCache,
            _clock,
            _stubDataProtectionProvider,
            new LoggerFactory().CreateLogger<DefaultDPoPProofValidator>());

        _payload = new Dictionary<string, object>
        {
            { "jti", "random" },
            { "htm", "POST" },
            { "htu", "https://identityserver/connect/token" },
            { "iat", _clock.UtcNow.ToUnixTimeSeconds() },
        };

        CreateHeaderValuesFromPublicKey();
    }

    void CreateHeaderValuesFromPublicKey()
    {
        var jwk = JsonSerializer.Deserialize<JsonElement>(_publicJWK);
        var jwkValues = new Dictionary<string, object>();
        foreach (var item in jwk.EnumerateObject())
        {
            if (item.Value.ValueKind == JsonValueKind.String)
            {
                var val = item.Value.GetString();
                if (!String.IsNullOrEmpty(val))
                {
                    jwkValues.Add(item.Name, val);
                }
            }
            if (item.Value.ValueKind == JsonValueKind.False)
            {
                jwkValues.Add(item.Name, false);
            }
            if (item.Value.ValueKind == JsonValueKind.True)
            {
                jwkValues.Add(item.Name, true);
            }
            if (item.Value.ValueKind == JsonValueKind.Number)
            {
                jwkValues.Add(item.Name, item.Value.GetInt64());
            }
        }
        _header = new Dictionary<string, object>()
        {
            //{ "alg", "RS265" }, // JsonWebTokenHandler requires adding this itself
            { "typ", "dpop+jwt" },
            { "jwk", jwkValues },
        };
    }

    string CreateDPoPProofToken(string alg = "RS256", SecurityKey key = null)
    {
        key ??= new Microsoft.IdentityModel.Tokens.JsonWebKey(_privateJWK);
        var handler = new JsonWebTokenHandler() { SetDefaultTimesOnTokenCreation = false };
        var token = handler.CreateToken(JsonSerializer.Serialize(_payload), new SigningCredentials(key, alg), _header);
        return token;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_dpop_jwt_should_pass_validation()
    {
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeFalse();
        result.JsonWebKeyThumbprint.Should().Be(_JKT);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ath_with_valid_access_token_should_pass_validation()
    {
        _context.ValidateAccessToken = true;
        _context.AccessToken = "access_token";

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(_context.AccessToken);
        var hash = sha.ComputeHash(bytes);
        var accessTokenHash = Base64Url.Encode(hash);
        _payload["ath"] = accessTokenHash;
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeFalse();
        result.JsonWebKeyThumbprint.Should().Be(_JKT);
        result.AccessTokenHash.Should().Be(accessTokenHash);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_ath_should_fail()
    {
        _context.ValidateAccessToken = true;
        _context.AccessToken = "access_token";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_ath_should_fail()
    {
        _context.ValidateAccessToken = true;
        _context.AccessToken = "access_token";

        _payload["ath"] = "invalid";
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task server_clock_skew_should_allow_tokens_outside_normal_duration()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        _options.DPoP.ServerClockSkew = TimeSpan.FromMinutes(5);

        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;

        {
            // test 1: client behind server
            _payload["jti"] = Guid.NewGuid().ToString();
            _payload["nonce"] = _stubDataProtectionProvider.Protect(new DateTimeOffset(_now).ToUnixTimeSeconds().ToString());
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(5);

            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeFalse();
        }

        {
            // test 2: client ahead of server
            _payload["jti"] = Guid.NewGuid().ToString();
            _payload["nonce"] = _stubDataProtectionProvider.Protect(new DateTimeOffset(_now).ToUnixTimeSeconds().ToString());
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(-5);

            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task server_clock_skew_should_extend_replay_cache()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        _options.DPoP.ServerClockSkew = TimeSpan.FromMinutes(5);

        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;

        {
            // test 1: client behind server
            _payload["jti"] = Guid.NewGuid().ToString();
            _payload["nonce"] = _stubDataProtectionProvider.Protect(new DateTimeOffset(_now).ToUnixTimeSeconds().ToString());
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(5);

            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeFalse();
            }
            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeTrue();
            }
        }

        {
            // test 2: client ahead of server
            _payload["jti"] = Guid.NewGuid().ToString();
            _payload["nonce"] = _stubDataProtectionProvider.Protect(new DateTimeOffset(_now).ToUnixTimeSeconds().ToString());
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(-5);
            
            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeFalse();
            }
            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeTrue();
            }
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_clock_skew_should_allow_tokens_outside_normal_duration()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        
        _context.ClientClockSkew = TimeSpan.FromMinutes(5);

        {
            // test 1: client behind server
            _payload["jti"] = Guid.NewGuid().ToString();
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(5);

            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeFalse();
        }

        {
            // test 2: client ahead of server
            _payload["jti"] = Guid.NewGuid().ToString();
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(-5);

            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_clock_skew_should_extend_replay_cache()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        
        _context.ClientClockSkew = TimeSpan.FromMinutes(5);

        {
            // test 1: client behind server
            _payload["jti"] = Guid.NewGuid().ToString();
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(5);

            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeFalse();
            }
            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeTrue();
            }
        }

        {
            // test 2: client ahead of server
            _payload["jti"] = Guid.NewGuid().ToString();
            _context.ProofToken = CreateDPoPProofToken();
            _now = _now.AddMinutes(-5);

            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeFalse();
            }
            {
                var result = await _subject.ValidateAsync(_context);
                result.IsError.Should().BeTrue();
            }
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task replayed_valid_dpop_jwt_should_fail_validation()
    {
        _options.DPoP.ProofTokenValidityDuration = TimeSpan.FromMinutes(1);
        _options.DPoP.ServerClockSkew = TimeSpan.Zero;

        _context.ProofToken = CreateDPoPProofToken();

        {
            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeFalse();
        }
        {
            var result = await _subject.ValidateAsync(_context);
            result.IsError.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task empty_string_should_fail_validation()
    {
        _context.ProofToken = "";

        var result = await _subject.ValidateAsync(_context);
        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task malformed_dpop_jwt_should_fail_validation()
    {
        _context.ProofToken = "malformed";

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_typ_should_fail_validation()
    {
        _header["typ"] = "JWT";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_alg_should_fail_validation()
    {
        var key = new SymmetricSecurityKey(IdentityModel.CryptoRandom.CreateRandomKey(32));
        _publicJWK = JsonSerializer.Serialize(key);
        CreateHeaderValuesFromPublicKey();
        
        _context.ProofToken = CreateDPoPProofToken("HS256", key);

        var result = await _subject.ValidateAsync(_context);
        
        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task private_key_should_fail_validation()
    {
        _publicJWK = _privateJWK;
        CreateHeaderValuesFromPublicKey();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task malformed_typ_should_fail_validation()
    {
        _header["typ"] = true;

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_jwk_should_fail_validation()
    {
        _header.Remove("jwk");

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task jwk_with_malformed_key_should_fail_validation()
    {
        _header["jwk"] = "malformed";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task mismatched_key_should_fail_validation()
    {
        var key = CryptoHelper.CreateRsaSecurityKey();
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        _privateJWK = JsonSerializer.Serialize(jwk);

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);
        
        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_jti_should_fail_validation()
    {
        _payload.Remove("jti");

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_htm_should_fail_validation()
    {
        _payload.Remove("htm");

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_htu_should_fail_validation()
    {
        _payload.Remove("htu");

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_htu_should_fail_validation()
    {
        _payload["htu"] = "https://identityserver";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_iat_should_fail_validation()
    {
        _payload.Remove("iat");

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task malformed_iat_should_fail_validation()
    {
        _payload["iat"] = "invalid";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_old_iat_should_fail_validation()
    {
        _payload["iat"] = _clock.UtcNow.Subtract(TimeSpan.FromSeconds(61)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_new_iat_should_fail_validation()
    {
        _payload["iat"] = _clock.UtcNow.Add(TimeSpan.FromSeconds(1)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_old_but_within_clock_skew_iat_should_succeed()
    {
        _payload["iat"] = _clock.UtcNow.Subtract(TimeSpan.FromSeconds(61)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();
        _context.ClientClockSkew = TimeSpan.FromMinutes(1);

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_old_past_clock_skew_iat_should_fail_validation()
    {
        _context.ClientClockSkew = TimeSpan.FromMinutes(1);

        _payload["iat"] = _clock.UtcNow.Subtract(TimeSpan.FromSeconds(121)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_new_but_within_clock_skew_iat_should_succeed()
    {
        _context.ClientClockSkew = TimeSpan.FromMinutes(1);

        _payload["iat"] = _clock.UtcNow.Add(TimeSpan.FromSeconds(59)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task too_new_past_clock_skew_iat_should_fail_validation()
    {
        _context.ClientClockSkew = TimeSpan.FromMinutes(1);

        _payload["iat"] = _clock.UtcNow.Add(TimeSpan.FromSeconds(61)).ToUnixTimeSeconds();

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_iat_but_nonce_provided_should_still_fail_validation()
    {
        _payload.Remove("iat");
        _payload["nonce"] = "nonce";

        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
    }

    // nonce validation
    [Fact]
    [Trait("Category", Category)]
    public async Task missing_nonce_when_required_should_fail_validation_and_issue_nonce()
    {
        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("use_dpop_nonce");
        result.ServerIssuedNonce.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task nonce_provided_when_required_should_succeed()
    {
        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        
        _payload["nonce"] = result.ServerIssuedNonce;

        _context.ProofToken = CreateDPoPProofToken();
        
        result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeFalse();
        result.JsonWebKeyThumbprint.Should().Be(_JKT);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_nonce_provided_when_required_should_fail_validation()
    {
        _context.ProofToken = CreateDPoPProofToken();
        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();

        _payload["nonce"] = result.ServerIssuedNonce + "invalid_stuff";

        _context.ProofToken = CreateDPoPProofToken();

        result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
        result.ServerIssuedNonce.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task expired_nonce_provided_when_required_should_fail_validation()
    {
        _context.ExpirationValidationMode = DPoPTokenExpirationValidationMode.Nonce;
        _context.ProofToken = CreateDPoPProofToken();

        var result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();

        _payload["nonce"] = result.ServerIssuedNonce;
        // too late
        _now = _now.AddSeconds(61);

        _context.ProofToken = CreateDPoPProofToken();

        result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
        result.ServerIssuedNonce.Should().NotBeNullOrEmpty();


        _payload["nonce"] = result.ServerIssuedNonce;
        // too early
        _now = _now.AddSeconds(-61);

        _context.ProofToken = CreateDPoPProofToken();

        result = await _subject.ValidateAsync(_context);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_dpop_proof");
        result.ServerIssuedNonce.Should().NotBeNullOrEmpty();
    }
}