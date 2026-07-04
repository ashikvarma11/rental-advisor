using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/compare")]
public class ComparatorController : ControllerBase
{
    private readonly AppDbContext _db;

    public ComparatorController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("suburb")]
    public async Task<IActionResult> CompareSuburb([FromQuery] string postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode)) return BadRequest(new { error = "postcode required" });

        var rents = await _db.Listings.Where(l => l.Postcode == postcode).Select(l => l.Rent).ToListAsync();
        var listingCount = rents.Count;

        decimal? median = null;
        decimal? avgRent = null;
        decimal? minRent = null;
        decimal? maxRent = null;
        decimal? stdDeviation = null;
        if (listingCount > 0)
        {
            var sorted = rents.OrderBy(r => r).ToList();
            var mid = listingCount / 2;
            median = listingCount % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
            avgRent = rents.Average();
            minRent = sorted[0];
            maxRent = sorted[^1];

            var mean = (double)avgRent.Value;
            var variance = rents.Average(r => Math.Pow((double)r - mean, 2));
            stdDeviation = (decimal)Math.Sqrt(variance);
        }

        decimal? diffPercent = null;
        if (median.HasValue && avgRent.HasValue && median.Value > 0)
            diffPercent = Math.Round((avgRent.Value - median.Value) / median.Value * 100, 2);

        return Ok(new
        {
            postcode,
            median = median,
            averageListingRent = avgRent,
            minRent,
            maxRent,
            stdDeviation,
            listingCount,
            isLowConfidence = listingCount < 3,
            differencePercent = diffPercent
        });
    }
}
