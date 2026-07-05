using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class ClauseExtractionWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ProcessesEnqueuedJob_UpdatesStatusToDone()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(new FakeHandler(() => throw new Exception("no api key configured"))));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<GroqClauseAnalyzer>();
        services.AddScoped<ClauseExtractionService>();
        services.AddSingleton<ClauseExtractionQueue>();
        var provider = services.BuildServiceProvider();

        using (var seedScope = provider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt", Content = "1. Rent. Weekly amount: $400." });
            await db.SaveChangesAsync();
        }

        var queue = provider.GetRequiredService<ClauseExtractionQueue>();
        var worker = new ClauseExtractionWorker(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ClauseExtractionWorker>.Instance);

        using var cts = new CancellationTokenSource();
        var runTask = worker.StartAsync(cts.Token);
        queue.Enqueue(new ClauseExtractionJob(1, 1));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (queue.Statuses.TryGetValue(1, out var s) && s.Status is JobStatus.Done or JobStatus.Failed) break;
            await Task.Delay(50);
        }

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.True(queue.Statuses.TryGetValue(1, out var status));
        Assert.Equal(JobStatus.Done, status!.Status);
    }
}
