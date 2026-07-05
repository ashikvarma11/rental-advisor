using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

// Minimal stand-in for IWebHostEnvironment; ClauseExtractionService only reads ContentRootPath
// to look up an on-disk fallback file when LeaseDocument.Content is already populated in these tests.
public class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "Backend.Tests";
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
}

public class ClauseExtractionServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static GroqClauseAnalyzer NewAnalyzer(HttpMessageHandler handler, string? apiKey)
    {
        var factory = new FakeHttpClientFactory(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey == null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Groq:ApiKey"] = apiKey })
            .Build();
        return new GroqClauseAnalyzer(factory, config, NullLogger<GroqClauseAnalyzer>.Instance);
    }

    private static ClauseExtractionService NewService(AppDbContext db, GroqClauseAnalyzer analyzer) =>
        new(db, analyzer, new FakeWebHostEnvironment(), NullLogger<ClauseExtractionService>.Instance);

    private const string LeaseText =
        "1. Term. This lease runs for 12 months.\n\n" +
        "2. Rent. Weekly amount: $450.\n\n" +
        "3. Termination. The landlord may terminate for breach with penalty.\n\n" +
        "Address of premises: 1 Test St, Adelaide SA 5000";

    [Fact]
    public async Task ExtractAndPersistAsync_NoApiKey_FallsBackToRegexScoring()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt", Content = LeaseText });
        await db.SaveChangesAsync();

        var analyzer = NewAnalyzer(new FakeHandler(() => throw new Exception("should not be called")), apiKey: null);
        var service = NewService(db, analyzer);

        var created = await service.ExtractAndPersistAsync(1, userId: 1);

        Assert.True(created > 0);
        Assert.Equal(created, db.Clauses.Count(c => c.LeaseDocumentId == 1));
        // Regex fallback should have picked up rent/suburb/postcode and created a Listing owned by the caller.
        var listing = db.Listings.Single();
        Assert.Equal(1, listing.UserId);
        Assert.Equal("5000", listing.Postcode);
    }

    [Fact]
    public async Task ExtractAndPersistAsync_ValidGroqResponse_PersistsClausesAndListing()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 7, FileName = "lease.txt", Content = LeaseText });
        await db.SaveChangesAsync();

        var extractClausesResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = JsonSerializer.Serialize(new[]
                {
                    new { text = "1. Term clause", riskScore = 0.1, suggestion = (string?)null },
                    new { text = "3. Termination clause", riskScore = 0.9, suggestion = "Seek legal advice" }
                }) } }
            }
        });
        var summaryResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = JsonSerializer.Serialize(new { rent = 450, suburb = "Adelaide", postcode = "5000" }) } }
            }
        });

        var responses = new Queue<string>(new[] { extractClausesResponse, summaryResponse });
        var handler = new FakeHandler(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responses.Dequeue())
        });
        var analyzer = NewAnalyzer(handler, apiKey: "test-key");
        var service = NewService(db, analyzer);

        var created = await service.ExtractAndPersistAsync(1, userId: 7);

        Assert.Equal(2, created);
        Assert.Equal(2, db.Clauses.Count(c => c.LeaseDocumentId == 1));
        var listing = db.Listings.Single();
        Assert.Equal(7, listing.UserId);
        Assert.Equal(450m, listing.Rent);
    }

    [Fact]
    public async Task ExtractAndPersistAsync_DocNotOwnedByUser_ThrowsLeaseDocumentNotFound()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt", Content = LeaseText });
        await db.SaveChangesAsync();

        var analyzer = NewAnalyzer(new FakeHandler(() => throw new Exception("should not be called")), apiKey: null);
        var service = NewService(db, analyzer);

        await Assert.ThrowsAsync<LeaseDocumentNotFoundException>(() => service.ExtractAndPersistAsync(1, userId: 2));
    }
}
