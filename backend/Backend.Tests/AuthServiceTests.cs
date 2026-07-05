using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class AuthServiceTests
{
    private static AuthService NewAuth() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-not-for-production-use-0123456789",
                ["Jwt:Issuer"] = "RentalAdvisorTests"
            })
            .Build());

    [Fact]
    public void HashPassword_VerifyPassword_RoundTrips()
    {
        var auth = NewAuth();
        var hash = auth.HashPassword("correct-horse-battery-staple");

        Assert.True(auth.VerifyPassword("correct-horse-battery-staple", hash));
        Assert.False(auth.VerifyPassword("wrong-password", hash));
    }

    [Fact]
    public void GenerateJwt_ContainsNameIdentifierClaim()
    {
        var auth = NewAuth();
        var user = new User { Id = 42, Email = "test@example.com" };

        var token = auth.GenerateJwt(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var claim = parsed.Claims.First(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Equal("42", claim.Value);
    }
}
