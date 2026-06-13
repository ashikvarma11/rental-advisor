using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;

namespace RentalAdvisor.Backend.Services;

public class DataImporter
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DataImporter(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task SeedSuburbStatsIfEmptyAsync()
    {
        if (await _db.SuburbStats.AnyAsync())
            return;
        // Look for CSV in several common locations: backend/data, backend/Data, repo-level Data
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, "data", "abs_median_rent.csv"),
            Path.Combine(_env.ContentRootPath, "Data", "abs_median_rent.csv"),
            Path.Combine(_env.ContentRootPath, "..", "Data", "abs_median_rent.csv")
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path == null)
            return;

        var lines = await File.ReadAllLinesAsync(path);
        var list = new List<SuburbStats>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 4) continue;
            if (!decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var median)) continue;
            if (!int.TryParse(parts[3], out var year)) year = DateTime.UtcNow.Year;
            list.Add(new SuburbStats
            {
                Suburb = parts[0].Trim(),
                Postcode = parts[1].Trim(),
                MedianRent = median,
                Year = year
            });
        }

        if (list.Any())
        {
            _db.SuburbStats.AddRange(list);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> ImportListingsFromCsvAsync(string relativePath)
    {
        // Accept absolute or relative paths. Try common fallbacks if not found.
        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath))
        {
            var linesRoot = await File.ReadAllLinesAsync(relativePath);
            return await ImportLinesAsync(linesRoot);
        }

        var filename = Path.GetFileName(relativePath);
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, relativePath),
            Path.Combine(_env.ContentRootPath, relativePath.Replace("data/", "Data/")),
            Path.Combine(_env.ContentRootPath, "Data", filename),
            Path.Combine(_env.ContentRootPath, "..", "Data", filename)
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path == null)
            throw new FileNotFoundException($"Could not find '{relativePath}' in expected locations.");

        var lines = await File.ReadAllLinesAsync(path);
        var created = 0;
        return await ImportLinesAsync(lines);
    }

    private async Task<int> ImportLinesAsync(string[] lines)
    {
        var created = 0;
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            if (!decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var rent)) continue;

            var listing = new Listing
            {
                Title = parts[0].Trim(),
                Suburb = parts[1].Trim(),
                Postcode = parts[2].Trim(),
                Rent = rent,
                LandlordAbn = parts.Length > 4 ? parts[4].Trim() : null
            };

            _db.Listings.Add(listing);
            created++;
        }

        await _db.SaveChangesAsync();
        return created;
    }
}
