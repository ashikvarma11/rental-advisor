using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using RentalAdvisor.Backend.Services;
using System.Text;
using UglyToad.PdfPig;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/leases")]
public class LeasesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly GroqClauseAnalyzer _aiAnalyzer;
    private readonly ILogger<LeasesController> _logger;

    public LeasesController(AppDbContext db, IWebHostEnvironment env, GroqClauseAnalyzer aiAnalyzer, ILogger<LeasesController> logger)
    {
        _db = db;
        _env = env;
        _aiAnalyzer = aiAnalyzer;
        _logger = logger;
    }

    [HttpPost("upload")]
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

        var doc = new LeaseDocument { FileName = Path.GetFileName(savePath), Content = content };
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

        var doc = new LeaseDocument { FileName = filename, Content = content };
        _db.LeaseDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return Ok(new { id = doc.Id });
    }

    [HttpPost("{id}/extract-clauses")]
    public async Task<IActionResult> ExtractClauses(int id)
    {
        var doc = await _db.LeaseDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        string? content = doc.Content;
        if (string.IsNullOrEmpty(content))
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "leases", doc.FileName);
            if (System.IO.File.Exists(path))
                content = await ExtractTextFromFileAsync(path);
        }

        if (string.IsNullOrEmpty(content))
            return BadRequest(new { error = "No text content available for clause extraction" });

        // Prefer letting Groq split the raw text into clauses and score them in one pass -
        // it handles messy PDF-extracted layout far better than a fixed regex/blank-line split.
        var extracted = _aiAnalyzer.IsConfigured ? await _aiAnalyzer.ExtractClausesAsync(content) : null;

        var created = 0;
        if (extracted != null)
        {
            foreach (var e in extracted)
            {
                _db.Clauses.Add(new Clause
                {
                    LeaseDocumentId = doc.Id,
                    Text = e.Text,
                    RiskScore = e.RiskScore,
                    Suggestion = e.Suggestion
                });
                created++;
            }
        }
        else
        {
            var clauses = SplitIntoClauses(content);
            var batch = _aiAnalyzer.IsConfigured ? await _aiAnalyzer.AnalyzeBatchAsync(clauses) : null;

            for (var i = 0; i < clauses.Count; i++)
            {
                var c = clauses[i];
                var analysis = batch?[i];

                decimal score;
                string? suggestion;
                if (analysis != null)
                {
                    score = analysis.RiskScore;
                    suggestion = analysis.Suggestion;
                }
                else
                {
                    score = 0m;
                    var t = c.ToLowerInvariant();
                    if (t.Contains("terminate") || t.Contains("breach") || t.Contains("penalty")) score += 0.6m;
                    if (t.Contains("rent") || t.Contains("increase")) score += 0.2m;
                    if (t.Contains("bond") || t.Contains("deposit")) score += 0.2m;
                    score = Math.Min(1m, score);
                    suggestion = score > 0.5m ? "Review with attention - possible high-risk clause." : null;
                }

                _db.Clauses.Add(new Clause
                {
                    LeaseDocumentId = doc.Id,
                    Text = c,
                    RiskScore = score,
                    Suggestion = suggestion
                });
                created++;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { created });
    }

    // PdfPig's page.Text has no blank-line paragraph breaks, so splitting on "\n\n" collapses
    // the whole document into one clause. Split on numbered clause headings (e.g. "12. Termination...")
    // instead, since that's the actual structural unit in these lease documents; fall back to
    // blank-line/line splitting for documents that aren't numbered.
    private static readonly System.Text.RegularExpressions.Regex NumberedClauseHeading =
        new(@"(?m)^\s*(\d{1,2})\.\s+\S", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static List<string> SplitIntoClauses(string content)
    {
        var matches = NumberedClauseHeading.Matches(content).ToList();
        if (matches.Count >= 3)
        {
            var parts = new List<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;
                parts.Add(content.Substring(start, end - start).Trim());
            }
            return parts.Where(t => t.Length > 20).Take(50).ToList();
        }

        var byBlankLine = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 20)
            .ToList();
        if (byBlankLine.Count > 1)
            return byBlankLine.Take(50).ToList();

        return content.Split('\n')
            .Select(t => t.Trim())
            .Where(t => t.Length > 20)
            .Take(50)
            .ToList();
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
        var doc = await _db.LeaseDocuments.FindAsync(id);
        if (doc == null) return NotFound();
        return Ok(new { doc.FileName, length = doc.Content?.Length ?? 0, content = doc.Content });
    }

    [HttpGet("{id}/clauses")]
    public async Task<IActionResult> GetClauses(int id)
    {
        var clauses = await _db.Clauses.Where(c => c.LeaseDocumentId == id).ToListAsync();
        return Ok(clauses);
    }

    [HttpGet("{id}/clauses/export")]
    public async Task<IActionResult> ExportClauses(int id)
    {
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
