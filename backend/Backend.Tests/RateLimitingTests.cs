using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Backend.Tests;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                $"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db")}");
        });
    }

    [Fact]
    public async Task Login_ExceedsRateLimit_Returns429WithErrorBody()
    {
        var client = _factory.CreateClient();
        HttpResponseMessage? last = null;

        // The "auth" policy allows 5 requests/minute; the 6th should be rejected.
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong" });
        }

        Assert.Equal((HttpStatusCode)429, last!.StatusCode);
        var body = await last.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Too many requests, please slow down.", body!["error"]);
    }
}
