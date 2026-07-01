using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RentalAdvisor.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentalAdvisor API", Version = "v1" }));
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.DataImporter>();
builder.Services.AddScoped<RentalAdvisor.Backend.Services.GroqClauseAnalyzer>();

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

app.UseHttpsRedirection();
app.UseCors("AllowLocal");
app.UseRouting();
app.UseAuthorization();
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
