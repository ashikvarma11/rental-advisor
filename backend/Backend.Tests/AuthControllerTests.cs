using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalAdvisor.Backend.Controllers;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class AuthControllerTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService NewAuth() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-not-for-production-use-0123456789",
                ["Jwt:Issuer"] = "RentalAdvisorTests"
            })
            .Build());

    [Fact]
    public async Task Register_NewEmail_CreatesUserAndReturnsToken()
    {
        using var db = NewDb();
        var controller = new AuthController(db, NewAuth());

        var result = await controller.Register(new AuthRequest("new@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value));
        Assert.Equal(1, db.Users.Count());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        using var db = NewDb();
        var auth = NewAuth();
        db.Users.Add(new RentalAdvisor.Backend.Models.User { Email = "dup@example.com", PasswordHash = auth.HashPassword("x") });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, auth);
        var result = await controller.Register(new AuthRequest("dup@example.com", "password123"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        using var db = NewDb();
        var auth = NewAuth();
        db.Users.Add(new RentalAdvisor.Backend.Models.User { Email = "user@example.com", PasswordHash = auth.HashPassword("password123") });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, auth);
        var result = await controller.Login(new AuthRequest("user@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var db = NewDb();
        var auth = NewAuth();
        db.Users.Add(new RentalAdvisor.Backend.Models.User { Email = "user@example.com", PasswordHash = auth.HashPassword("password123") });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, auth);
        var result = await controller.Login(new AuthRequest("user@example.com", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
