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
    public async Task<IActionResult> CompareSuburb([FromQuery] string suburb, [FromQuery] string postcode)
    {
        if (string.IsNullOrWhiteSpace(suburb)) return BadRequest(new { error = "suburb required" });

        var suburbLower = suburb.ToLowerInvariant();
        var stats = await _db.SuburbStats
            .Where(s => s.Suburb.ToLower() == suburbLower && (string.IsNullOrEmpty(postcode) || s.Postcode == postcode))
            .OrderByDescending(s => s.Year)
            .FirstOrDefaultAsync();

        var listingsQuery = _db.Listings.Where(l => l.Suburb.ToLower() == suburbLower && (string.IsNullOrEmpty(postcode) || l.Postcode == postcode));
        var listingCount = await listingsQuery.CountAsync();
        decimal? avgRent = null;
        if (listingCount > 0)
        {
            var rents = await listingsQuery.Select(l => l.Rent).ToListAsync();
            avgRent = rents.Any() ? rents.Average() : (decimal?)null;
        }

        var median = stats?.MedianRent;

        decimal? diffPercent = null;
        if (median.HasValue && avgRent.HasValue && median.Value > 0)
            diffPercent = Math.Round((avgRent.Value - median.Value) / median.Value * 100, 2);

        return Ok(new
        {
            suburb,
            postcode,
            median = median,
            averageListingRent = avgRent,
            listingCount,
            differencePercent = diffPercent
        });
    }
}
