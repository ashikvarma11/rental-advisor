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

        // A postcode can span multiple suburbs, so average the latest-year median across all of them.
        var latestYearPerSuburb = await _db.SuburbStats
            .Where(s => s.Postcode == postcode)
            .GroupBy(s => s.Suburb)
            .Select(g => g.OrderByDescending(s => s.Year).First())
            .ToListAsync();
        decimal? median = latestYearPerSuburb.Any() ? latestYearPerSuburb.Average(s => s.MedianRent) : (decimal?)null;

        var listingsQuery = _db.Listings.Where(l => l.Postcode == postcode);
        var listingCount = await listingsQuery.CountAsync();
        decimal? avgRent = null;
        if (listingCount > 0)
        {
            var rents = await listingsQuery.Select(l => l.Rent).ToListAsync();
            avgRent = rents.Any() ? rents.Average() : (decimal?)null;
        }

        decimal? diffPercent = null;
        if (median.HasValue && avgRent.HasValue && median.Value > 0)
            diffPercent = Math.Round((avgRent.Value - median.Value) / median.Value * 100, 2);

        return Ok(new
        {
            postcode,
            median = median,
            averageListingRent = avgRent,
            listingCount,
            differencePercent = diffPercent
        });
    }
}
