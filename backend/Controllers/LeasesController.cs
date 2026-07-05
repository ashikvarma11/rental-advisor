using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using System.Text;
using UglyToad.PdfPig;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/leases")]
[Authorize]
public class LeasesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LeasesController> _logger;
    private readonly ClauseExtractionQueue _queue;

    public LeasesController(AppDbContext db, IWebHostEnvironment env, ILogger<LeasesController> logger, ClauseExtractionQueue queue)
    {
        _db = db;
        _env = env;
        _logger = logger;
        _queue = queue;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetLeases()
    {
        var leases = await _db.LeaseDocuments
            .Where(d => d.UserId == UserId)
            .OrderByDescending(d => d.Id)
            .Select(d => new { d.Id, d.FileName, d.UploadedAt })
            .ToListAsync();
        return Ok(leases);
    }

    [HttpPost("upload")]
    [EnableRateLimiting("user-or-ip")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null) return BadRequest(new { error = "file required" });

        var dataDir = Path.Combine(_env.ContentRootPath, "Data", "leases");
        Directory.CreateDirectory(dataDir);
        var savePath = Path.Combine(dataDir, Path.GetFileName(file.FileName));

        await using (var fs = System.IO.File.Create(savePath))
        {
            await file.CopyToAsync(fs);
        }

        string? content = await ExtractTextFromFileAsync(savePath);

        var doc = new LeaseDocument { UserId = UserId, FileName = Path.GetFileName(savePath), Content = content };
        _db.LeaseDocuments.Add(doc);
        await _db.SaveChangesAsync();

        // For privacy in prototype mode: remove original uploaded file after extracting text
        try
        {
            if (System.IO.File.Exists(savePath))
            {
                System.IO.File.Delete(savePath);
            }
        }
        catch
        {
            // ignore deletion failures for now
        }

        return Ok(new { id = doc.Id });
    }

    // Helper for local testing: create lease from file already on server under Data/leases
    [HttpPost("upload-from-server")]
    public async Task<IActionResult> UploadFromServer([FromQuery] string filename)
    {
        if (string.IsNullOrEmpty(filename)) return BadRequest(new { error = "filename required" });
        var path = Path.Combine(_env.ContentRootPath, "Data", "leases", filename);
        if (!System.IO.File.Exists(path)) return NotFound(new { error = "file not found" });

        var content = await ExtractTextFromFileAsync(path);

        var doc = new LeaseDocument { UserId = UserId, FileName = filename, Content = content };
        _db.LeaseDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return Ok(new { id = doc.Id });
    }

    [HttpPost("{id}/extract-clauses")]
    [EnableRateLimiting("user-or-ip")]
    public async Task<IActionResult> ExtractClauses(int id)
    {
        var owned = await _db.LeaseDocuments.AnyAsync(d => d.Id == id && d.UserId == UserId);
        if (!owned) return NotFound();

        _queue.Enqueue(new ClauseExtractionJob(id, UserId));
        return Accepted(new { status = "queued" });
    }

    [HttpGet("{id}/extract-status")]
    public IActionResult ExtractStatus(int id)
    {
        if (_queue.Statuses.TryGetValue(id, out var status))
            return Ok(status);
        return Ok(new JobStatusInfo(JobStatus.Unknown, null, null));
    }

    private async Task<string?> ExtractTextFromFileAsync(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".txt")
        {
            return await System.IO.File.ReadAllTextAsync(path);
        }
        if (ext == ".pdf")
        {
            try
            {
                var sb = new StringBuilder();
                using (var pdf = PdfDocument.Open(path))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF text extraction failed for {Path}", path);
                return null;
            }
        }

        return null;
    }

    [HttpGet("{id}/debug-content")]
    public async Task<IActionResult> DebugContent(int id)
    {
        var doc = await _db.LeaseDocuments.FirstOrDefaultAsync(d => d.Id == id && d.UserId == UserId);
        if (doc == null) return NotFound();
        return Ok(new { doc.FileName, length = doc.Content?.Length ?? 0, content = doc.Content });
    }

    [HttpGet("{id}/clauses")]
    public async Task<IActionResult> GetClauses(int id)
    {
        var owned = await _db.LeaseDocuments.AnyAsync(d => d.Id == id && d.UserId == UserId);
        if (!owned) return NotFound();
        var clauses = await _db.Clauses.Where(c => c.LeaseDocumentId == id).ToListAsync();
        return Ok(clauses);
    }

    [HttpGet("{id}/clauses/export")]
    public async Task<IActionResult> ExportClauses(int id)
    {
        var owned = await _db.LeaseDocuments.AnyAsync(d => d.Id == id && d.UserId == UserId);
        if (!owned) return NotFound();
        var clauses = await _db.Clauses.Where(c => c.LeaseDocumentId == id).ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,LeaseDocumentId,IsResolved,RiskScore,Suggestion,Text");
        foreach (var c in clauses)
        {
            var text = c.Text?.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'") ?? "";
            sb.AppendLine($"{c.Id},{c.LeaseDocumentId},{c.IsResolved},{c.RiskScore},\"{c.Suggestion ?? ""}\",\"{text}\"");
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"clauses_{id}.csv");
    }
}
