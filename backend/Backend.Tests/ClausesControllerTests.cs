using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Controllers;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using Xunit;

namespace Backend.Tests;

public class ClausesControllerTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ClausesController NewController(AppDbContext db, int userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"));
        return new ClausesController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [Fact]
    public async Task Resolve_ExistingClause_ReturnsOkAndPersists()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt" });
        db.Clauses.Add(new Clause { Id = 1, LeaseDocumentId = 1, Text = "Some clause" });
        await db.SaveChangesAsync();

        var controller = NewController(db, userId: 1);
        var result = await controller.Resolve(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, db.Clauses.Find(1)!.Id);
        Assert.True(db.Clauses.Find(1)!.IsResolved);
        Assert.Equal(true, ok.Value!.GetType().GetProperty("isResolved")!.GetValue(ok.Value));
    }

    [Fact]
    public async Task Resolve_NonexistentClause_ReturnsNotFound()
    {
        using var db = NewDb();
        var controller = NewController(db, userId: 1);

        var result = await controller.Resolve(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Resolve_ClauseOwnedByDifferentUser_ReturnsNotFound()
    {
        using var db = NewDb();
        db.LeaseDocuments.Add(new LeaseDocument { Id = 1, UserId = 1, FileName = "lease.txt" });
        db.Clauses.Add(new Clause { Id = 1, LeaseDocumentId = 1, Text = "Some clause" });
        await db.SaveChangesAsync();

        var controller = NewController(db, userId: 2);
        var result = await controller.Resolve(1);

        Assert.IsType<NotFoundResult>(result);
        Assert.False(db.Clauses.Find(1)!.IsResolved);
    }
}
