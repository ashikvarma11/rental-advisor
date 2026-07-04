using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

// Returns a canned response for every request; can throw instead if configured.
public class FakeHandler : DelegatingHandler
{
    private readonly Func<HttpResponseMessage> _respond;
    public int CallCount { get; private set; }

    public FakeHandler(Func<HttpResponseMessage> respond) => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(_respond());
    }
}

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;
    public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
    public HttpClient CreateClient(string name) => new HttpClient(_handler);
}

public class GroqClauseAnalyzerTests
{
    private static IConfiguration ConfigWithKey(string? apiKey = "test-key") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey == null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Groq:ApiKey"] = apiKey })
            .Build();

    private static HttpResponseMessage GroqResponse(string content)
    {
        var body = JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } });
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
    }

    [Fact]
    public async Task AnalyzeAsync_NoApiKey_ReturnsNullImmediately()
    {
        var factory = new FakeHttpClientFactory(new FakeHandler(() => throw new Exception("should not be called")));
        var analyzer = new GroqClauseAnalyzer(factory, ConfigWithKey(null), NullLogger<GroqClauseAnalyzer>.Instance);

        var result = await analyzer.AnalyzeAsync("some clause");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeAsync_ValidResponse_ParsesAndClampsRiskScore()
    {
        var handler = new FakeHandler(() => GroqResponse("{\"riskScore\":1.8,\"suggestion\":\"seek advice\"}"));
        var factory = new FakeHttpClientFactory(handler);
        var analyzer = new GroqClauseAnalyzer(factory, ConfigWithKey(), NullLogger<GroqClauseAnalyzer>.Instance);

        var result = await analyzer.AnalyzeAsync("some clause");

        Assert.NotNull(result);
        Assert.Equal(1m, result!.RiskScore); // clamped to [0,1]
        Assert.Equal("seek advice", result.Suggestion);
    }

    [Fact]
    public async Task AnalyzeAsync_HttpCallThrows_ReturnsNull()
    {
        var handler = new FakeHandler(() => throw new HttpRequestException("network down"));
        var factory = new FakeHttpClientFactory(handler);
        var analyzer = new GroqClauseAnalyzer(factory, ConfigWithKey(), NullLogger<GroqClauseAnalyzer>.Instance);

        var result = await analyzer.AnalyzeAsync("some clause");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeAsync_NonSuccessStatus_ReturnsNull()
    {
        var handler = new FakeHandler(() => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        var factory = new FakeHttpClientFactory(handler);
        var analyzer = new GroqClauseAnalyzer(factory, ConfigWithKey(), NullLogger<GroqClauseAnalyzer>.Instance);

        var result = await analyzer.AnalyzeAsync("some clause");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeBatchAsync_MismatchedLength_RetriesOnceThenReturnsNull()
    {
        // Input has 2 clauses but response array only has 1 item, on both attempts.
        var handler = new FakeHandler(() => GroqResponse("[{\"riskScore\":0.3,\"suggestion\":null}]"));
        var factory = new FakeHttpClientFactory(handler);
        var analyzer = new GroqClauseAnalyzer(factory, ConfigWithKey(), NullLogger<GroqClauseAnalyzer>.Instance);

        var result = await analyzer.AnalyzeBatchAsync(new List<string> { "clause one", "clause two" });

        Assert.Null(result);
        Assert.Equal(2, handler.CallCount); // one initial attempt + one retry
    }
}
