using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;

namespace RentalAdvisor.Backend.Controllers;

public record AuthRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "email and password required" });

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists) return BadRequest(new { error = "email already registered" });

        var user = new User { Email = request.Email, PasswordHash = _auth.HashPassword(request.Password) };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { token = _auth.GenerateJwt(user) });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !_auth.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "invalid email or password" });

        return Ok(new { token = _auth.GenerateJwt(user) });
    }
}
