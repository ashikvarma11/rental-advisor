using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RentalAdvisor.Backend.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentalAdvisor API", Version = "v1" }));
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.DataImporter>();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.GroqClauseAnalyzer>();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.AuthService>();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.ClauseExtractionService>();
builder.Services.AddSingleton<RentalAdvisor.Backend.Services.ClauseExtractionQueue>();
builder.Services.AddHostedService<RentalAdvisor.Backend.Services.ClauseExtractionWorker>();

// JWT auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-insecure-key-change-me-1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RentalAdvisor";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// Rate limiting: partition by authenticated user id when available, else by remote IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests, please slow down." }, cancellationToken: ct);
    };

    static string PartitionKey(HttpContext ctx) =>
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("user-or-ip", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("external-api", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

// CORS for local frontend during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// EF Core / SQLite
var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Data/rental.db";
var dbDir = Path.GetDirectoryName(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(conn).DataSource);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(conn);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(a => a.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    }));
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors("AllowLocal");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "OK" }));
// Ensure DB and seed data (apply migrations when available)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RentalAdvisor.Backend.Data.AppDbContext>();
    db.Database.Migrate();
    var importer = scope.ServiceProvider.GetRequiredService<RentalAdvisor.Backend.Services.DataImporter>();
    await importer.SeedSuburbStatsIfEmptyAsync();
}

app.Run();

// Exposes the top-level Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
