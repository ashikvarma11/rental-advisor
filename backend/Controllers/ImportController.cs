using Microsoft.AspNetCore.Mvc;
using RentalAdvisor.Backend.Services;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly DataImporter _importer;

    public ImportController(DataImporter importer)
    {
        _importer = importer;
    }

    [HttpPost("suburbstats/seed")]
    public async Task<IActionResult> SeedSuburbStats()
    {
        await _importer.SeedSuburbStatsIfEmptyAsync();
        return Ok(new { status = "seeded" });
    }

    [HttpPost("listings/import-sample")]
    public async Task<IActionResult> ImportSampleListings()
    {
        var created = await _importer.ImportListingsFromCsvAsync("data/sample_listings.csv");
        return Ok(new { created });
    }

    [HttpPost("listings/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadListings(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "file required" });

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "only .csv files are accepted" });

        await using var stream = file.OpenReadStream();
        var result = await _importer.ImportListingsFromStreamAsync(stream);

        return Ok(new { created = result.Created, errors = result.Errors });
    }
}
