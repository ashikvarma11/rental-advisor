using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;

namespace RentalAdvisor.Backend.Services;

public class ListingImportResult
{
    public int Created { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DataImporter
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] RequiredListingHeaders = { "title", "suburb", "postcode", "rent" };

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
            var parts = CsvParser.ParseLine(line);
            if (parts.Count < 4) continue;
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
        string? path = Path.IsPathRooted(relativePath) && File.Exists(relativePath) ? relativePath : null;

        if (path == null)
        {
            var filename = Path.GetFileName(relativePath);
            var candidates = new[]
            {
                Path.Combine(_env.ContentRootPath, relativePath),
                Path.Combine(_env.ContentRootPath, relativePath.Replace("data/", "Data/")),
                Path.Combine(_env.ContentRootPath, "Data", filename),
                Path.Combine(_env.ContentRootPath, "..", "Data", filename)
            };
            path = candidates.FirstOrDefault(File.Exists);
        }

        if (path == null)
            throw new FileNotFoundException($"Could not find '{relativePath}' in expected locations.");

        var lines = await File.ReadAllLinesAsync(path);
        var result = ImportListingLines(lines);
        if (result.Created > 0)
            await _db.SaveChangesAsync();
        return result.Created;
    }

    public async Task<ListingImportResult> ImportListingsFromStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var result = ImportListingLines(lines);
        if (result.Created > 0)
            await _db.SaveChangesAsync();
        return result;
    }

    private ListingImportResult ImportListingLines(string[] lines)
    {
        var result = new ListingImportResult();
        if (lines.Length == 0)
        {
            result.Errors.Add("File is empty.");
            return result;
        }

        var header = CsvParser.ParseLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        var missingHeaders = RequiredListingHeaders.Where(h => !header.Contains(h)).ToList();
        if (missingHeaders.Any())
        {
            result.Errors.Add($"Missing required column(s): {string.Join(", ", missingHeaders)}. Expected header: Title,Suburb,Postcode,Rent[,LandlordABN]");
            return result;
        }

        var titleIdx = header.IndexOf("title");
        var suburbIdx = header.IndexOf("suburb");
        var postcodeIdx = header.IndexOf("postcode");
        var rentIdx = header.IndexOf("rent");
        var abnIdx = header.IndexOf("landlordabn");

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var rowNum = i + 1;
            var parts = CsvParser.ParseLine(line);
            if (parts.Count < header.Count)
            {
                result.Errors.Add($"Row {rowNum}: expected {header.Count} columns, found {parts.Count}. Skipped.");
                continue;
            }

            var suburb = parts[suburbIdx].Trim();
            var postcode = parts[postcodeIdx].Trim();
            var rentRaw = parts[rentIdx].Trim();

            if (string.IsNullOrWhiteSpace(suburb))
            {
                result.Errors.Add($"Row {rowNum}: Suburb is required. Skipped.");
                continue;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(postcode, "^\\d{4}$"))
            {
                result.Errors.Add($"Row {rowNum}: Postcode '{postcode}' must be 4 digits. Skipped.");
                continue;
            }
            if (!decimal.TryParse(rentRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var rent) || rent <= 0)
            {
                result.Errors.Add($"Row {rowNum}: Rent '{rentRaw}' is not a valid positive number. Skipped.");
                continue;
            }

            var listing = new Listing
            {
                Title = titleIdx >= 0 ? parts[titleIdx].Trim() : string.Empty,
                Suburb = suburb,
                Postcode = postcode,
                Rent = rent,
                LandlordAbn = abnIdx >= 0 && abnIdx < parts.Count && !string.IsNullOrWhiteSpace(parts[abnIdx]) ? parts[abnIdx].Trim() : null
            };

            _db.Listings.Add(listing);
            result.Created++;
        }

        return result;
    }
}
