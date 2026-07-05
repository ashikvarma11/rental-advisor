using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentalAdvisor.Backend.Controllers;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class LeasesControllerTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static LeasesController NewController(AppDbContext db, int userId, ClauseExtractionQueue? queue = null)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"));
        return new LeasesController(db, new FakeWebHostEnvironment(), NullLogger<LeasesController>.Instance, queue ?? new ClauseExtractionQueue())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [Fact]
    public async Task ExtractClauses_ValidDoc_EnqueuesJobAndReturns202()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt", Content = "some text" });
        await db.SaveChangesAsync();
        var queue = new ClauseExtractionQueue();

        var controller = NewController(db, userId: 1, queue);
        var result = await controller.ExtractClauses(1);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.True(queue.Statuses.TryGetValue(1, out var status));
        Assert.Equal(JobStatus.Queued, status!.Status);
    }

    [Fact]
    public async Task ExtractClauses_DocNotFoundOrNotOwned_ReturnsNotFound()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt", Content = "some text" });
        await db.SaveChangesAsync();

        var controller = NewController(db, userId: 2);
        var result = await controller.ExtractClauses(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void ExtractStatus_UnknownId_ReturnsUnknownStatus()
    {
        using var db = NewDb();
        var controller = NewController(db, userId: 1);

        var result = controller.ExtractStatus(999);

        var ok = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<JobStatusInfo>(ok.Value);
        Assert.Equal(JobStatus.Unknown, status.Status);
    }

    [Fact]
    public void ExtractStatus_KnownId_ReturnsCurrentStatus()
    {
        using var db = NewDb();
        var queue = new ClauseExtractionQueue();
        queue.Enqueue(new ClauseExtractionJob(1, 1));
        var controller = NewController(db, userId: 1, queue);

        var result = controller.ExtractStatus(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<JobStatusInfo>(ok.Value);
        Assert.Equal(JobStatus.Queued, status.Status);
    }
}
