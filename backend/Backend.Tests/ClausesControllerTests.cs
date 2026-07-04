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

    [Fact]
    public async Task Resolve_ExistingClause_ReturnsOkAndPersists()
    {
        using var db = NewDb();
        db.Clauses.Add(new Clause { Id = 1, LeaseDocumentId = 1, Text = "Some clause" });
        await db.SaveChangesAsync();

        var controller = new ClausesController(db);
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
        var controller = new ClausesController(db);

        var result = await controller.Resolve(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
